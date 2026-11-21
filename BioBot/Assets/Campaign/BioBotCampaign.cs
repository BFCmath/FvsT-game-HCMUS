using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    [RequireComponent(typeof(TwoLayerBoard))]
    public sealed partial class BioBotCampaign : MonoBehaviour
    {
        private enum Overlay { None, Briefing, Guide, Shop, Achievements, Pause }

        private const int StageCount = CampaignCatalog.PlayableStageCount;
        private static readonly Color Ink = new Color32(12, 22, 24, 248);
        private static readonly Color Panel = new Color32(21, 35, 37, 250);
        private static readonly Color CardBg = new Color32(27, 44, 46, 255);
        private static readonly Color CardSel = new Color32(48, 78, 70, 255);
        private static readonly Color Muted = new Color32(178, 196, 191, 255);
        private static readonly Color Teal = new Color32(131, 223, 193, 255);
        private static readonly Color Cyan = new Color32(118, 214, 255, 255);
        private static readonly Color Gold = new Color32(245, 194, 124, 255);
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
        private Texture2D robotSprite;
        private Texture2D estuary;
        private Texture2D combatSprites;
        private Texture2D dangerTriangle;
        private Texture2D circleTex;

        private int selectedLevel = 1;
        private bool pendingEndless;
        private int selection;
        private bool shovelMode;
        private bool autoPilot;
        private bool fastForward;
        private string controlHint;
        private int hoveredCardId = -1;
        private Vector2 hoveredCardPos;
        private Overlay overlay = Overlay.None;
        private int guideTab;
        private float guideScroll;
        private float wakeTimer;
        private float robotBank;
        private float visualTime;
        private Vector2 shake;
        private Vector2 diveFrom;
        private Layer diveOrigin;
        private float diveTime;
        private float previousRobotY;
        private bool recorded;
        private bool linearColorSpace;

        private GUIStyle small, smallCenter, energyCaption, dangerPrompt, text, heading, title, button, primaryButton, toolbarIcon, wrap;

        public CampaignMatch CurrentMatch => match;

        private void Awake()
        {
            linearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
            board = GetComponent<TwoLayerBoard>();
            board.CampaignHud = true;
            if (demoBottle != null) demoBottle.SetActive(false);

            progress = CampaignProgress.Load();
            selectedLevel = Mathf.Min(progress.UnlockedLevel, StageCount);
            vfx = new CampaignVfx();
            audioSys = new CampaignAudio(gameObject, progress.Muted);

            atlas = Resources.Load<Texture2D>("BioBot/TidalRoster");
            PrepareWaterLens();
            robotSprite = Resources.Load<Texture2D>("BioBot/TidalBioBot");
            estuary = Resources.Load<Texture2D>("BioBot/TidalEstuary");
            combatSprites = Resources.Load<Texture2D>("BioBot/TidalCombat");
            dangerTriangle = Resources.Load<Texture2D>("BioBot/TidalWarning");
            circleTex = BuildCircleTexture(32);
            ringTexture = MakeRadialTexture(true);
            shadowTexture = MakeRadialTexture(false);
        }

        private void Start() => robot.TerrainSpeed = 0f;

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) match?.InterruptCleanup();
        }

        public void OpenBriefing(int level, bool endless = false)
        {
            if (!endless && (level < 1 || level > Mathf.Min(progress.UnlockedLevel, StageCount))) return;
            match = null;
            selectedLevel = Mathf.Clamp(level, 1, StageCount);
            pendingEndless = endless;
            overlay = Overlay.Briefing;
            audioSys.Play(SfxId.Click);
        }

        public void StartLevel(int level, bool endless = false)
        {
            if (!endless && (level < 1 || level > Mathf.Min(progress.UnlockedLevel, StageCount))) return;
            selectedLevel = Mathf.Clamp(level, 1, StageCount);
            pendingEndless = endless;
            match = new CampaignMatch(selectedLevel, progress, endless);
            BindMatchEvents(match);
            vfx.Clear();

            selection = 0;
            shovelMode = false;
            autoPilot = false;
            fastForward = false;
            wakeTimer = 0f;
            visualTime = 0f;
            shake = Vector2.zero;
            robotBank = 0f;
            diveTime = 0f;
            previousRobotY = match.RobotY;
            recorded = false;
            overlay = Overlay.None;

            board.ResetToSurface();
            robot.TerrainSpeed = 0f;
        }

        private void BindMatchEvents(CampaignMatch m)
        {
            m.OnSfx = id => audioSys.Play(id);
            m.OnBurst = (layer, px, col, cnt) => vfx.SpawnBurst(layer, px, col, cnt);
            m.OnRing = (layer, px, col, sz) => vfx.SpawnRing(layer, px, col, sz);
            m.OnFloatText = (layer, px, txt, col) => vfx.SpawnText(layer, px, txt, col);
            m.OnShake = (dur, intensity) => vfx.TriggerShake(dur, intensity);
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
            vfx.Tick(rawDelta, overlay == Overlay.None);

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

            if (match.Ended)
            {
                RecordMatchEnd();
                if (kb != null && kb.enterKey.wasPressedThisFrame) AdvanceFromResult();
                else if (kb != null && kb.escapeKey.wasPressedThisFrame) ReturnToMenu();
                return;
            }

            if (overlay == Overlay.Pause && kb != null && kb.enterKey.wasPressedThisFrame)
            {
                overlay = Overlay.None;
                return;
            }

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                match.InterruptCleanup();
                overlay = overlay == Overlay.None ? Overlay.Pause : Overlay.None;
                audioSys.Play(SfxId.Click);
                return;
            }

            if (overlay != Overlay.None)
            {
                match.InterruptCleanup();
                return;
            }

            float simDelta = Mathf.Min(Time.deltaTime, 0.05f) * (fastForward ? 1.85f : 1f);

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
            visualTime += simDelta;
            shake = vfx.ShakeOffset();
            diveTime = Mathf.Max(0f, diveTime - rawDelta);
            robotBank = Mathf.Lerp(robotBank, Mathf.Clamp((match.RobotY - previousRobotY) * 80f, -9f, 9f), rawDelta * 12f);
            previousRobotY = match.RobotY;

            if (match.Ended) { RecordMatchEnd(); return; }

            wakeTimer -= simDelta;
            if (wakeTimer <= 0f && match.Respawn <= 0f && match.RobotMoving)
            {
                wakeTimer = match.IsDashing ? 0.06f : 0.28f;
                Vector2 rPx = CampaignMatch.CellToPixel(match.RobotLayer, match.RobotX, match.RobotY);
                vfx.SpawnRing(match.RobotLayer, rPx + new Vector2(0, 6),
                    match.IsDashing ? Cyan : new Color(0.75f, 0.96f, 1f, 0.35f), match.IsDashing ? 24f : 16f);
            }

            if (autoPilot) match.StepAutoPilot();
            HandleMatchHotkeys(kb);
            match.UpdateCleanup(kb != null && kb.fKey.isPressed, Mathf.Min(Time.unscaledDeltaTime, 0.15f));
        }

        private void HandleMatchHotkeys(Keyboard kb)
        {
            if (kb == null) return;
            var cell = match.RobotCell;
            if (kb.qKey.wasPressedThisFrame) ChangeLayer();
            if (kb.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (kb.digit5Key.wasPressedThisFrame) SelectSlot(4);
            if (kb.spaceKey.wasPressedThisFrame)
            {
                if (shovelMode) { if (match.RemoveAlly(match.RobotLayer, cell.y, cell.x)) shovelMode = false; }
                else match.Plant(selection, match.RobotLayer, cell.y, cell.x);
            }
            if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                match.TriggerHydroDash(match.RobotFacingLeft ? -1 : 1);
            if (kb.pKey.wasPressedThisFrame) { autoPilot = !autoPilot; audioSys.Play(SfxId.Click); }
            if (kb.vKey.wasPressedThisFrame) { shovelMode = !shovelMode; audioSys.Play(SfxId.Click); }
        }

        private void ChangeLayer()
        {
            Layer previousLayer = match.RobotLayer;
            Vector2 departure = RobotDrawPosition();
            match.ToggleRobotLayer();
            if (match.RobotLayer == previousLayer) return;
            diveFrom = departure;
            diveTime = DiveDuration;
            diveOrigin = previousLayer;
        }

        private void AdvanceFromResult()
        {
            if (!match.Endless && match.Won && selectedLevel < StageCount)
                OpenBriefing(selectedLevel + 1);
            else if (!match.Endless && match.Won)
                ReturnToMenu();
            else
                StartLevel(selectedLevel, match.Endless);
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
                    Mathf.Min(selectedLevel + 1, StageCount));
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
            CampaignCatalog.UnlockedAllyCount(match != null && match.Endless ? StageCount : selectedLevel);

        private void SelectSlot(int slot)
        {
            if (slot < UnlockedAllyCount())
            {
                selection = slot;
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
            bool prevEnabled = GUI.enabled;
            Overlay visibleOverlay = overlay;
            bool ended = match != null && match.Ended;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(pixelRect.x, Screen.height - pixelRect.yMax, 0f),
                Quaternion.identity,
                new Vector3(pixelRect.width / 768f, pixelRect.height / 432f, 1f));

            controlHint = null;
            GUI.enabled = prevEnabled && visibleOverlay == Overlay.None && !ended;
            if (match == null)
            {
                DrawMenu();
            }
            else
            {
                // The board alone shakes. Input and interface keep their fixed coordinates.
                HandleSplitBoardMouse();
                if (Event.current.type == EventType.Repaint)
                {
                    DrawPoolBackdrop();
                    DrawPlacementPreview();

                    // The arena's contents shake without moving the HUD or hit targets.
                    Matrix4x4 hud = GUI.matrix;
                    if (GUI.enabled)
                        GUI.matrix = hud * Matrix4x4.Translate(new Vector3(shake.x, shake.y, 0f));
                    DrawUpdrafts();
                    DrawActiveLayer();
                    DrawTacticalEffects();
                    if (diveTime > 0f) DrawBioBotInSplitView();
                    if (match.Respawn > 0f) DrawRespawnMarker();
                    DrawOrbsAndVfx();
                    GUI.matrix = hud;
                }

                DrawTopSeedBank();
                DrawBottomDock();
                DrawInteractiveTutorial();
                DrawHoverTooltips();
                DrawCleanupPrompt();
            }

            GUI.enabled = prevEnabled;
            if (ended) DrawResult();
            else if (visibleOverlay == Overlay.Briefing) DrawBriefing();
            else if (visibleOverlay == Overlay.Shop) DrawShop();
            else if (visibleOverlay == Overlay.Achievements) DrawAchievements();
            else if (visibleOverlay == Overlay.Pause) DrawPause();
            else if (visibleOverlay == Overlay.Guide) DrawGuide();

            if (match == null && vfx.ToastTime > 0f && (visibleOverlay == Overlay.None || visibleOverlay == Overlay.Shop))
                DrawAchievementToast();

            GUI.matrix = prevMatrix;
            GUI.enabled = prevEnabled;
            GUI.color = prevColor;
        }

        private void HandleSplitBoardMouse()
        {
            if (!GUI.enabled) return;
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button > 1) return;
            Vector2 pos = e.mousePosition;
            if (!BoardViewport.Contains(pos)) return;

            if (e.button == 0 && match.CollectOrbAt(pos)) { e.Use(); return; }
            if (!CampaignMatch.TryPixelToCell(pos, true, out Layer layer, out int col, out int row)) return;

            if (e.button == 1 && shovelMode)
                shovelMode = false;
            else if (e.button == 1)
                return;
            else if (shovelMode)
            {
                if (match.RemoveAlly(layer, row, col)) shovelMode = false;
            }
            else if (match.Trash[(int)layer, row, col] == null)
                match.Plant(selection, layer, row, col);

            e.Use();
        }

        private void DrawUpdrafts()
        {
            if (!match.DeepEnabled) return;
            Layer layer = match.RobotLayer;
            for (int row = 0; row < CampaignMatch.Rows; row++)
                for (int col = 0; col < CampaignMatch.Columns; col++)
                    if (match.HasUpdraftAt(row, col))
                        DrawBubbles(CampaignMatch.CellToPixel(layer, col, row), row + col, 0.65f);
        }

        private void DrawPlacementPreview()
        {
            if (!GUI.enabled || !CampaignMatch.TryPixelToCell(Event.current.mousePosition, match.DeepEnabled,
                out Layer layer, out int col, out int row) || layer != match.RobotLayer) return;

            int side = (int)layer;
            bool hasWaste = match.Trash[side, row, col] != null;
            bool hasAlly = match.Allies[side, row, col] != null;
            bool canAct = match.Respawn <= 0f &&
                (shovelMode ? hasAlly : hasWaste || match.CanPlant(selection, layer, row, col, out _));
            Color accent = !canAct ? Danger : shovelMode ? Gold : Teal;
            DrawTileHighlight(layer, col, row, accent);

            if (canAct && !hasWaste && !shovelMode)
            {
                Vector2 pos = CampaignMatch.CellToPixel(layer, col, row);
                DrawWaterUnit(CampaignCatalog.AllySpriteRow[selection], UnitRect(pos, 37f, 37f), pos,
                    layer, 5f, 0, WithAlpha(Color.white, 0.56f), false);
            }
        }

        private void DrawActiveLayer()
        {
            Layer layer = match.RobotLayer;
            float t = visualTime;
            int side = (int)layer;

            for (int r = 0; r < CampaignMatch.Rows; r++)
            {
                float depthScale = 0.94f + r * 0.032f;

                // Spinning Hydro-Turbine at left dam edge of this row
                if (match.TurbineReady[side, r])
                {
                    Vector2 tp = CampaignMatch.CellToPixel(layer, -0.35f, r);
                    DrawCircle(tp, 15f, WithAlpha(Ink, 0.97f));
                    DrawRing(tp, 16f, 16f, WithAlpha(Teal, 0.94f));
                    for (int blade = 0; blade < 3; blade++)
                    {
                        float phase = t * 5f + r + blade * Mathf.PI * 2f / 3f;
                        Vector2 spoke = new Vector2(Mathf.Cos(phase), Mathf.Sin(phase)) * 3.5f;
                        DrawCircle(tp + spoke, 3.6f, WithAlpha(Teal, 1f));
                    }
                }

                // Draw objects in this row before the next row's foreground.
                for (int c = 0; c < CampaignMatch.Columns; c++)
                {
                    Vector2 p = CampaignMatch.CellToPixel(layer, c, r);
                    var waste = match.Trash[side, r, c];
                    if (waste != null)
                        DrawDebris(waste, layer, p, depthScale, c + r);

                    var ally = match.Allies[side, r, c];
                    if (ally?.ShieldSource is TrashEffect shield)
                        DrawTrashShield(ally, shield, layer, p, depthScale);
                    else if (ally != null)
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
                            sx *= 1f + ft * 0.22f; sy *= 1f - ft * 0.14f; ox += ft * 4.5f;
                            if (ft > 0.65f) DrawCircle(p + new Vector2(15, -4), 10f * ft, new Color(0.85f, 1f, 0.85f, 0.78f));
                        }
                        if (ally.HurtAnim > 0f) ox -= Mathf.Sin(ally.HurtAnim * 38f) * 3.5f;

                        float size = 43f * depthScale;
                        Vector2 contact = p + new Vector2(ox, 0f);
                        Rect body = UnitRect(contact, size * sx, size * sy);
                        body.y += breathe * (layer == Layer.Surface ? 1.2f : 2.7f);
                        Color tint = ally.HurtAnim > 0f ? new Color(1f, 0.72f, 0.72f, 1f)
                            : ally.Jammed > 0f ? new Color(0.75f, 0.65f, 1f, 1f) : Color.white;
                        bool rooted = CampaignCatalog.Allies[ally.Spec].Ability == Ability.Guard && ally.Spec != 2;
                        DrawWaterUnit(CampaignCatalog.AllySpriteRow[ally.Spec], body, contact, layer,
                            ally.FireAnim > 0f ? 11f : 6f, c + r, tint, rooted);
                        if (ally.Draining)
                            DrawBubbles(contact, ally.Phase, 0.84f);
                        if (ally.Health < ally.MaxHealth)
                            Bar(new Rect(p.x - 12, p.y + 8, 24, 2), ally.Health / (float)ally.MaxHealth, ally.Draining ? Gold : Green);
                    }
                }

                // Sliding Bowling Bottles in this row
                foreach (var sb in match.SlidingBottles)
                {
                    if (sb.Layer != layer || sb.Row != r) continue;
                    Vector2 p = CampaignMatch.CellToPixel(layer, sb.X, sb.Row);
                    float size = 36f * depthScale;
                    Vector2 trail = p - new Vector2(Mathf.Sign(sb.VelX) * 13f, 0f);
                    DrawWake(trail, size * 0.8f, sb.X, 0.9f);
                    DrawWaterUnit(34, UnitRect(p, size, size), p, layer, 10, 0, Color.white, false);
                }

                // Turbine Waves & Beams in this row
                foreach (var tw in match.Turbines)
                {
                    if (tw.Layer != layer || tw.Row != r) continue;
                    Vector2 p = CampaignMatch.CellToPixel(layer, tw.X, tw.Row);
                    Fill(new Rect(p.x - 16, p.y - 12, 28, 24), new Color(0.35f, 0.92f, 1f, 0.88f));
                }
                foreach (var b in match.Beams)
                {
                    if (b.Layer != layer || b.Row != r) continue;
                    Vector2 a = CampaignMatch.CellToPixel(layer, b.From, b.Row);
                    Vector2 c = CampaignMatch.CellToPixel(layer, b.To, b.Row);
                    Color col = b.Color; col.a = Mathf.Clamp01(b.Left * 5f);
                    Fill(new Rect(Mathf.Min(a.x, c.x), a.y - 3, Mathf.Max(8f, Mathf.Abs(a.x - c.x)), 6), col);
                }

                // Projectiles in this row
                foreach (var proj in match.Projectiles)
                {
                    if (proj.Layer != layer || proj.Row != r) continue;
                    Vector2 p = CampaignMatch.CellToPixel(layer, proj.X, proj.Row) - new Vector2(0, 4);
                    DrawProjectile(proj, p, depthScale);
                }

                // Enemies in this row (2.5D depth-sorted!)
                foreach (var e in match.Enemies)
                {
                    if (e.Layer != layer || e.Row != r || e.SquidPull != null) continue;
                    var eSpec = CampaignCatalog.Enemies[e.Spec];
                    Vector2 p = CampaignMatch.CellToPixel(layer, Mathf.Clamp(e.X, -0.3f, CampaignMatch.Columns - 0.1f), e.Row);
                    float baseSz = (eSpec.Trait == Trait.Boss ? 56f : (e.Small ? 26f : (layer == Layer.Deep ? 39f : 36f))) * depthScale;
                    if (eSpec.Trait == Trait.Ferry) baseSz *= 1.37f;
                    float waddle = Mathf.Abs(Mathf.Sin(e.WalkPhase));
                    float sx = 1f + (0.5f - waddle) * 0.08f, sy = 1f + (waddle - 0.5f) * 0.08f;
                    float ox = 0f, oy = -waddle * 2.5f;
                    if (e.BiteAnim > 0f) ox -= Mathf.Sin((e.BiteAnim / 0.25f) * Mathf.PI) * 5.5f;
                    if (e.HurtAnim > 0f) { float ht = e.HurtAnim / 0.16f; ox += ht * 4f; sx *= 1f - ht * 0.16f; }
                    bool firing = e.ShootAnim > 0f;
                    if (firing) ox += 4f * (e.ShootAnim / CampaignMatch.LitterRecoilSeconds);
                    Vector2 contact = p + new Vector2(ox, 0f);
                    Rect body = UnitRect(contact, baseSz * sx, baseSz * sy);
                    body.y += oy;
                    if (e.DiveWindup > 0f)
                    {
                        float windup = 1f - e.DiveWindup / CampaignMatch.FerryWindupSeconds;
                        body.y += Mathf.SmoothStep(0f, 10f, windup);
                        DrawSplash(contact, windup, 0.68f);
                    }
                    if (e.DiveArrival > 0f)
                    {
                        float arrival = e.DiveArrival / CampaignMatch.FerryArrivalSeconds;
                        body.y -= arrival * 8f;
                        DrawBubbles(contact, r, arrival);
                    }
                    Color tint = e.Flash > 0f ? new Color(1f, 0.55f, 0.45f, 1f)
                        : e.Stun > 0f ? new Color(1f, 0.94f, 0.55f, 1f)
                        : e.Slow > 0f ? new Color(0.62f, 0.88f, 1f, 1f) : Color.white;
                    float fps = e.Slow > 0f ? 4f : 8f;
                    int frame = eSpec.Trait == Trait.Litter
                        ? (firing ? 3 : SpriteFrame(fps, e.Row, 3))
                        : SpriteFrame(fps, e.Row);
                    DrawWaterUnit(CampaignCatalog.EnemySpriteRow(e.Spec), body, contact, layer, 0f, frame,
                        tint, eSpec.Trait == Trait.Burrow);
                    float bw = eSpec.Trait == Trait.Boss ? 44f : 24f;
                    if (e.Health < e.MaxHealth)
                        Bar(new Rect(p.x - bw * 0.5f, body.y + 2f, bw, 2f), e.Health / (float)e.MaxHealth, Danger);
                }

                if (diveTime <= 0f && match.RobotCell.y == r)
                    DrawBioBotInSplitView();
            }
        }

        private const float DiveDuration = 0.32f;

        private Vector2 RobotDrawPosition()
        {
            Vector2 target = CampaignMatch.CellToPixel(match.RobotLayer, match.RobotX, match.RobotY);
            if (diveTime <= 0f) return target;
            return Vector2.Lerp(diveFrom, target, Mathf.SmoothStep(0f, 1f, 1f - diveTime / DiveDuration));
        }

        private void DrawBioBotInSplitView()
        {
            if (match.Respawn > 0f) return;
            Vector2 p = RobotDrawPosition();
            float direction = match.RobotFacingLeft ? -1f : 1f;
            float bob = Mathf.Sin(visualTime * (match.RobotMoving ? 9f : 3f)) * (match.RobotMoving ? 1.3f : 2f);
            float size = 44f + match.RobotY * 1.2f;
            bool transitioning = diveTime > 0f;
            if (transitioning)
            {
                float transition = 1f - diveTime / DiveDuration;
                if (diveOrigin == Layer.Surface)
                    DrawSplash(diveFrom, transition, 0.82f);
                else if (transition > 0.72f)
                    DrawSplash(CampaignMatch.CellToPixel(Layer.Surface, match.RobotX, match.RobotY),
                        (transition - 0.72f) / 0.28f, 0.70f);
            }
            Layer layer = transitioning && diveTime > DiveDuration * 0.58f ? diveOrigin : match.RobotLayer;
            if (match.RobotMoving && !transitioning)
            {
                Vector2 trail = p - new Vector2(direction * 17f, 0f);
                if (layer == Layer.Surface)
                    DrawWake(trail, size * (match.IsDashing ? 1.05f : 0.72f), 0f, 0.8f);
                else
                    DrawBubbles(trail, 0f, match.IsDashing ? 0.86f : 0.45f);
            }

            if (robotSprite == null) return;
            Rect body = UnitRect(p, size, size);
            body.y += bob;
            if (transitioning)
                body.y += Mathf.Sin(diveTime / DiveDuration * Mathf.PI) * 4f * (match.RobotLayer == Layer.Deep ? 1f : -1f);
            int frame = Mathf.FloorToInt(visualTime * (match.RobotMoving ? 12f : 5f)) % 4;
            Rect uv = new Rect((frame + (match.RobotFacingLeft ? 1 : 0)) * 0.25f, 0f,
                match.RobotFacingLeft ? -0.25f : 0.25f, 1f);
            Matrix4x4 previous = GUI.matrix;
            if (layer == Layer.Deep)
                GUIUtility.RotateAroundPivot(direction * robotBank + (transitioning ? 12f : 0f), p);
            float condition = match.RobotHealth / (float)match.MaxRobotHealth;
            Color tint = Color.Lerp(new Color(0.66f, 0.61f, 0.58f), Color.white, condition);
            DrawWaterSprite(robotSprite, uv, body, p, layer, tint, false, visualTime * 2f);
            DrawBotDamage(body, direction, condition);
            GUI.matrix = previous;
        }

        private void DrawBotDamage(Rect body, float direction, float condition)
        {
            if (condition >= 1f) return;
            float x = body.center.x - direction * body.width * 0.16f;
            float y = body.y + body.height * 0.43f;
            Fill(new Rect(x, y, 4f, 2f), new Color32(71, 57, 62, 255));
            if (condition > 0.5f) return;

            Fill(new Rect(x + 2f, y - 3f, 2f, 4f), new Color32(55, 48, 59, 255));
            float cycle = Mathf.Repeat(visualTime * (condition < 0.4f ? 6f : 2.5f), 1f);
            if (cycle > 0.46f) return;
            DrawCircle(new Vector2(x - direction * cycle * 7f, y - cycle * 12f), 3f, Gold);
            Fill(new Rect(x + 3f, y - 2f - cycle * 6f, 1.5f, 1.5f), Cyan);
        }

        private void DrawRespawnMarker()
        {
            Layer layer = match.RobotLayer;
            Vector2 dock = CampaignMatch.CellToPixel(layer, 0.5f, match.RobotY);
            DrawRing(dock, 32f, 11f, Danger);
            Label(dock.x - 27f, dock.y - 17f, 54f, 15f, $"{match.Respawn:0.0}s", smallCenter, Danger);
        }

        private static Rect CombatUV(int slot) => new Rect(slot / 5f, 0f, 0.2f, 1f);

        private void DrawProjectile(CampaignMatch.Projectile projectile, Vector2 p, float scale)
        {
            bool charged = projectile.Kind == ProjectileKind.ChargedBubble;
            Color tint = charged ? Gold : Cyan;
            float size = (charged ? 22f : 15f) * scale;
            DrawCircle(p - new Vector2(size * 0.45f, 0f), size * 0.36f, WithAlpha(tint, 0.44f));
            if (charged)
                DrawRing(p, size * 1.08f, size * 0.78f, WithAlpha(Gold, 0.44f + 0.20f * Mathf.Sin(projectile.Age * 25f)));

            if (combatSprites == null)
            {
                DrawCircle(p, size * 0.5f, tint);
                return;
            }

            int slot = charged ? 2 : projectile.Kind == ProjectileKind.Seed ? 0 : 1;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(Around(p, size), combatSprites, CombatUV(slot));
        }

        private void DrawTrashShield(CampaignMatch.Ally ally, TrashEffect source, Layer layer, Vector2 contact, float scale)
        {
            float pop = Mathf.Clamp01(ally.PlantPop / 0.34f);
            float size = (46f + 6f * pop) * scale;
            Color accent = source == TrashEffect.Shell ? Gold : Cyan;
            Rect body = UnitRect(contact, size, size);
            body.y -= 4f + 5f * pop;
            if (combatSprites != null)
                DrawWaterSprite(combatSprites, CombatUV(source == TrashEffect.Shell ? 3 : 4), body, contact, layer, Color.white, true, visualTime);
            else
                DrawWaterUnit(1, body, contact, layer, 0f, 0, accent, true);

            if (pop > 0f)
                DrawRing(contact, size * (1.3f - pop * 0.58f), size * 0.47f, WithAlpha(accent, pop));
            if (ally.Health < ally.MaxHealth)
            {
                float health = ally.Health / (float)ally.MaxHealth;
                Bar(new Rect(contact.x - 12f, contact.y + 9f, 24f, 2f), health, accent);
            }
        }

        private void DrawOrbsAndVfx()
        {
            float t = match == null ? Time.unscaledTime : visualTime;
            foreach (var orb in match.Orbs)
            {
                Vector2 p = orb.DrawPosition;
                if (!orb.FlyingToBank && orb.Layer != match.RobotLayer) continue;
                const float alpha = 1f;
                DrawCircle(p, 20f + Mathf.Sin(t * 7f + orb.Bob) * 2.5f, WithAlpha(Gold, 0.18f * alpha));
                DrawRing(p, 14f, 14f, WithAlpha(Gold, 0.65f * alpha));
                DrawCircle(p, 9f, WithAlpha(Gold, alpha));
                DrawCircle(p - new Vector2(1.5f, 1.5f), 3f, WithAlpha(Color.white, alpha));
            }
            foreach (var p in vfx.Particles)
            {
                if (p.Layer != match.RobotLayer) continue;
                float frac = Mathf.Clamp01(p.Life / p.MaxLife);
                Color c = p.Color; c.a *= frac;
                if (p.Ring)
                {
                    float r = p.Size * (1f - frac * 0.65f);
                    DrawRing(p.Pos, r * 1.34f, r * 0.65f, c);
                }
                else DrawCircle(p.Pos, p.Size * 1.8f, c);
            }
            foreach (var ft in vfx.Texts)
            {
                if (ft.Layer != match.RobotLayer) continue;
                Color c = ft.Color; c.a *= Mathf.Clamp01(ft.Life / ft.MaxLife);
                Label(ft.Pos.x - 48, ft.Pos.y - 7, 96, 16, ft.Text, smallCenter, c);
            }

        }

        private static Rect CardRect(int slot) => new Rect(110 + slot * 86, 9, 82, 57);

        private void DrawTopSeedBank()
        {
            Surface(new Rect(6, 4, 756, 68), Ink, Seam);
            Surface(new Rect(12, 10, 68, 56), CardBg, WithAlpha(Gold, 0.56f));
            float glow = 1f + match.EnergyBounce * 2f;
            DrawCircle(new Vector2(30, 32), 20 * glow, WithAlpha(Gold, 0.17f));
            DrawRing(new Vector2(30, 32), 16 * glow, 16 * glow, Gold);
            DrawCircle(new Vector2(30, 32), 7, Gold);
            Label(40, 22, 38, 20, $"{match.Energy}", text, Ivory);
            Label(17, 48, 58, 14, "NĂNG LƯỢNG", energyCaption, Muted);

            int unlocked = UnlockedAllyCount();
            hoveredCardId = -1;
            Vector2 cursor = Event.current.mousePosition;
            for (int id = 0; id < unlocked; id++)
            {
                Rect card = CardRect(id);
                var ally = CampaignCatalog.Allies[id];
                bool ready = match.Energy >= ally.Cost && match.CardCooldowns[id] <= 0f;
                bool selected = !shovelMode && selection == id;
                bool fits = CampaignCatalog.Fits(ally.Habitat, match.RobotLayer);
                bool emergency = CampaignMatch.IsEmergencyDeepPlacement(id, match.RobotLayer);
                Color accent = selected ? Gold : ready && (fits || emergency) ? Seam : WithAlpha(Seam, 0.42f);

                Surface(card, selected ? CardSel : CardBg, accent);
                DrawAllySprite(id, new Rect(card.x + 4, card.y + 3, 35, 35), ready ? 5 : 0, id,
                    ready && (fits || emergency) ? Color.white : new Color(0.66f, 0.72f, 0.69f, 0.9f));
                Label(card.x + 48, card.y + 6, 30, 13, $"{id + 1}", smallCenter, Muted);
                Label(card.x + 39, card.y + 20, 40, 14, $"{ally.Cost} NL", smallCenter, match.Energy >= ally.Cost ? Gold : Danger);
                Label(card.x + 3, card.y + 35, 76, 13, ally.Name, smallCenter, Ivory);

                string state = match.CardCooldowns[id] > 0f ? $"{match.CardCooldowns[id]:0.0}s"
                    : emergency ? "O₂ giảm ở đáy" : HabitatTag(ally.Habitat);
                Label(card.x + 3, card.y + 46, 76, 10, state, smallCenter, emergency ? Gold : fits ? Teal : Muted);

                if (GUI.enabled && card.Contains(cursor))
                {
                    hoveredCardId = id;
                    hoveredCardPos = new Vector2(card.x, card.yMax + 5);
                }
                if (Click(card, "", GUIStyle.none)) SelectSlot(id);
            }

            if (IconButton(new Rect(568, 13, 40, 46), "×", shovelMode, "Gỡ đồng minh [V]"))
            { shovelMode = !shovelMode; audioSys.Play(SfxId.Click); }
            if (IconButton(new Rect(614, 13, 40, 46), "≫", fastForward, "Tốc độ"))
            { fastForward = !fastForward; audioSys.Play(SfxId.Click); }
            if (IconButton(new Rect(660, 13, 40, 46), "AI", autoPilot, "Tự động [P]"))
            { autoPilot = !autoPilot; audioSys.Play(SfxId.Click); }
            if (IconButton(new Rect(706, 13, 46, 46), "Ⅱ", false, "Tạm dừng [Esc]"))
            { overlay = Overlay.Pause; audioSys.Play(SfxId.Click); }
        }

        private void DrawBottomDock()
        {
            Surface(new Rect(6, 366, 756, 60), Ink, Seam);
            string wave = match.Wave == 0 ? "Chuẩn bị" : match.Endless ? $"Đợt {match.Wave}" : $"Đợt {match.Wave}/{match.TotalWaves}";
            string mission = match.Endless ? "Sinh tồn vô tận" : $"{selectedLevel:00}  {match.Rules.Name}";
            Label(17, 372, 445, 16, $"{mission}   /   {wave}", text, Ivory);
            Color gate = match.Integrity <= 3 ? Danger : Teal;
            Label(470, 373, 106, 16, $"Đê  {match.Integrity}/{CampaignMatch.MaxIntegrity}", text, gate);
            Rect timeline = new Rect(17, 393, 439, 3);
            Bar(new Rect(470, 393, 106, 3), match.Integrity / (float)CampaignMatch.MaxIntegrity, gate);
            Bar(timeline, match.WaveProgress, Teal);

            if (Click(new Rect(590, 373, 74, 25), audioSys.Muted ? "Âm ×" : "Âm ♪")) ToggleMute();
            if (Click(new Rect(669, 373, 83, 25), "Sổ sinh thái"))
            { guideTab = 0; guideScroll = 0f; overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }

            if (vfx.ToastTime > 0f && GUI.enabled)
                Label(17, 404, 733, 16, $"{vfx.ToastTitle}  ·  {vfx.ToastSub}", small, Gold);
            else
            {
                string tip = controlHint ?? (match.Feedback > 0f ? match.Message : DefaultActionHint());
                Label(17, 404, 733, 16, tip, small, match.Feedback > 0f ? Teal : Muted);
            }
        }

        private string DefaultActionHint()
        {
            if (shovelMode) return "×  Chọn đồng minh để gỡ · V hủy";
            return "";
        }

        private void ToggleMute()
        {
            audioSys.Muted = !audioSys.Muted;
            progress.Muted = audioSys.Muted;
            progress.Save();
        }

        private void DrawInteractiveTutorial()
        {
            if (!GUI.enabled || match.Endless || Event.current.type != EventType.Repaint) return;
            Color pulse = WithAlpha(Gold, 0.56f + 0.34f * Mathf.Sin(visualTime * 5f));
            string guide = null;
            if (selectedLevel == 1)
            {
                if (match.PlantedIncomeCount == 0)
                {
                    guide = "1 / 3   Chọn [1] và đặt Rái Cá vào ô sáng để tạo năng lượng.";
                    Border(CardRect(0), pulse);
                    DrawTileHighlight(Layer.Surface, 0, 2, pulse);
                }
                else if (match.PlantedFighterCount == 0)
                {
                    guide = "2 / 3   Chọn [2] và đặt Vịt Bắn Bọt ở hàng giữa.";
                    Border(CardRect(1), pulse);
                    DrawTileHighlight(Layer.Surface, 1, 2, pulse);
                }
                else if (match.OrbsCollectedCount == 0)
                {
                    guide = "3 / 3   Bấm hạt vàng hoặc bơi lại gần để thu năng lượng.";
                    foreach (var orb in match.Orbs)
                        if (!orb.FlyingToBank && orb.Layer == match.RobotLayer)
                            DrawRing(orb.DrawPosition, 27, 27, pulse);
                }
            }
            else if (selectedLevel == 3 && match.FishRescued == 0)
            {
                guide = match.RobotLayer == Layer.Surface ? "Bấm [Q] để lặn." : "Bơi tới Dây Cước rồi giữ [F] để thả cá.";
                if (match.RobotLayer == Layer.Deep)
                {
                    for (int row = 0; row < CampaignMatch.Rows; row++)
                        for (int col = 0; col < CampaignMatch.Columns; col++)
                        {
                            var trash = match.Trash[1, row, col];
                            if (trash != null && CampaignCatalog.Trash[trash.Spec].Effect == TrashEffect.Filament)
                                DrawTileHighlight(Layer.Deep, col, row, pulse);
                        }
                }
            }

            if (guide != null)
            {
                Surface(new Rect(178, 205, 462, 17), Ink, WithAlpha(Gold, 0.5f), 3f);
                Label(187, 206, 444, 15, guide, small, Gold);
            }
        }

        private void DrawHoverTooltips()
        {
            if (!GUI.enabled || Event.current.type != EventType.Repaint) return;
            if (hoveredCardId >= 0 && hoveredCardId < CampaignCatalog.Allies.Length)
            {
                var a = CampaignCatalog.Allies[hoveredCardId];
                float tx = Mathf.Clamp(hoveredCardPos.x, 12f, 510f);
                Rect tip = new Rect(tx, hoveredCardPos.y, 248, 66);
                Surface(tip, Ink, Seam);
                string habFull = a.Habitat == Habitat.Both ? "Đặt được ở Cả 2 Tầng" : a.Habitat == Habitat.Deep ? "Đặt ở Đáy Biển (nửa dưới)" : "Đặt ở Mặt Nước (nửa trên)";
                Label(tip.x + 8, tip.y + 4, 232, 16, $"{a.Name} ({a.ShortTag}) · {a.Cost} NL", small, Gold);
                Label(tip.x + 8, tip.y + 18, 232, 14, habFull, small, a.Habitat == Habitat.Deep ? Cyan : Teal);
                Label(tip.x + 8, tip.y + 32, 232, 30, a.Hint, wrap, Ivory);
                return;
            }

            Vector2 mPos = Event.current != null ? Event.current.mousePosition : new Vector2(-999, -999);
            if (CampaignMatch.TryPixelToCell(mPos, match.DeepEnabled, out Layer l, out int hc, out int hr) && l == match.RobotLayer)
            {
                var w = match.Trash[(int)l, hr, hc];
                if (w != null && w != match.WasteAtRobot)
                {
                    var spec = CampaignCatalog.Trash[w.Spec];
                    Vector2 p = CampaignMatch.CellToPixel(l, hc, hr);
                    Rect box = new Rect(Mathf.Clamp(p.x - 105, 16, 535), Mathf.Max(74, p.y - 39), 210, 31);
                    Fill(box, new Color(0.06f, 0.13f, 0.2f, 0.96f));
                    Border(box, Gold);
                    Label(box.x + 6, box.y + 2, 198, 14, spec.Name, smallCenter, Gold);
                    Label(box.x + 6, box.y + 15, 198, 14, spec.Effect == TrashEffect.Float
                        ? "Bơi chạm để đẩy chai" : "Bơi tới ô này · " + CleanupPrompt(spec), smallCenter, Teal);
                }
            }
        }

        private static string CleanupPrompt(TrashSpec spec) => spec.Effect switch
        {
            TrashEffect.Float => "Bơi chạm để đẩy chai",
            TrashEffect.Ballast => "Giữ [F] để dựng khiên ↑",
            TrashEffect.Salvage => "Giữ [F] để lấy +20 NL",
            TrashEffect.Spill => "Giữ [F] để dọn dầu",
            TrashEffect.Filament or TrashEffect.Shell or TrashEffect.Clutter => spec.QuickAction,
            _ => "Giữ [F] để thu gom"
        };

        private void DrawCleanupPrompt()
        {
            if (!GUI.enabled || Event.current.type != EventType.Repaint || match.WasteAtRobot is not { } waste) return;
            var spec = CampaignCatalog.Trash[waste.Spec];
            TrashEffect effect = spec.Effect;
            bool canClean = match.CanCleanWasteAtRobot(out string blockedReason);
            Vector2Int cell = match.RobotCell;
            Vector2 p = CampaignMatch.CellToPixel(match.RobotLayer, cell.x, cell.y);
            float top = match.RobotLayer == Layer.Surface ? CampaignMatch.SurfY0 : CampaignMatch.DeepY0;
            float y = cell.y == 0 ? p.y + 13f : Mathf.Max(top + 1f, p.y - 38f);
            Rect box = new Rect(Mathf.Clamp(p.x - 73f, 18f, 602f), y, 146f, 23f);
            Color accent = blockedReason != null ? Danger
                : effect is TrashEffect.Shell or TrashEffect.Ballast or TrashEffect.Filament ? Cyan : Gold;
            Surface(box, Ink, accent, 4f);
            Label(box.x + 4f, box.y + 1f, box.width - 8f, canClean ? 15f : 21f,
                blockedReason ?? CleanupPrompt(spec), smallCenter, blockedReason != null ? accent : Ivory);
            if (canClean)
                Bar(new Rect(box.x + 6f, box.yMax - 6f, box.width - 12f, 3f), match.CleanupProgress, accent);
        }

        // ==================== BANNERS, MENU & OVERLAYS ====================

        private void DrawAchievementToast()
        {
            Rect r = new Rect(376, 306, 262, 50);
            Surface(r, Ink, Gold);
            Label(r.x + 12, r.y + 7, r.width - 20, 18, "★ " + vfx.ToastTitle, small, Gold);
            Label(r.x + 12, r.y + 26, r.width - 20, 18, vfx.ToastSub, small, Teal);
        }

        private void DrawMenu()
        {
            GUI.color = Color.white;
            if (estuary != null) GUI.DrawTexture(new Rect(0, 0, 768, 432), estuary, ScaleMode.ScaleAndCrop);
            else Fill(new Rect(0, 0, 768, 432), Ink);
            Fill(new Rect(0, 0, 768, 432), new Color(0.03f, 0.07f, 0.08f, 0.48f));

            float t = match == null ? Time.unscaledTime : visualTime;
            for (int i = 0; i < 14; i++)
            {
                float bx = 30f + (i * 57f) % 708f + Mathf.Sin(t * 1.8f + i) * 8f;
                float by = 420f - ((t * (18f + (i % 3) * 8f) + i * 43f) % 240f);
                DrawCircle(new Vector2(bx, by), 5f + (i % 3) * 2f, new Color(0.7f, 0.96f, 1f, 0.3f));
            }

            Label(28, 14, 460, 43, "BIO-BOT", title, Ivory);
            Label(30, 53, 460, 19, "ĐÊ XANH CẢM TỬ", text, Gold);

            Surface(new Rect(506, 18, 236, 44), Ink, Seam);
            Label(518, 24, 108, 16, $"★ {progress.TotalStars()}/{StageCount * 3} Sao", small, Gold);
            Label(518, 41, 108, 16, $"● {progress.EcoCoins} Xu", small, Teal);
            if (Click(new Rect(636, 26, 98, 26), audioSys.Muted ? "Âm: Tắt" : "Âm: Bật"))
            ToggleMute();

            Label(28, 84, 360, 18, "CHIẾN DỊCH", text, Gold);
            for (int i = 0; i < StageCount; i++)
            {
                int lv = i + 1;
                Rect card = new Rect(28, 106 + i * 64, 374, 58);
                bool unlocked = lv <= progress.UnlockedLevel;
                bool chosen = selectedLevel == lv;
                Surface(card, chosen ? CardSel : unlocked ? Panel : WithAlpha(Ink, 0.90f), chosen ? Gold : Seam);
                int stars = progress.Stars[i];
                string starStr = unlocked ? new string('★', stars) + new string('☆', 3 - stars) : "Chưa mở khóa";
                Label(card.x + 14, card.y + 5, 345, 19, $"{lv:00}. {CampaignCatalog.Levels[i].Name}", text, unlocked ? Color.white : Muted);
                Label(card.x + 14, card.y + 24, 345, 15, CampaignCatalog.Levels[i].Subtitle, small, Muted);
                Label(card.x + 14, card.y + 39, 345, 16, starStr, small, stars > 0 ? Gold : Muted);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && unlocked;
                if (Click(card, "", GUIStyle.none)) { selectedLevel = lv; audioSys.Play(SfxId.Click); }
                GUI.enabled = enabled;
            }

            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Rect info = new Rect(428, 106, 314, 246);
            Surface(info, WithAlpha(Panel, 0.94f), Seam);
            Label(442, 115, 286, 14, $"NHIỆM VỤ {selectedLevel:00}", small, Gold);
            Label(442, 132, 286, 23, spec.Name, heading, Ivory);
            Label(442, 159, 286, 45, spec.Story, wrap, Muted);
            Label(442, 206, 286, 14, "ĐỒNG MINH MỚI", small, Teal);

            int shown = 0;
            for (int id = 0; id < CampaignCatalog.Allies.Length && shown < 4; id++)
            {
                if (CampaignCatalog.Allies[id].Unlock == selectedLevel)
                {
                    float ax = 442 + shown * 72;
                    DrawAllySprite(id, new Rect(ax + 12, 223, 40, 40), 5, id, Color.white);
                    Label(ax, 265, 68, 13, CampaignCatalog.Allies[id].Name, smallCenter, Color.white);
                    Label(ax, 278, 68, 13, CampaignCatalog.Allies[id].ShortTag, smallCenter, Gold);
                    shown++;
                }
            }

            if (Click(new Rect(442, 304, 286, 36), $"Xem nhiệm vụ {selectedLevel:00}  [Enter]", primaryButton))
                OpenBriefing(selectedLevel, false);

            if (Click(new Rect(28, 368, 186, 44), $"Sinh tồn vô tận\nKỷ lục đợt {progress.EndlessBestWave}"))
                OpenBriefing(progress.UnlockedLevel, true);
            if (Click(new Rect(224, 368, 166, 44), $"Xưởng Bio-Bot\n{progress.EcoCoins} xu"))
            { overlay = Overlay.Shop; audioSys.Play(SfxId.Click); }
            if (Click(new Rect(400, 368, 166, 44), $"Thành tích\n{progress.TotalAchievements()}/{CampaignCatalog.PlayableAchievementIds.Length}"))
            { overlay = Overlay.Achievements; audioSys.Play(SfxId.Click); }
            int entries = CampaignCatalog.UnlockedAllyCount(StageCount) +
                CampaignCatalog.PlayableEnemyIds.Length + CampaignCatalog.PlayableTrashIds.Length;
            if (Click(new Rect(576, 368, 166, 44), $"Sổ sinh thái\n{entries} thực thể"))
            { guideTab = 0; guideScroll = 0f; overlay = Overlay.Guide; audioSys.Play(SfxId.Click); }
        }

        private void DrawBriefing()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.05f, 0.06f, 0.82f));
            Surface(new Rect(124, 54, 520, 324), Panel, Seam, 8f);
            var spec = CampaignCatalog.Levels[selectedLevel - 1];
            Label(147, 72, 474, 16, pendingEndless ? "SINH TỒN" : $"NHIỆM VỤ {selectedLevel:00}", small, Gold);
            Label(147, 91, 474, 29, pendingEndless ? "Sinh tồn vô tận" : spec.Name, heading, Ivory);
            Label(147, 124, 474, 44, pendingEndless
                ? "Cả hai tầng và bốn đồng minh đã mở. Giữ cửa đê lâu nhất có thể."
                : spec.Story, wrap, Muted);

            Surface(new Rect(147, 175, 474, 130), Ink, Seam);
            Label(159, 187, 450, 16, "CHỈ DẪN", small, Teal);
            Label(159, 207, 450, 91, pendingEndless
                ? "Bấm [Q] để lặn hoặc nổi. Địch ở cả hai tầng tiếp tục di chuyển khi Bio-Bot đổi tầng. Địch mạnh dần qua từng đợt."
                : spec.ConceptBody, wrap, Ivory);

            if (Click(new Rect(147, 324, 303, 36), "Triển khai  [Enter]", primaryButton))
                StartLevel(selectedLevel, pendingEndless);
            if (Click(new Rect(462, 324, 159, 36), "Quay lại  [Esc]")) overlay = Overlay.None;
        }

        private void DrawResult()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.84f));
            Rect r = new Rect(154, 62, 460, 308);
            Surface(r, Panel, match.Won ? Teal : Danger, 8f);
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

            bool next = !match.Endless && match.Won && selectedLevel < StageCount;
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
            string action = next ? "Màn tiếp theo  [Enter]" : match.Won && !match.Endless ? "Hoàn tất  [Enter]" : "Chơi lại  [Enter]";
            if (Click(new Rect(174, 286, 204, 38), action, primaryButton)) AdvanceFromResult();
            if (Click(new Rect(390, 286, 204, 38), "Về Bản Đồ & Xưởng")) ReturnToMenu();
        }

        private void DrawShop()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.92f));
            Rect r = new Rect(44, 28, 680, 376);
            Surface(r, Panel, Seam, 8f);
            Label(64, 42, 380, 26, "XƯỞNG NÂNG CẤP", heading, Gold);
            Label(444, 46, 160, 20, $"Xu hiện có: {progress.EcoCoins}", text, Teal);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            int upgradeSlot = 0;
            for (int i = 0; i < CampaignCatalog.Upgrades.Length; i++)
            {
                if (i == CampaignCatalog.RetiredPulseUpgrade) continue;
                int slot = upgradeSlot++;
                var up = CampaignCatalog.Upgrades[i];
                int lv = progress.UpgradeLevel(i);
                Rect card = new Rect(64 + (slot % 2) * 324, 86 + (slot / 2) * 96, 312, 84);
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
            Surface(r, Panel, Seam, 8f);
            Label(64, 42, 420, 26, $"★ THÀNH TÍCH ({progress.TotalAchievements()}/{CampaignCatalog.PlayableAchievementIds.Length})", heading, Gold);
            if (Click(new Rect(620, 40, 84, 28), "Đóng")) overlay = Overlay.None;

            for (int i = 0; i < CampaignCatalog.PlayableAchievementIds.Length; i++)
            {
                int id = CampaignCatalog.PlayableAchievementIds[i];
                var a = CampaignCatalog.Achievements[id];
                bool done = progress.Achievements[id];
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
            Surface(new Rect(28, 22, 712, 388), Panel, Seam, 8f);
            Label(48, 36, 340, 24, "SỔ SINH THÁI", heading, Teal);
            if (Click(new Rect(632, 34, 88, 28), "Đóng")) overlay = Overlay.None;

            string[] tabs = { "Đồng minh", "Đối thủ", "Rác & thiết bị", "Điều khiển" };
            for (int i = 0; i < tabs.Length; i++)
                if (Click(new Rect(48 + i * 144, 70, 136, 26), tabs[i], guideTab == i ? primaryButton : null))
                { guideTab = i; guideScroll = 0f; audioSys.Play(SfxId.Click); }

            if (guideTab == 3)
            {
                Fill(new Rect(48, 106, 672, 286), Ink);
                string howTo =
                    "DI CHUYỂN\nWASD / Mũi tên để bơi. Q lặn hoặc nổi. Đáy biển mở ở màn 02.\n\n" +
                    "ĐẶT ĐỒNG MINH\nChọn thẻ bằng chuột hoặc 1–5. Bấm vào ô ở tầng đang đứng hoặc dùng Space tại ô của Bio-Bot. V bật chế độ gỡ đồng minh.\n\n" +
                    "THU GOM\nBấm hạt vàng hoặc bơi gần để hút năng lượng. Giữ F tại ô rác để dựng khiên, lấy năng lượng, thả cá hoặc dọn Rác Cản. Cục Chì cần ô mặt nước tương ứng còn trống.\n\n" +
                    "PHÒNG THỦ\nShift lướt theo hướng đang nhìn. Đẩy Chai Phao về đê để nhận 50 NL hoặc đẩy về phía địch để tấn công. Esc tạm dừng.\n\n" +
                    $"NGUY HIỂM\nCảnh báo hiện trong {CampaignMatch.DangerDuration:0} giây khi địch đầu tiên xuất hiện ở tầng kia hoặc khi địch vượt qua 5 cột ngoài cùng bên phải. Bấm Q để sang tầng đang báo và tắt cảnh báo đó.";
                Label(64, 118, 640, 265, howTo, wrap, Color.white);
                return;
            }

            int count = guideTab == 0 ? CampaignCatalog.UnlockedAllyCount(StageCount)
                : guideTab == 1 ? CampaignCatalog.PlayableEnemyIds.Length : CampaignCatalog.PlayableTrashIds.Length;
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
                    int enemyId = CampaignCatalog.PlayableEnemyIds[i];
                    var e = CampaignCatalog.Enemies[enemyId];
                    int surfaceHp = e.SpawnHealth(Layer.Surface);
                    int deepHp = e.SpawnHealth(Layer.Deep);
                    string hp = e.Habitat == Habitat.Both ? $"{surfaceHp} mặt / {deepHp} đáy"
                        : (e.Habitat == Habitat.Deep ? deepHp : surfaceHp).ToString();
                    name = e.Name; meta = $"{HabitatTag(e.Habitat)} · HP {hp} · Màn {e.Unlock}"; hint = e.Hint;
                    DrawSprite(CampaignCatalog.EnemySpriteRow(enemyId), new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                }
                else
                {
                    int trashId = CampaignCatalog.PlayableTrashIds[i];
                    var tr = CampaignCatalog.Trash[trashId];
                    name = tr.Name; meta = $"{HabitatTag(tr.Habitat)} · Màn {tr.Unlock}"; hint = tr.Hint;
                    DrawSprite(CampaignCatalog.TrashSpriteRow(trashId), new Rect(4, y + 3, 38, 38), 5, i, Color.white);
                }
                Label(50, y + 4, 186, 18, name, text, Color.white);
                Label(50, y + 24, 186, 16, meta, small, Gold);
                Label(240, y + 6, 396, 34, hint, wrap, Muted);
            }
            GUI.EndScrollView();
            if (guideTab == 1)
                Label(54, 392, 660, 16, "HP lúc xuất hiện · Địch sinh dưới nước được +40% HP, trừ trùm · Vô Tận tăng HP theo đợt.", small, Muted);
        }

        private void DrawPause()
        {
            Fill(new Rect(0, 0, 768, 432), new Color(0.02f, 0.06f, 0.1f, 0.78f));
            Rect r = new Rect(244, 114, 280, 204);
            Surface(r, Panel, Seam, 8f);
            Label(244, 132, 280, 26, "TẠM DỪNG", heading, Gold);
            if (IconButton(new Rect(340, 169, 88, 37), "▶", false, "Tiếp tục [Esc]")) overlay = Overlay.None;
            if (Click(new Rect(274, 216, 220, 34), "↻  Chơi lại")) StartLevel(selectedLevel, match.Endless);
            if (Click(new Rect(274, 258, 220, 34), "Bản đồ")) ReturnToMenu();
        }

        // ==================== HELPERS ====================

        private void EnsureStyles()
        {
            if (text != null) return;
            small = MakeStyle(10, false, TextAnchor.UpperLeft);
            smallCenter = MakeStyle(10, true, TextAnchor.MiddleCenter);
            energyCaption = MakeStyle(8, true, TextAnchor.MiddleCenter);
            dangerPrompt = MakeStyle(14, true, TextAnchor.MiddleCenter);
            text = MakeStyle(12, true, TextAnchor.UpperLeft);
            heading = MakeStyle(16, true, TextAnchor.UpperLeft);
            title = MakeStyle(37, true, TextAnchor.UpperLeft);
            wrap = MakeStyle(11, false, TextAnchor.UpperLeft);
            wrap.wordWrap = true;
            button = MakeStyle(11, true, TextAnchor.MiddleCenter);
            button.clipping = TextClipping.Clip;
            toolbarIcon = MakeStyle(20, true, TextAnchor.MiddleCenter);
            primaryButton = new GUIStyle(button);
        }

        private static GUIStyle MakeStyle(int size, bool bold, TextAnchor align) =>
            new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                clipping = TextClipping.Clip,
                richText = false,
                wordWrap = false,
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
            GUI.color = HudTint(color);
            GUI.DrawTexture(new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter), circleTex);
        }

        private void DrawAllySprite(int allyId, Rect rect, float fps, int offset, Color tint)
        {
            int row = allyId >= 0 && allyId < CampaignCatalog.AllySpriteRow.Length ? CampaignCatalog.AllySpriteRow[allyId] : allyId;
            DrawSprite(row, rect, fps, offset, tint);
        }

        private static Rect AtlasUV(int index, int frame, int frames)
        {
            const int rows = CampaignCatalog.TotalAtlasRows;
            return new Rect(frame / (float)frames, 1f - (index + 1f) / rows, 1f / frames, 1f / rows);
        }

        private int SpriteFrame(float fps, int offset, int frames = 4)
        {
            float clock = match == null ? Time.unscaledTime : visualTime;
            return (Mathf.FloorToInt(clock * fps) + offset) % frames;
        }

        private void DrawSprite(int index, Rect rect, float fps, int offset, Color tint)
        {
            if (atlas == null) return;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, AtlasUV(index, SpriteFrame(fps, offset), 4));
        }

        private static Rect Around(Vector2 center, float size) => new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        private static string LayerName(Layer l) => l == Layer.Deep ? "ĐÁY BIỂN" : "MẶT NƯỚC";
        private static string HabitatTag(Habitat h) => h == Habitat.Both ? "Cả 2 Tầng" : h == Habitat.Deep ? "Đáy Biển" : "Mặt Nước";

        private void Label(float x, float y, float w, float h, string val, GUIStyle style, Color col)
        {
            if (style == title || style == heading)
            {
                GUI.color = new Color(0f, 0f, 0f, col.a * 0.65f);
                GUI.Label(new Rect(x + 1.2f, y + 1.2f, w, h), val, style);
            }
            GUI.color = HudTint(col);
            GUI.Label(new Rect(x, y, w, h), val, style);
        }

        private bool Click(Rect rect, string val, GUIStyle style = null)
        {
            if (style != GUIStyle.none)
            {
                bool primary = style == primaryButton;
                bool hot = GUI.enabled && rect.Contains(Event.current.mousePosition);
                Color accent = primary ? Gold : hot ? Teal : Seam;
                Color face = primary ? (hot ? new Color32(255, 218, 164, 255) : Gold) : hot ? CardSel : CardBg;
                Surface(rect, face, accent, 4f);
                Label(rect.x + 1, rect.y, rect.width - 2, rect.height, val, style ?? button,
                    primary ? Ink : GUI.enabled ? Ivory : Muted);
            }

            GUI.color = Color.white;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private bool IconButton(Rect rect, string icon, bool active, string hint)
        {
            bool hot = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color edge = active ? (icon == "×" ? Danger : Teal) : hot ? Muted : Seam;
            Surface(rect, active ? CardSel : CardBg, edge);
            Label(rect.x, rect.y, rect.width, rect.height, icon, toolbarIcon, active ? edge : Ivory);
            GUI.color = Color.white;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) return true;
            if (hot && Event.current.type == EventType.Repaint)
                controlHint = hint;
            return false;
        }

        private void Fill(Rect rect, Color col)
        {
            GUI.color = HudTint(col);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private void Bar(Rect rect, float frac, Color col)
        {
            Fill(rect, new Color(0.06f, 0.12f, 0.16f, 0.9f));
            rect.width *= Mathf.Clamp01(frac);
            Fill(rect, col);
        }

        private void Border(Rect rect, Color col)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1.5f), col);
            Fill(new Rect(rect.x, rect.yMax - 1.5f, rect.width, 1.5f), col);
            Fill(new Rect(rect.x, rect.y, 1.5f, rect.height), col);
            Fill(new Rect(rect.xMax - 1.5f, rect.y, 1.5f, rect.height), col);
        }
    }
}
