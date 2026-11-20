using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    [RequireComponent(typeof(TwoLayerBoard))]
    public sealed class BioBotCampaign : MonoBehaviour
    {
        private enum Overlay { None, Briefing, Guide, Shop, Achievements, Pause }

        private const int PageSize = 6;
        private static readonly Color Ink = new Color32(12, 26, 38, 240);
        private static readonly Color Panel = new Color32(19, 43, 60, 245);
        private static readonly Color CardBg = new Color32(28, 60, 80, 255);
        private static readonly Color CardSel = new Color32(46, 108, 106, 255);
        private static readonly Color Muted = new Color32(168, 204, 212, 255);
        private static readonly Color Teal = new Color32(114, 238, 204, 255);
        private static readonly Color Cyan = new Color32(118, 214, 255, 255);
        private static readonly Color Gold = new Color32(255, 212, 92, 255);
        private static readonly Color Danger = new Color32(255, 118, 108, 255);
        private static readonly Color Green = new Color32(112, 232, 134, 255);

        [SerializeField] private BioBotMovement robot;
        [SerializeField] private GameObject demoBottle;

        private TwoLayerBoard board;
        private CampaignProgress progress;
        private CampaignAudio audioSys;
        private CampaignVfx vfx;
        private CampaignMatch match;

        private Texture2D atlas;
        private Texture2D menuBackdrop;
        private Texture2D circleTex;

        private int selectedLevel = 1;
        private bool pendingEndless;
        private int page, selection;
        private bool shovelMode;
        private bool autoPilot;
        private bool fastForward;
        private bool glassWaterPeek;
        private Overlay overlay = Overlay.None;
        private int guideTab;
        private float guideScroll;
        private float lastMove;
        private float wakeTimer;
        private int lastFacingX = 1;
        private Vector2Int previousCell;
        private Layer previousLayer;
        private bool wasUnderwater;
        private bool wasRespawning;
        private bool recorded;
        private float baseOrthoSize = 4.5f;

        private GUIStyle small, smallCenter, text, heading, title, button, wrap;

        public CampaignMatch CurrentMatch => match;

        private void Awake()
        {
            board = GetComponent<TwoLayerBoard>();
            board.CampaignHud = true;
            if (demoBottle != null) demoBottle.SetActive(false);

            progress = CampaignProgress.Load();
            selectedLevel = progress.UnlockedLevel;
            vfx = new CampaignVfx();
            audioSys = new CampaignAudio(gameObject, progress.Muted);

            atlas = Resources.Load<Texture2D>("BioBot/RosterV2") ?? Resources.Load<Texture2D>("BioBot/Roster");
            if (atlas != null) atlas.filterMode = FilterMode.Point;
            menuBackdrop = Resources.Load<Texture2D>("BioBot/MenuBackdrop");
            circleTex = BuildCircleTexture(32);

            ApplyEnhancedPlayfields();
            if (board.BoardCamera != null) baseOrthoSize = board.BoardCamera.orthographicSize;
        }

        private void ApplyEnhancedPlayfields()
        {
            SwapBoardSprite("Surface Board/Grid Artwork", "BioBot/SurfacePlayfield");
            SwapBoardSprite("Underwater Board/Grid Artwork", "BioBot/DeepPlayfield");
        }

        private static void SwapBoardSprite(string path, string resource)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) return;
            var sr = go.GetComponent<SpriteRenderer>();
            var tex = Resources.Load<Texture2D>(resource);
            if (sr == null || tex == null) return;
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 16f);
        }

        private void Start() => robot.TerrainSpeed = 0f;

        public void OpenBriefing(int level, bool endless = false)
        {
            if (!endless && (level < 1 || level > progress.UnlockedLevel)) return;
            selectedLevel = Mathf.Clamp(level, 1, CampaignCatalog.Levels.Length);
            pendingEndless = endless;
            overlay = Overlay.Briefing;
            audioSys.Play(SfxId.Click);
        }

        public void StartLevel(int level, bool endless = false)
        {
            if (!endless && (level < 1 || level > progress.UnlockedLevel)) return;
            selectedLevel = Mathf.Clamp(level, 1, CampaignCatalog.Levels.Length);
            pendingEndless = endless;
            match = new CampaignMatch(selectedLevel, progress, endless);
            BindMatchEvents(match);
            vfx.Clear();

            page = 0;
            selection = 0;
            shovelMode = false;
            autoPilot = false;
            fastForward = false;
            glassWaterPeek = false;
            recorded = false;
            wasRespawning = false;
            wasUnderwater = false;
            overlay = Overlay.None;

            board.ResetToSurface();
            robot.TerrainSpeed = match.RobotSpeedBonus;
            previousCell = board.RobotCell;
            previousLayer = Layer.Surface;
            lastMove = -0.4f;
            audioSys.Play(SfxId.WaveHorn);
        }

        private void BindMatchEvents(CampaignMatch m)
        {
            m.OnSfx = id => audioSys.Play(id);
            m.OnBurst = (layer, px, col, cnt) => vfx.SpawnBurst(layer, px, col, cnt);
            m.OnRing = (layer, px, col, sz) => vfx.SpawnRing(layer, px, col, sz);
            m.OnFloatText = (layer, px, txt, col) => vfx.SpawnText(layer, px, txt, col);
            m.OnShake = (dur, intensity) => vfx.TriggerShake(dur, intensity);
            m.OnWaveBanner = txt => vfx.ShowWaveBanner(txt);
            m.OnAchievement = id =>
            {
                if (progress.UnlockAchievement(id, out var spec))
                {
                    vfx.ShowToast($"THÀNH TÍCH: {spec.Title}", $"+{spec.RewardCoins} Xu Sinh Thái!");
                    audioSys.Play(SfxId.Coin);
                }
            };
        }

        private void Update()
        {
            float rawDelta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            vfx.Tick(rawDelta);

            if (board.BoardCamera != null)
            {
                float zoom = 0f;
                if (board.IsTransitioning)
                {
                    float arc = Mathf.Sin(board.TransitionProgress * Mathf.PI);
                    zoom = board.TargetUnderwater ? -0.42f * arc : 0.42f * arc;
                }
                else if (match != null && match.IsDashing)
                {
                    zoom = -0.14f;
                }
                board.BoardCamera.orthographicSize = Mathf.Lerp(board.BoardCamera.orthographicSize, baseOrthoSize + zoom, rawDelta * 18f);
            }

            Keyboard keyboard = Keyboard.current;
            if (match == null)
            {
                if (overlay == Overlay.Briefing && keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                    StartLevel(selectedLevel, pendingEndless);
                else if (overlay == Overlay.None && keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                    OpenBriefing(selectedLevel, false);
                else if (overlay != Overlay.None && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    overlay = Overlay.None;
                return;
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                overlay = overlay == Overlay.None ? Overlay.Pause : Overlay.None;
                audioSys.Play(SfxId.Click);
                return;
            }

            if (match.Ended)
            {
                robot.TerrainSpeed = 0f;
                RecordMatchEnd();
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                {
                    bool next = !match.Endless && match.Won && selectedLevel < CampaignCatalog.Levels.Length;
                    StartLevel(next ? selectedLevel + 1 : selectedLevel, match.Endless);
                }
                return;
            }

            if (overlay != Overlay.None)
            {
                robot.TerrainSpeed = 0f;
                return;
            }

            if (board.IsUnderwater != wasUnderwater)
            {
                wasUnderwater = board.IsUnderwater;
                audioSys.Play(wasUnderwater ? SfxId.Dive : SfxId.Surface);
                Vector2 rPx = CampaignMatch.CellToPixel(board.RobotColumn, board.RobotCell.y);
                vfx.SpawnBurst(wasUnderwater ? Layer.Deep : Layer.Surface, rPx, Cyan, 14, 95f, 4.8f);
                vfx.SpawnRing(wasUnderwater ? Layer.Deep : Layer.Surface, rPx, Color.white, 56f);
            }

            float simDelta = Mathf.Min(Time.deltaTime, 0.05f) * (fastForward ? 1.85f : 1f);
            Layer layer = board.IsUnderwater ? Layer.Deep : Layer.Surface;
            Vector2Int cell = board.RobotCell;

            match.Tick(simDelta, layer, cell.y, board.RobotColumn);
            if (match.Ended)
            {
                robot.TerrainSpeed = 0f;
                RecordMatchEnd();
                return;
            }

            bool respawning = match.Respawn > 0f;
            if (wasRespawning && !respawning) board.ReturnToDock();
            wasRespawning = respawning;

            var hazard = match.Trash[(int)layer, cell.y, cell.x];
            var hType = hazard == null ? (TrashEffect?)null : CampaignCatalog.Trash[hazard.Spec].Effect;
            float terrainMult = hType is TrashEffect.Filament or TrashEffect.Spill ? 0.58f : 1f;
            robot.TerrainSpeed = respawning ? 0f : match.RobotSpeedBonus * terrainMult;

            if (respawning || board.IsTransitioning) return;

            // Animated water-ripple wake behind Bio-Bot!
            wakeTimer -= simDelta;
            if (wakeTimer <= 0f)
            {
                wakeTimer = match.IsDashing ? 0.05f : 0.22f;
                Vector2 rPx = CampaignMatch.CellToPixel(board.RobotColumn, cell.y);
                vfx.SpawnRing(layer, rPx + new Vector2(0, 10),
                    match.IsDashing ? new Color(0.55f, 1f, 1f, 0.8f) : new Color(0.75f, 0.96f, 1f, 0.35f),
                    match.IsDashing ? 32f : 22f);
            }

            if (autoPilot) match.StepAutoPilot(board, simDelta);

            if (previousLayer == layer && previousCell.y == cell.y && previousCell.x != cell.x && match.Time - lastMove > 0.14f)
            {
                int dir = cell.x - previousCell.x;
                lastFacingX = dir > 0 ? 1 : -1;
                if (Mathf.Abs(dir) == 1 && match.PushFloat(layer, cell.y, cell.x, dir, match.IsDashing))
                    lastMove = match.Time;
            }
            previousCell = cell;
            previousLayer = layer;

            HandleMatchHotkeys(keyboard, layer, cell);
        }

        private void HandleMatchHotkeys(Keyboard kb, Layer layer, Vector2Int cell)
        {
            if (kb == null) return;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) lastFacingX = -1;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) lastFacingX = 1;

            if (kb.zKey.wasPressedThisFrame) ChangePage(-1);
            if (kb.xKey.wasPressedThisFrame) ChangePage(1);
            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);
            if (kb.digit6Key.wasPressedThisFrame) SelectSlot(5);
            if (kb.spaceKey.wasPressedThisFrame)
            {
                if (shovelMode) { match.RemoveAlly(layer, cell.y, cell.x); shovelMode = false; }
                else match.Plant(selection, layer, cell.y, cell.x);
            }
            if (kb.fKey.wasPressedThisFrame) match.Collect(layer, cell.y, cell.x);
            if (kb.gKey.wasPressedThisFrame) match.UseTrash(layer, cell.y, cell.x);
            if (kb.rKey.wasPressedThisFrame) match.RobotPulse(layer, cell.y, board.RobotColumn);
            if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                match.TriggerHydroDash(layer, cell.y, board.RobotColumn, lastFacingX);
            if (kb.cKey.wasPressedThisFrame) { glassWaterPeek = !glassWaterPeek; audioSys.Play(SfxId.Click); }
            if (kb.pKey.wasPressedThisFrame) { autoPilot = !autoPilot; audioSys.Play(SfxId.Click); }
            if (kb.vKey.wasPressedThisFrame) { shovelMode = !shovelMode; audioSys.Play(SfxId.Click); }
        }

        private void RecordMatchEnd()
        {
            if (recorded) return;
            recorded = true;
            progress.EcoCoins += match.CoinsEarned;
            if (match.Endless)
            {
                progress.EndlessBestWave = Mathf.Max(progress.EndlessBestWave, match.Wave);
                progress.EndlessBestScore = Mathf.Max(progress.EndlessBestScore, match.Score);
            }
            else if (match.Won)
            {
                int stars = match.ComputeStars();
                progress.Stars[selectedLevel - 1] = Mathf.Max(progress.Stars[selectedLevel - 1], stars);
                progress.UnlockedLevel = Mathf.Max(progress.UnlockedLevel,
                    Mathf.Min(selectedLevel + 1, CampaignCatalog.Levels.Length));
            }
            progress.Save();
        }

        private void ReturnToMenu()
        {
            match = null;
            overlay = Overlay.None;
            autoPilot = false;
            fastForward = false;
            glassWaterPeek = false;
            board.ResetToSurface();
            robot.TerrainSpeed = 0f;
            audioSys.Play(SfxId.Click);
        }

        private int UnlockedAllyCount() =>
            match != null && match.Endless ? CampaignCatalog.Allies.Length : CampaignCatalog.UnlockedAllyCount(selectedLevel);

        private int PageCount() => Mathf.Max(1, Mathf.CeilToInt(UnlockedAllyCount() / (float)PageSize));

        private void ChangePage(int dir)
        {
            int count = PageCount();
            page = (page + dir + count) % count;
            selection = Mathf.Min(page * PageSize, UnlockedAllyCount() - 1);
            shovelMode = false;
            audioSys.Play(SfxId.Click);
        }

        private void SelectSlot(int slot)
        {
            int id = page * PageSize + slot;
            if (id < UnlockedAllyCount())
            {
                selection = id;
                shovelMode = false;
                audioSys.Play(SfxId.Click);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            Rect pixelRect = board.PixelRect;
            if (pixelRect.width < 1 || pixelRect.height < 1) return;

            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;
            Vector2 shake = match != null && overlay == Overlay.None ? vfx.ShakeOffset() : Vector2.zero;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(pixelRect.x + shake.x, Screen.height - pixelRect.yMax + shake.y, 0f),
                Quaternion.identity,
                new Vector3(pixelRect.width / 768f, pixelRect.height / 432f, 1f));

            if (match == null)
            {
                DrawMenu();
                if (overlay == Overlay.Briefing) DrawBriefing();
                else if (overlay == Overlay.Shop) DrawShop();
                else if (overlay == Overlay.Achievements) DrawAchievements();
                else if (overlay == Overlay.Guide) DrawGuide();
            }
            else
            {
                Layer layer = board.IsUnderwater ? Layer.Deep : Layer.Surface;
                HandleBoardMouse(layer);
                DrawLivingWaterAmbience(layer);
                DrawMatchWorld(layer);
                DrawTopSeedBank(layer);
                DrawRightSidebar(layer, board.RobotCell);
                DrawBottomDock(layer);
                DrawVfx(layer);

                if (board.TransitionOpacity > 0f) DrawDiveTransition();
                if (vfx.WaveBannerTime > 0f) DrawWaveBanner();
                if (match.Ended) DrawResult();
                else if (overlay == Overlay.Pause) DrawPause();
                else if (overlay == Overlay.Guide) DrawGuide();
            }

            if (vfx.ToastTime > 0f) DrawAchievementToast();

            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        private void HandleBoardMouse(Layer layer)
        {
            if (overlay != Overlay.None || match.Ended || board.IsTransitioning) return;
            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown) return;
            Vector2 pos = e.mousePosition;

            if (e.button == 0 && match.CollectOrbAt(layer, pos))
            {
                e.Use();
                return;
            }

            if (TryPixelToCell(pos, out int col, out int row))
            {
                if (e.button == 1)
                {
                    if (shovelMode) shovelMode = false;
                    else if (match.Trash[(int)layer, row, col] != null) match.UseTrash(layer, row, col);
                    e.Use();
                }
                else if (e.button == 0)
                {
                    if (shovelMode)
                    {
                        match.RemoveAlly(layer, row, col);
                        shovelMode = false;
                    }
                    else if (match.Trash[(int)layer, row, col] != null)
                    {
                        match.Collect(layer, row, col);
                    }
                    else
                    {
                        match.Plant(selection, layer, row, col);
                    }
                    e.Use();
                }
            }
        }

        private static bool TryPixelToCell(Vector2 pos, out int col, out int row)
        {
            col = Mathf.FloorToInt((pos.x - 30f) / 60f);
            row = Mathf.FloorToInt((pos.y - 84f) / 54f);
            return col >= 0 && col < CampaignMatch.Columns && row >= 0 && row < CampaignMatch.Rows;
        }

        // ==================== LIVING ANIMATED WATER ENVIRONMENT ====================

        private void DrawLivingWaterAmbience(Layer layer)
        {
            float t = Time.unscaledTime;
            // Subtle Eco-Surge shimmering aura across the entire estuary!
            if (match.IsEcoSurge)
            {
                float glow = 0.08f + 0.05f * Mathf.Sin(t * 7f);
                Fill(new Rect(30, 84, 540, 270), new Color(0.45f, 1f, 0.88f, glow));
            }
            else if (match.WaterClarity < 25f)
            {
                // Murky pollution tint when clarity is low
                Fill(new Rect(30, 84, 540, 270), new Color(0.22f, 0.18f, 0.12f, 0.14f));
            }

            if (layer == Layer.Surface)
            {
                // Animated sunlit caustic shimmer bars drifting across water
                for (int i = 0; i < 16; i++)
                {
                    float sx = 36f + ((i * 73f + t * (16f + (i % 3) * 6f)) % 526f);
                    float sy = 92f + ((i * 41f) % 252f) + Mathf.Sin(t * 2.5f + i) * 3f;
                    float sw = 12f + (i % 4) * 5f;
                    Fill(new Rect(sx, sy, sw, 2f), new Color(0.85f, 1f, 0.96f, 0.24f));
                }
            }
            else
            {
                // Animated rising underwater bioluminescent bubbles
                for (int i = 0; i < 20; i++)
                {
                    float bx = 42f + ((i * 61f) % 518f) + Mathf.Sin(t * 2.2f + i) * 6f;
                    float by = 350f - ((t * (22f + (i % 4) * 9f) + i * 37f) % 262f);
                    float sz = 3f + (i % 3) * 1.8f;
                    DrawCircle(new Vector2(bx, by), sz, new Color(0.65f, 0.95f, 1f, 0.28f));
                }
            }
        }

        // ==================== WORLD RENDERING WITH FULL ANIMATION ====================

        private void DrawMatchWorld(Layer layer)
        {
            float t = Time.unscaledTime;
            int side = (int)layer;
            int otherSide = (int)CampaignCatalog.Opposite(layer);
            Vector2Int robotCell = board.RobotCell;

            // 1. Cross-Layer Glass-Water X-Ray Peek + Updraft Columns!
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    Vector2 p = CampaignMatch.CellToPixel(c, r);

                    // Animated Vertical Updraft Column (Deep Vent/Periscope boosting Surface!)
                    if (match.HasUpdraftAt(r, c))
                    {
                        float upA = 0.22f + 0.14f * Mathf.Sin(t * 6f + c);
                        DrawCircle(p, 38f, new Color(0.45f, 0.98f, 1f, upA * 0.55f));
                        for (int b = 0; b < 3; b++)
                        {
                            float by = p.y + 16f - ((t * 36f + b * 12f) % 34f);
                            DrawCircle(new Vector2(p.x + Mathf.Sin(t * 4f + b) * 8f, by), 4.5f, new Color(0.75f, 1f, 1f, 0.55f));
                        }
                    }

                    var oppAlly = match.Allies[otherSide, r, c];
                    var oppTrash = match.Trash[otherSide, r, c];
                    if (glassWaterPeek)
                    {
                        if (oppAlly != null)
                            DrawSprite(oppAlly.Spec, Around(p, 36), 4, c + r, new Color(0.55f, 0.95f, 1f, 0.42f));
                        if (oppTrash != null)
                            DrawSprite(34 + oppTrash.Spec, Around(p, 32), 4, c + r, new Color(1f, 0.85f, 0.45f, 0.4f));
                    }
                    else
                    {
                        if (oppAlly != null)
                            DrawCircle(p, 16f, new Color(0.45f, 0.95f, 0.85f, 0.18f));
                        if (oppTrash != null && oppTrash.Active)
                            Border(new Rect(p.x - 23, p.y - 20, 46, 40), new Color(0.45f, 0.95f, 1f, 0.48f));
                    }
                }
            }
            foreach (var e in match.Enemies)
            {
                if (e.Layer != layer)
                {
                    Vector2 p = CampaignMatch.CellToPixel(Mathf.Clamp(e.X, 0f, 8.6f), e.Row);
                    if (glassWaterPeek)
                        DrawSprite(20 + e.Spec, Around(p, 38), 6, e.Row, new Color(1f, 0.45f, 0.45f, 0.45f));
                    else
                        DrawCircle(p, 12f, new Color(1f, 0.35f, 0.35f, 0.24f + 0.14f * Mathf.Sin(t * 8f)));
                }
            }

            // 2. Animated Spinning Hydro-Turbines on Left Eco-Dam
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                if (match.TurbineReady[side, r])
                {
                    Rect tr = new Rect(5, 93 + r * 54, 20, 32);
                    Fill(tr, new Color(0.18f, 0.78f, 0.84f, 0.9f));
                    Border(tr, Color.white);
                    float spin = Mathf.Sin(t * 10f + r);
                    Fill(new Rect(8, 107 + r * 54 + spin * 6f, 14, 4), Color.white);
                }
            }

            // 3. Hover Ghost Preview & Bio-Bot Tile Frame
            if (overlay == Overlay.None && TryPixelToCell(Event.current.mousePosition, out int hCol, out int hRow))
            {
                Rect hr = new Rect(30 + hCol * 60, 84 + hRow * 54, 58, 52);
                if (shovelMode)
                {
                    Fill(hr, new Color(1f, 0.35f, 0.35f, 0.24f));
                    Border(hr, Danger);
                }
                else
                {
                    bool valid = match.CanPlant(selection, layer, hRow, hCol, out _);
                    Fill(hr, valid ? new Color(0.45f, 1f, 0.72f, 0.2f) : new Color(1f, 0.4f, 0.35f, 0.16f));
                    Border(hr, valid ? Green : Danger);
                    if (valid)
                        DrawSprite(selection, Around(CampaignMatch.CellToPixel(hCol, hRow), 42), 5, 0, new Color(1, 1, 1, 0.52f));
                }
            }
            Border(new Rect(30 + robotCell.x * 60, 84 + robotCell.y * 54, 58, 52), match.IsDashing ? Cyan : Gold);

            // 4. Trash & Allies with Squash-and-Stretch, Breathing, Recoil & Hurt Wobble!
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    Vector2 p = CampaignMatch.CellToPixel(c, r);
                    var waste = match.Trash[side, r, c];
                    if (waste != null)
                    {
                        float bobY = Mathf.Sin(t * 3.6f + waste.BobPhase) * 2.5f;
                        if (waste.Active)
                        {
                            float glow = 0.55f + 0.35f * Mathf.Sin(t * 7f);
                            Border(new Rect(p.x - 23, p.y - 20, 46, 40), new Color(0.45f, 1f, 0.92f, glow));
                        }
                        DrawSprite(34 + waste.Spec, Around(p + new Vector2(0, bobY), 40), 4, c + r, Color.white);
                        if (CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Filament)
                            Label(p.x - 18, p.y - 26 + bobY, 36, 14, "CỨU!", smallCenter, Cyan);
                    }

                    var ally = match.Allies[side, r, c];
                    if (ally != null)
                    {
                        // Elastic plant pop + breathing squash-and-stretch + firing recoil!
                        float breathe = Mathf.Sin(t * 4.2f + ally.Phase);
                        float sx = 1f + breathe * 0.055f;
                        float sy = 1f - breathe * 0.055f;
                        float ox = 0f;

                        if (ally.PlantPop > 0f)
                        {
                            float pt = 1f - ally.PlantPop / 0.34f;
                            float elastic = 1f + Mathf.Sin(pt * Mathf.PI * 2f) * (1f - pt) * 0.38f;
                            sx *= elastic;
                            sy *= (2f - elastic);
                        }
                        if (ally.FireAnim > 0f)
                        {
                            float ft = ally.FireAnim / 0.24f;
                            sx *= 1f + ft * 0.22f;
                            sy *= 1f - ft * 0.14f;
                            ox += ft * 5f;
                            if (ft > 0.65f) DrawCircle(p + new Vector2(18, -2), 14f * ft, new Color(0.85f, 1f, 0.85f, 0.75f));
                        }
                        if (ally.HurtAnim > 0f)
                        {
                            ox -= Mathf.Sin(ally.HurtAnim * 38f) * 3.5f;
                        }

                        float w = 44f * sx;
                        float h = 44f * sy;
                        Color tint = ally.HurtAnim > 0f ? new Color(1f, 0.72f, 0.72f, 1f)
                            : ally.Jammed > 0f ? new Color(0.75f, 0.65f, 1f, 1f)
                            : match.IsEcoSurge ? new Color(0.9f, 1f, 0.92f, 1f) : Color.white;

                        // Subtle water ripple ring beneath ally
                        DrawCircle(p + new Vector2(0, 15), 26f + breathe * 2f, new Color(0.75f, 0.96f, 1f, 0.16f));
                        DrawSprite(ally.Spec, new Rect(p.x - w * 0.5f + ox, p.y - h * 0.5f + (44f - h) * 0.35f, w, h),
                            ally.FireAnim > 0f ? 12 : 5, c + r, tint);

                        if (layer == Layer.Surface && match.HasUpdraftAt(r, c))
                            Label(p.x - 16, p.y - 26, 32, 13, "▲+55%", smallCenter, Cyan);
                        if (ally.Health < ally.MaxHealth)
                            Bar(new Rect(p.x - 14, p.y + 19, 28, 4), ally.Health / (float)ally.MaxHealth, Green);
                    }
                }
            }

            // 5. Smooth Sliding Bowling Bottles with Foam Wake Trails!
            foreach (var sb in match.SlidingBottles)
            {
                if (sb.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(sb.X, sb.Row);
                float trailDir = sb.VelX > 0 ? -1f : 1f;
                for (int k = 1; k <= 3; k++)
                    DrawCircle(p + new Vector2(trailDir * k * 10f, 4f), 14f - k * 3f, new Color(0.75f, 0.98f, 1f, 0.45f - k * 0.1f));
                DrawSprite(34, Around(p, 42), 12, 0, Color.white);
            }

            // 6. Active Hydro-Turbine Waves
            foreach (var tw in match.Turbines)
            {
                if (tw.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(tw.X, tw.Row);
                Fill(new Rect(p.x - 22, p.y - 25, 38, 50), new Color(0.35f, 0.92f, 1f, 0.85f));
                Fill(new Rect(p.x + 8, p.y - 25, 10, 50), Color.white);
            }

            // 7. Beams
            foreach (var b in match.Beams)
            {
                if (b.Layer != layer) continue;
                Vector2 a = CampaignMatch.CellToPixel(b.From, b.Row);
                Vector2 c = CampaignMatch.CellToPixel(b.To, b.Row);
                Color col = b.Color;
                col.a = Mathf.Clamp01(b.Left * 5f);
                float h = b.Thick ? 9f : 4f;
                Fill(new Rect(Mathf.Min(a.x, c.x), a.y - h * 0.5f, Mathf.Max(8f, Mathf.Abs(a.x - c.x)), h), col);
            }

            // 8. Styled Projectiles with Motion Trails!
            foreach (var proj in match.Projectiles)
            {
                if (proj.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(proj.X, proj.Row);
                p.y += Mathf.Sin(proj.Age * 14f) * (proj.Kind == ProjectileKind.Bubble ? 3f : 1f);
                Color pc = proj.Kind switch
                {
                    ProjectileKind.Bubble => Cyan,
                    ProjectileKind.Zap => Gold,
                    ProjectileKind.Spear => Green,
                    ProjectileKind.Slow => new Color(0.6f, 0.92f, 1f, 1f),
                    ProjectileKind.Push => new Color(0.85f, 1f, 0.45f, 1f),
                    ProjectileKind.Geyser => new Color(1f, 0.62f, 0.3f, 1f),
                    _ => new Color(0.55f, 1f, 0.55f, 1f)
                };
                // Trail
                DrawCircle(p - new Vector2(10, 0), 7f, new Color(pc.r, pc.g, pc.b, 0.32f));
                DrawCircle(p - new Vector2(18, 0), 4.5f, new Color(pc.r, pc.g, pc.b, 0.16f));
                if (proj.Kind == ProjectileKind.Spear)
                    Fill(new Rect(p.x - 10, p.y - 2.5f, 20, 5), pc);
                else if (proj.Kind == ProjectileKind.Push)
                    Fill(new Rect(p.x - 4, p.y - 12, 8, 24), pc);
                else
                    DrawCircle(p, proj.Kind == ProjectileKind.Geyser ? 15f : 11f, pc);
                DrawCircle(p - new Vector2(2, 2), 4f, Color.white);
            }

            // 9. Enemies with Waddle Hop, Bite Lunge & Hit Squash!
            foreach (var e in match.Enemies)
            {
                if (e.Layer != layer) continue;
                var eSpec = CampaignCatalog.Enemies[e.Spec];
                Vector2 p = CampaignMatch.CellToPixel(Mathf.Clamp(e.X, -0.3f, 8.7f), e.Row);
                float baseSz = eSpec.Trait == Trait.Boss ? 70f : (e.Small ? 32f : 46f);

                float waddle = Mathf.Abs(Mathf.Sin(e.WalkPhase));
                float sx = 1f + (0.5f - waddle) * 0.08f;
                float sy = 1f + (waddle - 0.5f) * 0.08f;
                float ox = 0f;
                float oy = -waddle * 3.5f;

                if (e.BiteAnim > 0f)
                {
                    float bt = Mathf.Sin((e.BiteAnim / 0.25f) * Mathf.PI);
                    ox -= bt * 8f;
                    sx *= 1f + bt * 0.16f;
                }
                if (e.HurtAnim > 0f)
                {
                    float ht = e.HurtAnim / 0.16f;
                    ox += ht * 5f;
                    sx *= 1f - ht * 0.18f;
                    sy *= 1f + ht * 0.18f;
                }

                float w = baseSz * sx;
                float h = baseSz * sy;
                Color tint = e.Flash > 0f ? new Color(1f, 0.55f, 0.45f, 1f)
                    : e.Slow > 0f ? new Color(0.62f, 0.88f, 1f, 1f) : Color.white;

                DrawCircle(p + new Vector2(0, baseSz * 0.36f), baseSz * 0.55f, new Color(0.05f, 0.1f, 0.15f, 0.28f));
                DrawSprite(20 + e.Spec, new Rect(p.x - w * 0.5f + ox, p.y - h * 0.5f + oy, w, h),
                    e.Slow > 0f ? 4 : 9, e.Row, tint);

                float bw = eSpec.Trait == Trait.Boss ? 54f : 28f;
                Bar(new Rect(p.x - bw * 0.5f, p.y - baseSz * 0.54f, bw, 4), e.Health / (float)e.MaxHealth, Danger);
            }

            // 10. Arcing Collectible Bio-Orbs with Sunburst Pulse!
            foreach (var orb in match.Orbs)
            {
                if (orb.Layer != layer) continue;
                float bob = orb.FlyingToBank ? 0f : Mathf.Sin(orb.Bob) * 3f;
                Vector2 p = orb.Pos + new Vector2(0, bob);
                float pulse = 20f + Mathf.Sin(t * 7f + orb.Bob) * 3f;
                DrawCircle(p, pulse, new Color(1f, 0.88f, 0.35f, 0.35f));
                DrawCircle(p, 13f, Gold);
                DrawCircle(p - new Vector2(3, 3), 5f, Color.white);
            }

            // 11. Boss HP Bar & Live Combo Badge
            var boss = match.ActiveBoss;
            if (boss != null)
            {
                Rect br = new Rect(136, 80, 336, 18);
                Fill(br, Ink);
                Border(br, Danger);
                Bar(new Rect(br.x + 3, br.y + 3, br.width - 6, br.height - 6), boss.Health / (float)boss.MaxHealth, Danger);
                Label(br.x, br.y + 1, br.width, 16, $"TÀU XẢ ĐỘC ({LayerName(boss.Layer)})  {boss.Health}/{boss.MaxHealth}", smallCenter, Color.white);
            }
            if (match.ComboCount >= 2)
            {
                Rect cr = new Rect(468, 86, 98, 22);
                Fill(cr, new Color(0.12f, 0.24f, 0.32f, 0.9f));
                Border(cr, Gold);
                Label(cr.x, cr.y + 3, cr.width, 16, $"ECO COMBO x{match.ComboCount}!", smallCenter, Gold);
            }

            if (match.Respawn > 0f)
            {
                Rect rr = new Rect(176, 182, 248, 48);
                Fill(rr, Ink);
                Border(rr, Danger);
                Label(rr.x, rr.y + 12, rr.width, 24, $"Bio-Bot hồi phục sau {Mathf.CeilToInt(match.Respawn)}s", smallCenter, Danger);
            }
        }

        // ==================== PvZ TOP SEED BANK ====================

        private void DrawTopSeedBank(Layer layer)
        {
            Fill(new Rect(8, 4, 752, 68), Ink);
            Border(new Rect(8, 4, 752, 68), match.IsEcoSurge ? Gold : new Color(0.28f, 0.58f, 0.65f, 1f));

            // Bouncing Sun / Bio-Energy Counter
            float bScale = match.EnergyBounce > 0f ? 1f + Mathf.Sin(match.EnergyBounce * 14f) * 0.2f : 1f;
            Rect sunBox = new Rect(14, 9, 76, 58);
            Fill(sunBox, new Color32(36, 62, 54, 255));
            Border(sunBox, Gold);
            DrawCircle(new Vector2(52, 27), 16f * bScale, Gold);
            DrawCircle(new Vector2(49, 24), 6f * bScale, Color.white);
            Label(14, 44, 76, 20, match.Energy.ToString(), smallCenter, Gold);

            int totalPages = PageCount();
            if (totalPages > 1 && Click(new Rect(96, 14, 22, 48), "◄")) ChangePage(-1);

            int unlockedCount = UnlockedAllyCount();
            for (int slot = 0; slot < PageSize; slot++)
            {
                int id = page * PageSize + slot;
                Rect card = new Rect(122 + slot * 82, 9, 78, 58);
                if (id >= unlockedCount)
                {
                    Fill(card, new Color(0.08f, 0.14f, 0.2f, 0.7f));
                    Border(card, new Color(0.2f, 0.3f, 0.38f, 0.5f));
                    continue;
                }

                var ally = CampaignCatalog.Allies[id];
                bool fits = CampaignCatalog.Fits(ally.Habitat, layer);
                bool afford = match.Energy >= ally.Cost;
                float cd = match.CardCooldowns[id];
                bool sel = !shovelMode && selection == id;

                Fill(card, sel ? CardSel : (fits && afford ? CardBg : new Color32(20, 34, 46, 255)));
                Border(card, sel ? Gold : (fits && afford ? Teal : new Color(0.3f, 0.4f, 0.46f, 0.7f)));

                DrawSprite(id, new Rect(card.x + 22, card.y + 4, 34, 34), 4, id, fits && afford ? Color.white : new Color(0.6f, 0.65f, 0.7f, 0.7f));
                Label(card.x + 3, card.y + 2, 16, 14, (slot + 1).ToString(), small, Muted);
                Label(card.x + 48, card.y + 2, 28, 14, HabitatTag(ally.Habitat), small, ally.Habitat == Habitat.Deep ? Cyan : ally.Habitat == Habitat.Surface ? Gold : Teal);
                Label(card.x + 2, card.y + 34, 74, 13, ally.Name, smallCenter, Color.white);
                Label(card.x + 2, card.y + 45, 74, 13, $"{ally.Cost} NL", smallCenter, afford ? Gold : Danger);

                if (cd > 0f)
                {
                    float frac = Mathf.Clamp01(cd / Mathf.Max(0.1f, ally.Recharge));
                    Fill(new Rect(card.x, card.y, card.width, card.height * frac), new Color(0f, 0f, 0f, 0.62f));
                }

                if (Click(card, "", GUIStyle.none)) SelectSlot(slot);
            }

            if (totalPages > 1 && Click(new Rect(616, 14, 22, 48), "►")) ChangePage(1);

            Fill(new Rect(644, 9, 110, 58), Panel);
            if (Click(new Rect(648, 12, 48, 24), shovelMode ? "Hủy" : "Xẻng"))
            { shovelMode = !shovelMode; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(700, 12, 50, 24), fastForward ? "2x »" : "1x"))
            { fastForward = !fastForward; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(648, 40, 48, 24), autoPilot ? "AI ON" : "AI"))
            { autoPilot = !autoPilot; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(700, 40, 50, 24), "Menu"))
            { overlay = Overlay.Pause; audioSys.Play(SfxId.Click); }
        }

        // ==================== RIGHT SIDEBAR (MINIMAP + ECO-CLARITY GAUGE) ====================

        private void DrawRightSidebar(Layer layer, Vector2Int cell)
        {
            Layer other = CampaignCatalog.Opposite(layer);
            Rect panel = new Rect(586, 78, 174, 280);
            Fill(panel, Ink);
            Border(panel, match.IsEcoSurge ? Gold : (layer == Layer.Deep ? Cyan : Gold));

            Label(596, 83, 154, 16, $"ĐANG Ở: {LayerName(layer)}", small, layer == Layer.Deep ? Cyan : Gold);
            if (Click(new Rect(596, 100, 102, 24), layer == Layer.Deep ? "▲ E: Nổi" : "▼ Q: Lặn"))
                board.ToggleLayer();
            if (Click(new Rect(702, 100, 48, 24), glassWaterPeek ? "Soi✓" : "C:Soi"))
            { glassWaterPeek = !glassWaterPeek; audioSys.Play(SfxId.Click); }

            // Interactive Dual-Layer Minimap
            Label(596, 127, 154, 14, $"TẦNG KIA: {LayerName(other)} (Click đổi)", small, Muted);
            Rect mapHit = new Rect(596, 142, 154, 74);
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    Rect t = new Rect(598 + c * 16.5f, 144 + r * 14f, 14.5f, 12f);
                    Color col = other == Layer.Deep ? new Color32(24, 68, 106, 255) : new Color32(36, 118, 118, 255);
                    if (match.Trash[(int)other, r, c] != null) col = Gold;
                    if (match.Allies[(int)other, r, c] != null) col = Teal;
                    Fill(t, col);
                    if (r == cell.y && c == cell.x) Border(t, Color.white);
                }
            }
            foreach (var e in match.Enemies)
            {
                if (e.Layer == other)
                {
                    float ex = 599 + Mathf.Clamp(Mathf.RoundToInt(e.X), 0, 8) * 16.5f;
                    Fill(new Rect(ex, 146 + e.Row * 14f, 10, 8), Danger);
                }
            }
            if (Click(mapHit, "", GUIStyle.none)) board.ToggleLayer();

            // Original Eco-Clarity / Eco-Surge Bar!
            Label(596, 220, 154, 15, match.IsEcoSurge
                ? $"✦ DÒNG CHẢY XANH ({Mathf.CeilToInt(match.EcoSurgeTime)}s)"
                : $"ĐỘ TRONG NƯỚC  {Mathf.RoundToInt(match.WaterClarity)}%",
                small, match.IsEcoSurge ? Gold : Cyan);
            Bar(new Rect(596, 235, 154, 7), match.WaterClarity / 100f, match.IsEcoSurge ? Gold : Cyan);

            // Eco-Dam Integrity & Bio-Bot HP
            Label(596, 246, 154, 15, $"CỬA ĐÊ  {match.Integrity}/{CampaignMatch.MaxIntegrity}", small, Teal);
            Bar(new Rect(596, 261, 154, 6), match.Integrity / (float)CampaignMatch.MaxIntegrity, Teal);

            Label(596, 271, 154, 15, $"BIO-BOT HP  {match.RobotHealth}/{match.MaxRobotHealth}", small, match.RobotHealth <= 1 ? Danger : Green);
            Bar(new Rect(596, 286, 154, 6), match.RobotHealth / (float)match.MaxRobotHealth, match.RobotHealth <= 1 ? Danger : Green);

            var debris = match.Trash[(int)layer, cell.y, cell.x];
            if (debris != null)
            {
                var spec = CampaignCatalog.Trash[debris.Spec];
                Label(596, 296, 154, 15, $"RÁC: {spec.Name}", small, Gold);
                Label(596, 311, 154, 42, spec.Hint, wrap, Muted);
            }
            else if (match.NextRow >= 0)
            {
                string warn = $"Sắp vào ({Mathf.CeilToInt(match.NextAttack)}s): Hàng {match.NextRow + 1} · {LayerName(match.NextLayer)}";
                Label(596, 298, 154, 34, warn, wrap, match.NextAttack < 4f ? Danger : Muted);
                Label(596, 334, 154, 18, $"Điểm: {match.Score}  ·  Xu: +{match.CoinsEarned}", small, Gold);
            }
            else
            {
                Label(596, 300, 154, 18, "ĐỢT CUỐI ĐANG CHẠY!", small, Gold);
                Label(596, 324, 154, 18, $"Điểm: {match.Score}  ·  Xu: +{match.CoinsEarned}", small, Gold);
            }
        }

        // ==================== BOTTOM DOCK (HYDRO-DASH + BIO-PULSE + WAVE FLAGS) ====================

        private void DrawBottomDock(Layer layer)
        {
            Fill(new Rect(8, 364, 752, 62), Ink);
            Border(new Rect(8, 364, 752, 62), new Color(0.24f, 0.5f, 0.58f, 1f));

            string dashTxt = match.DashCooldownLeft > 0f
                ? $"Shift: Lướt ({match.DashCooldownLeft:0.0}s)"
                : "Shift: HYDRO-DASH";
            if (Click(new Rect(14, 371, 118, 28), dashTxt))
                match.TriggerHydroDash(layer, board.RobotCell.y, board.RobotColumn, lastFacingX);

            string pulseTxt = match.PulseCooldownLeft > 0f
                ? $"R: Xung ({Mathf.CeilToInt(match.PulseCooldownLeft)}s)"
                : "R: BIO-PULSE (15)";
            if (Click(new Rect(136, 371, 116, 28), pulseTxt))
                match.RobotPulse(layer, board.RobotCell.y, board.RobotColumn);

            // PvZ Wave Progress Flag Bar
            Label(262, 369, 326, 16, match.Endless
                ? $"VÔ TẬN · ĐỢT {match.Wave}"
                : $"Màn {selectedLevel:00}: {match.Rules.Name}  ·  Đợt {match.Wave}/{match.TotalWaves}", small, Color.white);
            Rect bar = new Rect(262, 387, 336, 10);
            Bar(bar, match.WaveProgress, Green);
            Border(bar, Color.white);
            int flags = Mathf.Max(1, match.TotalWaves);
            for (int f = 1; f <= flags; f++)
            {
                float fx = bar.x + (bar.width * f / flags) - 3;
                Fill(new Rect(fx, bar.y - 4, 2, 14), Gold);
                Fill(new Rect(fx + 2, bar.y - 4, 6, 5), Danger);
            }

            if (Click(new Rect(610, 371, 68, 28), audioSys.Muted ? "Âm: Tắt" : "Âm: Bật"))
            {
                audioSys.Muted = !audioSys.Muted;
                progress.Muted = audioSys.Muted;
                progress.Save();
            }
            if (Click(new Rect(682, 371, 70, 28), "Sổ ST"))
            { overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }

            string tip = match.Feedback > 0f
                ? match.Message
                : $"[{CampaignCatalog.Allies[selection].Name}] {CampaignCatalog.Allies[selection].Hint} · Shift: Lướt bowling rác · C: Soi xuyên 2 tầng.";
            Label(16, 405, 736, 18, tip, small, match.Feedback > 0f ? Teal : Muted);
        }

        // ==================== VFX, BANNERS & TOASTS ====================

        private void DrawVfx(Layer layer)
        {
            foreach (var p in vfx.Particles)
            {
                if (p.Layer != layer) continue;
                float t = Mathf.Clamp01(p.Life / p.MaxLife);
                Color c = p.Color;
                c.a *= t;
                if (p.Ring)
                {
                    float r = p.Size * (1f - t * 0.65f);
                    Border(new Rect(p.Pos.x - r * 0.5f, p.Pos.y - r * 0.5f, r, r), c);
                }
                else
                {
                    DrawCircle(p.Pos, p.Size * 2f, c);
                }
            }
            foreach (var ft in vfx.Texts)
            {
                if (ft.Layer != layer) continue;
                Color c = ft.Color;
                c.a = Mathf.Clamp01(ft.Life / ft.MaxLife);
                Label(ft.Pos.x - 52, ft.Pos.y - 8, 104, 18, ft.Text, smallCenter, c);
            }
        }

        private void DrawDiveTransition()
        {
            float op = board.TransitionOpacity;
            Color tint = board.TargetUnderwater ? new Color(0.04f, 0.16f, 0.28f, op * 0.85f) : new Color(0.18f, 0.55f, 0.62f, op * 0.72f);
            Fill(new Rect(0, 0, 768, 432), tint);
            for (int i = 0; i < 20; i++)
            {
                float bx = (i * 47 + 23) % 768;
                float by = ((board.TargetUnderwater ? (1f - board.TransitionProgress) : board.TransitionProgress) * 480f + i * 53) % 432f;
                DrawCircle(new Vector2(bx, by), 8f + (i % 4) * 3f, new Color(0.7f, 0.96f, 1f, op * 0.65f));
            }
        }

        private void DrawWaveBanner()
        {
            float alpha = Mathf.Clamp01(vfx.WaveBannerTime * 1.2f);
            Rect r = new Rect(114, 174, 540, 56);
            Fill(r, new Color(0.06f, 0.12f, 0.18f, 0.9f * alpha));
            Border(r, new Color(1f, 0.55f, 0.35f, alpha));
            Label(r.x, r.y + 15, r.width, 30, vfx.WaveBannerText, heading, new Color(1f, 0.86f, 0.4f, alpha));
        }

        private void DrawAchievementToast()
        {
            Rect r = new Rect(478, 12, 278, 52);
            Fill(r, new Color(0.08f, 0.18f, 0.24f, 0.95f));
            Border(r, Gold);
            Label(r.x + 12, r.y + 7, r.width - 20, 18, "★ " + vfx.ToastTitle, small, Gold);
            Label(r.x + 12, r.y + 26, r.width - 20, 18, vfx.ToastSub, small, Teal);
        }

        // ==================== MENU & CAMPAIGN MAP ====================

        private void DrawMenu()
        {
            if (menuBackdrop != null) GUI.DrawTexture(new Rect(0, 0, 768, 432), menuBackdrop);
            else Fill(new Rect(0, 0, 768, 432), Ink);
            Fill(new Rect(0, 0, 768, 432), new Color(0.03f, 0.08f, 0.14f, 0.48f));

            // Animated rising menu bubbles!
            float t = Time.unscaledTime;
            for (int i = 0; i < 14; i++)
            {
                float bx = 30f + (i * 57f) % 708f + Mathf.Sin(t * 1.8f + i) * 8f;
                float by = 420f - ((t * (18f + (i % 3) * 8f) + i * 43f) % 240f);
                DrawCircle(new Vector2(bx, by), 5f + (i % 3) * 2f, new Color(0.7f, 0.96f, 1f, 0.3f));
            }

            Label(28, 18, 440, 42, "BIO-BOT: ĐÊ XANH CẢM TỬ", title, Color.white);
            Label(30, 58, 460, 20, "Phòng thủ liên tầng Mặt Nước & Đáy Biển · Dòng Chảy Xanh & Hydro-Dash", small, Teal);

            Fill(new Rect(506, 18, 236, 44), Ink);
            Border(new Rect(506, 18, 236, 44), Gold);
            Label(518, 24, 108, 16, $"★ {progress.TotalStars()}/{CampaignCatalog.Levels.Length * 3} Sao", small, Gold);
            Label(518, 41, 108, 16, $"● {progress.EcoCoins} Xu", small, Teal);
            if (Click(new Rect(636, 26, 98, 26), audioSys.Muted ? "Âm: Tắt" : "Âm: Bật"))
            {
                audioSys.Muted = !audioSys.Muted;
                progress.Muted = audioSys.Muted;
                progress.Save();
            }

            Label(28, 86, 360, 20, "CHIẾN DỊCH 8 MÀN CHƠI", text, Gold);
            for (int i = 0; i < CampaignCatalog.Levels.Length; i++)
            {
                int lv = i + 1;
                int gx = i % 2;
                int gy = i / 2;
                Rect card = new Rect(28 + gx * 194, 110 + gy * 62, 186, 54);
                bool unlocked = lv <= progress.UnlockedLevel;
                bool chosen = selectedLevel == lv;
                Fill(card, chosen ? CardSel : (unlocked ? Panel : new Color(0.08f, 0.13f, 0.18f, 0.85f)));
                Border(card, chosen ? Gold : (unlocked ? Teal : new Color(0.25f, 0.32f, 0.38f, 0.7f)));

                int stars = progress.Stars[i];
                string starStr = unlocked ? new string('★', stars) + new string('☆', 3 - stars) : "🔒 KHÓA";
                Label(card.x + 8, card.y + 6, 170, 18, $"{lv:00}. {CampaignCatalog.Levels[i].Name}", small, unlocked ? Color.white : Muted);
                Label(card.x + 8, card.y + 23, 170, 14, CampaignCatalog.Levels[i].Subtitle, small, Muted);
                Label(card.x + 8, card.y + 37, 170, 14, starStr, small, stars > 0 ? Gold : Muted);

                GUI.enabled = unlocked;
                if (Click(card, "", GUIStyle.none)) { selectedLevel = lv; audioSys.Play(SfxId.Click); }
                GUI.enabled = true;
            }

            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Rect info = new Rect(428, 110, 314, 240);
            Fill(info, Panel);
            Border(info, Teal);
            Label(442, 120, 286, 24, $"MÀN {selectedLevel:00}: {spec.Name}", heading, Gold);
            Label(442, 146, 286, 54, spec.Story, wrap, Color.white);
            Label(442, 202, 286, 18, "ĐỒNG MINH MỞ KHÓA Ở MÀN NÀY:", small, Teal);

            int shown = 0;
            for (int id = 0; id < CampaignCatalog.Allies.Length && shown < 4; id++)
            {
                if (CampaignCatalog.Allies[id].Unlock == selectedLevel)
                {
                    float ax = 442 + shown * 72;
                    DrawSprite(id, new Rect(ax + 12, 224, 42, 42), 5, id, Color.white);
                    Label(ax, 268, 68, 14, CampaignCatalog.Allies[id].Name, smallCenter, Color.white);
                    Label(ax, 281, 68, 14, HabitatTag(CampaignCatalog.Allies[id].Habitat), smallCenter, Muted);
                    shown++;
                }
            }

            if (Click(new Rect(442, 304, 286, 36), $"▶  CHƠI MÀN {selectedLevel:00} (Enter)"))
                OpenBriefing(selectedLevel, false);

            if (Click(new Rect(28, 368, 186, 44), $"♾ SINH TỒN VÔ TẬN\nKỷ lục: Đợt {progress.EndlessBestWave}"))
                OpenBriefing(progress.UnlockedLevel, true);
            if (Click(new Rect(224, 368, 166, 44), $"⚙ XƯỞNG BIO-BOT\n({progress.EcoCoins} Xu)"))
            { overlay = Overlay.Shop; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(400, 368, 166, 44), $"★ THÀNH TÍCH\n({progress.TotalAchievements()}/{CampaignCatalog.Achievements.Length})"))
            { overlay = Overlay.Achievements; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(576, 368, 166, 44), "📖 SỔ SINH THÁI\n(44 Thực thể)"))
            { overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }
        }

        // ==================== OVERLAYS ====================

        private void DrawBriefing()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.86f));
            Rect r = new Rect(144, 86, 480, 260);
            Fill(r, Panel);
            Border(r, Gold);
            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Label(166, 104, 436, 26, pendingEndless ? "CHẾ ĐỘ SINH TỒN VÔ TẬN" : $"MÀN {selectedLevel:00}: {spec.Name}", heading, Gold);
            Label(166, 138, 436, 68, pendingEndless
                ? "Toàn bộ 20 đồng minh đã sẵn sàng! Đối mặt với làn sóng rác thải không hồi kết và Siêu Boss mỗi 5 đợt!"
                : spec.Story, wrap, Color.white);
            Fill(new Rect(166, 212, 436, 58), Ink);
            Label(176, 220, 416, 44, "Mẹo chiến thuật: " + spec.Lesson, wrap, Teal);
            if (Click(new Rect(166, 286, 290, 40), "▶  BẮT ĐẦU NGAY (Enter)")) StartLevel(selectedLevel, pendingEndless);
            if (Click(new Rect(468, 286, 134, 40), "Quay lại")) overlay = Overlay.None;
        }

        private void DrawResult()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.82f));
            Rect r = new Rect(154, 62, 460, 308);
            Fill(r, Panel);
            Border(r, match.Won ? Teal : Danger);

            string head = match.Endless ? $"KẾT THÚC VÔ TẬN · ĐỢT {match.Wave}"
                : match.Won ? "CHIẾN THẮNG! ĐÊ XANH AN TOÀN!" : "CỬA ĐÊ BỊ VỠ!";
            Label(174, 80, 420, 28, head, heading, match.Won ? Teal : Danger);

            if (match.Won && !match.Endless)
            {
                int stars = match.ComputeStars();
                Label(174, 110, 420, 26, new string('★', stars) + new string('☆', 3 - stars), heading, Gold);
            }

            Label(174, 144, 420, 20, $"Điểm: {match.Score}   ·   Xu nhận được: +{match.CoinsEarned}   ·   Tiêu diệt: {match.Kills}", text, Color.white);
            Label(174, 168, 420, 18, $"Cá đã cứu: {match.FishRescued}   ·   Chai vào bến: {match.BottlesDocked}", small, Muted);

            bool next = !match.Endless && match.Won && selectedLevel < CampaignCatalog.Levels.Length;
            if (next)
            {
                Fill(new Rect(174, 196, 420, 74), Ink);
                Label(186, 202, 396, 16, $"MỞ KHÓA MÀN {selectedLevel + 1}: {CampaignCatalog.Levels[selectedLevel].Name}!", small, Gold);
                int xIdx = 0;
                for (int id = 0; id < CampaignCatalog.Allies.Length && xIdx < 3; id++)
                {
                    if (CampaignCatalog.Allies[id].Unlock == selectedLevel + 1)
                    {
                        DrawSprite(id, new Rect(188 + xIdx * 130, 222, 38, 38), 5, id, Color.white);
                        Label(230 + xIdx * 130, 232, 88, 20, CampaignCatalog.Allies[id].Name, small, Teal);
                        xIdx++;
                    }
                }
            }

            if (Click(new Rect(174, 286, 204, 38), next ? "▶  Màn tiếp theo (Enter)" : "↻  Chơi lại (Enter)"))
                StartLevel(next ? selectedLevel + 1 : selectedLevel, match.Endless);
            if (Click(new Rect(390, 286, 204, 38), "Về Bản Đồ & Xưởng")) ReturnToMenu();
        }

        private void DrawShop()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.92f));
            Rect r = new Rect(44, 28, 680, 376);
            Fill(r, Panel);
            Border(r, Gold);
            Label(64, 42, 380, 26, "⚙ XƯỞNG NÂNG CẤP BIO-BOT", heading, Gold);
            Label(444, 46, 160, 20, $"Xu hiện có: {progress.EcoCoins}", text, Teal);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            for (int i = 0; i < CampaignCatalog.Upgrades.Length; i++)
            {
                var up = CampaignCatalog.Upgrades[i];
                int lv = progress.UpgradeLevel(i);
                int col = i % 2;
                int row = i / 2;
                Rect card = new Rect(64 + col * 324, 86 + row * 96, 312, 84);
                Fill(card, Ink);
                Border(card, lv >= up.MaxLevel ? Teal : new Color(0.3f, 0.5f, 0.58f, 1f));
                Label(card.x + 12, card.y + 8, 210, 20, $"{up.Name}  (Lv {lv}/{up.MaxLevel})", text, Color.white);
                Label(card.x + 12, card.y + 30, 200, 42, up.Desc, wrap, Muted);

                if (lv >= up.MaxLevel)
                    Label(card.x + 220, card.y + 30, 82, 24, "TỐI ĐA", smallCenter, Teal);
                else
                {
                    int cost = up.CostAt(lv);
                    GUI.enabled = progress.EcoCoins >= cost;
                    if (Click(new Rect(card.x + 218, card.y + 24, 84, 36), $"Mua\n{cost} Xu"))
                    {
                        if (progress.BuyUpgrade(i))
                        {
                            audioSys.Play(SfxId.Coin);
                            if (progress.UnlockAchievement("eco_shopper", out var a))
                                vfx.ShowToast($"THÀNH TÍCH: {a.Title}", $"+{a.RewardCoins} Xu!");
                        }
                    }
                    GUI.enabled = true;
                }
            }
        }

        private void DrawAchievements()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.92f));
            Rect r = new Rect(44, 28, 680, 376);
            Fill(r, Panel);
            Border(r, Teal);
            Label(64, 42, 420, 26, $"★ THÀNH TÍCH ({progress.TotalAchievements()}/{CampaignCatalog.Achievements.Length})", heading, Gold);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            for (int i = 0; i < CampaignCatalog.Achievements.Length; i++)
            {
                var a = CampaignCatalog.Achievements[i];
                bool done = progress.Achievements[i];
                int col = i % 2;
                int row = i / 2;
                Rect card = new Rect(64 + col * 324, 84 + row * 60, 312, 52);
                Fill(card, done ? CardSel : Ink);
                Border(card, done ? Gold : new Color(0.24f, 0.36f, 0.44f, 1f));
                Label(card.x + 10, card.y + 6, 230, 18, (done ? "★ " : "○ ") + a.Title, small, done ? Gold : Color.white);
                Label(card.x + 240, card.y + 6, 64, 18, $"+{a.RewardCoins} Xu", small, Teal);
                Label(card.x + 10, card.y + 24, 294, 24, a.Desc, small, Muted);
            }
        }

        private void DrawGuide()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.93f));
            Fill(new Rect(28, 22, 712, 388), Panel);
            Border(new Rect(28, 22, 712, 388), Teal);
            Label(48, 36, 340, 24, "📖 SỔ SINH THÁI (ALMANAC)", heading, Teal);
            if (Click(new Rect(632, 34, 88, 28), "Đóng")) overlay = Overlay.None;

            string[] tabs = { "20 Cá & Cây", "14 Quái Rác", "10 Loại Rác" };
            for (int i = 0; i < tabs.Length; i++)
                if (Click(new Rect(48 + i * 142, 70, 132, 26), tabs[i]))
                { guideTab = i; guideScroll = 0f; audioSys.Play(SfxId.Click); }

            int count = guideTab == 0 ? CampaignCatalog.Allies.Length
                : guideTab == 1 ? CampaignCatalog.Enemies.Length : CampaignCatalog.Trash.Length;
            Vector2 sc = GUI.BeginScrollView(new Rect(48, 106, 672, 286),
                new Vector2(0, guideScroll), new Rect(0, 0, 648, count * 48));
            guideScroll = sc.y;
            for (int i = 0; i < count; i++)
            {
                string name, meta, hint;
                int art;
                if (guideTab == 0)
                {
                    var a = CampaignCatalog.Allies[i];
                    name = a.Name; meta = $"{HabitatTag(a.Habitat)} · {a.Cost} NL · Màn {a.Unlock}"; hint = a.Hint; art = i;
                }
                else if (guideTab == 1)
                {
                    var e = CampaignCatalog.Enemies[i];
                    name = e.Name; meta = $"{HabitatTag(e.Habitat)} · HP {e.Health} · Màn {e.Unlock}"; hint = e.Hint; art = 20 + i;
                }
                else
                {
                    var t = CampaignCatalog.Trash[i];
                    name = t.Name; meta = $"{HabitatTag(t.Habitat)} · Màn {t.Unlock}"; hint = t.Hint; art = 34 + i;
                }
                int y = i * 48;
                Fill(new Rect(0, y, 644, 44), Ink);
                DrawSprite(art, new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                Label(50, y + 4, 180, 18, name, text, Color.white);
                Label(50, y + 24, 180, 16, meta, small, Gold);
                Label(236, y + 6, 400, 34, hint, wrap, Muted);
            }
            GUI.EndScrollView();
        }

        private void DrawPause()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.78f));
            Rect r = new Rect(244, 114, 280, 204);
            Fill(r, Panel);
            Border(r, Teal);
            Label(244, 132, 280, 26, "TẠM DỪNG", heading, Gold);
            if (Click(new Rect(274, 174, 220, 34), "▶  Tiếp tục (Esc)")) overlay = Overlay.None;
            if (Click(new Rect(274, 216, 220, 34), "↻  Chơi lại màn này")) StartLevel(selectedLevel, match.Endless);
            if (Click(new Rect(274, 258, 220, 34), "Về Bản Đồ Chiến Dịch")) ReturnToMenu();
        }

        // ==================== HELPERS ====================

        private void EnsureStyles()
        {
            if (text != null) return;
            small = MakeStyle(10, false, TextAnchor.UpperLeft);
            smallCenter = MakeStyle(10, true, TextAnchor.MiddleCenter);
            text = MakeStyle(12, true, TextAnchor.UpperLeft);
            heading = MakeStyle(16, true, TextAnchor.UpperLeft);
            title = MakeStyle(28, true, TextAnchor.UpperLeft);
            wrap = MakeStyle(11, false, TextAnchor.UpperLeft);
            wrap.wordWrap = true;
            button = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        private static GUIStyle MakeStyle(int size, bool bold, TextAnchor align) =>
            new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = align,
                padding = new RectOffset(0, 0, 0, 0)
            };

        private static Texture2D BuildCircleTexture(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float c = (sz - 1) * 0.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((c - d) * 1.4f)));
                }
            }
            tex.Apply();
            return tex;
        }

        private void DrawCircle(Vector2 center, float diameter, Color color)
        {
            if (circleTex == null) return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter), circleTex);
        }

        private void DrawSprite(int index, Rect rect, float fps, int offset, Color tint)
        {
            if (atlas == null) return;
            int rows = CampaignCatalog.TotalAtlasRows;
            int clamped = Mathf.Clamp(index, 0, rows - 1);
            int frame = (Mathf.FloorToInt(Time.unscaledTime * fps) + offset) % 4;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(frame * 0.25f, 1f - (clamped + 1f) / rows, 0.25f, 1f / rows));
        }

        private static Rect Around(Vector2 center, float size) => new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        private static string LayerName(Layer l) => l == Layer.Deep ? "ĐÁY BIỂN" : "MẶT NƯỚC";
        private static string HabitatTag(Habitat h) => h == Habitat.Both ? "2 Tầng" : h == Habitat.Deep ? "Đáy" : "Mặt";

        private void Label(float x, float y, float w, float h, string val, GUIStyle style, Color col)
        {
            GUI.color = col;
            GUI.Label(new Rect(x, y, w, h), val, style);
        }

        private bool Click(Rect rect, string val, GUIStyle style = null)
        {
            GUI.color = Color.white;
            return GUI.Button(rect, val, style ?? button);
        }

        private static void Fill(Rect rect, Color col)
        {
            GUI.color = col;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void Bar(Rect rect, float frac, Color col)
        {
            Fill(rect, new Color(0.06f, 0.12f, 0.16f, 0.9f));
            rect.width *= Mathf.Clamp01(frac);
            Fill(rect, col);
        }

        private static void Border(Rect rect, Color col)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1.5f), col);
            Fill(new Rect(rect.x, rect.yMax - 1.5f, rect.width, 1.5f), col);
            Fill(new Rect(rect.x, rect.y, 1.5f, rect.height), col);
            Fill(new Rect(rect.xMax - 1.5f, rect.y, 1.5f, rect.height), col);
        }
    }
}
