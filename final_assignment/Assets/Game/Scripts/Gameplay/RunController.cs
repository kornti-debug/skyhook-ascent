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
        private string resultTitle;
        private bool hasStartedRun;
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
            DrawRunHud(scale);

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
                158f * scale);
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
            Color accentColor = resultTitle == "TOWER CLEARED"
                ? new Color(0.3f, 1f, 0.65f, 1f)
                : new Color(1f, 0.25f, 0.08f, 1f);
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
                resultTitle, titleStyle);
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
