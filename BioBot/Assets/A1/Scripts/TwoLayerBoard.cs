using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    public sealed class TwoLayerBoard : MonoBehaviour
    {
        public const int Rows = 5;
        public const int Columns = 9;
        private const float TransitionSeconds = .35f;
        private const float TargetAspect = 16f / 9f;
        private const float LogicalWidth = 768f;
        private const float LogicalHeight = 432f;

        [SerializeField] private Camera boardCamera;
        [SerializeField] private BioBotMovement robot;
        [SerializeField] private Transform underwaterOrigin;
        [SerializeField] private Rigidbody2D bottle;

        private Rigidbody2D robotBody;
        private bool underwater;
        private bool targetUnderwater;
        private float transitionElapsed;
        private bool transitioning;
        private Color accent;
        private GUIStyle header;
        private GUIStyle label;
        private GUIStyle hint;

        public bool IsUnderwater => underwater;
        public bool IsTransitioning => transitioning;
        public Rect PixelRect => boardCamera.pixelRect;
        public bool CampaignHud { get; set; }
        public Vector2 LocalRobotPosition => robotBody.position - (underwater ? (Vector2)underwaterOrigin.position : Vector2.zero);
        public Vector2Int RobotCell => CellAt(LocalRobotPosition);
        public float RobotColumn => (LocalRobotPosition.x + 163f / 24f) / (30f / 24f);
        public float TransitionOpacity => transitioning
            ? Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(2f * Mathf.Clamp01(transitionElapsed / TransitionSeconds) - 1f))
            : 0f;

        public void ReturnToDock()
        {
            Vector2 dock = new Vector2(-163f / 24f, -.02f);
            robotBody.position = underwater ? dock + (Vector2)underwaterOrigin.position : dock;
            robotBody.linearVelocity = Vector2.zero;
        }

        public void ResetToSurface()
        {
            underwater = targetUnderwater = transitioning = false;
            Vector3 position = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(0, 0, position.z);
            robotBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            robot.Suspend(false);
            ReturnToDock();
        }

        private void Awake()
        {
            robotBody = robot.GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!transitioning && keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                    BeginTransition(true);
                else if (keyboard.eKey.wasPressedThisFrame)
                    BeginTransition(false);
            }

            if (!transitioning)
                return;

            transitionElapsed += Time.unscaledDeltaTime;
            if (transitionElapsed >= TransitionSeconds * .5f && underwater != targetUnderwater)
            {
                Vector2 offset = underwaterOrigin.position;
                robotBody.interpolation = RigidbodyInterpolation2D.None;
                robotBody.position += targetUnderwater ? offset : -offset;
                robot.transform.position = robotBody.position;
                Vector3 cameraPosition = boardCamera.transform.position;
                cameraPosition.x = targetUnderwater ? offset.x : 0f;
                cameraPosition.y = targetUnderwater ? offset.y : 0f;
                boardCamera.transform.position = cameraPosition;
                underwater = targetUnderwater;
            }

            if (transitionElapsed >= TransitionSeconds)
            {
                robotBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                robot.Suspend(false);
                transitioning = false;
            }
        }

        private void LateUpdate()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            float aspect = (float)Screen.width / Screen.height;
            if (aspect > TargetAspect)
            {
                float width = TargetAspect / aspect;
                boardCamera.rect = new Rect((1f - width) * .5f, 0f, width, 1f);
            }
            else
            {
                float height = aspect / TargetAspect;
                boardCamera.rect = new Rect(0f, (1f - height) * .5f, 1f, height);
            }
        }

        private void BeginTransition(bool dive)
        {
            if (transitioning || dive == underwater)
                return;

            targetUnderwater = dive;
            transitionElapsed = 0f;
            transitioning = true;
            robot.Suspend(true);
        }

        private static Vector2Int CellAt(Vector2 localPosition)
        {
            int column = Mathf.FloorToInt((localPosition.x + 178f / 24f) / (30f / 24f));
            int row = Mathf.FloorToInt((67f / 24f - localPosition.y) / (27f / 24f));
            return new Vector2Int(Mathf.Clamp(column, 0, Columns - 1), Mathf.Clamp(row, 0, Rows - 1));
        }

        private void OnGUI()
        {
            if (CampaignHud)
                return;
            header ??= MakeStyle(22);
            label ??= MakeStyle(14);
            hint ??= MakeStyle(12);
            accent = underwater ? new Color32(142, 207, 249, 255) : new Color32(255, 202, 131, 255);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            Rect cameraPixels = boardCamera.pixelRect;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(cameraPixels.x, Screen.height - cameraPixels.yMax),
                Quaternion.identity,
                new Vector3(cameraPixels.width / LogicalWidth, cameraPixels.height / LogicalHeight, 1f));

            GUI.color = accent;
            GUI.Label(new Rect(24, 28, 540, 28), underwater ? "DƯỚI NƯỚC" : "MẶT NƯỚC", header);
            GUI.color = Color.white;
            Vector2Int cell = RobotCell;
            Outline(new Rect(30 + cell.x * 60, 84 + cell.y * 54, 56, 50),
                new Color(accent.r, accent.g, accent.b, .80f));
            GUI.color = Color.white;
            GUI.Label(new Rect(300, 37, 277, 20), $"HÀNG {cell.y + 1}   ·   CỘT {cell.x + 1}", label);
            GUI.Label(new Rect(606, 86, 137, 20), underwater ? "MẶT NƯỚC" : "DƯỚI NƯỚC", label);
            DrawMinimap(cell);
            if (GUI.Button(new Rect(605, 230, 134, 33), underwater ? "E   Nổi lên" : "Q   Lặn xuống"))
                BeginTransition(!underwater);
            GUI.Label(new Rect(24, 403, 700, 18), "WASD / ↑ ↓ ← →   Di chuyển     ·     Q   Lặn     ·     E   Nổi", hint);

            if (transitioning)
            {
                float progress = Mathf.Clamp01(transitionElapsed / TransitionSeconds);
                float opacity = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(2f * progress - 1f));
                Paint(new Rect(0, 0, LogicalWidth, LogicalHeight), new Color(.02f, .09f, .15f, opacity));
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawMinimap(Vector2Int cell)
        {
            Color inactive = underwater ? new Color32(52, 103, 104, 255) : new Color32(48, 83, 115, 255);
            const float tileWidth = 14f;
            const float tileHeight = 14f;
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var tile = new Rect(606 + column * tileWidth, 124 + row * tileHeight, tileWidth - 2, tileHeight - 2);
                    Paint(tile, inactive);
                    if (cell.x == column && cell.y == row)
                        Outline(tile, accent);
                }
            }

            if (underwater)
            {
                Vector2Int bottleCell = CellAt(bottle.position);
                var marker = new Rect(609 + bottleCell.x * tileWidth, 127 + bottleCell.y * tileHeight, 6, 6);
                Paint(marker, new Color32(243, 163, 88, 255));
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(606, 199, 145, 19), "Viền: ô tương ứng", hint);
        }

        private static GUIStyle MakeStyle(int size)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = size, clipping = TextClipping.Overflow };
        }

        private static void Outline(Rect rect, Color color)
        {
            Paint(new Rect(rect.x, rect.y, rect.width, 2), color);
            Paint(new Rect(rect.x, rect.yMax - 2, rect.width, 2), color);
            Paint(new Rect(rect.x, rect.y, 2, rect.height), color);
            Paint(new Rect(rect.xMax - 2, rect.y, 2, rect.height), color);
        }

        private static void Paint(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
