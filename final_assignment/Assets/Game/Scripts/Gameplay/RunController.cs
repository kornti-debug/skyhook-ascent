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

        [Header("Run")]
        [SerializeField, Min(0f)] private float hazardContactMargin = 0.85f;
        [SerializeField] private int displayedSeed = 104729;

        [Header("Flood Warning")]
        [SerializeField, Min(0.1f)] private float floodWarningDistance = 6f;
        [SerializeField, Min(0.1f)] private float floodCriticalDistance = 2.5f;
        [SerializeField, Range(0f, 0.5f)] private float maximumWarningAlpha = 0.28f;
        [SerializeField, Min(1f)] private float maximumWarningBorderWidth = 38f;

        [Header("HUD")]
        [SerializeField, Min(0.5f)] private float initialRunIntroDuration = 8f;
        [SerializeField, Min(0.5f)] private float runIntroDuration = 5f;
        [SerializeField, Min(0.1f)] private float runIntroFadeDuration = 1.25f;

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
        private float runStartedAt;
        private float activeRunIntroDuration;
        private float timeScaleBeforePerkChoice = 1f;
        private CursorLockMode cursorLockBeforePerkChoice;
        private bool hasStartedRun;
        private bool cursorVisibleBeforePerkChoice;
        private bool perkChoiceOpen;
        private bool runEnded;
        private int lastPerkStageIndex;
        private int quickRecallStacks;
        private int climbersPaceStacks;
        private int lightFeetStacks;

        public bool RunEnded => runEnded;
        public float CurrentHeight => runHeight;
        public float BestHeight => bestHeight;
        public float FloodClearance => floodClearance;

        public void ConfigureSeed(int seed)
        {
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
            ClosePerkChoice();
        }

        private void Update()
        {
            if (restartAction != null && restartAction.WasPressedThisFrame())
            {
                StartNewRun();
                return;
            }

            if (perkChoiceOpen)
            {
                HandlePerkChoiceInput();
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

            if (towerGenerator != null && towerGenerator.HasGeneratedTower)
            {
                int stageIndex = towerGenerator.GetStageIndexAtHeight(player.position.y);
                hazard.ReportPlayerProgress(
                    runHeight,
                    stageIndex,
                    towerGenerator.GetStageProgressAtHeight(player.position.y));

                TryOpenPerkChoice(stageIndex);
            }
            else
            {
                hazard.ReportPlayerHeight(runHeight);
            }

            floodClearance = player.position.y -
                (hazard.SurfaceHeight + hazardContactMargin);
            if (floodClearance <= 0f)
            {
                EndRun();
            }
        }

        public void StartNewRun()
        {
            ClosePerkChoice();
            grappleController?.ResetForNewRun();
            playerController?.ResetForNewRun();

            if (towerGenerator == null || !towerGenerator.GenerateNextTower())
            {
                Debug.LogWarning(
                    "New run request was cancelled because no valid replacement tower was generated.",
                    this);
                StartRun();
                return;
            }

            StartRun();
        }

        public void StartRun()
        {
            ClosePerkChoice();
            runEnded = false;
            runHeight = 0f;
            floodClearance = float.PositiveInfinity;
            lastPerkStageIndex = 0;
            quickRecallStacks = 0;
            climbersPaceStacks = 0;
            lightFeetStacks = 0;
            activeRunIntroDuration = hasStartedRun
                ? runIntroDuration
                : initialRunIntroDuration;
            hasStartedRun = true;
            runStartedAt = Time.unscaledTime;

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

        private void EndRun()
        {
            ClosePerkChoice();
            runEnded = true;
            bestHeight = Mathf.Max(bestHeight, runHeight);
            hazard.Stop();
            playerController?.SetMovementEnabled(false);
            grappleController?.SetGrappleEnabled(false);

            if (crosshair != null)
            {
                crosshair.Visible = false;
            }
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(0.8f, Screen.height / 900f);
            if (!perkChoiceOpen)
            {
                DrawFloodWarning(scale);
            }

            DrawRunHud(scale);

            if (perkChoiceOpen)
            {
                DrawPerkChoicePanel(scale);
                return;
            }

            if (!runEnded)
            {
                DrawRunIntro(scale);
                return;
            }

            DrawRunEndPanel(scale);
        }

        private void DrawRunHud(float scale)
        {
            float margin = 18f * scale;
            Rect panel = new Rect(
                margin,
                margin,
                306f * scale,
                180f * scale);
            DrawFilledRect(panel, new Color(0.015f, 0.035f, 0.065f, 0.82f));
            DrawFilledRect(
                new Rect(panel.x, panel.y, 5f * scale, panel.height),
                new Color(0.06f, 0.78f, 1f, 0.95f));

            GUIStyle captionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.86f, 1f, 1f) }
            };
            GUIStyle heightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(30f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.93f, 1f, 1f) }
            };
            GUIStyle seedStyle = new GUIStyle(detailStyle)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                normal = { textColor = new Color(0.62f, 0.72f, 0.82f, 1f) }
            };
            GUIStyle perkStyle = new GUIStyle(seedStyle)
            {
                fontSize = Mathf.RoundToInt(11f * scale),
                normal = { textColor = new Color(0.48f, 0.83f, 0.96f, 1f) }
            };

            float contentX = panel.x + 18f * scale;
            float contentWidth = panel.width - 32f * scale;
            GUI.Label(
                new Rect(contentX, panel.y + 8f * scale, contentWidth, 20f * scale),
                "SKYHOOK ASCENT  /  HEIGHT",
                captionStyle);
            GUI.Label(
                new Rect(contentX, panel.y + 28f * scale, contentWidth, 42f * scale),
                $"{runHeight:0.0} m",
                heightStyle);
            GUI.Label(
                new Rect(contentX, panel.y + 72f * scale, contentWidth, 24f * scale),
                $"BEST  {bestHeight:0.0} m        STAGE  {(hazard != null ? hazard.CurrentStage + 1 : 1)}",
                detailStyle);
            GUI.Label(
                new Rect(contentX, panel.y + 99f * scale, contentWidth, 24f * scale),
                $"FLOOD SPEED  {(hazard != null ? hazard.CurrentSpeed : 0f):0.00} m/s",
                detailStyle);
            GUI.Label(
                new Rect(contentX, panel.y + 128f * scale, contentWidth, 20f * scale),
                $"SEED {displayedSeed}   |   R  NEW TOWER",
                seedStyle);
            GUI.Label(
                new Rect(contentX, panel.y + 151f * scale, contentWidth, 18f * scale),
                $"PERKS  RECALL {quickRecallStacks}  PACE {climbersPaceStacks}  JUMP {lightFeetStacks}",
                perkStyle);
        }

        private void DrawPerkChoicePanel(float scale)
        {
            float panelWidth = Mathf.Min(890f * scale, Screen.width - 32f * scale);
            float panelHeight = 330f * scale;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            DrawFilledRect(panel, new Color(0.01f, 0.035f, 0.06f, 0.96f));
            DrawFilledRect(
                new Rect(panel.x, panel.y, panel.width, 5f * scale),
                new Color(0.06f, 0.82f, 1f, 1f));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(25f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.69f, 0.82f, 0.92f, 1f) }
            };
            GUI.Label(
                new Rect(panel.x + 12f * scale, panel.y + 14f * scale,
                    panel.width - 24f * scale, 38f * scale),
                $"STAGE {lastPerkStageIndex + 1}  /  CHOOSE ONE PERK",
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 12f * scale, panel.y + 53f * scale,
                    panel.width - 24f * scale, 26f * scale),
                "The climb is paused. Choose a permanent upgrade for this run.",
                subtitleStyle);

            RunPerk[] perks =
            {
                RunPerk.QuickRecall,
                RunPerk.ClimbersPace,
                RunPerk.LightFeet
            };
            int[] currentStacks =
            {
                quickRecallStacks,
                climbersPaceStacks,
                lightFeetStacks
            };
            float margin = 20f * scale;
            float gap = 14f * scale;
            float cardY = panel.y + 91f * scale;
            float cardHeight = 183f * scale;
            float cardWidth = (panel.width - margin * 2f - gap * 2f) / 3f;
            GUIStyle cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.9f, 1f, 1f) }
            };
            GUIStyle cardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUIStyle stackStyle = new GUIStyle(subtitleStyle)
            {
                fontSize = Mathf.RoundToInt(12f * scale),
                fontStyle = FontStyle.Bold
            };
            GUIStyle chooseButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold
            };

            for (int i = 0; i < perks.Length; i++)
            {
                Rect card = new Rect(
                    panel.x + margin + i * (cardWidth + gap),
                    cardY,
                    cardWidth,
                    cardHeight);
                DrawFilledRect(card, new Color(0.035f, 0.09f, 0.13f, 1f));
                DrawFilledRect(
                    new Rect(card.x, card.y, card.width, 3f * scale),
                    new Color(0.06f, 0.65f, 0.88f, 1f));
                GUI.Label(
                    new Rect(card.x + 10f * scale, card.y + 11f * scale,
                        card.width - 20f * scale, 28f * scale),
                    GetPerkTitle(perks[i]),
                    cardTitleStyle);
                GUI.Label(
                    new Rect(card.x + 16f * scale, card.y + 46f * scale,
                        card.width - 32f * scale, 69f * scale),
                    GetPerkDescription(perks[i]),
                    cardBodyStyle);
                GUI.Label(
                    new Rect(card.x + 10f * scale, card.y + 117f * scale,
                        card.width - 20f * scale, 20f * scale),
                    $"CURRENT STACKS: {currentStacks[i]}",
                    stackStyle);

                Rect button = new Rect(
                    card.x + 18f * scale,
                    card.y + 144f * scale,
                    card.width - 36f * scale,
                    31f * scale);
                if (GUI.Button(button, $"TAKE  [{i + 1}]", chooseButtonStyle))
                {
                    ChoosePerk(perks[i]);
                }
            }

            GUI.Label(
                new Rect(panel.x + 12f * scale, panel.y + 286f * scale,
                    panel.width - 24f * scale, 22f * scale),
                "Perks stack for this run. Restart with R to begin fresh.",
                subtitleStyle);
        }

        private void TryOpenPerkChoice(int stageIndex)
        {
            TowerChunk groundedChunk = playerController != null
                ? playerController.GroundedChunk
                : null;
            if (perkChoiceOpen || stageIndex <= 0 ||
                stageIndex <= lastPerkStageIndex ||
                groundedChunk == null || !groundedChunk.IsStageTransition)
            {
                return;
            }

            lastPerkStageIndex = stageIndex;
            perkChoiceOpen = true;
            timeScaleBeforePerkChoice = Time.timeScale;
            cursorLockBeforePerkChoice = Cursor.lockState;
            cursorVisibleBeforePerkChoice = Cursor.visible;

            playerController.SetMovementEnabled(false);
            grappleController?.SetGrappleEnabled(false);
            if (crosshair != null)
            {
                crosshair.Visible = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        private void HandlePerkChoiceInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame ||
                keyboard.numpad1Key.wasPressedThisFrame)
            {
                ChoosePerk(RunPerk.QuickRecall);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame ||
                keyboard.numpad2Key.wasPressedThisFrame)
            {
                ChoosePerk(RunPerk.ClimbersPace);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame ||
                keyboard.numpad3Key.wasPressedThisFrame)
            {
                ChoosePerk(RunPerk.LightFeet);
            }
        }

        private void ChoosePerk(RunPerk perk)
        {
            switch (perk)
            {
                case RunPerk.QuickRecall:
                    quickRecallStacks++;
                    grappleController?.SetQuickRecallStacks(quickRecallStacks);
                    break;
                case RunPerk.ClimbersPace:
                    climbersPaceStacks++;
                    playerController?.ApplyMovementPerkStacks(
                        climbersPaceStacks,
                        lightFeetStacks);
                    break;
                case RunPerk.LightFeet:
                    lightFeetStacks++;
                    playerController?.ApplyMovementPerkStacks(
                        climbersPaceStacks,
                        lightFeetStacks);
                    break;
            }

            ClosePerkChoice();
        }

        private void ClosePerkChoice()
        {
            if (!perkChoiceOpen)
            {
                return;
            }

            perkChoiceOpen = false;
            Time.timeScale = timeScaleBeforePerkChoice;
            Cursor.lockState = cursorLockBeforePerkChoice;
            Cursor.visible = cursorVisibleBeforePerkChoice;

            if (!runEnded)
            {
                playerController?.SetMovementEnabled(true);
                grappleController?.SetGrappleEnabled(true);
                if (crosshair != null)
                {
                    crosshair.Visible = true;
                }
            }
        }

        private static string GetPerkTitle(RunPerk perk)
        {
            return perk switch
            {
                RunPerk.QuickRecall => "QUICK RECALL",
                RunPerk.ClimbersPace => "CLIMBER'S PACE",
                RunPerk.LightFeet => "LIGHT FEET",
                _ => "UNKNOWN PERK"
            };
        }

        private static string GetPerkDescription(RunPerk perk)
        {
            return perk switch
            {
                RunPerk.QuickRecall => "Missed-hook return speed +35% per pick.",
                RunPerk.ClimbersPace => "Ground walk and run speed +10% per pick.",
                RunPerk.LightFeet => "Jump height +10% per pick. Air steering is unchanged.",
                _ => string.Empty
            };
        }

        private void DrawRunIntro(float scale)
        {
            float elapsed = Time.unscaledTime - runStartedAt;
            if (elapsed >= activeRunIntroDuration)
            {
                return;
            }

            float fadeStart = Mathf.Max(
                0f,
                activeRunIntroDuration - runIntroFadeDuration);
            float alpha = elapsed <= fadeStart
                ? 1f
                : 1f - Mathf.InverseLerp(
                    fadeStart,
                    activeRunIntroDuration,
                    elapsed);
            float panelWidth = Mathf.Min(510f * scale, Screen.width - 32f * scale);
            float panelHeight = 142f * scale;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                Screen.height * 0.28f,
                panelWidth,
                panelHeight);
            DrawFilledRect(panel, new Color(0.01f, 0.025f, 0.05f, 0.86f * alpha));
            DrawFilledRect(
                new Rect(panel.x, panel.y, panel.width, 4f * scale),
                new Color(0.06f, 0.82f, 1f, alpha));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(27f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.9f, 1f, alpha) }
            };
            GUIStyle controlsStyle = new GUIStyle(messageStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.94f, 1f, alpha) }
            };

            GUI.Label(
                new Rect(panel.x, panel.y + 10f * scale, panel.width, 38f * scale),
                "CLIMB. ESCAPE THE FLOOD.",
                titleStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 49f * scale, panel.width, 25f * scale),
                "Reach higher platforms before the water catches you.",
                messageStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 82f * scale, panel.width, 27f * scale),
                "WASD  MOVE   |   SPACE  JUMP",
                controlsStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 108f * scale, panel.width, 27f * scale),
                "MOUSE  AIM   |   LMB  GRAPPLE",
                controlsStyle);
        }

        private void DrawRunEndPanel(float scale)
        {
            float panelWidth = Mathf.Min(500f * scale, Screen.width - 36f * scale);
            float panelHeight = 244f * scale;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            Color accentColor = new Color(1f, 0.25f, 0.08f, 1f);
            DrawFilledRect(panel, new Color(0.01f, 0.025f, 0.05f, 0.94f));
            DrawFilledRect(
                new Rect(panel.x, panel.y, panel.width, 5f * scale),
                accentColor);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(32f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = accentColor }
            };
            GUIStyle resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(19f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUIStyle restartStyle = new GUIStyle(resultStyle)
            {
                fontSize = Mathf.RoundToInt(21f * scale),
                normal = { textColor = new Color(0.45f, 0.86f, 1f, 1f) }
            };

            GUI.Label(new Rect(panel.x, panel.y + 26f * scale, panel.width, 52f * scale),
                "THE FLOOD CAUGHT YOU", titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 94f * scale, panel.width, 32f * scale),
                $"RUN HEIGHT  {runHeight:0.0} m", resultStyle);
            GUI.Label(new Rect(panel.x, panel.y + 128f * scale, panel.width, 32f * scale),
                $"SESSION BEST  {bestHeight:0.0} m", resultStyle);
            GUI.Label(new Rect(panel.x, panel.y + 184f * scale, panel.width, 36f * scale),
                "R  START A NEW TOWER", restartStyle);
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
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
            initialRunIntroDuration = Mathf.Max(0.5f, initialRunIntroDuration);
            runIntroDuration = Mathf.Max(0.5f, runIntroDuration);
            runIntroFadeDuration = Mathf.Clamp(
                runIntroFadeDuration,
                0.1f,
                runIntroDuration);
        }
    }
}
