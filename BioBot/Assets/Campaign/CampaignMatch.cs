using System;
using System.Collections.Generic;
using UnityEngine;

namespace BioBot
{
    public enum ProjectileKind { Seed, Bubble, ChargedBubble, Spear, Zap, Push, Geyser, Slow }

    public sealed class CampaignMatch
    {
        public const int Rows = 5, Columns = 11, MaxIntegrity = 10;
        // Shared by the board renderer and hit testing.
        public const float SurfX0 = 100f, SurfY0 = 82f;
        public const float DeepX0 = 76f, DeepY0 = 226f;
        public const float CellW = 56f, CellH = 24f, ShearX = 10f;

        public sealed class Ally
        {
            public readonly int Spec;
            public int Health, MaxHealth, ShotsInCycle;
            public float Cooldown, Jammed, Temporary, PlantPop, FireAnim, HurtAnim, Phase, DrainTimer;
            public bool Draining;
            public TrashEffect? ShieldSource;
            public Ally(int spec, int health, float temporary = 0f, float phase = 0f, bool draining = false)
            {
                Spec = spec;
                Health = MaxHealth = health;
                Temporary = temporary;
                PlantPop = 0.34f;
                Cooldown = 0.55f;
                Phase = phase;
                Draining = draining;
                DrainTimer = OxygenInterval;
            }
        }

        public sealed class Waste
        {
            public readonly int Spec;
            public readonly bool Natural;
            public bool Active;
            public float Cooldown, BobPhase;
            public Waste(int spec, float phase = 0f, bool natural = false)
            {
                Spec = spec;
                BobPhase = phase;
                Natural = natural;
            }
        }

        public sealed class Enemy
        {
            public readonly int Spec;
            public Layer Layer;
            public int Row, Health, MaxHealth;
            public float X, Attack, Slow, Stun, Recovery, Flash, BossTimer, ActionTimer, BiteAnim, HurtAnim, WalkPhase;
            public bool Shifted, Broken, Small, Charged;
            public float DiveWindup, DiveArrival, LitterDist, ShootAnim;
            public SquidPullEffect SquidPull;
            internal int WarnedLayers;

            public Enemy(int spec, Layer layer, int row, float x, int health, bool small = false, float walkPhase = 0f)
            {
                Spec = spec;
                Layer = layer;
                Row = row;
                X = x;
                Health = MaxHealth = health;
                Small = small;
                BossTimer = 9f;
                ActionTimer = 4.5f;
                WalkPhase = walkPhase;
            }
        }

        public sealed class Projectile
        {
            public Layer Layer;
            public int Row, Damage, PierceLeft;
            public float X, Speed, Age;
            public ProjectileKind Kind;
            public bool Pulse, Slow, Push;
            public readonly HashSet<Enemy> Hit = new HashSet<Enemy>();
        }

        public sealed class SlidingBottle
        {
            public Layer Layer;
            public int Row, Hits;
            public float X, VelX;
            public readonly HashSet<Enemy> Hit = new HashSet<Enemy>();
        }

        public sealed class BioOrb
        {
            public Layer Layer;
            public Vector2 Pos;
            public float VelX, VelY, TargetY, Value, Life, Bob;
            public bool FlyingToBank;
            public Vector2 DrawPosition => Pos + new Vector2(0f, FlyingToBank ? 0f : Mathf.Sin(Bob) * 2.5f);
        }

        public sealed class TurbineWave
        {
            public Layer Layer;
            public int Row;
            public float X;
        }

        public sealed class BeamEffect
        {
            public Layer Layer;
            public int Row;
            public float From, To, Left;
            public Color Color;
            public bool Thick;
        }

        public sealed class SquidPullEffect
        {
            public Enemy Enemy;
            public Layer From, To;
            public int Row, AllySpec;
            public float X, Age;
        }

        public sealed class LitterShot
        {
            public Layer Layer;
            public Vector2 Start;
            public int Row, Col;
            public float Age;
        }

        private struct Arrival
        {
            public float Time;
            public int Wave, Spec, Row;
            public Layer Layer;
            public bool StartsSwarm;
        }

        public readonly Ally[,,] Allies = new Ally[2, Rows, Columns];
        public readonly Waste[,,] Trash = new Waste[2, Rows, Columns];
        public readonly bool[,] TurbineReady = new bool[2, Rows];
        public readonly float[] CardCooldowns = new float[CampaignCatalog.Allies.Length];
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<SlidingBottle> SlidingBottles = new List<SlidingBottle>();
        public readonly List<BioOrb> Orbs = new List<BioOrb>();
        public readonly List<TurbineWave> Turbines = new List<TurbineWave>();
        public readonly List<BeamEffect> Beams = new List<BeamEffect>();
        public readonly List<SquidPullEffect> SquidPulls = new List<SquidPullEffect>();
        public readonly List<LitterShot> LitterShots = new List<LitterShot>();

        private readonly List<Arrival> arrivals = new List<Arrival>();
        private readonly CampaignProgress progress;
        private readonly System.Random rng;
        private int nextArrival;
        private const int AmbientOrbValue = 15;
        private const float AmbientOrbInterval = 18f;
        public const float IncomeInterval = 10.5f;
        private const float OxygenInterval = 4.2f;
        private const float CleanupSeconds = 2.5f;
        public const float ClutterCleanupSeconds = 0.5f;
        public const float SquidPullSeconds = 0.42f;
        public const float SquidStunSeconds = 0.6f;
        public const float LitterFlightSeconds = 0.55f;
        public const float LitterRecoilSeconds = 0.28f;
        public const float LitterTileInterval = 2f;
        public const int LitterFirstColumn = 3, LitterLastColumn = 6;
        private const int NaturalTrashLimitPerLayer = 4;
        private static readonly Color SquidWake = new Color(0.85f, 0.65f, 1f);
        private static readonly Color LitterSplash = new Color(0.68f, 0.85f, 0.78f);
        public const float FerryWindupSeconds = 0.95f;
        public const float FerryArrivalSeconds = 0.45f;
        private const float RescueSeconds = 1.35f;

        private float skyOrbTimer = 13f;
        private Waste cleanupWaste;
        private float cleanupHeld;
        private bool cleanupRequiresRelease;
        private float trashDropTimer = 20f;
        private float invulnerable;
        private float dashCooldown;
        private float dashActive;
        private int dashDirection;
        private int endlessWaveScheduled;
        private readonly float[] nextDangerWarning = new float[2];
        private readonly float[] dangerVisibleUntil = new float[2];
        private int encounteredLayers;
        private readonly int[][] eligibleTrash;
        private const float DangerCooldown = 10f;
        public const float DangerDuration = 4.0f;

