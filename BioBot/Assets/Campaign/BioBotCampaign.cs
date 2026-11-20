using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    [RequireComponent(typeof(TwoLayerBoard))]
    public sealed class BioBotCampaign : MonoBehaviour
    {
        private const string SaveKey = "BioBot.Campaign.Unlocked";
        private const int PageSize = 4;
        private static readonly Color Ink = new Color32(13, 26, 39, 255);
        private static readonly Color Panel = new Color32(20, 40, 55, 255);
        private static readonly Color Muted = new Color32(159, 191, 196, 255);
        private static readonly Color Teal = new Color32(141, 229, 201, 255);
        private static readonly Color Sand = new Color32(255, 198, 119, 255);
        private static readonly Color Danger = new Color32(255, 139, 126, 255);

        [SerializeField] private BioBotMovement robot;
        [SerializeField] private GameObject demoBottle;

        private TwoLayerBoard board;
        private Texture2D atlas;
        private CampaignMatch match;
        private int unlocked;
        private int selectedLevel = 1;
        private int page, selection;
        private int fieldGuide;
        private bool inspecting;
        private float lastMove;
        private float fieldGuideScroll;
        private Vector2Int previous;
        private Layer previousLayer;
        private bool wasRespawning;
        private bool recorded;
        private GUIStyle small, text, heading, title, button, wrap;

        public CampaignMatch CurrentMatch => match;

        private void Awake()
        {
            board = GetComponent<TwoLayerBoard>();
            board.CampaignHud = true;
            demoBottle.SetActive(false);
            atlas = Resources.Load<Texture2D>("BioBot/Roster");
            unlocked = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 1), 1, CampaignCatalog.Levels.Length);
            selectedLevel = unlocked;
        }

        private void Start() => robot.TerrainSpeed = 0f;

        public void StartLevel(int level)
        {
            if (level < 1 || level > unlocked) return;
            selectedLevel = level;
            match = new CampaignMatch(level);
            page = selection = 0;
            recorded = wasRespawning = false;
            inspecting = false;
            board.ResetToSurface();
            robot.TerrainSpeed = 1f;
            previous = board.RobotCell;
            previousLayer = Layer.Surface;
            lastMove = -.4f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (match == null)
            {
                if (!inspecting && keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartLevel(selectedLevel);
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ReturnToMenu();
                return;
            }
            if (match.Ended)
            {
                robot.TerrainSpeed = 0;
                UpdateProgress();
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                    StartLevel(match.Won && selectedLevel < CampaignCatalog.Levels.Length ? selectedLevel + 1 : selectedLevel);
                return;
            }
            if (inspecting)
            {
                robot.TerrainSpeed = 0f;
                return;
            }

            Layer layer = board.IsUnderwater ? Layer.Deep : Layer.Surface;
            Vector2Int cell = board.RobotCell;
            match.Tick(Mathf.Min(Time.deltaTime, .05f), layer, cell.y, board.RobotColumn);
            if (match.Ended)
            {
                robot.TerrainSpeed = 0f;
                UpdateProgress();
                return;
            }

            bool respawning = match.Respawn > 0;
            if (wasRespawning && !respawning)
                board.ReturnToDock();
            wasRespawning = respawning;
            var hazard = match.Trash[(int)layer, cell.y, cell.x];
            var type = hazard == null ? (TrashEffect?)null : CampaignCatalog.Trash[hazard.Spec].Effect;
            robot.TerrainSpeed = respawning ? 0f :
                type is TrashEffect.Filament or TrashEffect.Spill ? .58f : 1f;
            if (respawning || board.IsTransitioning)
                return;

            if (previousLayer == layer && previous.y == cell.y && previous.x != cell.x && match.Time - lastMove > .18f)
            {
                int direction = cell.x - previous.x;
                if (Mathf.Abs(direction) == 1 && match.PushFloat(layer, cell.y, cell.x, direction))
                    lastMove = match.Time;
            }
            previous = cell;
            previousLayer = layer;

            if (keyboard == null) return;
            if (keyboard.zKey.wasPressedThisFrame) ChangePage(-1);
            if (keyboard.xKey.wasPressedThisFrame) ChangePage(1);
            if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
            if (keyboard.digit4Key.wasPressedThisFrame) Select(3);
            if (keyboard.spaceKey.wasPressedThisFrame) match.Plant(selection, layer, cell.y, cell.x);
            if (keyboard.fKey.wasPressedThisFrame) match.Collect(layer, cell.y, cell.x);
            if (keyboard.gKey.wasPressedThisFrame) match.UseTrash(layer, cell.y, cell.x);
            if (keyboard.rKey.wasPressedThisFrame) match.RobotPulse(layer, cell.y, board.RobotColumn);
        }

        private void UpdateProgress()
        {
            if (recorded || !match.Won) return;
            recorded = true;
            unlocked = Mathf.Max(unlocked, Mathf.Min(selectedLevel + 1, CampaignCatalog.Levels.Length));
            PlayerPrefs.SetInt(SaveKey, unlocked);
            PlayerPrefs.Save();
        }

        private void ReturnToMenu()
        {
            match = null;
            inspecting = false;
            board.ResetToSurface();
            robot.TerrainSpeed = 0;
        }

        private void ChangePage(int direction)
        {
            page = (page + direction + selectedLevel) % selectedLevel;
            selection = page * PageSize;
        }

        private void Select(int slot) => selection = page * PageSize + slot;

        private void OnGUI()
        {
            EnsureStyles();
            Rect pixelRect = board.PixelRect;
            if (pixelRect.width < 1 || pixelRect.height < 1) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(pixelRect.x, Screen.height - pixelRect.yMax),
                Quaternion.identity, new Vector3(pixelRect.width / 768f, pixelRect.height / 432f, 1));
            if (match == null) DrawMenu();
            else
            {
                DrawMatch(board.IsUnderwater ? Layer.Deep : Layer.Surface);
                if (board.TransitionOpacity > 0)
                    Fill(new Rect(0, 0, 768, 432), new Color(.025f, .075f, .115f, board.TransitionOpacity));
                if (match.Ended) DrawResult();
            }
            if (inspecting) DrawGuide();
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (text != null) return;
            small = Style(10);
            text = Style(13);
            heading = Style(17, true);
            title = Style(33, true);
            wrap = Style(12);
            wrap.wordWrap = true;
            button = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        }

        private static GUIStyle Style(int size, bool bold = false) =>
            new GUIStyle(GUI.skin.label)
            {
                fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                padding = new RectOffset(0, 0, 0, 0)
            };

        private void DrawMenu()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(.04f, .09f, .14f, .96f));
            Fill(new Rect(31, 30, 706, 3), Teal);
            Label(32, 41, 420, 23, "CHIẾN DỊCH BẢO VỆ ĐÊ", heading, Teal);
            Label(31, 64, 355, 45, "BIO-BOT", title, Color.white);
            Label(33, 111, 304, 48, "Dọn rác, cứu cá và giữ hai tầng nước qua các đợt tấn công.", wrap, Muted);

            for (int i = 0; i < CampaignCatalog.Levels.Length; i++)
            {
                int level = i + 1;
                Rect card = new Rect(31, 162 + i * 37, 292, 30);
                bool chosen = selectedLevel == level;
                Fill(card, chosen ? new Color32(47, 91, 92, 255) : Panel);
                if (chosen) Border(card, Teal);
                GUI.enabled = level <= unlocked;
                if (Click(card, $"{level:00}   {CampaignCatalog.Levels[i].Name}{(level > unlocked ? "   ·   khóa" : "")}"))
                    selectedLevel = level;
                GUI.enabled = true;
            }

            LevelSpec levelSpec = CampaignCatalog.Levels[selectedLevel - 1];
            Fill(new Rect(351, 68, 386, 274), Panel);
            Label(372, 85, 342, 25, levelSpec.Name, heading, Sand);
            Label(372, 111, 342, 63, levelSpec.Lesson, wrap, Muted);
            Label(372, 181, 345, 20, "ĐỒNG MINH MỚI", text, Teal);
            for (int slot = 0; slot < PageSize; slot++)
            {
                int id = (selectedLevel - 1) * PageSize + slot;
                float x = 371 + slot * 82;
                Sprite(id, new Rect(x + 11, 207, 48, 48), 4, slot, Color.white);
                Label(x, 260, 80, 20, CampaignCatalog.Allies[id].Name, small, Color.white);
                Label(x, 277, 80, 18, HabitatLabel(CampaignCatalog.Allies[id].Habitat), small, Muted);
            }

            if (Click(new Rect(351, 355, 250, 39), $"Enter   Chơi màn {selectedLevel:00}")) StartLevel(selectedLevel);
            if (Click(new Rect(609, 355, 128, 39), "Sổ sinh thái")) inspecting = true;
        }

        private void DrawMatch(Layer layer)
        {
            Fill(new Rect(16, 10, 736, 66), Ink);
            Label(26, 15, 268, 25, $"{selectedLevel:00}  {match.Rules.Name}", heading, Color.white);
            Label(26, 42, 226, 20, LayerName(layer), text, layer == Layer.Deep ? Teal : Sand);
            Label(251, 19, 225, 21, $"NĂNG LƯỢNG  {match.Energy}", heading, Sand);
            Label(251, 46, 230, 20, $"ĐỢT  {match.Wave}/{match.Rules.Waves}", text, Muted);
            Label(500, 20, 245, 22, $"CỬA ĐÊ  {match.Integrity}/{CampaignMatch.MaxIntegrity}", heading, Teal);
            Label(500, 47, 245, 19, $"BIO-BOT  {match.RobotHealth}/{CampaignMatch.MaxRobot}", text,
                match.RobotHealth <= 1 ? Danger : Muted);

            Vector2Int cell = board.RobotCell;
            Border(new Rect(30 + cell.x * 60, 84 + cell.y * 54, 56, 50), Sand);
            DrawCreatures(layer);
            DrawSide(layer, cell);
            DrawCards(layer);

            if (match.Respawn > 0)
            {
                Fill(new Rect(176, 173, 239, 53), Ink);
                Label(191, 187, 217, 25, $"Bio-Bot hồi sau {Mathf.CeilToInt(match.Respawn)} giây", heading, Danger);
            }
        }

        private void DrawCreatures(Layer layer)
        {
            int side = (int)layer;
            for (int row = 0; row < CampaignMatch.Rows; row++)
            {
                for (int col = 0; col < CampaignMatch.Columns; col++)
                {
                    Vector2 p = CellPixel(col, row);
                    var waste = match.Trash[side,row,col];
                    var ally = match.Allies[side,row,col];
                    if (waste != null)
                    {
                        if (waste.Active) Border(new Rect(p.x - 20,p.y - 18,40,36), Teal);
                        Sprite(32 + waste.Spec, Around(p, 37), 4, col + row, Color.white);
                    }
                    if (ally != null)
                    {
                        var spec = CampaignCatalog.Allies[ally.Spec];
                        Sprite(ally.Spec, Around(p, 41), ally.Cooldown < .18f ? 10 : 5, col, Color.white);
                        Bar(new Rect(p.x - 11,p.y + 18,22,3), ally.Health / (float)spec.Health, Teal);
                    }
                }
            }
            foreach (var effect in match.Effects)
            {
                if (effect.Layer != layer) continue;
                Vector2 a = CellPixel(effect.From, effect.Row), b = CellPixel(effect.To, effect.Row);
                var color = effect.Pulse ? Teal : Sand;
                color.a = Mathf.Clamp01(effect.Left * 7);
                float width = Mathf.Max(4, Mathf.Abs(a.x - b.x));
                Fill(new Rect(Mathf.Min(a.x, b.x), a.y - 2, width, effect.Pulse ? 4 : 2), color);
            }
            foreach (var enemy in match.Enemies)
            {
                if (enemy.Layer != layer) continue;
                Vector2 p = CellPixel(Mathf.Clamp(enemy.X, -.25f, 8.6f), enemy.Row);
                Color tint = enemy.Flash > 0 ? new Color32(255, 181, 163, 255) : Color.white;
                Sprite(20 + enemy.Spec, Around(p, enemy.Small ? 31 : 45), enemy.Slow > 0 ? 5 : 10, enemy.Row, tint);
                Bar(new Rect(p.x - 12,p.y - 23,24,3), enemy.Health / (float)enemy.MaxHealth, Danger);
            }
        }

        private void DrawSide(Layer layer, Vector2Int cell)
        {
            Layer other = CampaignCatalog.Opposite(layer);
            Fill(new Rect(591, 80, 161, 272), Ink);
            Label(604, 92, 143, 20, LayerName(other), text, Teal);
            for (int row = 0; row < CampaignMatch.Rows; row++)
                for (int col = 0; col < CampaignMatch.Columns; col++)
                {
                    Rect tile = new Rect(605 + col * 14, 119 + row * 13, 12, 11);
                    Color color = new Color32(55, 92, 112, 255);
                    if (match.Trash[(int)other,row,col] != null) color = Sand;
                    if (match.Allies[(int)other,row,col] != null) color = Teal;
                    Fill(tile, color);
                    if (row == cell.y && col == cell.x) Border(tile, Color.white);
                }
            foreach (var enemy in match.Enemies)
                if (enemy.Layer == other)
                    Fill(new Rect(606 + Mathf.Clamp(Mathf.RoundToInt(enemy.X),0,8)*14,
                        120 + enemy.Row*13,8,7), Danger);

            if (match.NextRow >= 0)
            {
                string warning = $"Sau {Mathf.CeilToInt(match.NextAttack)}s · hàng {match.NextRow+1}\n{LayerName(match.NextLayer)}";
                Label(605, 191, 139, 40, warning, wrap, match.NextAttack < 4 ? Danger : Muted);
            }
            else Label(605, 191, 139, 25, "Đợt cuối đang chạy", text, Muted);

            var debris = match.Trash[(int)layer,cell.y,cell.x];
            if (debris != null)
            {
                var spec = CampaignCatalog.Trash[debris.Spec];
                Label(605, 231, 140, 20, spec.Name, text, Sand);
                Label(605, 254, 139, 63, spec.Hint, wrap, Muted);
            }
            else
            {
                Label(605, 233, 139, 19, $"Ô {cell.y+1} / {cell.x+1}", text, Muted);
                Label(605, 254, 139, 58, "Q / E  đổi tầng\nF  thu rác · G  dùng rác\nR  xung Bio-Bot", wrap, Muted);
            }
            if (Click(new Rect(605, 321, 134, 22), "Sổ sinh thái")) inspecting = true;
        }

        private void DrawCards(Layer layer)
        {
            Fill(new Rect(15, 354, 737, 71), Ink);
            if (Click(new Rect(22, 361, 36, 31), "Z")) ChangePage(-1);
            for (int slot = 0; slot < PageSize; slot++)
            {
                int id = page * PageSize + slot;
                var ally = CampaignCatalog.Allies[id];
                Rect card = new Rect(64 + slot*144, 359, 139, 36);
                Fill(card, selection == id ? new Color32(52, 92, 94, 255) : Panel);
                if (selection == id) Border(card, Sand);
                Sprite(id, new Rect(card.x + 3, card.y + 4, 25, 25), 5, id, Color.white);
                Label(card.x+30, card.y+4, 104, 15, $"{slot+1}  {ally.Name}", small,
                    CampaignCatalog.Fits(ally.Habitat,layer) ? Color.white : Muted);
                Label(card.x+30, card.y+19, 104, 14, $"{ally.Cost} NL  ·  {HabitatLabel(ally.Habitat)}", small, Sand);
                if (Click(card, "" , GUIStyle.none)) Select(slot);
            }
            if (Click(new Rect(643, 361, 36, 31), "X")) ChangePage(1);
            Label(688, 370, 53, 14, $"{page+1}/{selectedLevel}", small, Muted);
            string message = match.Feedback > 0 ? match.Message :
                $"Space  đặt  ·  {CampaignCatalog.Allies[selection].Hint}";
            Label(25, 402, 719, 16, message, small, match.Feedback > 0 ? Teal : Muted);
        }

        private void DrawResult()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(.03f,.08f,.11f,.8f));
            var rect = new Rect(193, 107, 382, 222);
            Fill(rect, Panel);
            Border(rect, match.Won ? Teal : Danger);
            Label(216, 129, 331, 36, match.Won ? "ĐÃ GIỮ ĐƯỢC ĐÊ" : "CỬA ĐÊ BỊ VỠ", heading, match.Won ? Teal : Danger);
            bool next = match.Won && selectedLevel < CampaignCatalog.Levels.Length;
            string description = next ? $"Mở màn {selectedLevel+1}: {CampaignCatalog.Levels[selectedLevel].Name}" :
                match.Won ? "Đã hoàn thành chiến dịch 5 màn." : "Dọn rác và bố trí phòng thủ ở các hàng được báo trước.";
            Label(216, 182, 335, 45, description, wrap, Color.white);
            if (Click(new Rect(216, 245, 336, 33), next ? "Enter   Màn tiếp theo" : "Enter   Chơi lại"))
                StartLevel(next ? selectedLevel + 1 : selectedLevel);
            if (Click(new Rect(216, 285, 336, 25), "Về danh sách màn")) ReturnToMenu();
        }

        private void DrawGuide()
        {
            Fill(new Rect(0,0,768,432), new Color(.02f,.06f,.09f,.93f));
            Fill(new Rect(29,24,710,383), Panel);
            Label(48, 40, 340, 24, "SỔ SINH THÁI", heading, Teal);
            if (Click(new Rect(633, 36, 85, 27), "Đóng")) inspecting = false;
            string[] tabs = { "20 Cá / Cây", "12 Đối thủ", "10 Loại rác" };
            for (int i = 0; i < tabs.Length; i++)
                if (Click(new Rect(47+i*135,75,122,24), tabs[i]))
                {
                    fieldGuide = i;
                    fieldGuideScroll = 0;
                }
            int count = fieldGuide == 0 ? CampaignCatalog.Allies.Length :
                fieldGuide == 1 ? CampaignCatalog.Enemies.Length : CampaignCatalog.Trash.Length;
            Vector2 scroll = GUI.BeginScrollView(new Rect(47,110,675,272),
                new Vector2(0, fieldGuideScroll), new Rect(0,0,652,count*46));
            fieldGuideScroll = scroll.y;
            for (int i = 0; i < count; i++) DrawGuideEntry(i);
            GUI.EndScrollView();
        }

        private void DrawGuideEntry(int index)
        {
            string name, hint;
            Habitat habitat;
            int unlock;
            switch (fieldGuide)
            {
                case 0:
                    var ally = CampaignCatalog.Allies[index];
                    name = ally.Name; hint = ally.Hint; habitat = ally.Habitat; unlock = ally.Unlock;
                    break;
                case 1:
                    var enemy = CampaignCatalog.Enemies[index];
                    name = enemy.Name; hint = enemy.Hint; habitat = enemy.Habitat; unlock = enemy.Unlock;
                    break;
                default:
                    var trash = CampaignCatalog.Trash[index];
                    name = trash.Name; hint = trash.Hint; habitat = trash.Habitat; unlock = trash.Unlock;
                    break;
            }
            int art = index + (fieldGuide == 0 ? 0 : fieldGuide == 1 ? 20 : 32);
            int y = index * 46;
            Fill(new Rect(0,y,644,43), Ink);
            Sprite(art,new Rect(3,y+2,38,38),5,index,Color.white);
            Label(50,y+4,169,20,name,text,Color.white);
            Label(50,y+23,173,15,$"{HabitatLabel(habitat)}  ·  Màn {unlock}",small,Sand);
            Label(228,y+5,408,33,hint,wrap,Muted);
        }

        private static Vector2 CellPixel(float column, int row) => new Vector2(58 + column*60, 109 + row*54);
        private static Rect Around(Vector2 center, float size) => new Rect(center.x-size/2,center.y-size/2,size,size);
        private static string LayerName(Layer layer) => layer == Layer.Deep ? "DƯỚI NƯỚC" : "MẶT NƯỚC";
        private static string HabitatLabel(Habitat habitat) => habitat == Habitat.Both ? "Cả hai" :
            habitat == Habitat.Deep ? "Dưới nước" : "Mặt nước";

        private void Sprite(int index, Rect rect, float fps, int offset, Color tint)
        {
            if (atlas == null) return;
            int frame = (Mathf.FloorToInt(Time.unscaledTime * fps) + offset) % 4;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(frame/4f, 1f-(index+1)/42f, .25f, 1f/42f));
        }

        private void Label(float x, float y, float width, float height, string value, GUIStyle style, Color color)
        {
            GUI.color = color;
            GUI.Label(new Rect(x, y, width, height), value, style);
        }

        private bool Click(Rect rect, string value, GUIStyle style = null)
        {
            GUI.color = Color.white;
            return GUI.Button(rect, value, style ?? button);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void Bar(Rect rect, float amount, Color color)
        {
            Fill(rect, Ink);
            rect.width *= Mathf.Clamp01(amount);
            Fill(rect, color);
        }

        private static void Border(Rect rect, Color color)
        {
            Fill(new Rect(rect.x,rect.y,rect.width,1),color);
            Fill(new Rect(rect.x,rect.yMax-1,rect.width,1),color);
            Fill(new Rect(rect.x,rect.y,1,rect.height),color);
            Fill(new Rect(rect.xMax-1,rect.y,1,rect.height),color);
        }
    }
}
