using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    [RequireComponent(typeof(TwoLayerBoard))]
    public sealed class BioBotCampaign : MonoBehaviour
    {
        private enum Overlay { None, Briefing, Guide, Shop, Achievements, Pause }

        private const int PageSize = 6;
        private static readonly Color Ink = new Color32(12, 26, 38, 244);
        private static readonly Color Panel = new Color32(19, 43, 60, 248);
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
        private Texture2D pool3DBackdrop;
        private Texture2D circleTex;

        private int selectedLevel = 1;
        private bool pendingEndless;
        private int page, selection;
        private bool shovelMode;
        private bool autoPilot;
        private bool fastForward;
        private bool conceptBoxOpen;
        private int hoveredCardId = -1;
        private Vector2 hoveredCardPos;
        private Overlay overlay = Overlay.None;
        private int guideTab;
        private float guideScroll;
        private float wakeTimer;
        private bool recorded;

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
            pool3DBackdrop = Resources.Load<Texture2D>("BioBot/Pool3DBackdrop");
            if (pool3DBackdrop != null) pool3DBackdrop.filterMode = FilterMode.Point;
            circleTex = BuildCircleTexture(32);
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
            selection = 0; // Starts on Rái Cá (Solar Otter!)
            shovelMode = false;
            autoPilot = false;
            fastForward = false;
            conceptBoxOpen = !endless; // Show clear Concept Text Box when a new stage introduces concepts!
            recorded = false;
            overlay = Overlay.None;

            board.ResetToSurface();
            robot.TerrainSpeed = 0f; // Split-view drives Bio-Bot directly on the 11-column board
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

            Keyboard kb = Keyboard.current;
            if (match == null)
            {
                if (overlay == Overlay.Briefing && kb != null && kb.enterKey.wasPressedThisFrame)
                    StartLevel(selectedLevel, pendingEndless);
                else if (overlay == Overlay.None && kb != null && kb.enterKey.wasPressedThisFrame)
                    OpenBriefing(selectedLevel, false);
                else if (overlay != Overlay.None && kb != null && kb.escapeKey.wasPressedThisFrame)
                    overlay = Overlay.None;
                return;
            }

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                overlay = overlay == Overlay.None ? Overlay.Pause : Overlay.None;
                audioSys.Play(SfxId.Click);
                return;
            }

            if (match.Ended)
            {
                RecordMatchEnd();
                if (kb != null && kb.enterKey.wasPressedThisFrame)
                {
                    bool next = !match.Endless && match.Won && selectedLevel < CampaignCatalog.Levels.Length;
                    StartLevel(next ? selectedLevel + 1 : selectedLevel, match.Endless);
                }
                return;
            }

            if (conceptBoxOpen)
            {
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                { conceptBoxOpen = false; audioSys.Play(SfxId.Click); }
                return;
            }

            if (overlay != Overlay.None) return;

            float simDelta = Mathf.Min(Time.deltaTime, 0.05f) * (fastForward ? 1.85f : 1f);

            // Smooth Bio-Bot WASD / Arrow movement inside Split-View 11x5x2!
            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            }
            if (move.sqrMagnitude > 1f) move.Normalize();
            match.MoveRobot(move, simDelta);
            match.Tick(simDelta);

            if (match.Ended) { RecordMatchEnd(); return; }

            wakeTimer -= simDelta;
            if (wakeTimer <= 0f && match.Respawn <= 0f)
            {
                wakeTimer = match.IsDashing ? 0.06f : 0.28f;
                Vector2 rPx = CampaignMatch.CellToPixel(match.RobotLayer, match.RobotX, match.RobotY);
                vfx.SpawnRing(match.RobotLayer, rPx + new Vector2(0, 6),
                    match.IsDashing ? Cyan : new Color(0.75f, 0.96f, 1f, 0.35f), match.IsDashing ? 24f : 16f);
            }

            if (autoPilot) match.StepAutoPilot(simDelta);
            HandleMatchHotkeys(kb);
        }

        private void HandleMatchHotkeys(Keyboard kb)
        {
            if (kb == null) return;
            var cell = match.RobotCell;
            if (kb.qKey.wasPressedThisFrame)
                match.ToggleRobotLayer();
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
                if (shovelMode) { match.RemoveAlly(match.RobotLayer, cell.y, cell.x); shovelMode = false; }
                else match.Plant(selection, match.RobotLayer, cell.y, cell.x);
            }
            if (kb.fKey.wasPressedThisFrame) match.Collect(match.RobotLayer, cell.y, cell.x);
            if (kb.gKey.wasPressedThisFrame) match.UseTrash(match.RobotLayer, cell.y, cell.x);
            if (kb.rKey.wasPressedThisFrame) match.RobotPulse();
            if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                match.TriggerHydroDash(match.RobotFacingLeft ? -1 : 1);
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
                HandleSplitBoardMouse();
                Draw3DPoolBackdrop();
                // Draw Deep face first, then Cross-Layer Beams, then Surface face for true 3D layering!
                Draw3DFaceEntities(Layer.Deep);
                DrawCrossLayerBeams();
                Draw3DFaceEntities(Layer.Surface);
                DrawBioBotInSplitView();
                DrawOrbsAndVfx();
                DrawTopSeedBank();
                DrawRightPanel();
                DrawBottomDock();
                DrawInteractiveTutorial();
                DrawHoverTooltips();

                if (conceptBoxOpen) DrawConceptTextBox();
                if (vfx.WaveBannerTime > 0f) DrawWaveBanner();
                if (match.Ended) DrawResult();
                else if (overlay == Overlay.Pause) DrawPause();
                else if (overlay == Overlay.Guide) DrawGuide();
            }

            if (vfx.ToastTime > 0f) DrawAchievementToast();

            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        // Strict Active-Layer Interaction: can only plant/interact on Bio-Bot's active 3D face!
        private void HandleSplitBoardMouse()
        {
            if (conceptBoxOpen || overlay != Overlay.None || match.Ended) return;
            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown) return;
            Vector2 pos = e.mousePosition;

            if (e.button == 0 && match.CollectOrbAt(pos)) { e.Use(); return; }

            if (CampaignMatch.TryPixelToCell(pos, match.DeepEnabled, out Layer layer, out int col, out int row))
            {
                if (layer != match.RobotLayer)
                {
                    match.CanPlant(selection, layer, row, col, out _); // triggers helpful "Bấm Q để đổi tầng!" message
                    audioSys.Play(SfxId.Click);
                    e.Use();
                    return;
                }
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

        // ==================== 3D OBLIQUE GLASS-TANK POOL RENDERER ====================

        private void Draw3DPoolBackdrop()
        {
            float t = Time.unscaledTime;
            if (pool3DBackdrop != null) GUI.DrawTexture(new Rect(0, 0, 768, 432), pool3DBackdrop);
            else Fill(new Rect(0, 0, 768, 432), new Color32(10, 26, 40, 255));

            // Animated sunlit ripples on Top Face
            for (int i = 0; i < 10; i++)
            {
                int r = i % CampaignMatch.Rows;
                float c = ((i * 2.3f + t * 0.65f) % (CampaignMatch.Columns - 1));
                Vector2 rp = CampaignMatch.CellToPixel(Layer.Surface, c, r);
                Fill(new Rect(rp.x - 8, rp.y, 16, 1.5f), new Color(0.88f, 1f, 0.98f, 0.28f));
            }
            // Animated rising bubbles in Bottom Face
            for (int i = 0; i < 12; i++)
            {
                float bx = 65f + (i * 44f) % 520f + Mathf.Sin(t * 2f + i) * 4f;
                float by = 344f - ((t * 22f + i * 27f) % 116f);
                DrawCircle(new Vector2(bx, by), 3.5f, new Color(0.65f, 0.95f, 1f, 0.3f));
            }

            if (match.IsEcoSurge)
            {
                float glow = 0.08f + 0.05f * Mathf.Sin(t * 7f);
                Fill(new Rect(20, 76, 616, 280), new Color(0.45f, 1f, 0.88f, glow));
            }

            // Frosted blur haze over the INACTIVE face!
            if (!match.DeepEnabled)
            {
                Fill(new Rect(18, 224, 608, 128), new Color(0.03f, 0.08f, 0.14f, 0.72f));
                Label(60, 276, 520, 24, "🌊 ĐÁY BIỂN Ô NHIỄM (Mở khóa từ Màn 2 — Bấm Q để lặn!)", smallCenter, Cyan);
            }
            else if (match.RobotLayer == Layer.Surface)
            {
                // On Surface -> Bottom Face is frosted/hazy underwater view!
                Fill(new Rect(18, 224, 608, 128), new Color(0.04f, 0.14f, 0.24f, 0.46f));
            }
            else
            {
                // On Deep -> Top Face is frosted sunlit refraction view!
                Fill(new Rect(36, 80, 600, 126), new Color(0.14f, 0.42f, 0.48f, 0.44f));
            }

            // Active / Blurred Face Corner Badges
            bool onSurf = match.RobotLayer == Layer.Surface;
            Fill(new Rect(104, 84, 126, 15), new Color(0.05f, 0.14f, 0.2f, 0.85f));
            Label(106, 84, 122, 15, onSurf ? "☀ MẶT NƯỚC (ĐANG Ở)" : "☀ MẶT NƯỚC (Mờ · Q nổi)", small, onSurf ? Gold : Muted);

            Fill(new Rect(80, 228, 146, 15), new Color(0.05f, 0.12f, 0.22f, 0.85f));
            Label(82, 228, 142, 15, !onSurf ? "🌊 ĐÁY BIỂN (+40% HP · ĐANG Ở)" : "🌊 ĐÁY BIỂN (Mờ · Q lặn)", small, !onSurf ? Cyan : Muted);
        }

        private void DrawCrossLayerBeams()
        {
            if (!match.DeepEnabled) return;
            float t = Time.unscaledTime;
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    if (match.HasUpdraftAt(r, c))
                    {
                        Vector2 topP = CampaignMatch.CellToPixel(Layer.Surface, c, r);
                        Vector2 botP = CampaignMatch.CellToPixel(Layer.Deep, c, r);
                        float a = 0.24f + 0.14f * Mathf.Sin(t * 6f + c);
                        // Slanted bubble beam connecting Deep(c,r) up to Surface(c,r)
                        for (int s = 0; s < 6; s++)
                        {
                            float frac = ((s / 6f) + t * 0.45f) % 1f;
                            Vector2 bp = Vector2.Lerp(botP, topP, frac);
                            DrawCircle(bp, 5f, new Color(0.55f, 0.98f, 1f, a));
                        }
                    }
                }
            }
        }

        private void Draw3DFaceEntities(Layer layer)
        {
            float t = Time.unscaledTime;
            int side = (int)layer;
            bool isActive = layer == match.RobotLayer;

            // Spinning Hydro-Turbines along slanted left dam edge
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                if (match.TurbineReady[side, r])
                {
                    Vector2 tp = CampaignMatch.CellToPixel(layer, -0.35f, r);
                    Rect tr = new Rect(tp.x - 8, tp.y - 9, 16, 18);
                    Fill(tr, new Color(0.2f, 0.8f, 0.86f, isActive ? 0.92f : 0.35f));
                    Border(tr, new Color(1, 1, 1, isActive ? 0.95f : 0.3f));
                    if (isActive)
                    {
                        float spin = Mathf.Sin(t * 10f + r) * 3.5f;
                        Fill(new Rect(tr.x + 2, tr.center.y - 1.5f + spin, 12, 3), Color.white);
                    }
                }
            }

            // Hover Ghost Preview (ONLY crisp on Active Face!)
            if (isActive && !conceptBoxOpen && overlay == Overlay.None && Event.current != null &&
                CampaignMatch.TryPixelToCell(Event.current.mousePosition, match.DeepEnabled, out Layer hLayer, out int hc, out int hr) &&
                hLayer == layer)
            {
                Vector2 hp = CampaignMatch.CellToPixel(layer, hc, hr);
                Rect hrRect = new Rect(hp.x - CampaignMatch.CellW * 0.48f, hp.y - CampaignMatch.CellH * 0.48f,
                    CampaignMatch.CellW * 0.96f, CampaignMatch.CellH * 0.96f);
                if (shovelMode)
                {
                    Fill(hrRect, new Color(1f, 0.35f, 0.35f, 0.28f));
                    Border(hrRect, Danger);
                }
                else
                {
                    bool valid = match.CanPlant(selection, layer, hr, hc, out _);
                    Fill(hrRect, valid ? new Color(0.45f, 1f, 0.72f, 0.24f) : new Color(1f, 0.4f, 0.35f, 0.2f));
                    Border(hrRect, valid ? Green : Danger);
                    if (valid)
                        DrawAllySprite(selection, Around(hp, 26), 5, 0, new Color(1, 1, 1, 0.55f));
                }
            }

            // Trash & Allies (Clear on active face, softly blurred 3-tap haze on inactive face!)
            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    Vector2 p = CampaignMatch.CellToPixel(layer, c, r);
                    var waste = match.Trash[side, r, c];
                    if (waste != null)
                    {
                        float bobY = Mathf.Sin(t * 3.6f + waste.BobPhase) * 1.6f;
                        if (isActive)
                        {
                            if (waste.Active)
                                Border(new Rect(p.x - 15, p.y - 11, 30, 22), new Color(0.45f, 1f, 0.92f, 0.85f));
                            DrawSprite(34 + waste.Spec, Around(p + new Vector2(0, bobY), 24), 4, c + r, Color.white);
                            if (CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Filament)
                                Label(p.x - 16, p.y - 15 + bobY, 32, 12, "CỨU!", smallCenter, Cyan);
                        }
                        else
                        {
                            DrawBlurredSprite(34 + waste.Spec, Around(p + new Vector2(0, bobY), 24), 4, c + r, new Color(0.65f, 0.82f, 0.92f, 0.32f));
                        }
                    }

                    var ally = match.Allies[side, r, c];
                    if (ally != null)
                    {
                        float breathe = Mathf.Sin(t * 4.2f + ally.Phase);
                        float sx = 1f + breathe * 0.06f, sy = 1f - breathe * 0.06f, ox = 0f;
                        if (ally.PlantPop > 0f)
                        {
                            float pt = 1f - ally.PlantPop / 0.34f;
                            float el = 1f + Mathf.Sin(pt * Mathf.PI * 2f) * (1f - pt) * 0.35f;
                            sx *= el; sy *= (2f - el);
                        }
                        if (ally.FireAnim > 0f)
                        {
                            float ft = ally.FireAnim / 0.24f;
                            sx *= 1f + ft * 0.22f; sy *= 1f - ft * 0.14f; ox += ft * 4f;
                            if (isActive && ft > 0.65f) DrawCircle(p + new Vector2(13, -1), 9f * ft, new Color(0.85f, 1f, 0.85f, 0.75f));
                        }
                        if (ally.HurtAnim > 0f) ox -= Mathf.Sin(ally.HurtAnim * 38f) * 3f;

                        float w = 28f * sx, h = 28f * sy;
                        Rect ar = new Rect(p.x - w * 0.5f + ox, p.y - h * 0.5f, w, h);
                        if (isActive)
                        {
                            Color tint = ally.HurtAnim > 0f ? new Color(1f, 0.72f, 0.72f, 1f)
                                : ally.Draining ? new Color(0.76f, 0.88f, 1f, 1f)
                                : ally.Jammed > 0f ? new Color(0.75f, 0.65f, 1f, 1f) : Color.white;
                            DrawAllySprite(ally.Spec, ar, ally.FireAnim > 0f ? 12 : 5, c + r, tint);
                            if (ally.Draining)
                                Label(p.x - 16, p.y - 17, 32, 11, "O₂↓", smallCenter, new Color(1f, 0.72f, 0.35f, 1f));
                            if (ally.Health < ally.MaxHealth)
                                Bar(new Rect(p.x - 11, p.y + 9, 22, 3), ally.Health / (float)ally.MaxHealth, ally.Draining ? Gold : Green);
                        }
                        else
                        {
                            int sRow = ally.Spec < CampaignCatalog.AllySpriteRow.Length ? CampaignCatalog.AllySpriteRow[ally.Spec] : ally.Spec;
                            DrawBlurredSprite(sRow, ar, 4, c + r, new Color(0.6f, 0.85f, 0.92f, 0.34f));
                        }
                    }
                }
            }

            // Sliding Bowling Bottles
            foreach (var sb in match.SlidingBottles)
            {
                if (sb.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(layer, sb.X, sb.Row);
                if (isActive)
                {
                    DrawCircle(p - new Vector2(Mathf.Sign(sb.VelX) * 9, 0), 9f, new Color(0.75f, 0.98f, 1f, 0.45f));
                    DrawSprite(34, Around(p, 26), 12, 0, Color.white);
                }
                else DrawBlurredSprite(34, Around(p, 26), 8, 0, new Color(0.7f, 0.9f, 1f, 0.32f));
            }

            // Turbine Waves & Beams
            foreach (var tw in match.Turbines)
            {
                if (tw.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(layer, tw.X, tw.Row);
                Fill(new Rect(p.x - 15, p.y - 11, 26, 22), new Color(0.35f, 0.92f, 1f, isActive ? 0.88f : 0.35f));
            }
            foreach (var b in match.Beams)
            {
                if (b.Layer != layer) continue;
                Vector2 a = CampaignMatch.CellToPixel(layer, b.From, b.Row);
                Vector2 c = CampaignMatch.CellToPixel(layer, b.To, b.Row);
                Color col = b.Color; col.a = Mathf.Clamp01(b.Left * 5f) * (isActive ? 1f : 0.35f);
                Fill(new Rect(Mathf.Min(a.x, c.x), a.y - 3, Mathf.Max(8f, Mathf.Abs(a.x - c.x)), 6), col);
            }

            // Projectiles
            foreach (var proj in match.Projectiles)
            {
                if (proj.Layer != layer) continue;
                Vector2 p = CampaignMatch.CellToPixel(layer, proj.X, proj.Row);
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
                pc.a *= isActive ? 1f : 0.32f;
                DrawCircle(p, proj.Kind == ProjectileKind.Geyser ? 10f : 7.5f, pc);
                if (isActive) DrawCircle(p - new Vector2(1.5f, 1.5f), 2.8f, Color.white);
            }

            // Enemies (Clear & vivid on active face; hazy blurred silhouettes on inactive face!)
            foreach (var e in match.Enemies)
            {
                if (e.Layer != layer) continue;
                var eSpec = CampaignCatalog.Enemies[e.Spec];
                Vector2 p = CampaignMatch.CellToPixel(layer, Mathf.Clamp(e.X, -0.3f, CampaignMatch.Columns - 0.1f), e.Row);
                float baseSz = eSpec.Trait == Trait.Boss ? 46f : (e.Small ? 21f : (layer == Layer.Deep ? 32f : 29f));
                float waddle = Mathf.Abs(Mathf.Sin(e.WalkPhase));
                float sx = 1f + (0.5f - waddle) * 0.08f, sy = 1f + (waddle - 0.5f) * 0.08f;
                float ox = 0f, oy = -waddle * 2.2f;
                if (e.BiteAnim > 0f) ox -= Mathf.Sin((e.BiteAnim / 0.25f) * Mathf.PI) * 5f;
                if (e.HurtAnim > 0f) { float ht = e.HurtAnim / 0.16f; ox += ht * 4f; sx *= 1f - ht * 0.16f; }
                Rect er = new Rect(p.x - baseSz * sx * 0.5f + ox, p.y - baseSz * sy * 0.5f + oy, baseSz * sx, baseSz * sy);

                if (isActive)
                {
                    if (layer == Layer.Deep) DrawCircle(p, baseSz * 0.85f, new Color(0.55f, 0.18f, 0.65f, 0.22f)); // Contaminated aura!
                    Color tint = e.Flash > 0f ? new Color(1f, 0.55f, 0.45f, 1f)
                        : e.Slow > 0f ? new Color(0.62f, 0.88f, 1f, 1f) : Color.white;
                    DrawSprite(20 + e.Spec, er, e.Slow > 0f ? 4 : 8, e.Row, tint);
                    float bw = eSpec.Trait == Trait.Boss ? 40f : 22f;
                    Bar(new Rect(p.x - bw * 0.5f, p.y - baseSz * 0.54f, bw, 3), e.Health / (float)e.MaxHealth, Danger);
                }
                else
                {
                    DrawBlurredSprite(20 + e.Spec, er, 5, e.Row, new Color(0.85f, 0.45f, 0.52f, 0.38f));
                }
            }
        }

        private void DrawBlurredSprite(int index, Rect rect, float fps, int offset, Color tint)
        {
            // 3-tap soft horizontal/vertical offset to visually blur the inactive 3D face!
            Color c = tint; c.a *= 0.65f;
            DrawSprite(index, new Rect(rect.x - 2.5f, rect.y - 1f, rect.width + 2f, rect.height + 1f), fps, offset, c);
            DrawSprite(index, new Rect(rect.x + 2.5f, rect.y + 1f, rect.width + 2f, rect.height + 1f), fps, offset, c);
            DrawSprite(index, rect, fps, offset, tint);
        }

        private void DrawBioBotInSplitView()
        {
            if (match.Respawn > 0f) return;
            Vector2 p = CampaignMatch.CellToPixel(match.RobotLayer, match.RobotX, match.RobotY);
            float bob = Mathf.Sin(Time.unscaledTime * (match.RobotMoving ? 10f : 4f)) * 2f;
            // Friendly glowing Bio-Bot submarine drone
            DrawCircle(p + new Vector2(0, bob), match.IsDashing ? 28f : 22f, new Color(1f, 0.82f, 0.35f, 0.25f));
            Fill(new Rect(p.x - 10, p.y - 8 + bob, 20, 15), new Color32(246, 178, 86, 255));
            Border(new Rect(p.x - 10, p.y - 8 + bob, 20, 15), Ink);
            float visorX = match.RobotFacingLeft ? p.x - 8 : p.x + 1;
            Fill(new Rect(visorX, p.y - 5 + bob, 7, 7), new Color32(84, 242, 232, 255));
            Fill(new Rect(p.x - 1, p.y - 12 + bob, 2, 4), Gold);
            Label(p.x - 16, p.y + 8 + bob, 32, 11, "BOT", smallCenter, Gold);
        }

        private void DrawOrbsAndVfx()
        {
            float t = Time.unscaledTime;
            foreach (var orb in match.Orbs)
            {
                float bob = orb.FlyingToBank ? 0f : Mathf.Sin(orb.Bob) * 2.5f;
                Vector2 p = orb.Pos + new Vector2(0, bob);
                DrawCircle(p, 18f + Mathf.Sin(t * 7f + orb.Bob) * 2.5f, new Color(1f, 0.88f, 0.35f, 0.38f));
                DrawCircle(p, 11f, Gold);
                DrawCircle(p - new Vector2(2.5f, 2.5f), 4f, Color.white);
            }
            foreach (var p in vfx.Particles)
            {
                float frac = Mathf.Clamp01(p.Life / p.MaxLife);
                Color c = p.Color; c.a *= frac;
                if (p.Ring)
                {
                    float r = p.Size * (1f - frac * 0.65f);
                    Border(new Rect(p.Pos.x - r * 0.5f, p.Pos.y - r * 0.5f, r, r), c);
                }
                else DrawCircle(p.Pos, p.Size * 1.8f, c);
            }
            foreach (var ft in vfx.Texts)
            {
                Color c = ft.Color; c.a = Mathf.Clamp01(ft.Life / ft.MaxLife);
                Label(ft.Pos.x - 48, ft.Pos.y - 7, 96, 16, ft.Text, smallCenter, c);
            }

            var boss = match.ActiveBoss;
            if (boss != null)
            {
                Rect br = new Rect(170, 74, 336, 17);
                Fill(br, Ink); Border(br, Danger);
                Bar(new Rect(br.x + 2, br.y + 2, br.width - 4, br.height - 4), boss.Health / (float)boss.MaxHealth, Danger);
                Label(br.x, br.y + 1, br.width, 15, $"SIÊU BOSS ({LayerName(boss.Layer)})  {boss.Health}/{boss.MaxHealth}", smallCenter, Color.white);
            }
        }

        // ==================== TOP SEED BANK WITH CLEAR ROLE TAGS ====================

        private void DrawTopSeedBank()
        {
            Fill(new Rect(6, 4, 756, 68), Ink);
            Border(new Rect(6, 4, 756, 68), match.IsEcoSurge ? Gold : new Color(0.28f, 0.58f, 0.65f, 1f));

            float bScale = match.EnergyBounce > 0f ? 1f + Mathf.Sin(match.EnergyBounce * 14f) * 0.2f : 1f;
            Rect sunBox = new Rect(12, 9, 76, 58);
            Fill(sunBox, new Color32(36, 62, 54, 255));
            Border(sunBox, Gold);
            DrawCircle(new Vector2(50, 26), 15f * bScale, Gold);
            DrawCircle(new Vector2(47, 23), 5.5f * bScale, Color.white);
            Label(12, 43, 76, 20, $"{match.Energy} NL", smallCenter, Gold);

            int totalPages = PageCount();
            if (totalPages > 1 && Click(new Rect(92, 14, 22, 48), "◄")) ChangePage(-1);

            int unlockedCount = UnlockedAllyCount();
            hoveredCardId = -1;
            Vector2 mPos = Event.current != null ? Event.current.mousePosition : new Vector2(-999, -999);
            for (int slot = 0; slot < PageSize; slot++)
            {
                int id = page * PageSize + slot;
                Rect card = new Rect(118 + slot * 85, 8, 82, 60);
                if (id >= unlockedCount)
                {
                    Fill(card, new Color(0.08f, 0.14f, 0.2f, 0.65f));
                    Border(card, new Color(0.2f, 0.3f, 0.38f, 0.45f));
                    continue;
                }

                var ally = CampaignCatalog.Allies[id];
                bool afford = match.Energy >= ally.Cost;
                float cd = match.CardCooldowns[id];
                bool sel = !shovelMode && selection == id;

                Fill(card, sel ? CardSel : (afford ? CardBg : new Color32(20, 34, 46, 255)));
                Border(card, sel ? Gold : (afford ? Teal : new Color(0.3f, 0.4f, 0.46f, 0.7f)));

                DrawAllySprite(id, new Rect(card.x + 2, card.y + 5, 28, 28), 4, id, afford ? Color.white : new Color(0.6f, 0.65f, 0.7f, 0.7f));
                Label(card.x + 31, card.y + 4, 49, 13, ally.Name, small, Color.white);
                Label(card.x + 31, card.y + 18, 49, 13, $"{ally.Cost} NL", small, afford ? Gold : Danger);
                // Crystal-clear Role Tag right on the card face!
                Fill(new Rect(card.x + 2, card.y + 35, 78, 22), new Color(0.06f, 0.13f, 0.19f, 0.85f));
                Label(card.x + 2, card.y + 36, 78, 12, ally.ShortTag, smallCenter, ally.Ability == Ability.Income ? Gold : Teal);
                Label(card.x + 2, card.y + 47, 78, 11, HabitatTag(ally.Habitat), smallCenter, ally.Habitat == Habitat.Deep ? Cyan : ally.Habitat == Habitat.Surface ? Gold : Green);

                if (cd > 0f)
                {
                    float frac = Mathf.Clamp01(cd / Mathf.Max(0.1f, ally.Recharge));
                    Fill(new Rect(card.x, card.y, card.width, card.height * frac), new Color(0f, 0f, 0f, 0.62f));
                }

                if (overlay == Overlay.None && card.Contains(mPos))
                {
                    hoveredCardId = id;
                    hoveredCardPos = new Vector2(card.x, card.yMax + 4);
                }

                if (Click(card, "", GUIStyle.none)) SelectSlot(slot);
            }

            if (totalPages > 1 && Click(new Rect(630, 14, 22, 48), "►")) ChangePage(1);

            // Clean right utility cluster
            Fill(new Rect(656, 8, 100, 60), Panel);
            if (Click(new Rect(660, 11, 44, 25), shovelMode ? "Hủy" : "Xẻng")) { shovelMode = !shovelMode; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(708, 11, 44, 25), fastForward ? "2x»" : "1x")) { fastForward = !fastForward; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(660, 40, 44, 25), autoPilot ? "AI✓" : "AI")) { autoPilot = !autoPilot; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(708, 40, 44, 25), "Menu")) { overlay = Overlay.Pause; audioSys.Play(SfxId.Click); }
        }

        // ==================== RIGHT COMPACT STATUS & ACTION PANEL ====================

        private void DrawRightPanel()
        {
            Rect panel = new Rect(652, 76, 110, 284);
            Fill(panel, Ink);
            Border(panel, match.IsEcoSurge ? Gold : Teal);

            Label(658, 82, 98, 14, $"CỬA ĐÊ {match.Integrity}/{CampaignMatch.MaxIntegrity}", small, Teal);
            Bar(new Rect(658, 97, 98, 7), match.Integrity / (float)CampaignMatch.MaxIntegrity, Teal);

            Label(658, 110, 98, 14, $"BOT HP {match.RobotHealth}/{match.MaxRobotHealth}", small, match.RobotHealth <= 1 ? Danger : Green);
            Bar(new Rect(658, 125, 98, 7), match.RobotHealth / (float)match.MaxRobotHealth, match.RobotHealth <= 1 ? Danger : Green);

            Label(658, 138, 98, 14, match.IsEcoSurge ? $"✦ XANH {Mathf.CeilToInt(match.EcoSurgeTime)}s" : $"TRONG {Mathf.RoundToInt(match.WaterClarity)}%", small, match.IsEcoSurge ? Gold : Cyan);
            Bar(new Rect(658, 153, 98, 7), match.WaterClarity / 100f, match.IsEcoSurge ? Gold : Cyan);

            if (match.DeepEnabled)
            {
                if (Click(new Rect(658, 168, 98, 26), match.RobotLayer == Layer.Deep ? "▲ Q: Nổi Lên" : "▼ Q: Lặn Xuống"))
                    match.ToggleRobotLayer();
            }

            if (selectedLevel >= 2 || match.Endless)
            {
                string dashTxt = match.DashCooldownLeft > 0f ? $"Shift ({match.DashCooldownLeft:0.0}s)" : "Shift: Lướt";
                if (Click(new Rect(658, 200, 98, 25), dashTxt))
                    match.TriggerHydroDash(match.RobotFacingLeft ? -1 : 1);

                string pulseTxt = match.PulseCooldownLeft > 0f ? $"R: Xung ({Mathf.CeilToInt(match.PulseCooldownLeft)}s)" : "R: Xung (15)";
                if (Click(new Rect(658, 230, 98, 25), pulseTxt))
                    match.RobotPulse();
            }

            if (match.ComboCount >= 2)
                Label(658, 262, 98, 16, $"COMBO x{match.ComboCount}!", smallCenter, Gold);

            Label(658, 282, 98, 14, $"Điểm: {match.Score}", small, Color.white);
            Label(658, 297, 98, 14, $"Xu: +{match.CoinsEarned}", small, Gold);

            if (Click(new Rect(658, 322, 98, 28), "📖 Cách Chơi"))
            { guideTab = 3; overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }
        }

        // ==================== BOTTOM DOCK ====================

        private void DrawBottomDock()
        {
            Fill(new Rect(6, 364, 756, 62), Ink);
            Border(new Rect(6, 364, 756, 62), new Color(0.24f, 0.5f, 0.58f, 1f));

            Label(16, 370, 320, 16, match.Endless
                ? $"SINH TỒN VÔ TẬN · ĐỢT {match.Wave}"
                : $"Màn {selectedLevel:00}: {match.Rules.Name}  ·  Đợt {match.Wave}/{match.TotalWaves}", small, Color.white);
            Rect bar = new Rect(16, 388, 560, 10);
            Bar(bar, match.WaveProgress, Green);
            Border(bar, Color.white);
            int flags = Mathf.Max(1, match.TotalWaves);
            for (int f = 1; f <= flags; f++)
            {
                float fx = bar.x + (bar.width * f / flags) - 3;
                Fill(new Rect(fx, bar.y - 4, 2, 14), Gold);
                Fill(new Rect(fx + 2, bar.y - 4, 6, 5), Danger);
            }

            if (Click(new Rect(590, 371, 78, 28), audioSys.Muted ? "Âm: Tắt" : "Âm: Bật"))
            { audioSys.Muted = !audioSys.Muted; progress.Muted = audioSys.Muted; progress.Save(); }
            if (Click(new Rect(674, 371, 80, 28), "Sổ Sinh Thái"))
            { guideTab = 0; overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }

            string tip = match.Feedback > 0f
                ? match.Message
                : $"Đang chọn [{CampaignCatalog.Allies[selection].Name}]: {CampaignCatalog.Allies[selection].Hint}";
            Label(16, 405, 736, 18, tip, small, match.Feedback > 0f ? Teal : Muted);
        }

        // ==================== INTERACTIVE STEP-BY-STEP COACH & TOOLTIPS ====================

        private void DrawInteractiveTutorial()
        {
            if (conceptBoxOpen || overlay != Overlay.None || match.Ended || match.Endless) return;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f);
            Color hi = new Color(1f, 0.88f, 0.32f, pulse);
            string guide = null;

            if (selectedLevel == 1)
            {
                if (match.PlantedIncomeCount == 0)
                {
                    guide = "BƯỚC 1/3: Click thẻ [Rái Cá ☀+25 NL] rồi click ô sáng cột trái để tự tạo Năng Lượng!";
                    if (page == 0) Border(new Rect(118, 8, 82, 60), hi);
                    Vector2 cp = CampaignMatch.CellToPixel(Layer.Surface, 0, 2);
                    Border(new Rect(cp.x - 24, cp.y - 12, 48, 24), hi);
                }
                else if (match.PlantedFighterCount == 0)
                {
                    guide = "BƯỚC 2/3: Click thẻ [Vịt Bắn Bọt ●Bắn] rồi click ô sáng hàng giữa để bắn quái rác!";
                    if (page == 0) Border(new Rect(118 + 85, 8, 82, 60), hi);
                    Vector2 cp = CampaignMatch.CellToPixel(Layer.Surface, 1, 2);
                    Border(new Rect(cp.x - 24, cp.y - 12, 48, 24), hi);
                }
                else if (match.OrbsCollectedCount == 0 && match.Orbs.Count > 0)
                {
                    guide = "BƯỚC 3/3: Click vào Hạt Vàng đang rơi (hoặc bơi Bio-Bot lại gần) để thu +25 NL!";
                    foreach (var o in match.Orbs)
                        if (!o.FlyingToBank && o.Layer == match.RobotLayer) { DrawCircle(o.Pos, 28f, new Color(1f, 0.9f, 0.3f, pulse * 0.5f)); break; }
                }
            }
            else if (selectedLevel == 3 && match.FishRescued == 0)
            {
                if (match.RobotLayer == Layer.Surface)
                {
                    guide = "MÀN 3 (1/2): Bấm phím Q để LẶN xuống Đáy Biển (tầng dưới đang mờ)!";
                    Border(new Rect(656, 166, 102, 30), hi);
                }
                else
                {
                    guide = "MÀN 3 (2/2): Click trực tiếp vào ô Dây Cước để CỨU Cá Xạ Thủ miễn phí!";
                    for (int r = 0; r < CampaignMatch.Rows; r++)
                        for (int c = 0; c < CampaignMatch.Columns; c++)
                            if (match.Trash[1, r, c] != null && CampaignCatalog.Trash[match.Trash[1, r, c].Spec].Effect == TrashEffect.Filament)
                            {
                                Vector2 cp = CampaignMatch.CellToPixel(Layer.Deep, c, r);
                                Border(new Rect(cp.x - 24, cp.y - 12, 48, 24), Cyan);
                                r = 99; break;
                            }
                }
            }

            if (guide != null)
            {
                Rect box = new Rect(114, 72, 530, 19);
                Fill(box, new Color(0.06f, 0.14f, 0.22f, 0.94f));
                Border(box, Gold);
                Label(box.x + 8, box.y + 2, box.width - 16, 15, "💡 " + guide, small, Gold);
            }
        }

        private void DrawConceptTextBox()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.05f, 0.09f, 0.72f));
            Rect box = new Rect(144, 114, 480, 204);
            Fill(box, Panel);
            Border(box, Gold);
            var spec = match.Rules;
            Label(164, 128, 440, 24, "📘 " + spec.ConceptTitle, heading, Gold);
            Label(164, 158, 440, 102, spec.ConceptBody, wrap, Color.white);
            if (Click(new Rect(264, 270, 240, 36), "▶  ĐÃ HIỂU! CHƠI NGAY (Enter/Space)"))
            { conceptBoxOpen = false; audioSys.Play(SfxId.Click); }
        }

        private void DrawHoverTooltips()
        {
            if (overlay != Overlay.None || match.Ended) return;
            if (hoveredCardId >= 0 && hoveredCardId < CampaignCatalog.Allies.Length)
            {
                var a = CampaignCatalog.Allies[hoveredCardId];
                float tx = Mathf.Clamp(hoveredCardPos.x, 12f, 510f);
                Rect tip = new Rect(tx, hoveredCardPos.y, 248, 56);
                Fill(tip, new Color(0.06f, 0.13f, 0.2f, 0.96f));
                Border(tip, Teal);
                string habFull = a.Habitat == Habitat.Both ? "Đặt được ở Cả 2 Tầng" : a.Habitat == Habitat.Deep ? "Đặt ở Đáy Biển (nửa dưới)" : "Đặt ở Mặt Nước (nửa trên)";
                Label(tip.x + 8, tip.y + 4, 232, 16, $"{a.Name} ({a.ShortTag}) · {a.Cost} NL", small, Gold);
                Label(tip.x + 8, tip.y + 18, 232, 14, habFull, small, a.Habitat == Habitat.Deep ? Cyan : Teal);
                Label(tip.x + 8, tip.y + 32, 232, 22, a.Hint, small, Color.white);
                return;
            }

            Vector2 mPos = Event.current != null ? Event.current.mousePosition : new Vector2(-999, -999);
            if (CampaignMatch.TryPixelToCell(mPos, match.DeepEnabled, out Layer l, out int hc, out int hr))
            {
                var w = match.Trash[(int)l, hr, hc];
                if (w != null)
                {
                    var spec = CampaignCatalog.Trash[w.Spec];
                    Vector2 p = CampaignMatch.CellToPixel(l, hc, hr);
                    Rect box = new Rect(Mathf.Clamp(p.x - 105, 16, 420), Mathf.Max(74, p.y - 38), 210, 30);
                    Fill(box, new Color(0.06f, 0.13f, 0.2f, 0.96f));
                    Border(box, Gold);
                    Label(box.x + 6, box.y + 2, 198, 14, spec.Name, smallCenter, Gold);
                    Label(box.x + 6, box.y + 15, 198, 14, spec.QuickAction, smallCenter, Teal);
                }
            }
        }

        // ==================== BANNERS, MENU & OVERLAYS ====================

        private void DrawWaveBanner()
        {
            float alpha = Mathf.Clamp01(vfx.WaveBannerTime * 1.2f);
            Rect r = new Rect(114, 186, 540, 52);
            Fill(r, new Color(0.06f, 0.12f, 0.18f, 0.92f * alpha));
            Border(r, new Color(1f, 0.55f, 0.35f, alpha));
            Label(r.x, r.y + 14, r.width, 28, vfx.WaveBannerText, heading, new Color(1f, 0.86f, 0.4f, alpha));
        }

        private void DrawAchievementToast()
        {
            Rect r = new Rect(478, 12, 278, 52);
            Fill(r, new Color(0.08f, 0.18f, 0.24f, 0.95f));
            Border(r, Gold);
            Label(r.x + 12, r.y + 7, r.width - 20, 18, "★ " + vfx.ToastTitle, small, Gold);
            Label(r.x + 12, r.y + 26, r.width - 20, 18, vfx.ToastSub, small, Teal);
        }

        private void DrawMenu()
        {
            if (menuBackdrop != null) GUI.DrawTexture(new Rect(0, 0, 768, 432), menuBackdrop);
            else Fill(new Rect(0, 0, 768, 432), Ink);
            Fill(new Rect(0, 0, 768, 432), new Color(0.03f, 0.08f, 0.14f, 0.48f));

            float t = Time.unscaledTime;
            for (int i = 0; i < 14; i++)
            {
                float bx = 30f + (i * 57f) % 708f + Mathf.Sin(t * 1.8f + i) * 8f;
                float by = 420f - ((t * (18f + (i % 3) * 8f) + i * 43f) % 240f);
                DrawCircle(new Vector2(bx, by), 5f + (i % 3) * 2f, new Color(0.7f, 0.96f, 1f, 0.3f));
            }

            Label(28, 16, 460, 40, "BIO-BOT: ĐÊ XANH CẢM TỬ", title, Color.white);
            Label(30, 56, 460, 18, "Mặt Cắt 2 Tầng Sống Động (5×11×2) · 20 Sinh Vật Biển Bảo Vệ Đại Dương", small, Teal);

            Fill(new Rect(506, 18, 236, 44), Ink);
            Border(new Rect(506, 18, 236, 44), Gold);
            Label(518, 24, 108, 16, $"★ {progress.TotalStars()}/{CampaignCatalog.Levels.Length * 3} Sao", small, Gold);
            Label(518, 41, 108, 16, $"● {progress.EcoCoins} Xu", small, Teal);
            if (Click(new Rect(636, 26, 98, 26), audioSys.Muted ? "Âm: Tắt" : "Âm: Bật"))
            { audioSys.Muted = !audioSys.Muted; progress.Muted = audioSys.Muted; progress.Save(); }

            Label(28, 84, 360, 18, "CHIẾN DỊCH 10 MÀN · 3 CHƯƠNG ĐẠI DƯƠNG", text, Gold);
            for (int i = 0; i < CampaignCatalog.Levels.Length; i++)
            {
                int lv = i + 1;
                int gx = i % 2, gy = i / 2;
                Rect card = new Rect(28 + gx * 194, 106 + gy * 51, 186, 45);
                bool unlocked = lv <= progress.UnlockedLevel;
                bool chosen = selectedLevel == lv;
                Fill(card, chosen ? CardSel : (unlocked ? Panel : new Color(0.08f, 0.13f, 0.18f, 0.85f)));
                Border(card, chosen ? Gold : (unlocked ? Teal : new Color(0.25f, 0.32f, 0.38f, 0.7f)));
                int stars = progress.Stars[i];
                string starStr = unlocked ? new string('★', stars) + new string('☆', 3 - stars) : "🔒 KHÓA";
                Label(card.x + 8, card.y + 4, 170, 16, $"{lv:00}. {CampaignCatalog.Levels[i].Name}", small, unlocked ? Color.white : Muted);
                Label(card.x + 8, card.y + 18, 170, 13, CampaignCatalog.Levels[i].Subtitle, small, Muted);
                Label(card.x + 8, card.y + 30, 170, 13, starStr, small, stars > 0 ? Gold : Muted);
                GUI.enabled = unlocked;
                if (Click(card, "", GUIStyle.none)) { selectedLevel = lv; audioSys.Play(SfxId.Click); }
                GUI.enabled = true;
            }

            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Rect info = new Rect(428, 106, 314, 246);
            Fill(info, Panel); Border(info, Teal);
            Label(442, 116, 286, 22, $"MÀN {selectedLevel:00}: {spec.Name}", heading, Gold);
            Label(442, 142, 286, 54, spec.Story, wrap, Color.white);
            Label(442, 198, 286, 16, "SINH VẬT MỞ KHÓA Ở MÀN NÀY:", small, Teal);

            int shown = 0;
            for (int id = 0; id < CampaignCatalog.Allies.Length && shown < 4; id++)
            {
                if (CampaignCatalog.Allies[id].Unlock == selectedLevel)
                {
                    float ax = 442 + shown * 72;
                    DrawAllySprite(id, new Rect(ax + 12, 218, 40, 40), 5, id, Color.white);
                    Label(ax, 260, 68, 13, CampaignCatalog.Allies[id].Name, smallCenter, Color.white);
                    Label(ax, 272, 68, 13, CampaignCatalog.Allies[id].ShortTag, smallCenter, Gold);
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
            { guideTab = 0; overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }
        }

        private void DrawBriefing()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.88f));
            Rect r = new Rect(134, 68, 500, 296);
            Fill(r, Panel); Border(r, Gold);
            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Label(156, 84, 456, 24, pendingEndless ? "CHẾ ĐỘ SINH TỒN VÔ TẬN" : $"MÀN {selectedLevel:00}: {spec.Name}", heading, Gold);
            Label(156, 114, 456, 54, pendingEndless
                ? "Toàn bộ 20 Sinh Vật Biển đã sẵn sàng! Đối mặt làn sóng rác không hồi kết và Siêu Boss mỗi 5 đợt!"
                : spec.Story, wrap, Color.white);

            // Clear Spotlight of New Guardians in this stage!
            Fill(new Rect(156, 174, 456, 76), Ink);
            Label(166, 180, 436, 15, "ĐỒNG MINH MỚI / CHỦ LỰC:", small, Teal);
            int idx = 0;
            for (int id = 0; id < CampaignCatalog.Allies.Length && idx < 3; id++)
            {
                if (CampaignCatalog.Allies[id].Unlock == selectedLevel)
                {
                    var a = CampaignCatalog.Allies[id];
                    float x = 166 + idx * 148;
                    DrawAllySprite(id, new Rect(x, 200, 36, 36), 5, id, Color.white);
                    Label(x + 40, 200, 102, 16, a.Name, small, Color.white);
                    Label(x + 40, 216, 102, 14, a.ShortTag, small, Gold);
                    Label(x + 40, 230, 102, 14, HabitatTag(a.Habitat), small, Cyan);
                    idx++;
                }
            }
            Label(156, 258, 456, 38, "💡 " + spec.Lesson, wrap, Teal);
            if (Click(new Rect(156, 306, 300, 40), "▶  BẮT ĐẦU NGAY (Enter)")) StartLevel(selectedLevel, pendingEndless);
            if (Click(new Rect(468, 306, 144, 40), "Quay lại")) overlay = Overlay.None;
        }

        private void DrawResult()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.84f));
            Rect r = new Rect(154, 62, 460, 308);
            Fill(r, Panel); Border(r, match.Won ? Teal : Danger);
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
                        DrawAllySprite(id, new Rect(188 + xIdx * 130, 222, 36, 36), 5, id, Color.white);
                        Label(228 + xIdx * 130, 226, 92, 16, CampaignCatalog.Allies[id].Name, small, Teal);
                        Label(228 + xIdx * 130, 242, 92, 14, CampaignCatalog.Allies[id].ShortTag, small, Gold);
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
            Fill(r, Panel); Border(r, Gold);
            Label(64, 42, 380, 26, "⚙ XƯỞNG NÂNG CẤP BIO-BOT", heading, Gold);
            Label(444, 46, 160, 20, $"Xu hiện có: {progress.EcoCoins}", text, Teal);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            for (int i = 0; i < CampaignCatalog.Upgrades.Length; i++)
            {
                var up = CampaignCatalog.Upgrades[i];
                int lv = progress.UpgradeLevel(i);
                Rect card = new Rect(64 + (i % 2) * 324, 86 + (i / 2) * 96, 312, 84);
                Fill(card, Ink); Border(card, lv >= up.MaxLevel ? Teal : new Color(0.3f, 0.5f, 0.58f, 1f));
                Label(card.x + 12, card.y + 8, 210, 20, $"{up.Name}  (Lv {lv}/{up.MaxLevel})", text, Color.white);
                Label(card.x + 12, card.y + 30, 200, 42, up.Desc, wrap, Muted);
                if (lv >= up.MaxLevel) Label(card.x + 220, card.y + 30, 82, 24, "TỐI ĐA", smallCenter, Teal);
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
            Fill(r, Panel); Border(r, Teal);
            Label(64, 42, 420, 26, $"★ THÀNH TÍCH ({progress.TotalAchievements()}/{CampaignCatalog.Achievements.Length})", heading, Gold);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            for (int i = 0; i < CampaignCatalog.Achievements.Length; i++)
            {
                var a = CampaignCatalog.Achievements[i];
                bool done = progress.Achievements[i];
                Rect card = new Rect(64 + (i % 2) * 324, 84 + (i / 2) * 60, 312, 52);
                Fill(card, done ? CardSel : Ink); Border(card, done ? Gold : new Color(0.24f, 0.36f, 0.44f, 1f));
                Label(card.x + 10, card.y + 6, 230, 18, (done ? "★ " : "○ ") + a.Title, small, done ? Gold : Color.white);
                Label(card.x + 240, card.y + 6, 64, 18, $"+{a.RewardCoins} Xu", small, Teal);
                Label(card.x + 10, card.y + 24, 294, 24, a.Desc, small, Muted);
            }
        }

        private void DrawGuide()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.94f));
            Fill(new Rect(28, 22, 712, 388), Panel);
            Border(new Rect(28, 22, 712, 388), Teal);
            Label(48, 36, 340, 24, "📖 SỔ SINH THÁI & HƯỚNG DẪN", heading, Teal);
            if (Click(new Rect(632, 34, 88, 28), "Đóng")) overlay = Overlay.None;

            string[] tabs = { "20 Sinh Vật Biển", "14 Quái Rác", "10 Loại Rác", "🎮 Cách Chơi" };
            for (int i = 0; i < tabs.Length; i++)
                if (Click(new Rect(48 + i * 144, 70, 136, 26), tabs[i]))
                { guideTab = i; guideScroll = 0f; audioSys.Play(SfxId.Click); }

            if (guideTab == 3)
            {
                Fill(new Rect(48, 106, 672, 286), Ink);
                string howTo =
                    "1. MÀN HÌNH 2 TẦNG CÙNG LÚC (5×11×2):\n" +
                    "   • Nửa trên = MẶT NƯỚC. Nửa dưới = ĐÁY BIỂN (mở từ Màn 2).\n" +
                    "   • Không cần chuyển cảnh! Click thẳng vào ô trên hoặc dưới để thả sinh vật.\n\n" +
                    "2. TRIỆU HỒI & THU NĂNG LƯỢNG:\n" +
                    "   • Thả [Rái Cá ☀+25 NL] sớm để tự tạo Hạt Vàng Năng Lượng.\n" +
                    "   • Click Hạt Vàng đang rơi (hoặc bơi Bio-Bot lại gần) để thu.\n" +
                    "   • Di chuột lên bất kỳ thẻ nào để xem chi tiết vai trò & tầng sống.\n\n" +
                    "3. BIO-BOT & TƯƠNG TÁC RÁC:\n" +
                    "   • WASD / Mũi tên: Bơi Bio-Bot hút Hạt Vàng & đẩy Chai Phao (trái = +50 NL, phải = Bowling).\n" +
                    "   • Click trái vào rác: Thu lấy NL hoặc Cắt Dây Cước CỨU CÁ miễn phí!\n" +
                    "   • Click phải vào rác: Dựng Khiên, Gương 2 tầng, Van Khí, hoặc nổ Pin EMP!\n" +
                    "   • Q/E/Tab: Cho Bio-Bot lặn/nổi · Shift: Lướt Hydro-Dash · R: Bio-Pulse (15 NL).";
                Label(64, 118, 640, 265, howTo, wrap, Color.white);
                return;
            }

            int count = guideTab == 0 ? CampaignCatalog.Allies.Length
                : guideTab == 1 ? CampaignCatalog.Enemies.Length : CampaignCatalog.Trash.Length;
            Vector2 sc = GUI.BeginScrollView(new Rect(48, 106, 672, 286),
                new Vector2(0, guideScroll), new Rect(0, 0, 648, count * 48));
            guideScroll = sc.y;
            for (int i = 0; i < count; i++)
            {
                string name, meta, hint;
                int y = i * 48;
                Fill(new Rect(0, y, 644, 44), Ink);
                if (guideTab == 0)
                {
                    var a = CampaignCatalog.Allies[i];
                    name = $"{a.Name} ({a.ShortTag})"; meta = $"{HabitatTag(a.Habitat)} · {a.Cost} NL · Màn {a.Unlock}"; hint = a.Hint;
                    DrawAllySprite(i, new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                }
                else if (guideTab == 1)
                {
                    var e = CampaignCatalog.Enemies[i];
                    name = e.Name; meta = $"{HabitatTag(e.Habitat)} · HP {e.Health} · Màn {e.Unlock}"; hint = e.Hint;
                    DrawSprite(20 + i, new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                }
                else
                {
                    var tr = CampaignCatalog.Trash[i];
                    name = tr.Name; meta = $"{HabitatTag(tr.Habitat)} · Màn {tr.Unlock}"; hint = tr.Hint;
                    DrawSprite(34 + i, new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                }
                Label(50, y + 4, 186, 18, name, text, Color.white);
                Label(50, y + 24, 186, 16, meta, small, Gold);
                Label(240, y + 6, 396, 34, hint, wrap, Muted);
            }
            GUI.EndScrollView();
        }

        private void DrawPause()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.78f));
            Rect r = new Rect(244, 114, 280, 204);
            Fill(r, Panel); Border(r, Teal);
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
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((c - Vector2.Distance(new Vector2(x, y), new Vector2(c, c))) * 1.4f)));
            tex.Apply();
            return tex;
        }

        private void DrawCircle(Vector2 center, float diameter, Color color)
        {
            if (circleTex == null) return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter), circleTex);
        }

        private void DrawAllySprite(int allyId, Rect rect, float fps, int offset, Color tint)
        {
            int row = allyId >= 0 && allyId < CampaignCatalog.AllySpriteRow.Length ? CampaignCatalog.AllySpriteRow[allyId] : allyId;
            DrawSprite(row, rect, fps, offset, tint);
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
        private static string HabitatTag(Habitat h) => h == Habitat.Both ? "Cả 2 Tầng" : h == Habitat.Deep ? "Đáy Biển" : "Mặt Nước";

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