        // Bio-Bot split-view swimming coordinates (0..Columns-1, 0..Rows-1)
        public Layer RobotLayer { get; private set; } = Layer.Surface;
        public float RobotX { get; set; } = 0.6f;
        public float RobotY { get; set; } = 2.0f;
        public bool RobotMoving { get; set; }
        public bool RobotFacingLeft { get; set; }
        public Vector2Int RobotCell => new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(RobotX), 0, Columns - 1),
            Mathf.Clamp(Mathf.RoundToInt(RobotY), 0, Rows - 1));

        public readonly int Level;
        public readonly bool Endless;
        public LevelSpec Rules => CampaignCatalog.Levels[Mathf.Clamp(Level - 1, 0, CampaignCatalog.Levels.Length - 1)];
        public bool DeepEnabled => Endless || Rules.DeepActive;
        public int Energy { get; private set; }
        public float EnergyBounce { get; private set; }
        public int Integrity { get; private set; } = MaxIntegrity;
        public int MaxRobotHealth => 4 + progress.UpgradeLevel(0);
        public int RobotHealth { get; private set; }
        public float RobotSpeedBonus => (1f + progress.UpgradeLevel(1) * 0.15f) * (dashActive > 0f ? 2.15f : 1f);
        public float DashCooldownLeft => Mathf.Max(0f, dashCooldown);
        public bool IsDashing => dashActive > 0f;
        public float MagnetRadius => 1.35f + progress.UpgradeLevel(3) * 0.45f;


        public int Wave { get; private set; }
        public int TotalWaves => Endless ? Mathf.Max(Wave, endlessWaveScheduled) : Rules.Waves;
        public float WaveProgress => arrivals.Count == 0 ? 0f : nextArrival / (float)arrivals.Count;
        public float Time { get; private set; }
        public float Respawn { get; private set; }
        public bool Won { get; private set; }
        public bool Lost => Integrity <= 0;
        public bool Ended => Won || Lost;
        public string Message { get; private set; }
        public float Feedback { get; private set; }
        public int Score { get; private set; }
        public int CoinsEarned { get; private set; }
        public int BottlesDocked { get; private set; }
        public int FishRescued { get; private set; }
        public int Kills { get; private set; }
        public int PlantedIncomeCount { get; private set; }
        public int PlantedFighterCount { get; private set; }
        public int OrbsCollectedCount { get; private set; }
        public int BottlesPushedCount { get; private set; }
        public int TrashUsedCount { get; private set; }
        public Waste WasteAtRobot
        {
            get
            {
                if (Ended || Respawn > 0f) return null;
                Vector2Int cell = RobotCell;
                return Trash[(int)RobotLayer, cell.y, cell.x];
            }
        }

        public bool CanCleanWasteAtRobot(out string blockedReason)
        {
            blockedReason = null;
            var waste = WasteAtRobot;
            if (waste == null) return false;
            var effect = CampaignCatalog.Trash[waste.Spec].Effect;
            if (effect == TrashEffect.Float) return false;
            if (effect == TrashEffect.Ballast)
            {
                Vector2Int cell = RobotCell;
                if (!Free(Layer.Surface, cell.y, cell.x))
                {
                    blockedReason = "Ô mặt nước bị chiếm";
                    return false;
                }
            }
            return true;
        }

        public float CleanupProgress => cleanupWaste != null && cleanupWaste == WasteAtRobot && CanCleanWasteAtRobot(out _)
            ? Mathf.Clamp01(cleanupHeld / CleanupDuration(cleanupWaste))
            : 0f;

        private static float CleanupDuration(Waste waste) =>
            CampaignCatalog.Trash[waste.Spec].Effect switch
            {
                TrashEffect.Filament => RescueSeconds,
                TrashEffect.Clutter => ClutterCleanupSeconds,
                _ => CleanupSeconds
            };

        public void InterruptCleanup()
        {
            if (cleanupWaste != null) cleanupRequiresRelease = true;
            cleanupWaste = null;
            cleanupHeld = 0f;
        }

        public void UpdateCleanup(bool held, float delta)
        {
            if (!held)
            {
                InterruptCleanup();
                cleanupRequiresRelease = false;
                return;
            }

            Waste target = CanCleanWasteAtRobot(out _) ? WasteAtRobot : null;
            if (cleanupWaste != null && cleanupWaste != target) InterruptCleanup();
            if (cleanupRequiresRelease || target == null) return;

            cleanupWaste = target;
            cleanupHeld += delta;
            if (cleanupHeld + 0.0001f < CleanupDuration(target)) return;

            Vector2Int cell = RobotCell;
            CollectWaste(RobotLayer, cell.y, cell.x, target);
            InterruptCleanup();
        }

        public bool HasDangerWarning(Layer layer) => Time < dangerVisibleUntil[(int)layer];

        public float DangerWarningLeft(Layer layer) =>
            Mathf.Max(0f, dangerVisibleUntil[(int)layer] - Time);


        public Enemy ActiveBoss
        {
            get
            {
                for (int i = 0; i < Enemies.Count; i++)
                    if (CampaignCatalog.Enemies[Enemies[i].Spec].Trait == Trait.Boss)
                        return Enemies[i];
                return null;
            }
        }

        public Action<SfxId> OnSfx;
        public Action<Layer, Vector2, Color, int> OnBurst;
        public Action<Layer, Vector2, Color, float> OnRing;
        public Action<Layer, Vector2, string, Color> OnFloatText;
        public Action<float, float> OnShake;
        public Action<string> OnAchievement;

        public CampaignMatch(int level, CampaignProgress progress, bool endless = false)
        {
            Level = level;
            Endless = endless;
            this.progress = progress;
            rng = new System.Random(1021 + level * 73 + (endless ? Environment.TickCount & 0xFFFF : 0));
            RobotHealth = MaxRobotHealth;
            Energy = (endless ? 275 : Rules.Budget) + progress.UpgradeLevel(4) * 15;

            for (int s = 0; s < 2; s++)
                for (int r = 0; r < Rows; r++)
                    TurbineReady[s, r] = s == (int)Layer.Surface || r == Rows / 2;

            Say(endless
                ? "CHẾ ĐỘ VÔ TẬN: Nhìn thấy cả 2 tầng cùng lúc — giữ Đê Xanh lâu nhất có thể!"
                : Rules.Lesson, 9f);

            eligibleTrash = new int[2][];
            for (int side = 0; side < (DeepEnabled ? 2 : 1); side++)
            {
                var trashIds = new List<int>();
                for (int i = 0; i < CampaignCatalog.Trash.Length; i++)
                {
                    var spec = CampaignCatalog.Trash[i];
                    if (spec.Unlock <= (Endless ? CampaignCatalog.PlayableStageCount : Level) &&
                        spec.Effect != TrashEffect.Clutter && CampaignCatalog.Fits(spec.Habitat, (Layer)side))
                        trashIds.Add(i);
                }
                eligibleTrash[side] = trashIds.ToArray();
            }

            if (endless) ScheduleEndlessWave(1, 11f);
            else CreateCampaignArrivals();
            PlaceStartingTrash();
        }

        // Exact 3D Oblique Projection for Top Face (Surface) and Bottom Face (Deep)!
        public static Vector2 CellToPixel(Layer layer, float col, float row)
        {
            float x0 = layer == Layer.Surface ? SurfX0 : DeepX0;
            float y0 = layer == Layer.Surface ? SurfY0 : DeepY0;
            float rCenter = row + 0.5f;
            float cCenter = col + 0.5f;
            return new Vector2(x0 - rCenter * ShearX + cCenter * CellW, y0 + rCenter * CellH);
        }

        public static Vector2 CellCorner(Layer layer, float col, float row)
        {
            float x0 = layer == Layer.Surface ? SurfX0 : DeepX0;
            float y0 = layer == Layer.Surface ? SurfY0 : DeepY0;
            return new Vector2(x0 - row * ShearX + col * CellW, y0 + row * CellH);
        }

        public static bool TryPixelToCell(Vector2 pos, bool deepAllowed, out Layer layer, out int col, out int row)
        {
            if (TryHitFace(pos, Layer.Surface, out col, out row)) { layer = Layer.Surface; return true; }
            if (deepAllowed && TryHitFace(pos, Layer.Deep, out col, out row)) { layer = Layer.Deep; return true; }
            layer = Layer.Surface;
            col = row = 0;
            return false;
        }

        private static bool TryHitFace(Vector2 pos, Layer face, out int col, out int row)
        {
            float x0 = face == Layer.Surface ? SurfX0 : DeepX0;
            float y0 = face == Layer.Surface ? SurfY0 : DeepY0;
            float fr = (pos.y - y0) / CellH;
            if (fr < 0f || fr >= Rows) { col = row = 0; return false; }
            float fc = (pos.x - (x0 - fr * ShearX)) / CellW;
            if (fc < 0f || fc >= Columns) { col = row = 0; return false; }
            row = Mathf.FloorToInt(fr);
            col = Mathf.FloorToInt(fc);
            return true;
        }

        public bool HasUpdraftAt(int row, int col)
        {
            var w = Trash[(int)Layer.Deep, row, col];
            if (w != null && w.Active && CampaignCatalog.Trash[w.Spec].Effect == TrashEffect.Vent) return true;
            var a = Allies[(int)Layer.Deep, row, col];
            return a != null && CampaignCatalog.Allies[a.Spec].BoostsSurface;
        }

        private void CreateCampaignArrivals()
        {
            if (Rules.Arrivals == null)
            {
                CreateReservedStageArrivals();
                return;
            }

            int[] rows = CampaignCatalog.ArrivalRows;
            for (int wave = 0; wave < Rules.Arrivals.Length; wave++)
            {
                foreach (WaveSegment segment in Rules.Arrivals[wave].Segments)
                {
                    float time = segment.Start;
                    int gapTransitions = Mathf.Max(1, segment.Enemies.Length - 2);
                    for (int i = 0; i < segment.Enemies.Length; i++)
                    {
                        int lane = segment.LanePattern?[i] ?? i;
                        arrivals.Add(new Arrival
                        {
                            Time = time,
                            Wave = wave + 1,
                            Spec = segment.Enemies[i],
                            Row = rows[(segment.LaneOffset + lane) % rows.Length],
                            Layer = segment.Layer,
                            StartsSwarm = segment.StartsSwarm && i == 0
                        });
                        time += Mathf.Lerp(segment.FirstGap, segment.LastGap, i / (float)gapTransitions);
                    }
                }
            }
            arrivals.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        private void CreateReservedStageArrivals()
        {
            var eligible = new List<int>();
            for (int i = 0; i < CampaignCatalog.Enemies.Length; i++)
            {
                var spec = CampaignCatalog.Enemies[i];
                if (spec.Trait != Trait.Boss && spec.Unlock <= Level)
                    eligible.Add(i);
            }

            void Add(float t, int wave, int spec, Layer layer, int row) =>
                arrivals.Add(new Arrival { Time = t, Wave = wave, Spec = spec, Layer = layer, Row = row });

            // Default schedule for reserved stages.
            float tCur = 8.5f;
            var introduced = eligible.FindAll(id => CampaignCatalog.Enemies[id].Unlock == Level);
            for (int wave = 1; wave <= Rules.Waves; wave++)
            {
                bool isHuge = wave >= 2;
                int count = isHuge ? (5 + Level / 2 + wave) : (4 + Level / 3);
                float spacing = isHuge ? Mathf.Max(1.35f, 2.3f - Level * 0.08f) : 3.2f;
                for (int n = 0; n < count; n++)
                {
                    int spec = (n < introduced.Count && wave == 1)
                        ? introduced[n]
                        : eligible[rng.Next(eligible.Count)];
                    Habitat hab = CampaignCatalog.Enemies[spec].Habitat;
                    Layer target = (!DeepEnabled || rng.Next(100) < Rules.SurfaceWeight) ? Layer.Surface : Layer.Deep;
                    if (DeepEnabled && !CampaignCatalog.Fits(hab, target)) target = CampaignCatalog.Opposite(target);
                    Add(tCur + n * spacing, wave, spec, target, rng.Next(Rows));
                }

                if (Rules.HasBoss && wave == Rules.Waves)
                    Add(tCur + count * spacing + 2.0f, wave, CampaignCatalog.BossId, Layer.Surface, 2);

                tCur += count * spacing + (isHuge ? 13f : 10f);
            }
        }

        private void ScheduleEndlessWave(int waveNum, float startTime)
        {
            endlessWaveScheduled = waveNum;
            // All previous arrivals have already spawned. Keep only the current wave.
            arrivals.Clear();
            nextArrival = 0;
            int count = 4 + waveNum * 2;
            float spacing = Mathf.Max(1.8f, 3.6f - waveNum * 0.1f);
            for (int n = 0; n < count; n++)
            {
                int spec = CampaignCatalog.PlayableEnemyIds[rng.Next(CampaignCatalog.PlayableEnemyIds.Length)];
                Habitat hab = CampaignCatalog.Enemies[spec].Habitat;
                Layer target = rng.Next(100) < 52 ? Layer.Surface : Layer.Deep;
                if (!CampaignCatalog.Fits(hab, target)) target = CampaignCatalog.Opposite(target);
                arrivals.Add(new Arrival
                {
                    Time = startTime + n * spacing,
                    Wave = waveNum,
                    Spec = spec,
                    Row = rng.Next(Rows),
                    Layer = target
                });
            }
        }

        private void PlaceStartingTrash()
        {
            int maxUnlock = Endless ? CampaignCatalog.PlayableStageCount : Level;
            foreach (var (spec, layer, row, col, lastStage) in CampaignCatalog.StartingTrash)
            {
                if (maxUnlock <= lastStage && CampaignCatalog.Trash[spec].Unlock <= maxUnlock &&
                    (layer == Layer.Surface || DeepEnabled))
                    AddWaste(spec, layer, row, col, natural: true);
            }
        }

        private void AddWaste(int id, Layer layer, int row, int col, bool natural = false)
        {
            if (col < 0 || col >= Columns || !Free(layer, row, col)) return;
            Trash[(int)layer, row, col] = new Waste(id, (float)rng.NextDouble() * 6.28f, natural);
        }

        private bool DropNaturalTrash(Layer layer)
        {
            int side = (int)layer;
            int count = 0;
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    if (Trash[side, row, col]?.Natural == true) count++;
            if (count >= NaturalTrashLimitPerLayer || eligibleTrash[side].Length == 0) return false;

            const int dropCells = Rows * (Columns - 5);
            int start = rng.Next(dropCells);
            for (int offset = 0; offset < dropCells; offset++)
            {
                int tile = (start + offset) % dropCells;
                int row = tile % Rows;
                int col = 3 + tile / Rows;
                if (!Free(layer, row, col)) continue;
                int[] choices = eligibleTrash[side];
                AddWaste(choices[rng.Next(choices.Length)], layer, row, col, natural: true);
                return true;
            }
            return false;
        }

        // Basic surface creatures (Vịt = 1, Rùa = 2) can be placed underwater in emergencies, but lose HP gradually!
        public static bool IsEmergencyDeepPlacement(int allyId, Layer layer) =>
            layer == Layer.Deep && (allyId == 1 || allyId == 2);

        private string CellActionBlocked(Layer layer, int row, int col)
        {
            if (Ended || row < 0 || row >= Rows || col < 0 || col >= Columns) return "";
            if (Respawn > 0f) return "Bio-Bot đang hồi phục.";
            if (layer == Layer.Deep && !DeepEnabled) return "Đáy biển mở ở màn 02.";
            if (layer != RobotLayer) return layer == Layer.Deep
                ? "Bấm [Q] để lặn xuống đáy biển." : "Bấm [Q] để nổi lên mặt nước.";
            return null;
        }

        public bool CanPlant(int id, Layer layer, int row, int col, out string reason)
        {
            reason = CellActionBlocked(layer, row, col);
            if (reason != null || id < 0 || id >= CampaignCatalog.Allies.Length) return false;
            var spec = CampaignCatalog.Allies[id];
            if (spec.Unlock > (Endless ? CampaignCatalog.PlayableStageCount : Level)) { reason = "Chưa mở khóa."; return false; }
            if (CardCooldowns[id] > 0f) { reason = "Thẻ đang hồi chiêu."; return false; }
            bool emergencyDeep = IsEmergencyDeepPlacement(id, layer);
            if (!CampaignCatalog.Fits(spec.Habitat, layer) && !emergencyDeep)
            {
                reason = spec.Habitat == Habitat.Deep
                    ? $"{spec.Name} là sinh vật đáy — KHÔNG thể sống trên Mặt Nước!"
                    : $"{spec.Name} sống ở mặt nước. Bấm [Q] để nổi.";
                return false;
            }
            if (!Free(layer, row, col))
            { reason = "Ô đã có vật thể. Bơi tới ô rác rồi giữ [F] để dọn."; return false; }
            if (Energy < spec.Cost) { reason = $"Cần {spec.Cost} NL."; return false; }
            return true;
        }

        public bool Plant(int id, Layer layer, int row, int col)
        {
            if (!CanPlant(id, layer, row, col, out string reason))
            {
                if (!string.IsNullOrEmpty(reason)) Say(reason, 2.4f);
                return false;
            }
            var spec = CampaignCatalog.Allies[id];
            bool emergencyDeep = IsEmergencyDeepPlacement(id, layer);
            Energy -= spec.Cost;
            float rechargeMult = Mathf.Max(0.45f, 1f - progress.UpgradeLevel(5) * 0.15f);
            CardCooldowns[id] = spec.Recharge * rechargeMult;
            var planted = new Ally(id, spec.Health, 0f, (float)rng.NextDouble() * 6.28f, emergencyDeep);
            if (spec.Ability == Ability.Income) planted.Cooldown = 2.7f;
            Allies[(int)layer, row, col] = planted;

            Vector2 px = CellToPixel(layer, col, row);
            OnSfx?.Invoke(SfxId.Plant);
            OnBurst?.Invoke(layer, px, emergencyDeep ? new Color(0.75f, 0.7f, 1f, 1f) : new Color(0.55f, 0.95f, 0.72f, 1f), 9);
            OnRing?.Invoke(layer, px, new Color(0.6f, 1f, 0.85f, 0.9f), 28f);
            OnFloatText?.Invoke(layer, px - new Vector2(0, 10), emergencyDeep ? $"{spec.Name} (O₂↓)" : spec.Name,
                emergencyDeep ? new Color(1f, 0.75f, 0.45f, 1f) : new Color(0.75f, 1f, 0.88f, 1f));
            if (spec.Ability == Ability.Income) PlantedIncomeCount++;
            else PlantedFighterCount++;
            if (spec.Reach != Reach.Local) OnAchievement?.Invoke("cross_layer");
            Say(emergencyDeep
                ? $"{spec.Name} đặt xuống Đáy sẽ bị ngộp nước & mất máu dần!"
                : "Đã triệu hồi " + spec.Name + "!", 2.5f);
            return true;
        }

        public bool RemoveAlly(Layer layer, int row, int col)
        {
            string blocked = CellActionBlocked(layer, row, col);
            if (blocked != null) return blocked.Length > 0 && Refuse(blocked);
            int side = (int)layer;
            var ally = Allies[side, row, col];
            if (ally == null) return Refuse("Ô này không có đồng minh để gỡ.");
            var spec = CampaignCatalog.Allies[ally.Spec];
            int refund = ally.Temporary > 0f ? 0 : spec.Cost / 2;
            Allies[side, row, col] = null;
            if (refund > 0) Energy += refund;
            Vector2 px = CellToPixel(layer, col, row);
            OnSfx?.Invoke(SfxId.Click);
            OnBurst?.Invoke(layer, px, new Color(0.85f, 0.9f, 0.95f, 1f), 6);
            if (refund > 0) OnFloatText?.Invoke(layer, px, $"+{refund} NL", new Color(1f, 0.88f, 0.45f, 1f));
            return true;
        }

        private void CollectWaste(Layer layer, int row, int col, Waste waste)
        {
            int side = (int)layer;
            var spec = CampaignCatalog.Trash[waste.Spec];
            if (spec.Effect is TrashEffect.Shell or TrashEffect.Ballast)
            {
                bool ballast = spec.Effect == TrashEffect.Ballast;
                Layer targetLayer = ballast ? Layer.Surface : layer;
                Trash[side, row, col] = null;
                DeployTrashShield(targetLayer, row, col, ballast ? 16 : 14, ballast ? 30f : 28f, spec.Effect);
                if (ballast)
                {
                    OnRing?.Invoke(layer, CellToPixel(layer, col, row), new Color(0.75f, 0.97f, 1f), 32f);
                    OnAchievement?.Invoke("cross_layer");
                }
                TrashUsedCount++;
                OnSfx?.Invoke(SfxId.Plant);
                Say(ballast ? "Đã dựng khiên trên mặt nước." : "Đã dựng khiên vỏ.", 2.5f);
                return;
            }

            Trash[side, row, col] = null;
            Vector2 px = CellToPixel(layer, col, row);
            if (spec.Effect == TrashEffect.Clutter)
            {
                OnSfx?.Invoke(SfxId.PushBottle);
                OnBurst?.Invoke(layer, px, LitterSplash, 6);
                Say("Đã dọn Rác Cản.", 2.2f);
                return;
            }

            int value = spec.Value;
            if (value > 0)
            {
                Energy += value;
                EnergyBounce = 0.25f;
            }
            CoinsEarned += 2;
            Score += 20;

            if (spec.Effect == TrashEffect.Filament && Allies[side, row, col] == null)
            {
                Allies[side, row, col] = new Ally(3, CampaignCatalog.Allies[3].Health, 0f, (float)rng.NextDouble() * 6.28f);
                FishRescued++;
                CoinsEarned += 5;
                Score += 75;
                OnSfx?.Invoke(SfxId.RescueFish);
                OnBurst?.Invoke(layer, px, new Color(0.4f, 0.95f, 1f, 1f), 14);
                OnRing?.Invoke(layer, px, new Color(0.45f, 1f, 0.95f, 1f), 38f);
                OnFloatText?.Invoke(layer, px - new Vector2(0, 10), "ĐÃ THẢ CÁ", new Color(0.45f, 1f, 0.95f, 1f));
                OnAchievement?.Invoke("first_rescue");
                Say("Đã cắt dây cước! Cá Xạ Thủ gia nhập Đáy Biển!", 3.2f);
            }
            else
            {
                OnSfx?.Invoke(SfxId.CollectOrb);
                OnBurst?.Invoke(layer, px, new Color(1f, 0.85f, 0.45f, 1f), 8);
                OnFloatText?.Invoke(layer, px, $"+{value} NL", new Color(1f, 0.88f, 0.45f, 1f));
                Say($"Thu gom {spec.Name}: +{value} NL.", 2.2f);
            }
        }

        public bool UseTrash(Layer layer, int row, int col)
        {
            string blocked = CellActionBlocked(layer, row, col);
            if (blocked != null) return blocked.Length > 0 && Refuse(blocked);
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste == null) return Refuse("Cần rác ở ô này để kích hoạt.");
            var spec = CampaignCatalog.Trash[waste.Spec];
            Layer opposite = CampaignCatalog.Opposite(layer);
            int other = (int)opposite;
            Vector2 px = CellToPixel(layer, col, row);

            switch (spec.Effect)
            {
                case TrashEffect.Float:
                    return Refuse("Bơi chạm vào chai để đẩy.");
                case TrashEffect.Filament:
                    return Refuse("Giữ [F] tại Dây Cước để thả cá.");
                case TrashEffect.Shell:
                case TrashEffect.Ballast:
                    return Refuse("Giữ [F] tại ô rác để dựng khiên.");
                case TrashEffect.Salvage:
                    return Refuse("Giữ [F] tại Phế Liệu Vàng để nhận năng lượng.");
                case TrashEffect.Clutter:
                    return Refuse(spec.QuickAction);
                case TrashEffect.Capsule:
                    if (!Free(opposite, row, col)) return Refuse("Ô tầng kia bị chiếm.");
                    Trash[other, row, col] = waste;
                    OnFloatText?.Invoke(layer, px, "ĐỔI TẦNG!", new Color(0.6f, 0.9f, 1f, 1f));
                    break;
                case TrashEffect.Mirror:
                case TrashEffect.Vent:
                    if (waste.Active) return Refuse("Đã kích hoạt.");
                    waste.Active = true;
                    TrashUsedCount++;
                    OnSfx?.Invoke(SfxId.Plant);
                    OnRing?.Invoke(layer, px, new Color(0.55f, 1f, 0.95f, 1f), 38f);
                    OnFloatText?.Invoke(layer, px, "LIÊN TẦNG!", new Color(0.55f, 1f, 0.95f, 1f));
                    OnAchievement?.Invoke("cross_layer");
                    Say(spec.Name + " đã kết nối 2 tầng!", 3f);
                    return true;
                case TrashEffect.Battery:
                    Shock(layer, row, col, 6);
                    Shock(opposite, row, col, 6);
                    OnSfx?.Invoke(SfxId.Pulse);
                    OnShake?.Invoke(0.22f, 4.5f);
                    OnAchievement?.Invoke("cross_layer");
                    break;
                case TrashEffect.Spill:
                    HitNear(layer, row, col, 1.5f, 4, true);
                    OnSfx?.Invoke(SfxId.Pulse);
                    OnBurst?.Invoke(layer, px, new Color(1f, 0.55f, 0.25f, 1f), 12);
                    break;
                case TrashEffect.Loom:
                    if (waste.Active) return Refuse("Lưới đã giăng.");
                    waste.Active = true;
                    waste.Cooldown = 22f;
                    TrashUsedCount++;
                    AddBeam(opposite, row, col, Columns - 1, new Color(0.5f, 0.95f, 0.85f, 1f), true);
                    OnSfx?.Invoke(SfxId.Plant);
                    OnAchievement?.Invoke("cross_layer");
                    Say("Lưới Cuộn hãm chậm cả hàng đối diện!", 3f);
                    return true;
            }
            Trash[side, row, col] = null;
            TrashUsedCount++;
            OnSfx?.Invoke(SfxId.PushBottle);
            OnBurst?.Invoke(layer, px, new Color(0.65f, 0.95f, 1f, 1f), 9);
            Say("Đã kích hoạt " + spec.Name + "!", 2.5f);
            return true;
        }

        private void DeployTrashShield(Layer layer, int row, int col, int health, float duration, TrashEffect source)
        {
            Allies[(int)layer, row, col] = new Ally(2, health, duration) { ShieldSource = source };
            Vector2 p = CellToPixel(layer, col, row);
            Color color = source == TrashEffect.Shell ? new Color(1f, 0.82f, 0.49f) : new Color(0.55f, 0.92f, 1f);
            OnRing?.Invoke(layer, p, color, 43f);
            OnBurst?.Invoke(layer, p, color, 13);
        }

        public bool PushFloat(Layer layer, int row, int col, int direction, bool turbo = false)
        {
            if (Ended || direction == 0) return false;
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste == null || CampaignCatalog.Trash[waste.Spec].Effect != TrashEffect.Float) return false;

            Trash[side, row, col] = null;
            BottlesPushedCount++;
            float speed = (turbo || dashActive > 0f) ? 8.2f : 5.6f;
            SlidingBottles.Add(new SlidingBottle { Layer = layer, Row = row, X = col, VelX = direction > 0 ? speed : -speed });
            Vector2 px = CellToPixel(layer, col, row);
            OnSfx?.Invoke(SfxId.PushBottle);
            OnBurst?.Invoke(layer, px, new Color(0.7f, 0.96f, 1f, 1f), 7);
            return true;
        }

        public bool TriggerHydroDash(int dirX)
        {
            if (Ended) return false;
            if (Respawn > 0f) return Refuse("Bio-Bot đang hồi phục.");
            if (dashCooldown > 0f) return Refuse("Lướt đang hồi.");
            dashDirection = dirX < 0 ? -1 : 1;
            RobotFacingLeft = dashDirection < 0;
            dashCooldown = 1.8f;
            dashActive = 0.28f;
            invulnerable = Mathf.Max(invulnerable, 0.45f);
            var cell = RobotCell;
            Vector2 px = CellToPixel(RobotLayer, RobotX, RobotY);
            OnSfx?.Invoke(SfxId.Dive);
            OnBurst?.Invoke(RobotLayer, px, new Color(0.55f, 0.98f, 1f, 1f), 12);
            OnRing?.Invoke(RobotLayer, px, new Color(0.55f, 0.98f, 1f, 0.9f), 34f);
            OnFloatText?.Invoke(RobotLayer, px - new Vector2(0, 10), "HYDRO-DASH!", new Color(0.55f, 0.98f, 1f, 1f));

            int adjacent = cell.x + dashDirection;
            if (!PushFloat(RobotLayer, cell.y, cell.x, dashDirection, true) && adjacent >= 0 && adjacent < Columns)
                PushFloat(RobotLayer, cell.y, adjacent, dashDirection, true);
            return true;
        }

        public bool CollectOrbAt(Vector2 pixelPos)
        {
            if (Ended) return false;
            BioOrb closest = null;
            float distance = 13f * 13f;
            foreach (var orb in Orbs)
            {
                if (orb.FlyingToBank || orb.Layer != RobotLayer) continue;
                float candidate = (orb.DrawPosition - pixelPos).sqrMagnitude;
                if (candidate > distance) continue;
                distance = candidate;
                closest = orb;
            }
            if (closest == null) return false;
            StartOrbFly(closest);
            return true;
        }

        private void StartOrbFly(BioOrb orb)
        {
            orb.FlyingToBank = true;
            orb.Life = 2f;
            OrbsCollectedCount++;
            OnSfx?.Invoke(SfxId.CollectOrb);
            OnBurst?.Invoke(orb.Layer, orb.Pos, new Color(1f, 0.94f, 0.42f, 1f), 5);
            OnFloatText?.Invoke(orb.Layer, orb.Pos, $"+{Mathf.RoundToInt(orb.Value)}", new Color(1f, 0.94f, 0.45f, 1f));
        }

        public void SpawnOrb(Layer layer, Vector2 startPixel, float targetY, int value, bool popArc = false)
        {
            Orbs.Add(new BioOrb
            {
                Layer = layer,
                Pos = startPixel,
                VelX = popArc ? (float)(rng.NextDouble() * 34.0 - 17.0) : 0f,
                VelY = popArc ? -92f : 12f,
                TargetY = targetY,
                Value = value,
                Life = 16f,
                Bob = (float)rng.NextDouble() * 6.28f
            });
        }

        public void MoveRobot(Vector2 input, float delta)
        {
            if (Ended || Respawn > 0f) { RobotMoving = false; return; }
            if (IsDashing) input = new Vector2(dashDirection, 0f);
            var cell = RobotCell;
            var w = Trash[(int)RobotLayer, cell.y, cell.x];
            var hType = w == null ? (TrashEffect?)null : CampaignCatalog.Trash[w.Spec].Effect;
            float terrain = hType is TrashEffect.Filament or TrashEffect.Spill ? 0.6f : 1f;
            float spd = 3.2f * RobotSpeedBonus * terrain;

            RobotMoving = input.sqrMagnitude > 0.01f;
            if (RobotMoving)
            {
                if (Mathf.Abs(input.x) > 0.05f) RobotFacingLeft = input.x < 0f;
                RobotX = Mathf.Clamp(RobotX + input.x * spd * delta, 0f, Columns - 1f);
                RobotY = Mathf.Clamp(RobotY - input.y * (spd * 0.85f) * delta, 0f, Rows - 1f);
                Vector2Int destination = RobotCell;
                if (destination != cell)
                {
                    InterruptCleanup();
                    int pushDirection = destination.x == cell.x ? (RobotFacingLeft ? -1 : 1) : Math.Sign(destination.x - cell.x);
                    PushFloat(RobotLayer, destination.y, destination.x, pushDirection, IsDashing);
                }
            }
        }

        public void ToggleRobotLayer()
        {
            if (Ended) return;
            if (Respawn > 0f) { Refuse("Bio-Bot đang hồi phục."); return; }
            if (!DeepEnabled) { Refuse("Đáy biển mở ở màn 02."); return; }
            InterruptCleanup();
            RobotLayer = CampaignCatalog.Opposite(RobotLayer);
            dangerVisibleUntil[(int)RobotLayer] = 0f;
            Vector2 px = CellToPixel(RobotLayer, RobotX, RobotY);
            OnSfx?.Invoke(RobotLayer == Layer.Deep ? SfxId.Dive : SfxId.Surface);
            OnBurst?.Invoke(RobotLayer, px, new Color(0.55f, 0.96f, 1f, 1f), 10);
            OnRing?.Invoke(RobotLayer, px, Color.white, 36f);
        }

        public void Tick(float delta)
        {
            if (Ended) return;
            Time += delta;
            Feedback = Mathf.Max(0f, Feedback - delta);
            invulnerable = Mathf.Max(0f, invulnerable - delta);
            dashCooldown = Mathf.Max(0f, dashCooldown - delta);
            dashActive = Mathf.Max(0f, dashActive - delta);
            EnergyBounce = Mathf.Max(0f, EnergyBounce - delta);

            for (int i = 0; i < CardCooldowns.Length; i++)
                if (CardCooldowns[i] > 0f) CardCooldowns[i] = Mathf.Max(0f, CardCooldowns[i] - delta);

            if (Respawn > 0f)
            {
                Respawn = Mathf.Max(0f, Respawn - delta);
                if (Respawn == 0f) { RobotHealth = MaxRobotHealth; invulnerable = 2.5f; RobotX = 0.5f; }
            }

            skyOrbTimer -= delta;
            if (skyOrbTimer <= 0f)
            {
                skyOrbTimer += AmbientOrbInterval;
                Vector2 tgt = CellToPixel(RobotLayer, 1f + (float)rng.NextDouble() * (Columns - 3), rng.Next(Rows));
                SpawnOrb(RobotLayer, new Vector2(tgt.x, RobotLayer == Layer.Surface ? 74f : 220f), tgt.y, AmbientOrbValue);
            }

            trashDropTimer -= delta;
            if (trashDropTimer <= 0f)
            {
                trashDropTimer = 22f;
                Layer layer = DeepEnabled && rng.Next(2) == 1 ? Layer.Deep : Layer.Surface;
                if (!DropNaturalTrash(layer) && DeepEnabled)
                    DropNaturalTrash(CampaignCatalog.Opposite(layer));
            }

            Vector2 bankPx = new Vector2(30f, 32f);
            Vector2 robotPx = CellToPixel(RobotLayer, RobotX, RobotY);
            float magnetPx = MagnetRadius * 52f;
            for (int i = Orbs.Count - 1; i >= 0; i--)
            {
                var orb = Orbs[i];
                orb.Bob += delta * 4.5f;
                if (orb.FlyingToBank)
                {
                    orb.Pos = Vector2.MoveTowards(orb.Pos, bankPx, 680f * delta);
                    if (Vector2.Distance(orb.Pos, bankPx) < 16f)
                    {
                        Energy += Mathf.RoundToInt(orb.Value);
                        EnergyBounce = 0.24f;
                        Score += 10;
                        Orbs.RemoveAt(i);
                    }
                    continue;
                }

                orb.Life -= delta;
                if (orb.VelY != 0f || orb.Pos.y < orb.TargetY)
                {
                    orb.Pos.x = Mathf.Clamp(orb.Pos.x + orb.VelX * delta, 44f, 750f);
                    orb.VelY += 280f * delta;
                    orb.Pos.y += orb.VelY * delta;
                    if (orb.Pos.y >= orb.TargetY) { orb.Pos.y = orb.TargetY; orb.VelX = 0f; orb.VelY = 0f; }
                }
                if (Respawn <= 0f && orb.Layer == RobotLayer)
                {
                    float dist = Vector2.Distance(orb.Pos, robotPx);
                    if (dist < magnetPx)
                    {
                        orb.Pos = Vector2.MoveTowards(orb.Pos, robotPx, 260f * delta);
                        if (dist < 22f) { StartOrbFly(orb); continue; }
                    }
                }
                if (orb.Life <= 0f) Orbs.RemoveAt(i);
            }

            TickSlidingBottles(delta);

            while (nextArrival < arrivals.Count && arrivals[nextArrival].Time <= Time)
            {
                var arrival = arrivals[nextArrival++];
                if (arrival.Wave > Wave)
                {
                    Wave = arrival.Wave;
                    Energy += 20;
                    if (!Endless && Rules.Arrivals == null) OnSfx?.Invoke(SfxId.WaveHorn);
                    if (Endless && Wave >= 5) OnAchievement?.Invoke("endless_5");
                }
                if (arrival.StartsSwarm)
                {
                    OnSfx?.Invoke(SfxId.WaveHorn);
                    Say("ĐỢT TẤN CÔNG LỚN!");
                }
                var eSpec = CampaignCatalog.Enemies[arrival.Spec];
                int baseHp = eSpec.SpawnHealth(arrival.Layer);
                int hp = Endless ? Mathf.RoundToInt(baseHp * (1f + (Wave - 1) * 0.14f)) : baseHp;
                Enemies.Add(new Enemy(arrival.Spec, arrival.Layer, arrival.Row, Columns - 0.25f, hp, false, (float)rng.NextDouble() * 6.28f));
            }

            if (Endless && nextArrival >= arrivals.Count && Enemies.Count <= 2)
                ScheduleEndlessWave(endlessWaveScheduled + 1, Time + 7f);

            for (int i = Beams.Count - 1; i >= 0; i--)
            {
                Beams[i].Left -= delta;
                if (Beams[i].Left <= 0f) Beams.RemoveAt(i);
            }

            for (int i = Turbines.Count - 1; i >= 0; i--)
            {
                var tw = Turbines[i];
                tw.X += 8.2f * delta;
                foreach (var e in Enemies)
                {
                    if (e.Health > 0 && e.Layer == tw.Layer && e.Row == tw.Row && Mathf.Abs(e.X - tw.X) < 0.72f)
                    {
                        var tr = CampaignCatalog.Enemies[e.Spec].Trait;
                        Damage(e, tr == Trait.Boss ? 28 : 999, true);
                    }
                }
                if (tw.X > Columns + 0.5f) Turbines.RemoveAt(i);
            }

            for (int side = 0; side < 2; side++)
                for (int row = 0; row < Rows; row++)
                    for (int col = 0; col < Columns; col++)
                        TickTile((Layer)side, row, col, delta);

            TickProjectiles(delta);
            TickTacticalEffects(delta);

            for (int i = Enemies.Count - 1; i >= 0; i--)
                TickEnemy(i, delta);

            CheckLaneDanger();

            if (!Endless && nextArrival == arrivals.Count && Enemies.Count == 0 && !Lost)
            {
                Won = true;
                CoinsEarned += Rules.CoinReward + Integrity * 3;
                OnSfx?.Invoke(SfxId.Win);
                if (Level == 1) OnAchievement?.Invoke("first_win");
                if (Integrity == MaxIntegrity) OnAchievement?.Invoke("flawless");
                if (Rules.HasBoss) OnAchievement?.Invoke("boss_slayer");
            }
        }

        private void CheckLaneDanger()
        {
            // Columns 6–10 are the five rightmost tiles; their left edge is X=5.5.
            const float boundary = Columns - 5.5f;
            foreach (var enemy in Enemies)
            {
                if (enemy.Health <= 0) continue;
                int side = (int)enemy.Layer;
                int layerBit = 1 << side;
                if ((encounteredLayers & layerBit) == 0)
                {
                    encounteredLayers |= layerBit;
                    if (enemy.Layer != RobotLayer) ShowDangerWarning(side);
                }

                if (enemy.X > boundary || (enemy.WarnedLayers & layerBit) != 0) continue;
                enemy.WarnedLayers |= layerBit;
                if (Time < nextDangerWarning[side]) continue;
                nextDangerWarning[side] = Time + DangerCooldown;
                ShowDangerWarning(side);
            }
        }

        private void ShowDangerWarning(int side)
        {
            if (Time >= dangerVisibleUntil[side]) OnSfx?.Invoke(SfxId.WaveHorn);
            dangerVisibleUntil[side] = Time + DangerDuration;
        }

        private void TickSlidingBottles(float delta)
        {
            for (int i = SlidingBottles.Count - 1; i >= 0; i--)
            {
                var b = SlidingBottles[i];
                b.X += b.VelX * delta;
                if (b.X < -0.15f)
                {
                    int bonus = 50 + progress.UpgradeLevel(4) * 15;
                    Energy += bonus;
                    EnergyBounce = 0.28f;
                    BottlesDocked++;
                    CoinsEarned += 5;
                    Score += 50;
                    Vector2 px = CellToPixel(b.Layer, 0, b.Row);
                    OnSfx?.Invoke(SfxId.Coin);
                    OnBurst?.Invoke(b.Layer, px, new Color(1f, 0.92f, 0.42f, 1f), 14);
                    OnFloatText?.Invoke(b.Layer, px, $"BẾN GOM! +{bonus}", new Color(1f, 0.94f, 0.42f, 1f));
                    if (BottlesDocked >= 3) OnAchievement?.Invoke("dock_master");
                    SlidingBottles.RemoveAt(i);
                    continue;
                }
                if (b.VelX > 0f)
                {
                    for (int eIdx = 0; eIdx < Enemies.Count; eIdx++)
                    {
                        var e = Enemies[eIdx];
                        if (e.Health <= 0 || e.Layer != b.Layer || e.Row != b.Row || b.Hit.Contains(e)) continue;
                        if (Mathf.Abs(e.X - b.X) <= 0.55f)
                        {
                            b.Hit.Add(e);
                            b.Hits++;
                            Damage(e, 9, true);
                            if (CampaignCatalog.Enemies[e.Spec].Trait != Trait.Anchor && CampaignCatalog.Enemies[e.Spec].Trait != Trait.Boss)
                                e.X = Mathf.Min(Columns - 0.3f, e.X + 1.4f);
                            Energy += 20;
                            Score += 50;
                            Vector2 ePx = CellToPixel(b.Layer, e.X, e.Row);
                            OnSfx?.Invoke(SfxId.ArmorCrack);
                            OnShake?.Invoke(0.18f, 4f);
                            OnBurst?.Invoke(b.Layer, ePx, new Color(1f, 0.72f, 0.35f, 1f), 12);
                            OnFloatText?.Invoke(b.Layer, ePx - new Vector2(0, 10), b.Hits > 1 ? $"STRIKE x{b.Hits}!" : "BOWLING!", new Color(1f, 0.8f, 0.3f, 1f));
                            OnAchievement?.Invoke("bottle_bowler");
                            if (b.Hits >= 2) break;
                        }
                    }
                }
                if (b.Hits >= 2 || b.X > Columns + 0.3f) SlidingBottles.RemoveAt(i);
            }
        }

        private void TickTile(Layer layer, int row, int col, float delta)
        {
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste != null && waste.Active)
            {
                var eff = CampaignCatalog.Trash[waste.Spec].Effect;
                if (eff == TrashEffect.Loom)
                {
                    waste.Cooldown -= delta;
                    foreach (var enemy in Enemies)
                        if (enemy.Layer != layer && enemy.Row == row && enemy.X >= col - 0.5f)
                            enemy.Slow = Mathf.Max(enemy.Slow, 0.8f);
                    if (waste.Cooldown <= 0f) Trash[side, row, col] = null;
                }
                else if (eff == TrashEffect.Vent)
                {
                    waste.Cooldown -= delta;
                    if (waste.Cooldown <= 0f)
                    {
                        Shock(layer, row, col, 4);
                        Shock(CampaignCatalog.Opposite(layer), row, col, 4);
                        waste.Cooldown = 5.0f;
                    }
                }
            }

            var ally = Allies[side, row, col];
            if (ally == null) return;
            if (ally.PlantPop > 0f) ally.PlantPop = Mathf.Max(0f, ally.PlantPop - delta);
            if (ally.FireAnim > 0f) ally.FireAnim = Mathf.Max(0f, ally.FireAnim - delta);
            if (ally.HurtAnim > 0f) ally.HurtAnim = Mathf.Max(0f, ally.HurtAnim - delta);
            if (ally.Temporary > 0f)
            {
                ally.Temporary -= delta;
                if (ally.Temporary <= 0f) { Allies[side, row, col] = null; return; }
            }

            // Surface creatures placed underwater lose oxygen / take gradual HP drain!
            if (ally.Draining)
            {
                ally.DrainTimer -= delta;
                if (ally.DrainTimer <= 0f)
                {
                    ally.DrainTimer += OxygenInterval;
                    ally.Health--;
                    ally.HurtAnim = 0.15f;
                    Vector2 px = CellToPixel(layer, col, row);
                    OnBurst?.Invoke(layer, px, new Color(0.65f, 0.85f, 1f, 0.9f), 4);
                    if (ally.Health <= 0)
                    {
                        Allies[side, row, col] = null;
                        OnFloatText?.Invoke(layer, px, "NGỘP NƯỚC!", new Color(1f, 0.5f, 0.45f, 1f));
                        return;
                    }
                }
            }

            ally.Jammed = Mathf.Max(0f, ally.Jammed - delta);
            var spec = CampaignCatalog.Allies[ally.Spec];
            float rateMult = ally.Jammed > 0f ? 0.45f : 1f;
            if (spec.Ability == Ability.Swap) return;
            if (spec.Ability != Ability.Income)
            {
                if (layer == Layer.Surface && HasUpdraftAt(row, col)) rateMult *= 1.55f;
            }

            ally.Cooldown -= delta * rateMult;
            if (ally.Cooldown > 0f) return;

            if (spec.Ability == Ability.Guard) { ally.Cooldown = 2f; return; }

            if (spec.Ability == Ability.Income)
            {
                ally.Cooldown = IncomeInterval;
                ally.FireAnim = 0.26f;
                Vector2 p = CellToPixel(layer, col, row);
                SpawnOrb(layer, p - new Vector2(0, 6), p.y + 6f, spec.Power, true);
                OnBurst?.Invoke(layer, p, new Color(1f, 0.94f, 0.45f, 1f), 5);
                return;
            }

            if (spec.Ability == Ability.Clean)
            {
                for (int x = Mathf.Max(0, col - 1); x <= Mathf.Min(Columns - 1, col + 1); x++)
                {
                    var nearby = Trash[side, row, x];
                    if (nearby != null && CampaignCatalog.Trash[nearby.Spec].Effect == TrashEffect.Spill)
                    {
                        Trash[side, row, x] = null;
                        OnBurst?.Invoke(layer, CellToPixel(layer, x, row), new Color(0.55f, 1f, 0.95f, 1f), 8);
                    }
                }
            }

            bool acted = false;
            if (spec.Reach != Reach.Other) acted |= FireAlly(ally, spec, layer, layer, row, col);
            if (spec.Reach != Reach.Local && DeepEnabled) acted |= FireAlly(ally, spec, layer, CampaignCatalog.Opposite(layer), row, col);

            if (acted) ally.FireAnim = 0.24f;
            ally.Cooldown = acted
                ? spec.Ability switch
                {
                    Ability.Repair => 2.8f,
                    Ability.Zap => 1.0f,
                    Ability.Pulse => 2.2f,
                    _ => 1.45f
                }
                : 0.35f;
        }

        private bool ActiveWaste(Layer layer, int row, int col, TrashEffect effect)
        {
            var waste = Trash[(int)layer, row, col];
            return waste != null && waste.Active && CampaignCatalog.Trash[waste.Spec].Effect == effect;
        }

        private bool FireAlly(Ally ally, AllySpec spec, Layer from, Layer target, int row, int col)
        {
            if (from != target && spec.Ability == Ability.Reflect &&
                !ActiveWaste(from, row, col, TrashEffect.Mirror) && !ActiveWaste(target, row, col, TrashEffect.Mirror))
                return false;
            if (from != target && spec.Ability == Ability.Link && Allies[(int)target, row, col] == null)
                return false;

            if (spec.Ability == Ability.Repair)
            {
                var patient = Allies[(int)target, row, col];
                if (patient != null && patient.Health < patient.MaxHealth)
                {
                    patient.Health = Mathf.Min(patient.MaxHealth, patient.Health + spec.Power);
                    Vector2 px = CellToPixel(target, col, row);
                    OnBurst?.Invoke(target, px, new Color(0.45f, 1f, 0.72f, 1f), 6);
                    OnFloatText?.Invoke(target, px - new Vector2(0, 8), $"+{spec.Power} HP", new Color(0.45f, 1f, 0.72f, 1f));
                    return true;
                }
                return false;
            }

            if (!HasEnemyInLane(target, row, col - 0.3f)) return false;

            int power = spec.Power;
            if (spec.Ability == Ability.Echo && (ActiveWaste(from, row, col, TrashEffect.Vent) ||
                ActiveWaste(target, row, col, TrashEffect.Vent)))
                power += 4;

            if (spec.Ability == Ability.Pulse || (spec.Ability == Ability.Habitat && from == Layer.Deep))
            {
                bool slowOnly = spec.Ability == Ability.Habitat;
                foreach (var e in Enemies)
                {
                    if (e.Health <= 0 || e.Layer != target || e.Row != row || e.X < col - 0.3f) continue;
                    if (slowOnly) e.Slow = Mathf.Max(e.Slow, 3.2f);
                    else Damage(e, power, true);
                }
                Color c = slowOnly ? new Color(0.55f, 0.9f, 1f, 1f) : new Color(0.5f, 0.85f, 1f, 1f);
                AddBeam(target, row, col, Columns - 0.5f, c, true);
                OnSfx?.Invoke(SfxId.Shoot);
                return true;
            }

            ProjectileKind kind = spec.Ability switch
            {
                Ability.Zap => ProjectileKind.Zap,
                Ability.Pierce => ProjectileKind.Spear,
                Ability.Pull => ProjectileKind.Push,
                Ability.Slow => ProjectileKind.Slow,
                Ability.Echo => ProjectileKind.Geyser,
                _ => target == Layer.Deep ? ProjectileKind.Bubble : ProjectileKind.Seed
            };

            if (ally.Spec == 3)
            {
                ally.ShotsInCycle = (ally.ShotsInCycle + 1) % 3;
                if (ally.ShotsInCycle == 0) kind = ProjectileKind.ChargedBubble;
            }

            Projectiles.Add(new Projectile
            {
                Layer = target,
                Row = row,
                X = col + 0.35f,
                Speed = kind == ProjectileKind.Zap ? 7.6f : kind == ProjectileKind.Spear ? 6.8f : 5.8f,
                Damage = spec.Ability == Ability.Slow ? 1 : power,
                PierceLeft = spec.Ability == Ability.Pierce ? 2 : 1,
                Kind = kind,
                Pulse = spec.Ability is Ability.Zap or Ability.Echo or Ability.Reflect,
                Slow = spec.Ability == Ability.Slow,
                Push = spec.Ability == Ability.Pull
            });

            if (from != target)
                OnRing?.Invoke(target, CellToPixel(target, col, row), new Color(0.5f, 0.95f, 1f, 0.95f), 22f);
            OnSfx?.Invoke(SfxId.Shoot);
            return true;
        }

        private void TickProjectiles(float delta)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var p = Projectiles[i];
                p.X += p.Speed * delta;
                p.Age += delta;
                if (p.X > Columns + 0.3f) { Projectiles.RemoveAt(i); continue; }

                bool consumed = false;
                for (int eIdx = 0; eIdx < Enemies.Count; eIdx++)
                {
                    var e = Enemies[eIdx];
                    if (e.Health <= 0 || e.Layer != p.Layer || e.Row != p.Row || p.Hit.Contains(e)) continue;
                    if (Mathf.Abs(e.X - p.X) <= 0.42f)
                    {
                        p.Hit.Add(e);
                        Damage(e, p.Damage, p.Pulse);
                        if (p.Slow) e.Slow = Mathf.Max(e.Slow, 3.0f);
                        var eTrait = CampaignCatalog.Enemies[e.Spec].Trait;
                        if (p.Push && eTrait != Trait.Anchor && eTrait != Trait.Boss)
                            e.X = Mathf.Min(Columns - 0.2f, e.X + 0.48f);
                        if (p.Kind == ProjectileKind.ChargedBubble && eTrait != Trait.Anchor && eTrait != Trait.Boss)
                        {
                            e.X = Mathf.Min(Columns - 0.2f, e.X + 0.14f);
                            e.Stun = Mathf.Max(e.Stun, 0.32f);
                        }

                        Vector2 hitPx = CellToPixel(p.Layer, e.X, e.Row);
                        Color burstCol = p.Kind is ProjectileKind.Zap or ProjectileKind.ChargedBubble ? new Color(1f, 0.92f, 0.45f, 1f) :
                            p.Slow ? new Color(0.6f, 0.95f, 1f, 1f) : new Color(0.7f, 1f, 0.72f, 1f);
                        OnBurst?.Invoke(p.Layer, hitPx, burstCol, 5);
                        OnSfx?.Invoke(SfxId.Hit);

                        p.PierceLeft--;
                        if (p.PierceLeft <= 0) { consumed = true; break; }
                    }
                }
                if (consumed) Projectiles.RemoveAt(i);
            }
        }

        private void Damage(Enemy enemy, int amount, bool pulse, bool canSink = false, bool ignoreArmor = false)
        {
            var spec = CampaignCatalog.Enemies[enemy.Spec];
            var trait = spec.Trait;
            if (!ignoreArmor && trait == Trait.Shell && !enemy.Broken && !enemy.Small)
            {
                if (pulse)
                {
                    enemy.Broken = true;
                    OnSfx?.Invoke(SfxId.ArmorCrack);
                    OnFloatText?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, enemy.X, enemy.Row) - new Vector2(0, 12),
                        "VỠ GIÁP!", new Color(1f, 0.85f, 0.35f, 1f));
                    OnAchievement?.Invoke("armor_breaker");
                }
                else amount = Mathf.Max(1, amount - 2);
            }
            if (!ignoreArmor && !pulse && !enemy.Small && (trait == Trait.Burrow || trait == Trait.Filter || trait == Trait.Phase))
                amount = Mathf.Max(1, amount - 1);

            enemy.Health -= amount;
            enemy.Flash = 0.14f;
            enemy.HurtAnim = 0.16f;

            if (canSink && enemy.Health > 0 && enemy.Layer == Layer.Surface && spec.Habitat == Habitat.Both &&
                trait != Trait.Boss && !enemy.Shifted)
            {
                enemy.Shifted = true;
                enemy.Layer = Layer.Deep;
                enemy.Slow = Mathf.Max(enemy.Slow, 2.4f);
                Vector2 px = CellToPixel(Layer.Surface, enemy.X, enemy.Row);
                OnRing?.Invoke(Layer.Surface, px, new Color(0.55f, 0.95f, 1f, 1f), 32f);
                OnFloatText?.Invoke(Layer.Surface, px - new Vector2(0, 8), "ĐÁNH CHÌM!", new Color(0.55f, 0.95f, 1f, 1f));
            }
        }

        private void TriggerSquid(Enemy enemy, int col, Ally trap)
        {
            Layer from = enemy.Layer;
            Layer to = DeepEnabled && CampaignCatalog.Enemies[enemy.Spec].Trait != Trait.Boss
                ? CampaignCatalog.Opposite(from) : from;
            Allies[(int)from, enemy.Row, col] = null;
            Damage(enemy, CampaignCatalog.Allies[trap.Spec].Power, false, ignoreArmor: true);
            enemy.Stun = Mathf.Max(enemy.Stun, SquidStunSeconds);
            enemy.Layer = to;
            var pull = new SquidPullEffect { Enemy = enemy, From = from, To = to, Row = enemy.Row, X = enemy.X, AllySpec = trap.Spec };
            enemy.SquidPull = pull;
            SquidPulls.Add(pull);

            Vector2 start = CellToPixel(from, enemy.X, enemy.Row);
            OnRing?.Invoke(from, start, SquidWake, 35f);
            OnBurst?.Invoke(from, start, SquidWake, 9);
            OnSfx?.Invoke(to == from ? SfxId.Hit : to == Layer.Deep ? SfxId.Dive : SfxId.Surface);
        }

        private void TryShootLitter(Enemy enemy)
        {
            Vector2Int? target = FindLitterTarget(enemy.Layer, enemy.Row, enemy.Row + 1)
                ?? FindLitterTarget(enemy.Layer, 0, Rows);
            if (!target.HasValue) return;

            enemy.ShootAnim = LitterRecoilSeconds;
            LitterShots.Add(new LitterShot
            {
                Layer = enemy.Layer,
                Start = CellToPixel(enemy.Layer, enemy.X, enemy.Row) - new Vector2(13f, 12f),
                Row = target.Value.y,
                Col = target.Value.x
            });
            OnSfx?.Invoke(SfxId.PushBottle);
        }

        private Vector2Int? FindLitterTarget(Layer layer, int firstRow, int rowLimit)
        {
            Vector2Int? target = null;
            int candidates = 0;
            for (int row = firstRow; row < rowLimit; row++)
                for (int col = LitterFirstColumn; col <= LitterLastColumn; col++)
                {
                    if (!Free(layer, row, col) || LitterShots.Exists(shot => shot.Layer == layer && shot.Row == row && shot.Col == col))
                        continue;
                    if (rng.Next(++candidates) == 0) target = new Vector2Int(col, row);
                }
            return target;
        }

        private void TickTacticalEffects(float delta)
        {
            for (int i = SquidPulls.Count - 1; i >= 0; i--)
            {
                var pull = SquidPulls[i];
                pull.Age += delta;
                if (pull.Age < SquidPullSeconds) continue;
                if (pull.Enemy.SquidPull == pull) pull.Enemy.SquidPull = null;
                SquidPulls.RemoveAt(i);
                if (pull.To == pull.From) continue;

                Vector2 end = CellToPixel(pull.To, pull.X, pull.Row);
                OnRing?.Invoke(pull.To, end, SquidWake, 38f);
                OnBurst?.Invoke(pull.To, end, SquidWake, 12);
            }

            for (int i = LitterShots.Count - 1; i >= 0; i--)
            {
                var shot = LitterShots[i];
                shot.Age += delta;
                if (shot.Age < LitterFlightSeconds) continue;
                LitterShots.RemoveAt(i);
                if (!Free(shot.Layer, shot.Row, shot.Col)) continue;

                AddWaste(CampaignCatalog.ClutterId, shot.Layer, shot.Row, shot.Col);
                Vector2 end = CellToPixel(shot.Layer, shot.Col, shot.Row);
                OnRing?.Invoke(shot.Layer, end, LitterSplash, 28f);
                OnBurst?.Invoke(shot.Layer, end, LitterSplash, 9);
            }
        }

        private void TickEnemy(int index, float delta)
        {
            var enemy = Enemies[index];
            var spec = CampaignCatalog.Enemies[enemy.Spec];
            int col = Mathf.Clamp(Mathf.RoundToInt(enemy.X), 0, Columns - 1);

            if (enemy.Health <= 0)
            {
                Vector2 px = CellToPixel(enemy.Layer, enemy.X, enemy.Row);
                Enemies.RemoveAt(index);
                Kills++;
                Score += spec.Bounty * 5;
                CoinsEarned += spec.Trait == Trait.Boss ? 50 : (rng.Next(3) == 0 ? 2 : 1);
                OnBurst?.Invoke(enemy.Layer, px, new Color(1f, 0.65f, 0.45f, 1f), spec.Trait == Trait.Boss ? 24 : 9);
                if (rng.Next(100) < (spec.Trait == Trait.Boss ? 100 : 25))
                    SpawnOrb(enemy.Layer, px - new Vector2(0, 4), px.y + 4f, spec.Trait == Trait.Boss ? 50 : 10, true);
                if (spec.Trait == Trait.Spill) AddWaste(7, enemy.Layer, enemy.Row, col);
                if (spec.Trait == Trait.Split && !enemy.Small)
                {
                    int rUp = Mathf.Max(0, enemy.Row - 1);
                    int rDn = Mathf.Min(Rows - 1, enemy.Row + 1);
                    Enemies.Add(new Enemy(enemy.Spec, enemy.Layer, rUp, enemy.X + 0.15f, 4, true, 0f));
                    Enemies.Add(new Enemy(enemy.Spec, enemy.Layer, rDn, enemy.X - 0.15f, 4, true, 3.14f));
                }
                return;
            }

            enemy.Attack = Mathf.Max(0f, enemy.Attack - delta);
            enemy.Slow = Mathf.Max(0f, enemy.Slow - delta);
            enemy.Flash = Mathf.Max(0f, enemy.Flash - delta);
            enemy.ShootAnim = Mathf.Max(0f, enemy.ShootAnim - delta);
            enemy.BiteAnim = Mathf.Max(0f, enemy.BiteAnim - delta);
            enemy.HurtAnim = Mathf.Max(0f, enemy.HurtAnim - delta);
            enemy.Recovery += delta;
            enemy.ActionTimer -= delta;

            var trap = Allies[(int)enemy.Layer, enemy.Row, col];
            if (trap != null && CampaignCatalog.Allies[trap.Spec].Ability == Ability.Swap &&
                Mathf.Abs(enemy.X - col) < 0.48f)
            {
                TriggerSquid(enemy, col, trap);
                return;
            }

            enemy.Stun = Mathf.Max(0f, enemy.Stun - delta);
            if (enemy.Stun > 0f) return;
            enemy.WalkPhase += delta * (enemy.Slow > 0f ? 2.5f : 5.2f);

            if (spec.Trait == Trait.Filter && enemy.Recovery >= 5.5f)
            {
                enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + 2);
                enemy.Recovery = 0f;
            }

            if (spec.Trait == Trait.Ferry && DeepEnabled && UpdateFerryDive(enemy, delta)) return;

            // Dynamic Enemy Behavior #2: Phao Hắc periodically leaks Oil Slicks mid-lane!
            if (spec.Trait == Trait.Spill && enemy.ActionTimer <= 0f)
            {
                enemy.ActionTimer = 6.5f;
                AddWaste(7, enemy.Layer, enemy.Row, col);
            }

            // Dynamic Enemy Behavior #3: Sứa Nilon phase-blinks forward 1.1 tiles!
            if (spec.Trait == Trait.Phase && enemy.ActionTimer <= 0f && enemy.X > 1.5f)
            {
                enemy.ActionTimer = 5.5f;
                enemy.X = Mathf.Max(0.4f, enemy.X - 1.1f);
                Vector2 px = CellToPixel(enemy.Layer, enemy.X, enemy.Row);
                OnRing?.Invoke(enemy.Layer, px, new Color(0.85f, 0.55f, 1f, 1f), 28f);
                OnFloatText?.Invoke(enemy.Layer, px - new Vector2(0, 8), "BIẾN ẢNH!", new Color(0.85f, 0.6f, 1f, 1f));
            }

            if (spec.Trait == Trait.Boss)
            {
                enemy.BossTimer -= delta;
                if (enemy.BossTimer <= 0f)
                {
                    enemy.BossTimer = 9.5f;
                    enemy.Layer = CampaignCatalog.Opposite(enemy.Layer);
                    enemy.Row = rng.Next(Rows);
                    OnRing?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, enemy.X, enemy.Row), new Color(1f, 0.35f, 0.35f, 1f), 48f);
                    OnShake?.Invoke(0.22f, 4f);
                    int escort = rng.Next(4);
                    Enemies.Add(new Enemy(escort, enemy.Layer, rng.Next(Rows), Mathf.Min(Columns - 0.4f, enemy.X + 0.4f),
                        CampaignCatalog.Enemies[escort].Health));
                    AddWaste(7, Layer.Surface, enemy.Row, Mathf.Clamp(col, 2, Columns - 2));
                }
            }

            int side = (int)enemy.Layer;
            var ally = Allies[side, enemy.Row, col];
            if (spec.Trait == Trait.Drift && !enemy.Shifted && ally != null)
            {
                int adj = enemy.Row == Rows - 1 ? enemy.Row - 1 : enemy.Row + 1;
                if (Allies[side, adj, col] == null)
                {
                    enemy.Row = adj;
                    enemy.Shifted = true;
                    ally = null;
                    OnFloatText?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, enemy.X, enemy.Row) - new Vector2(0, 8), "NÉ LÀN!", new Color(1f, 0.85f, 0.45f, 1f));
                }
            }

            if (spec.Trait == Trait.Jam || spec.Trait == Trait.Phase)
            {
                var opp = Allies[(int)CampaignCatalog.Opposite(enemy.Layer), enemy.Row, col];
                if (opp != null) opp.Jammed = 1.4f;
            }

            var heroCell = RobotCell;
            if (Respawn <= 0f && invulnerable <= 0f && enemy.Layer == RobotLayer &&
                enemy.Row == heroCell.y && Mathf.Abs(enemy.X - RobotX) < 0.38f)
            {
                RobotHealth--;
                invulnerable = 1.8f;
                OnShake?.Invoke(0.14f, 3.5f);
                OnSfx?.Invoke(SfxId.Hit);
                OnFloatText?.Invoke(RobotLayer, CellToPixel(RobotLayer, RobotX, RobotY) - new Vector2(0, 10), "-1 HP", new Color(1f, 0.45f, 0.45f, 1f));
                if (RobotHealth <= 0)
                {
                    Respawn = 4f;
                    Say("Bio-Bot đang hồi phục tại bến!", 3.5f);
                }
            }

            if (ally != null && Mathf.Abs(enemy.X - col) < 0.48f)
            {
                if (enemy.Attack <= 0f)
                {
                    enemy.Attack = 1.15f;
                    enemy.BiteAnim = 0.25f;
                    ally.HurtAnim = 0.18f;
                    // Dynamic Enemy Behavior #4: Thùng Húc's first ram slams hard with screen shake!
                    int bite = spec.Trait == Trait.Boss ? 4 : (spec.Trait == Trait.Ram ? (enemy.Charged ? 2 : 5) : 1);
                    if (spec.Trait == Trait.Ram && !enemy.Charged)
                    {
                        enemy.Charged = true;
                        OnShake?.Invoke(0.18f, 4f);
                        OnFloatText?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, col, enemy.Row) - new Vector2(0, 10), "HÚC MẠNH!", new Color(1f, 0.5f, 0.4f, 1f));
                    }
                    ally.Health -= bite;
                    OnBurst?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, col, enemy.Row), new Color(1f, 0.85f, 0.55f, 1f), 4);
                    var def = CampaignCatalog.Allies[ally.Spec];
                    if (ally.ShieldSource == null && def.Ability == Ability.Guard && def.Power > 0)
                        Damage(enemy, def.Power, false);
                    if (ally.Health <= 0)
                    {
                        Allies[side, enemy.Row, col] = null;
                        OnBurst?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, col, enemy.Row), new Color(0.8f, 0.9f, 0.7f, 1f), 8);
                    }
                }
                return;
            }

            float speed = spec.Speed * (enemy.Slow > 0f ? 0.45f : 1f) * (enemy.Small ? 1.45f : 1f);
            var waste = Trash[side, enemy.Row, col];
            if (spec.Trait == Trait.Skim && waste != null && CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Spill)
                speed *= 1.7f;
            foreach (var peer in Enemies)
            {
                if (peer == enemy || peer.Layer != enemy.Layer || peer.Health <= 0) continue;
                bool school = spec.Trait == Trait.School && peer.Spec == enemy.Spec && peer.Row == enemy.Row;
                if (school || CampaignCatalog.Enemies[peer.Spec].Trait == Trait.Anchor)
                {
                    speed *= 1.2f;
                    break;
                }
            }

            float distance = speed * delta;
            enemy.X -= distance;
            if (spec.Trait == Trait.Litter)
            {
                enemy.LitterDist += distance;
                while (enemy.LitterDist + 0.00001f >= LitterTileInterval)
                {
                    enemy.LitterDist = Mathf.Max(0f, enemy.LitterDist - LitterTileInterval);
                    TryShootLitter(enemy);
                }
            }
            if (enemy.X < -0.25f)
            {
                if (TurbineReady[side, enemy.Row])
                {
                    TurbineReady[side, enemy.Row] = false;
                    Turbines.Add(new TurbineWave { Layer = enemy.Layer, Row = enemy.Row, X = -0.2f });
                    OnSfx?.Invoke(SfxId.Turbine);
                    OnShake?.Invoke(0.3f, 5f);
                    OnFloatText?.Invoke(enemy.Layer, CellToPixel(enemy.Layer, 0.6f, enemy.Row), "TUABIN XẢ LŨ!", new Color(0.45f, 0.95f, 1f, 1f));
                    Say("Tuabin Cửa Đê kích hoạt quét sạch hàng!", 3f);
                    return;
                }
                if (enemy.X < -0.58f)
                {
                    Enemies.RemoveAt(index);
                    int loss = spec.Trait == Trait.Boss ? 5 : spec.Trait == Trait.Anchor ? 2 : 1;
                    Integrity = Mathf.Max(0, Integrity - loss);
                    OnShake?.Invoke(0.25f, 5f);
                    OnSfx?.Invoke(SfxId.Lose);
                    Say("Cảnh báo! Rác đã lọt qua Cửa Đê!", 3f);
                }
            }
        }

        private bool UpdateFerryDive(Enemy enemy, float delta)
        {
            enemy.DiveArrival = Mathf.Max(0f, enemy.DiveArrival - delta);
            if (enemy.Shifted || enemy.X > Columns - 3f) return false;

            Vector2 p = CellToPixel(enemy.Layer, enemy.X, enemy.Row);
            Color wake = new Color(0.65f, 0.95f, 1f);
            if (enemy.DiveWindup <= 0f)
            {
                enemy.DiveWindup = FerryWindupSeconds;
                OnRing?.Invoke(enemy.Layer, p, wake, 26f);
                return true;
            }

            enemy.DiveWindup = Mathf.Max(0f, enemy.DiveWindup - delta);
            if (enemy.DiveWindup > 0f) return true;

            enemy.Shifted = true;
            OnRing?.Invoke(enemy.Layer, p, wake, 40f);
            OnBurst?.Invoke(enemy.Layer, p, wake, 12);
            enemy.Layer = CampaignCatalog.Opposite(enemy.Layer);
            enemy.DiveArrival = FerryArrivalSeconds;
            p = CellToPixel(enemy.Layer, enemy.X, enemy.Row);
            OnRing?.Invoke(enemy.Layer, p, wake, 31f);
            OnBurst?.Invoke(enemy.Layer, p, wake, 10);
            OnSfx?.Invoke(enemy.Layer == Layer.Deep ? SfxId.Dive : SfxId.Surface);
            return true;
        }

        private bool HasEnemyInLane(Layer layer, int row, float minCol)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e = Enemies[i];
                if (e.Health > 0 && e.Layer == layer && e.Row == row && e.X >= minCol) return true;
            }
            return false;
        }

        private void HitNear(Layer layer, int row, float col, float range, int power, bool pulse, bool canSink = false)
        {
            foreach (var e in Enemies)
                if (e.Health > 0 && e.Layer == layer && e.Row == row && Mathf.Abs(e.X - col) <= range)
                    Damage(e, power, pulse, canSink);
        }

        private void Shock(Layer layer, int row, int col, int power)
        {
            HitNear(layer, row, col, 1.6f, power, true, DeepEnabled);
            AddBeam(layer, row, Mathf.Max(0, col - 1), Mathf.Min(Columns - 1, col + 1), new Color(0.55f, 0.95f, 1f, 1f), true);
            OnRing?.Invoke(layer, CellToPixel(layer, col, row), new Color(0.55f, 0.95f, 1f, 1f), 38f);
        }

        private void AddBeam(Layer layer, int row, float from, float to, Color color, bool thick) =>
            Beams.Add(new BeamEffect { Layer = layer, Row = row, From = from, To = to, Left = 0.24f, Color = color, Thick = thick });

        private bool Free(Layer layer, int row, int col) =>
            Trash[(int)layer, row, col] == null && Allies[(int)layer, row, col] == null;

        private void Say(string msg, float dur = 2.8f) { Message = msg; Feedback = dur; }
        private bool Refuse(string msg) { Say(msg, 2.2f); return false; }

        public int ComputeStars()
        {
            if (!Won) return 0;
            if (Integrity >= MaxIntegrity) return 3;
            if (Integrity >= MaxIntegrity - 3) return 2;
            return 1;
        }

        public void StepAutoPilot()
        {
            if (Ended || Respawn > 0f) return;
            // Collect orbs
            if (Orbs.Count > 0)
            {
                foreach (var o in Orbs)
                {
                    if (!o.FlyingToBank && o.Layer == RobotLayer) { StartOrbFly(o); break; }
                }
            }
            int side = (int)RobotLayer;
            Enemy urgent = null;
            foreach (var enemy in Enemies)
                if (enemy.Layer == RobotLayer && (urgent == null || enemy.X < urgent.X))
                    urgent = enemy;

            if (urgent != null)
            {
                int defender = RobotLayer == Layer.Deep && (Endless || Level >= 3) ? 3 : 1;
                for (int col = Mathf.Min(4, Mathf.FloorToInt(urgent.X - 0.6f)); col >= 1; col--)
                    if (CanPlant(defender, RobotLayer, urgent.Row, col, out _))
                    {
                        Plant(defender, RobotLayer, urgent.Row, col);
                        return;
                    }
            }

            int otters = 0;
            for (int row = 0; row < Rows; row++)
                if (Allies[side, row, 0]?.Spec == 0) otters++;
            if (otters >= (urgent == null ? 2 : 1)) return;
            for (int row = 0; row < Rows; row++)
                if (CanPlant(0, RobotLayer, row, 0, out _))
                {
                    Plant(0, RobotLayer, row, 0);
                    return;
                }
        }
    }
}
