using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class TowerGenerator : MonoBehaviour
    {
        private const int MinimumSeedAttempts = 32;

        [Header("Course References")]
        [SerializeField] private GameObject authoredFallback;
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private TowerChunk startChunkPrefab;
        [SerializeField] private TowerChunk transitionChunkPrefab;
        [SerializeField] private TowerChunk[] chunkPrefabs;
        [SerializeField] private Material[] stageMaterials;
        [SerializeField] private Transform finishGoal;
        [SerializeField] private RunController runController;
        [SerializeField] private Transform player;
        [SerializeField] private RisingHazard hazard;

        [Header("Deterministic Run")]
        [SerializeField] private int seed = 104729;
        [SerializeField, Min(4)] private int generatedChunkCount = 24;
        [SerializeField, Min(1)] private int recentHistoryLength = 2;
        [SerializeField, Min(1)] private int maximumSeedAttempts = 32;
        [SerializeField] private bool randomizeInitialSeed = true;

        [Header("Endless Streaming")]
        [SerializeField] private bool endlessMode = true;
        [SerializeField, Min(5f)] private float generationAheadDistance = 35f;
        [SerializeField, Min(0f)] private float cleanupBelowWaterMargin = 2f;
        [SerializeField, Min(0.1f)] private float appendRetryDelay = 1f;

        [Header("Placement")]
        [SerializeField, Min(5f)] private float maximumHorizontalRadius = 12f;
        [SerializeField, Range(45f, 135f)] private float maximumTurnAngle = 100f;
        [SerializeField, Min(0f)] private float overlapPadding = 0.1f;
        [SerializeField, Min(4)] private int maximumPlacementAttempts = 24;
        [SerializeField] private bool generateOnAwake = true;

        private readonly List<TowerChunk> spawnedChunks = new List<TowerChunk>();
        private readonly List<Bounds> placedBounds = new List<Bounds>();
        private readonly List<TraversalClearanceSegment> protectedClearances =
            new List<TraversalClearanceSegment>();
        private readonly List<StageRecord> activeStages = new List<StageRecord>();

        private string lastSequence = string.Empty;
        private string lastGenerationFailure = string.Empty;
        private int directionRejections;
        private int clearanceRejections;
        private Vector3 fallbackFinishPosition;
        private Quaternion fallbackFinishRotation;
        private Vector3 nextAttachmentPoint;
        private TowerChunk lastPlacedChunk;
        private float highestGeneratedY;
        private float nextAppendAttemptTime;
        private int nextStageIndex;
        private bool capturedFallbackFinish;
        private bool isGenerating;

        public int Seed => seed;
        public string LastSequence => lastSequence;
        public string LastGenerationFailure => lastGenerationFailure;
        public bool UsingGeneratedCourse { get; private set; }
        public bool EndlessMode => endlessMode;
        public int ActiveStageCount => activeStages.Count;
        public float HighestGeneratedY => highestGeneratedY;
        public int GeneratedChunkTotal
        {
            get
            {
                int total = 0;
                for (int i = 0; i < activeStages.Count; i++)
                {
                    total += activeStages[i].ChunkCount;
                }

                return total;
            }
        }

        private void Awake()
        {
            maximumSeedAttempts = Mathf.Max(
                MinimumSeedAttempts,
                maximumSeedAttempts);
            ResolveRuntimeReferences();
            CaptureFallbackFinish();
            if (!generateOnAwake)
            {
                return;
            }

            if (randomizeInitialSeed)
            {
                GenerateNextCourse();
            }
            else
            {
                GenerateCourse();
            }
        }

        private void Update()
        {
            if (!endlessMode || !UsingGeneratedCourse || isGenerating || player == null)
            {
                return;
            }

            if (TowerStreamingRules.ShouldAppend(
                    highestGeneratedY,
                    player.position.y,
                    generationAheadDistance) &&
                Time.time >= nextAppendAttemptTime)
            {
                if (!TryAppendNextStage())
                {
                    nextAppendAttemptTime = Time.time + appendRetryDelay;
                }
            }

            CleanupSubmergedStages();
        }

        public int GetStageIndexAtHeight(float worldHeight)
        {
            if (activeStages.Count == 0)
            {
                return 0;
            }

            int stageIndex = activeStages[0].StageIndex;
            for (int i = 0; i < activeStages.Count; i++)
            {
                if (worldHeight + 0.1f < activeStages[i].LowestY)
                {
                    break;
                }

                stageIndex = activeStages[i].StageIndex;
            }

            return stageIndex;
        }

        public float GetStageProgressAtHeight(float worldHeight)
        {
            if (activeStages.Count == 0)
            {
                return 0f;
            }

            StageRecord current = activeStages[0];
            for (int i = 0; i < activeStages.Count; i++)
            {
                if (worldHeight + 0.1f < activeStages[i].LowestY)
                {
                    break;
                }

                current = activeStages[i];
            }

            return Mathf.InverseLerp(current.LowestY, current.HighestY, worldHeight);
        }


        public bool GenerateCourse()
        {
            if (!HasValidConfiguration())
            {
                ActivateFallback("Generator configuration is incomplete.");
                return false;
            }

            if (TryBuildInitialStage(seed, out StageBuild build, out string failureReason))
            {
                CommitNewCourse(seed, build);
                return true;
            }

            ActivateFallback($"Seed {seed}: {failureReason}");
            return false;
        }

        public bool GenerateNextCourse()
        {
            if (!HasValidConfiguration())
            {
                if (!UsingGeneratedCourse)
                {
                    ActivateFallback("Generator configuration is incomplete.");
                }

                return false;
            }

            isGenerating = true;
            try
            {
                List<string> rejectedSeeds = new List<string>();
                HashSet<int> attemptedSeeds = new HashSet<int>();
                for (int attempt = 0; attempt < maximumSeedAttempts; attempt++)
                {
                    int candidateSeed;
                    do
                    {
                        candidateSeed = CreateRuntimeSeed();
                    }
                    while (candidateSeed == seed || !attemptedSeeds.Add(candidateSeed));

                    if (TryBuildInitialStage(
                        candidateSeed,
                        out StageBuild build,
                        out string failureReason))
                    {
                        CommitNewCourse(candidateSeed, build);
                        if (rejectedSeeds.Count > 0)
                        {
                            Debug.LogWarning(
                                $"Skipped {rejectedSeeds.Count} invalid tower seed(s) before " +
                                $"using {candidateSeed}: {SummarizeRejectedSeeds(rejectedSeeds)}.",
                                this);
                        }

                        return true;
                    }

                    rejectedSeeds.Add($"{candidateSeed} ({failureReason})");
                }

                lastGenerationFailure =
                    $"Could not generate a valid tower after {maximumSeedAttempts} seed attempts.";
                if (!UsingGeneratedCourse)
                {
                    ActivateFallback(lastGenerationFailure);
                }
                else
                {
                    Debug.LogError(
                        $"{lastGenerationFailure} Keeping the current course and seed {seed}. " +
                        $"Rejected: {SummarizeRejectedSeeds(rejectedSeeds)}.",
                        this);
                }

                return false;
            }
            finally
            {
                isGenerating = false;
            }
        }

        private bool TryBuildInitialStage(
            int candidateSeed,
            out StageBuild build,
            out string failureReason)
        {
            return TryBuildStage(
                candidateSeed,
                stageIndex: 0,
                startChunkPrefab.Entry.localPosition,
                previousChunk: null,
                includeStartChunk: true,
                out build,
                out failureReason);
        }

        private bool TryAppendNextStage()
        {
            if (lastPlacedChunk == null)
            {
                lastGenerationFailure = "Cannot append because the current tower has no final chunk.";
                return false;
            }

            isGenerating = true;
            try
            {
                List<string> rejectedStages = new List<string>();
                for (int attempt = 0; attempt < maximumSeedAttempts; attempt++)
                {
                    int stageSeed = TowerStreamingRules.DeriveStageSeed(
                        seed,
                        nextStageIndex,
                        attempt);
                    if (TryBuildStage(
                        stageSeed,
                        nextStageIndex,
                        nextAttachmentPoint,
                        lastPlacedChunk,
                        includeStartChunk: false,
                        out StageBuild build,
                        out string failureReason))
                    {
                        CommitAppendedStage(build);
                        return true;
                    }

                    rejectedStages.Add($"{stageSeed} ({failureReason})");
                }

                lastGenerationFailure =
                    $"Stage {nextStageIndex + 1} could not be generated after " +
                    $"{maximumSeedAttempts} attempts.";
                Debug.LogError(
                    $"{lastGenerationFailure} The current playable tower remains active. " +
                    $"Rejected: {SummarizeRejectedSeeds(rejectedStages)}.",
                    this);
                return false;
            }
            finally
            {
                isGenerating = false;
            }
        }

        private bool TryBuildStage(
            int stageSeed,
            int stageIndex,
            Vector3 attachmentPoint,
            TowerChunk previousChunk,
            bool includeStartChunk,
            out StageBuild build,
            out string failureReason)
        {
            ResetPlacementState();
            GameObject rootObject = new GameObject(
                $"Stage_{stageIndex:00}_Seed_{stageSeed}");
            Transform stageRoot = rootObject.transform;
            stageRoot.SetParent(generatedRoot, false);

            if (previousChunk != null)
            {
                placedBounds.Add(previousChunk.GetWorldBounds());
                previousChunk.AppendWorldClearanceSegments(protectedClearances);
            }

            System.Random random = new System.Random(stageSeed);
            Queue<string> recentIds = new Queue<string>();
            ChunkTraversalCategory? previousCategory = null;
            int previousCategoryStreak = 0;
            TowerChunk currentPrevious = previousChunk;
            Vector3 currentAttachment = attachmentPoint;
            StringBuilder sequence = new StringBuilder();
            float lowestY = float.PositiveInfinity;
            float highestY = float.NegativeInfinity;

            for (int chunkNumber = 0; chunkNumber < generatedChunkCount; chunkNumber++)
            {
                TowerChunk placedChunk;
                if (chunkNumber == 0)
                {
                    TowerChunk firstPrefab = includeStartChunk
                        ? startChunkPrefab
                        : transitionChunkPrefab;
                    placedChunk = TryPlaceChunk(
                        firstPrefab,
                        currentAttachment,
                        random,
                        currentPrevious,
                        stageRoot,
                        forceIdentityRotation: includeStartChunk,
                        allowDirectionReset: !includeStartChunk);
                }
                else
                {
                    int globalChunkNumber = stageIndex * generatedChunkCount + chunkNumber;
                    int maximumDifficulty = 1 + globalChunkNumber / 4;
                    placedChunk = TrySelectAndPlaceChunk(
                        currentAttachment,
                        maximumDifficulty,
                        recentIds,
                        previousCategory,
                        previousCategoryStreak,
                        currentPrevious,
                        stageRoot,
                        random);
                }

                if (placedChunk == null)
                {
                    failureReason = $"no valid placement for chunk {chunkNumber}";
                    DetachAndDestroyCourseRoot(stageRoot);
                    ResetPlacementState();
                    build = null;
                    return false;
                }

                ApplyStageMaterial(placedChunk, stageIndex);
                spawnedChunks.Add(placedChunk);
                Bounds bounds = placedChunk.GetWorldBounds();
                placedBounds.Add(bounds);
                placedChunk.AppendWorldClearanceSegments(protectedClearances);
                lowestY = Mathf.Min(lowestY, bounds.min.y);
                highestY = Mathf.Max(highestY, bounds.max.y);
                currentAttachment = placedChunk.Exit.position;
                currentPrevious = placedChunk;
                previousCategoryStreak = previousCategory.HasValue &&
                    previousCategory.Value == placedChunk.TraversalCategory
                    ? previousCategoryStreak + 1
                    : 1;
                previousCategory = placedChunk.TraversalCategory;
                recentIds.Enqueue(placedChunk.ChunkId);
                while (recentIds.Count > recentHistoryLength)
                {
                    recentIds.Dequeue();
                }

                if (sequence.Length > 0)
                {
                    sequence.Append(" -> ");
                }

                sequence.Append(placedChunk.ChunkId);
            }

            build = new StageBuild(
                stageIndex,
                stageRoot,
                spawnedChunks.Count,
                lowestY,
                highestY,
                currentAttachment,
                currentPrevious,
                sequence.ToString());
            failureReason = string.Empty;
            return true;
        }

        private void CommitNewCourse(int candidateSeed, StageBuild build)
        {
            build.Root.SetParent(null, true);
            ClearGeneratedCourse();
            build.Root.SetParent(generatedRoot, true);

            seed = candidateSeed;
            activeStages.Add(build.ToRecord());
            nextAttachmentPoint = build.NextAttachmentPoint;
            lastPlacedChunk = build.LastChunk;
            highestGeneratedY = build.HighestY;
            nextStageIndex = 1;
            nextAppendAttemptTime = 0f;
            lastSequence = build.Sequence;
            lastGenerationFailure = string.Empty;

            authoredFallback.SetActive(false);
            UsingGeneratedCourse = true;
            ConfigureFinishForCurrentMode(build);
            runController.ConfigureCourse(finishGoal, seed);
            Debug.Log(
                $"Generated endless tower seed {seed}, stage 1: {lastSequence}. " +
                $"Rejected rotations: direction={directionRejections}, " +
                $"clearance={clearanceRejections}.",
                this);
        }

        private void CommitAppendedStage(StageBuild build)
        {
            activeStages.Add(build.ToRecord());
            nextAttachmentPoint = build.NextAttachmentPoint;
            lastPlacedChunk = build.LastChunk;
            highestGeneratedY = Mathf.Max(highestGeneratedY, build.HighestY);
            nextStageIndex = build.StageIndex + 1;
            lastSequence = build.Sequence;
            lastGenerationFailure = string.Empty;
            Debug.Log(
                $"Streamed stage {build.StageIndex + 1} for run seed {seed}. " +
                $"Active stages={activeStages.Count}, chunks={GeneratedChunkTotal}, " +
                $"top={highestGeneratedY:0.0} m.",
                this);
        }

        public void Configure(
            GameObject fallback,
            Transform outputRoot,
            TowerChunk startPrefab,
            TowerChunk[] reusablePrefabs,
            Transform finish,
            RunController controller,
            int runSeed,
            int chunkCount)
        {
            authoredFallback = fallback;
            generatedRoot = outputRoot;
            startChunkPrefab = startPrefab;
            chunkPrefabs = reusablePrefabs;
            finishGoal = finish;
            runController = controller;
            seed = runSeed;
            generatedChunkCount = Mathf.Max(4, chunkCount);
        }

        private TowerChunk TrySelectAndPlaceChunk(
            Vector3 attachmentPoint,
            int maximumDifficulty,
            IReadOnlyCollection<string> recentIds,
            ChunkTraversalCategory? previousCategory,
            int previousCategoryStreak,
            TowerChunk previousChunk,
            Transform outputRoot,
            System.Random random)
        {
            List<TowerChunk> remaining = new List<TowerChunk>(chunkPrefabs);
            List<ChunkCandidate> candidates = new List<ChunkCandidate>(remaining.Count);
            for (int i = 0; i < remaining.Count; i++)
            {
                candidates.Add(remaining[i].ToCandidate());
            }

            int attempts = Mathf.Min(maximumPlacementAttempts, remaining.Count * 4);
            for (int attempt = 0; attempt < attempts && remaining.Count > 0; attempt++)
            {
                int selectedIndex = ChunkSelectionRules.ChooseIndex(
                    random,
                    candidates,
                    maximumDifficulty,
                    recentIds,
                    previousCategory,
                    previousCategoryStreak);

                if (selectedIndex < 0)
                {
                    return null;
                }

                TowerChunk selected = remaining[selectedIndex];
                TowerChunk placed = TryPlaceChunk(
                    selected,
                    attachmentPoint,
                    random,
                    previousChunk,
                    outputRoot,
                    forceIdentityRotation: false);
                if (placed != null)
                {
                    return placed;
                }

                remaining.RemoveAt(selectedIndex);
                candidates.RemoveAt(selectedIndex);
            }

            return null;
        }

        private TowerChunk TryPlaceChunk(
            TowerChunk prefab,
            Vector3 attachmentPoint,
            System.Random random,
            TowerChunk previousChunk,
            Transform outputRoot,
            bool forceIdentityRotation,
            bool allowDirectionReset = false)
        {
            int firstRotation = forceIdentityRotation ? 0 : random.Next(4);
            int rotationCount = forceIdentityRotation ? 1 : 4;

            for (int rotationAttempt = 0; rotationAttempt < rotationCount; rotationAttempt++)
            {
                int quarterTurns = (firstRotation + rotationAttempt) % 4;
                Quaternion rotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
                TowerChunk instance = Instantiate(prefab, outputRoot);
                instance.name =
                    $"Generated_{spawnedChunks.Count:00}_{prefab.ChunkId}";
                instance.transform.SetPositionAndRotation(Vector3.zero, rotation);
                Vector3 entryOffset = instance.Entry.position - instance.transform.position;
                instance.transform.position = attachmentPoint - entryOffset;

                Bounds bounds = instance.GetWorldBounds();
                bool directionAllowed = allowDirectionReset || previousChunk == null ||
                    ChunkPlacementRules.IsTurnAllowed(
                        previousChunk.WorldExitDirection,
                        instance.WorldEntryDirection,
                        maximumTurnAngle);
                if (!directionAllowed)
                {
                    directionRejections++;
                    DeactivateAndDestroy(instance.gameObject);
                    continue;
                }

                if (instance.BlocksAnyClearance(protectedClearances))
                {
                    clearanceRejections++;
                    DeactivateAndDestroy(instance.gameObject);
                    continue;
                }

                if (IsInsideTower(instance.Exit.position, bounds) &&
                    !OverlapsEarlierChunk(bounds))
                {
                    return instance;
                }

                DeactivateAndDestroy(instance.gameObject);
            }

            return null;
        }

        private bool IsInsideTower(Vector3 exitPosition, Bounds bounds)
        {
            Vector2 exitHorizontal = new Vector2(exitPosition.x, exitPosition.z);
            Vector2 centerHorizontal = new Vector2(bounds.center.x, bounds.center.z);
            float horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return exitHorizontal.magnitude <= maximumHorizontalRadius &&
                centerHorizontal.magnitude + horizontalExtent <= maximumHorizontalRadius + 3f;
        }

        private bool OverlapsEarlierChunk(Bounds candidate)
        {
            Bounds padded = candidate;
            padded.Expand(-Mathf.Min(
                overlapPadding,
                Mathf.Min(candidate.size.x, candidate.size.z) * 0.25f));

            int comparisonCount = Mathf.Max(0, placedBounds.Count - 1);
            for (int i = 0; i < comparisonCount; i++)
            {
                Bounds earlier = placedBounds[i];
                earlier.Expand(-overlapPadding);
                if (padded.Intersects(earlier))
                {
                    return true;
                }
            }

            return false;
        }

        private void CleanupSubmergedStages()
        {
            if (hazard == null || activeStages.Count <= 2)
            {
                return;
            }

            while (activeStages.Count > 2 &&
                TowerStreamingRules.CanRecycle(
                    activeStages[0].HighestY,
                    hazard.SurfaceHeight,
                    cleanupBelowWaterMargin))
            {
                StageRecord removed = activeStages[0];
                activeStages.RemoveAt(0);
                DetachAndDestroyCourseRoot(removed.Root);
                Debug.Log(
                    $"Recycled submerged stage {removed.StageIndex + 1}. " +
                    $"Active stages={activeStages.Count}.",
                    this);
            }
        }

        private void ApplyStageMaterial(TowerChunk chunk, int stageIndex)
        {
            if (chunk == null || stageMaterials == null || stageMaterials.Length == 0)
            {
                return;
            }

            Material material = stageMaterials[stageIndex % stageMaterials.Length];
            if (material == null)
            {
                return;
            }

            Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponentInParent<GrappleAnchor>() != null)
                {
                    continue;
                }

                renderers[i].sharedMaterial = material;
            }
        }

        private void ConfigureFinishForCurrentMode(StageBuild build)
        {
            if (finishGoal == null)
            {
                return;
            }

            finishGoal.gameObject.SetActive(!endlessMode);
            if (endlessMode)
            {
                return;
            }

            finishGoal.SetPositionAndRotation(
                build.NextAttachmentPoint + Vector3.up * 0.2f,
                build.LastChunk.Exit.rotation);
            FinishGoal finishComponent = finishGoal.GetComponent<FinishGoal>();
            finishComponent?.Configure(runController);
        }

        private bool HasValidConfiguration()
        {
            if (authoredFallback == null || generatedRoot == null ||
                startChunkPrefab == null || finishGoal == null || runController == null ||
                chunkPrefabs == null || chunkPrefabs.Length < 4)
            {
                return false;
            }

            if (endlessMode && (transitionChunkPrefab == null || player == null || hazard == null))
            {
                return false;
            }

            if (startChunkPrefab.Entry == null || startChunkPrefab.Exit == null)
            {
                return false;
            }

            if (transitionChunkPrefab != null &&
                (transitionChunkPrefab.Entry == null || transitionChunkPrefab.Exit == null))
            {
                return false;
            }

            for (int i = 0; i < chunkPrefabs.Length; i++)
            {
                if (chunkPrefabs[i] == null || chunkPrefabs[i].Entry == null ||
                    chunkPrefabs[i].Exit == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveRuntimeReferences()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (hazard == null)
            {
                hazard = FindFirstObjectByType<RisingHazard>();
            }
        }

        private void ActivateFallback(string reason)
        {
            ClearGeneratedCourse();
            UsingGeneratedCourse = false;
            lastSequence = string.Empty;
            lastGenerationFailure = reason;
            if (authoredFallback != null)
            {
                authoredFallback.SetActive(true);
            }

            if (capturedFallbackFinish && finishGoal != null)
            {
                finishGoal.gameObject.SetActive(true);
                finishGoal.SetPositionAndRotation(
                    fallbackFinishPosition,
                    fallbackFinishRotation);
                runController?.ConfigureCourse(finishGoal, seed);
            }

            Debug.LogWarning($"Procedural tower fallback: {reason}", this);
        }

        private void CaptureFallbackFinish()
        {
            if (capturedFallbackFinish || finishGoal == null)
            {
                return;
            }

            fallbackFinishPosition = finishGoal.position;
            fallbackFinishRotation = finishGoal.rotation;
            capturedFallbackFinish = true;
        }

        private void ClearGeneratedCourse()
        {
            for (int i = generatedRoot != null ? generatedRoot.childCount - 1 : -1;
                i >= 0;
                i--)
            {
                DetachAndDestroyCourseRoot(generatedRoot.GetChild(i));
            }

            activeStages.Clear();
            lastPlacedChunk = null;
            highestGeneratedY = 0f;
            nextStageIndex = 0;
            ResetPlacementState();
        }

        private void ResetPlacementState()
        {
            spawnedChunks.Clear();
            placedBounds.Clear();
            protectedClearances.Clear();
            directionRejections = 0;
            clearanceRejections = 0;
        }

        private static int CreateRuntimeSeed()
        {
            int runtimeSeed = Guid.NewGuid().GetHashCode() & int.MaxValue;
            return runtimeSeed == 0 ? 1 : runtimeSeed;
        }

        private static string SummarizeRejectedSeeds(List<string> rejectedSeeds)
        {
            const int maximumShown = 5;
            int shownCount = Mathf.Min(maximumShown, rejectedSeeds.Count);
            string summary = string.Join(
                ", ",
                rejectedSeeds.GetRange(0, shownCount));
            int remainingCount = rejectedSeeds.Count - shownCount;
            return remainingCount > 0
                ? $"{summary}, and {remainingCount} more"
                : summary;
        }

        private static void DeactivateAndDestroy(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            target.SetActive(false);
            Destroy(target);
        }

        private static void DetachAndDestroyCourseRoot(Transform courseRoot)
        {
            if (courseRoot == null)
            {
                return;
            }

            courseRoot.gameObject.SetActive(false);
            courseRoot.SetParent(null, true);
            Destroy(courseRoot.gameObject);
        }

        private void OnValidate()
        {
            generatedChunkCount = Mathf.Max(4, generatedChunkCount);
            recentHistoryLength = Mathf.Max(1, recentHistoryLength);
            maximumSeedAttempts = Mathf.Max(
                MinimumSeedAttempts,
                maximumSeedAttempts);
            generationAheadDistance = Mathf.Max(5f, generationAheadDistance);
            cleanupBelowWaterMargin = Mathf.Max(0f, cleanupBelowWaterMargin);
            appendRetryDelay = Mathf.Max(0.1f, appendRetryDelay);
            maximumHorizontalRadius = Mathf.Max(5f, maximumHorizontalRadius);
            maximumTurnAngle = Mathf.Clamp(maximumTurnAngle, 45f, 135f);
            overlapPadding = Mathf.Max(0f, overlapPadding);
            maximumPlacementAttempts = Mathf.Max(4, maximumPlacementAttempts);
        }

        private sealed class StageBuild
        {
            public StageBuild(
                int stageIndex,
                Transform root,
                int chunkCount,
                float lowestY,
                float highestY,
                Vector3 nextAttachmentPoint,
                TowerChunk lastChunk,
                string sequence)
            {
                StageIndex = stageIndex;
                Root = root;
                ChunkCount = chunkCount;
                LowestY = lowestY;
                HighestY = highestY;
                NextAttachmentPoint = nextAttachmentPoint;
                LastChunk = lastChunk;
                Sequence = sequence;
            }

            public int StageIndex { get; }
            public Transform Root { get; }
            public int ChunkCount { get; }
            public float LowestY { get; }
            public float HighestY { get; }
            public Vector3 NextAttachmentPoint { get; }
            public TowerChunk LastChunk { get; }
            public string Sequence { get; }

            public StageRecord ToRecord()
            {
                return new StageRecord(
                    StageIndex,
                    Root,
                    ChunkCount,
                    LowestY,
                    HighestY);
            }
        }

        private sealed class StageRecord
        {
            public StageRecord(
                int stageIndex,
                Transform root,
                int chunkCount,
                float lowestY,
                float highestY)
            {
                StageIndex = stageIndex;
                Root = root;
                ChunkCount = chunkCount;
                LowestY = lowestY;
                HighestY = highestY;
            }

            public int StageIndex { get; }
            public Transform Root { get; }
            public int ChunkCount { get; }
            public float LowestY { get; }
            public float HighestY { get; }
        }
    }
}
