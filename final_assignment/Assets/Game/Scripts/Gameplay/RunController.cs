using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RunController : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string RestartActionName = "Restart";

        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform player;
        [SerializeField] private RisingHazard hazard;
        [SerializeField] private Transform finish;

        [Header("Run")]
        [SerializeField, Min(0f)] private float hazardContactMargin = 0.85f;
        [SerializeField] private int displayedSeed = 104729;

        [Header("Flood Warning")]
        [SerializeField, Min(0.1f)] private float floodWarningDistance = 6f;
        [SerializeField, Min(0.1f)] private float floodCriticalDistance = 2.5f;
        [SerializeField, Range(0f, 0.5f)] private float maximumWarningAlpha = 0.28f;
        [SerializeField, Min(1f)] private float maximumWarningBorderWidth = 38f;

        private InputAction restartAction;
        private PlayerController playerController;
        private GrappleController grappleController;
        private TowerGenerator towerGenerator;
        private CrosshairPresenter crosshair;
        private Vector3 playerStartPosition;
        private Quaternion playerStartRotation;
        private float bestHeight;
        private float runHeight;
        private float floodClearance = float.PositiveInfinity;
        private string resultTitle;
        private bool runEnded;

        public bool RunEnded => runEnded;
        public float CurrentHeight => runHeight;
        public float BestHeight => bestHeight;
        public float FloodClearance => floodClearance;

        public void ConfigureCourse(Transform courseFinish, int seed)
        {
            finish = courseFinish;
            displayedSeed = seed;
        }

        private void Awake()
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

            if (finish == null)
            {
                GameObject finishObject = GameObject.Find("FinishGoal");
                if (finishObject != null)
                {
                    finish = finishObject.transform;
                }
            }

            towerGenerator = FindFirstObjectByType<TowerGenerator>();

            if (player != null)
            {
                playerStartPosition = player.position;
                playerStartRotation = player.rotation;
                playerController = player.GetComponent<PlayerController>();
                grappleController = player.GetComponent<GrappleController>();
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                crosshair = mainCamera.GetComponent<CrosshairPresenter>();
            }
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                InputActionMap gameplayMap = inputActions.FindActionMap(
                    GameplayMapName,
                    true);
                restartAction = gameplayMap.FindAction(RestartActionName, true);
                restartAction.Enable();
            }

        }

        private void Start()
        {
            StartRun();
        }

        private void OnDisable()
        {
            restartAction?.Disable();
        }

        private void Update()
        {
            if (restartAction != null && restartAction.WasPressedThisFrame())
            {
                StartNewRun();
                return;
            }

            if (runEnded || player == null || hazard == null)
            {
                return;
            }

            runHeight = Mathf.Max(
                runHeight,
                player.position.y - playerStartPosition.y);
            bestHeight = Mathf.Max(bestHeight, runHeight);

            if (towerGenerator != null && towerGenerator.EndlessMode)
            {
                hazard.ReportPlayerProgress(
                    runHeight,
                    towerGenerator.GetStageIndexAtHeight(player.position.y),
                    towerGenerator.GetStageProgressAtHeight(player.position.y));
            }
            else
            {
                hazard.ReportPlayerHeight(runHeight);
            }

            floodClearance = player.position.y -
                (hazard.SurfaceHeight + hazardContactMargin);
            if (floodClearance <= 0f)
            {
                EndRun("THE FLOOD CAUGHT YOU", freezePlayer: false);
            }
        }

        public void CompleteRun()
        {
            if (!runEnded)
            {
                EndRun("TOWER CLEARED", freezePlayer: true);
            }
        }

        public void StartNewRun()
        {
            grappleController?.ResetForNewRun();
            playerController?.ResetForNewRun();

            if (towerGenerator == null || !towerGenerator.GenerateNextCourse())
            {
                Debug.LogWarning(
                    "New run request was cancelled because no valid replacement course was generated.",
                    this);
                StartRun();
                return;
            }

            StartRun();
        }

        public void StartRun()
        {
            runEnded = false;
            resultTitle = string.Empty;
            runHeight = 0f;
            floodClearance = float.PositiveInfinity;

            if (player != null)
            {
                player.SetPositionAndRotation(
                    playerStartPosition,
                    playerStartRotation);

                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.position = playerStartPosition;
                    playerBody.rotation = playerStartRotation;
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                    playerBody.useGravity = true;
                }
            }

            Physics.SyncTransforms();
            playerController?.ResetForNewRun();
            playerController?.SetMovementEnabled(true);
            grappleController?.ResetForNewRun();

            if (crosshair != null)
            {
                crosshair.Visible = true;
            }

            if (hazard != null)
            {
                hazard.ResetHazard();
                hazard.Begin();
            }
        }

        private void EndRun(string title, bool freezePlayer)
        {
            runEnded = true;
            resultTitle = title;
            bestHeight = Mathf.Max(bestHeight, runHeight);
            hazard.Stop();
            playerController?.SetMovementEnabled(false);
            grappleController?.SetGrappleEnabled(false);

            if (freezePlayer && player != null)
            {
                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                    playerBody.useGravity = false;
                }
            }

            if (crosshair != null)
            {
                crosshair.Visible = false;
            }
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(0.8f, Screen.height / 900f);
            DrawFloodWarning(scale);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Box(new Rect(20f, 20f, 370f * scale, 176f * scale), string.Empty);
            GUI.Label(new Rect(36f, 30f, 330f * scale, 30f * scale),
                $"HEIGHT  {runHeight:0.0} m", labelStyle);
            GUI.Label(new Rect(36f, 62f, 330f * scale, 30f * scale),
                $"BEST      {bestHeight:0.0} m", labelStyle);
            GUI.Label(new Rect(36f, 94f, 330f * scale, 30f * scale),
                $"SEED  {displayedSeed}", labelStyle);
            GUI.Label(new Rect(36f, 126f, 330f * scale, 30f * scale),
                $"STAGE  {(hazard != null ? hazard.CurrentStage + 1 : 1)}", labelStyle);
            GUI.Label(new Rect(36f, 158f, 330f * scale, 30f * scale),
                $"FLOOD  {(hazard != null ? hazard.CurrentSpeed : 0f):0.00} m/s", labelStyle);

            if (!runEnded)
            {
                return;
            }

            float panelWidth = 480f * scale;
            float panelHeight = 230f * scale;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            GUI.Box(panel, string.Empty);

            GUIStyle titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.RoundToInt(32f * scale),
                alignment = TextAnchor.MiddleCenter
            };
            GUIStyle centerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(panel.x, panel.y + 22f * scale, panel.width, 55f * scale),
                resultTitle, titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 88f * scale, panel.width, 40f * scale),
                $"Height: {runHeight:0.0} m    Best: {bestHeight:0.0} m", centerStyle);
            GUI.Label(new Rect(panel.x, panel.y + 144f * scale, panel.width, 45f * scale),
                "R  New Tower", centerStyle);
        }

        private void DrawFloodWarning(float scale)
        {
            if (runEnded || !float.IsFinite(floodClearance))
            {
                return;
            }

            float warningStrength = FloodWarningRules.CalculateStrength(
                floodClearance,
                floodWarningDistance);
            if (warningStrength <= 0f)
            {
                return;
            }

            float criticalStrength = FloodWarningRules.CalculateCriticalStrength(
                floodClearance,
                floodCriticalDistance);
            float pulseSpeed = Mathf.Lerp(3.5f, 8f, criticalStrength);
            float pulse = 0.82f +
                (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f) * 0.28f;
            float alpha = Mathf.Clamp01(
                maximumWarningAlpha * warningStrength * pulse);
            float borderWidth = Mathf.Lerp(
                8f,
                maximumWarningBorderWidth,
                warningStrength) * scale;
            Color warningColor = Color.Lerp(
                new Color(0.05f, 0.72f, 1f, alpha),
                new Color(1f, 0.16f, 0.05f, alpha),
                criticalStrength);

            Color previousColor = GUI.color;
            GUI.color = warningColor;
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, borderWidth),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(0f, Screen.height - borderWidth, Screen.width, borderWidth),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(0f, borderWidth, borderWidth, Screen.height - borderWidth * 2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(
                    Screen.width - borderWidth,
                    borderWidth,
                    borderWidth,
                    Screen.height - borderWidth * 2f),
                Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUIStyle warningStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            float labelWidth = 310f * scale;
            GUI.Box(
                new Rect(
                    (Screen.width - labelWidth) * 0.5f,
                    22f * scale,
                    labelWidth,
                    42f * scale),
                $"FLOOD CLOSE  {Mathf.Max(0f, floodClearance):0.0} m",
                warningStyle);
        }

        private void OnValidate()
        {
            hazardContactMargin = Mathf.Max(0f, hazardContactMargin);
            floodWarningDistance = Mathf.Max(0.1f, floodWarningDistance);
            floodCriticalDistance = Mathf.Clamp(
                floodCriticalDistance,
                0.1f,
                floodWarningDistance);
            maximumWarningAlpha = Mathf.Clamp(maximumWarningAlpha, 0f, 0.5f);
            maximumWarningBorderWidth = Mathf.Max(1f, maximumWarningBorderWidth);
        }
    }
}
