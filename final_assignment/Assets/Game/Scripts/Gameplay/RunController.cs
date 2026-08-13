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
        [SerializeField, Min(0f)] private float finishDistance = 3f;
        [SerializeField] private int displayedSeed = 104729;

        private InputAction restartAction;
        private PlayerController playerController;
        private GrappleController grappleController;
        private CrosshairPresenter crosshair;
        private Vector3 playerStartPosition;
        private Quaternion playerStartRotation;
        private float bestHeight;
        private float runHeight;
        private string resultTitle;
        private bool runEnded;

        public bool RunEnded => runEnded;
        public float CurrentHeight => runHeight;
        public float BestHeight => bestHeight;

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
                StartRun();
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
            hazard.ReportPlayerHeight(runHeight);

            if (player.position.y <= hazard.SurfaceHeight + hazardContactMargin)
            {
                EndRun("THE FLOOD CAUGHT YOU");
                return;
            }

            if (finish != null &&
                Vector3.Distance(player.position, finish.position) <= finishDistance)
            {
                EndRun("TOWER CLEARED");
            }
        }

        public void StartRun()
        {
            runEnded = false;
            resultTitle = string.Empty;
            runHeight = 0f;

            if (player != null)
            {
                player.SetPositionAndRotation(
                    playerStartPosition,
                    playerStartRotation);

                Rigidbody playerBody = player.GetComponent<Rigidbody>();
                if (playerBody != null)
                {
                    playerBody.linearVelocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                    playerBody.useGravity = true;
                }
            }

            playerController?.SetZipMovementActive(false);
            playerController?.SetMovementEnabled(true);
            grappleController?.SetGrappleEnabled(true);

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

        private void EndRun(string title)
        {
            runEnded = true;
            resultTitle = title;
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
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Box(new Rect(20f, 20f, 280f * scale, 112f * scale), string.Empty);
            GUI.Label(new Rect(36f, 30f, 250f * scale, 30f * scale),
                $"HEIGHT  {runHeight:0.0} m", labelStyle);
            GUI.Label(new Rect(36f, 62f, 250f * scale, 30f * scale),
                $"BEST      {bestHeight:0.0} m", labelStyle);
            GUI.Label(new Rect(36f, 94f, 250f * scale, 30f * scale),
                $"SEED      {displayedSeed}", labelStyle);

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
                "Press R to restart", centerStyle);
        }

        private void OnValidate()
        {
            hazardContactMargin = Mathf.Max(0f, hazardContactMargin);
            finishDistance = Mathf.Max(0f, finishDistance);
        }
    }
}
