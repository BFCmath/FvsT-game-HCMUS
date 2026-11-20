using System;
using System.Collections.Generic;
using UnityEngine;

namespace BioBot
{
    public enum ProjectileKind { Seed, Bubble, Spear, Zap, Push, Geyser, Slow }

    public sealed class CampaignMatch
    {
        public const int Rows = 5, Columns = 9, MaxIntegrity = 10;

        public sealed class Ally
        {
            public readonly int Spec;
            public int Health, MaxHealth;
            public float Cooldown, Jammed, Temporary, PlantPop, FireAnim, HurtAnim, Phase;
            public Ally(int spec, int health, float temporary = 0f, float phase = 0f)
            {
                Spec = spec;
                Health = MaxHealth = health;
                Temporary = temporary;
                PlantPop = 0.34f;
                Cooldown = 0.55f;
                Phase = phase;
            }
        }

        public sealed class Waste
        {
            public readonly int Spec;
            public bool Active;
            public float Cooldown, BobPhase;
            public Waste(int spec, float phase = 0f) { Spec = spec; BobPhase = phase; }
        }

        public sealed class Enemy
        {
            public readonly int Spec;
            public Layer Layer;
            public int Row, Health, MaxHealth;
            public float X, Attack, Slow, Recovery, Flash, BossTimer, BiteAnim, HurtAnim, WalkPhase;
            public bool Shifted, Broken, Small;

            public Enemy(int spec, Layer layer, int row, float x, int health, bool small = false, float walkPhase = 0f)
            {
                Spec = spec;
                Layer = layer;
                Row = row;
                X = x;
                Health = MaxHealth = health;
                Small = small;
                BossTimer = 7f;
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

        private struct Arrival
        {
            public float Time;
            public int Wave, Spec, Row;
            public Layer Layer;
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

        private readonly List<Arrival> arrivals = new List<Arrival>();
        private readonly CampaignProgress progress;
        private readonly System.Random rng;
        private int nextArrival;
        private float skyOrbTimer = 5.2f;
        private float trashDropTimer = 14.5f;
        private float surgeOrbTimer;
        private float invulnerable;
        private float pulseCooldown;
        private float dashCooldown;
        private float dashActive;
        private int endlessWaveScheduled;

        public readonly int Level;
        public readonly bool Endless;
        public LevelSpec Rules => CampaignCatalog.Levels[Mathf.Clamp(Level - 1, 0, CampaignCatalog.Levels.Length - 1)];
        public int Energy { get; private set; }
        public float EnergyBounce { get; private set; }
        public int Integrity { get; private set; } = MaxIntegrity;
        public int MaxRobotHealth => 4 + progress.UpgradeLevel(0);
        public int RobotHealth { get; private set; }
        public float RobotSpeedBonus => (1f + progress.UpgradeLevel(1) * 0.15f) * (dashActive > 0f ? 2.15f : 1f);
        public float PulseCooldownLeft => Mathf.Max(0f, pulseCooldown);
        public float PulseCooldownMax => Mathf.Max(1.4f, 3.0f - progress.UpgradeLevel(2) * 0.45f);
        public float DashCooldownLeft => Mathf.Max(0f, dashCooldown);
        public bool IsDashing => dashActive > 0f;
        public float MagnetRadius => 1.15f + progress.UpgradeLevel(3) * 0.45f;

        // Original Eco-Clarity & Eco-Surge ("Dòng Chảy Xanh") + Combo Chain!
        public float WaterClarity { get; private set; } = 45f;
        public float EcoSurgeTime { get; private set; }
        public bool IsEcoSurge => EcoSurgeTime > 0f;
        public int ComboCount { get; private set; }
        public float ComboTimer { get; private set; }

        public int Wave { get; private set; }
        public int TotalWaves => Endless ? Mathf.Max(Wave, endlessWaveScheduled) : Rules.Waves;
        public float WaveProgress => Endless
            ? Mathf.Clamp01((Time % 22f) / 22f)
            : (arrivals.Count == 0 ? 1f : Mathf.Clamp01(nextArrival / (float)arrivals.Count));
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
        public float NextAttack => nextArrival < arrivals.Count ? Mathf.Max(0f, arrivals[nextArrival].Time - Time) : -1f;
        public int NextRow => nextArrival < arrivals.Count ? arrivals[nextArrival].Row : -1;
        public Layer NextLayer => nextArrival < arrivals.Count ? arrivals[nextArrival].Layer : Layer.Surface;

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
        public Action<string> OnWaveBanner;
        public Action<string> OnAchievement;

        public CampaignMatch(int level, CampaignProgress progress, bool endless = false)
        {
            Level = level;
            Endless = endless;
            this.progress = progress;
            rng = new System.Random(1021 + level * 73 + (endless ? Environment.TickCount & 0xFFFF : 0));
            RobotHealth = MaxRobotHealth;
            Energy = (endless ? 250 : Rules.Budget) + progress.UpgradeLevel(4) * 15;

            for (int s = 0; s < 2; s++)
                for (int r = 0; r < Rows; r++)
                    TurbineReady[s, r] = true;

            Say(endless
                ? "CHẾ ĐỘ VÔ TẬN: Giữ Độ Trong Nước đạt 100% để kích hoạt Dòng Chảy Xanh!"
                : Rules.Lesson, 8f);

            if (endless) ScheduleEndlessWave(1, 7f);
            else CreateCampaignArrivals();
            PlaceStartingTrash();
        }

        public static Vector2 CellToPixel(float col, float row) => new Vector2(58f + col * 60f, 109f + row * 54f);

        public bool HasUpdraftAt(int row, int col)
        {
            var w = Trash[(int)Layer.Deep, row, col];
            if (w != null && w.Active && CampaignCatalog.Trash[w.Spec].Effect == TrashEffect.Vent) return true;
            var a = Allies[(int)Layer.Deep, row, col];
            return a != null && (a.Spec == 8 || a.Spec == 12 || a.Spec == 17); // Periscope, Volt Eel, Barracuda create updraft!
        }

        private void AddEcoProgress(float clarityGain, int comboStep = 1)
        {
            if (comboStep > 0)
            {
                ComboCount = Mathf.Min(12, ComboCount + comboStep);
                ComboTimer = 5.5f;
            }
            if (IsEcoSurge) return;
            float mult = 1f + Mathf.Max(0, ComboCount - 1) * 0.15f;
            WaterClarity = Mathf.Clamp(WaterClarity + clarityGain * mult, 0f, 100f);
            if (WaterClarity >= 100f)
            {
                EcoSurgeTime = 10f;
                surgeOrbTimer = 0.4f;
                WaterClarity = 100f;
                OnSfx?.Invoke(SfxId.Win);
                OnShake?.Invoke(0.25f, 4f);
                OnWaveBanner?.Invoke("✦ DÒNG CHẢY XANH! x2 TỐC ĐỘ BẮN & MƯA NĂNG LƯỢNG ✦");
                Say("Độ Trong Nước đạt 100%! Toàn bộ hệ sinh thái bùng nổ sức mạnh trong 10 giây!", 5f);
            }
        }

        private void CreateCampaignArrivals()
        {
            float start = 8.5f;
            var eligible = new List<int>();
            for (int i = 0; i < CampaignCatalog.Enemies.Length; i++)
            {
                var spec = CampaignCatalog.Enemies[i];
                if (spec.Trait != Trait.Boss && spec.Unlock <= Level)
                    eligible.Add(i);
            }

            for (int wave = 1; wave <= Rules.Waves; wave++)
            {
                var introduced = eligible.FindAll(id => CampaignCatalog.Enemies[id].Unlock == Level);
                int count = 3 + Level + wave * 2;
                float spacing = Mathf.Max(1.35f, 2.5f - Level * 0.12f);
                for (int n = 0; n < count; n++)
                {
                    int spec = (n < introduced.Count && wave == 1)
                        ? introduced[n]
                        : eligible[rng.Next(eligible.Count)];
                    Habitat hab = CampaignCatalog.Enemies[spec].Habitat;
                    Layer target = rng.Next(100) < Rules.SurfaceWeight ? Layer.Surface : Layer.Deep;
                    if (!CampaignCatalog.Fits(hab, target)) target = CampaignCatalog.Opposite(target);
                    int row = (Level == 1 && wave == 1 && n < 2) ? 2 : rng.Next(Rows);
                    arrivals.Add(new Arrival { Time = start + n * spacing, Wave = wave, Spec = spec, Row = row, Layer = target });
                }

                if (Rules.HasBoss && wave == Rules.Waves)
                {
                    arrivals.Add(new Arrival
                    {
                        Time = start + count * spacing + 1.5f,
                        Wave = wave,
                        Spec = CampaignCatalog.Enemies.Length - 1,
                        Row = 2,
                        Layer = Layer.Surface
                    });
                }
                start += count * spacing + 10.5f;
            }
        }

        private void ScheduleEndlessWave(int waveNum, float startTime)
        {
            endlessWaveScheduled = waveNum;
            int count = 5 + waveNum * 2;
            float spacing = Mathf.Max(0.95f, 2.2f - waveNum * 0.08f);
            for (int n = 0; n < count; n++)
            {
                int spec = rng.Next(CampaignCatalog.Enemies.Length - 1);
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
            if (waveNum % 5 == 0)
            {
                arrivals.Add(new Arrival
                {
                    Time = startTime + count * spacing + 1f,
                    Wave = waveNum,
                    Spec = CampaignCatalog.Enemies.Length - 1,
                    Row = 2,
                    Layer = Layer.Surface
                });
            }
        }

        private void PlaceStartingTrash()
        {
            int effLevel = Endless ? 10 : Level;
            AddWaste(0, Layer.Surface, 2, 3);
            AddWaste(1, Layer.Surface, 3, 2);
            if (effLevel == 1) { AddWaste(0, Layer.Surface, 1, 5); return; }
            AddWaste(2, Layer.Deep, 2, 2);
            AddWaste(3, Layer.Deep, 1, 4);
            if (effLevel >= 4) { AddWaste(4, Layer.Deep, 2, 4); AddWaste(5, Layer.Surface, 0, 5); }
            if (effLevel >= 6) { AddWaste(6, Layer.Deep, 3, 3); AddWaste(7, Layer.Surface, 2, 6); }
            if (effLevel >= 8) { AddWaste(8, Layer.Deep, 2, 5); AddWaste(9, Layer.Surface, 4, 5); }
            if (effLevel >= 9) { AddWaste(2, Layer.Deep, 4, 3); AddWaste(0, Layer.Surface, 0, 4); }
        }

        private void AddWaste(int id, Layer layer, int row, int col)
        {
            if (Trash[(int)layer, row, col] == null && Allies[(int)layer, row, col] == null)
                Trash[(int)layer, row, col] = new Waste(id, (float)rng.NextDouble() * 6.28f);
        }

        public bool CanPlant(int id, Layer layer, int row, int col, out string reason)
        {
            reason = null;
            if (Ended || Respawn > 0f || id < 0 || id >= CampaignCatalog.Allies.Length) return false;
            var spec = CampaignCatalog.Allies[id];
            if (!Endless && spec.Unlock > Level) { reason = "Chưa mở khóa."; return false; }
            if (CardCooldowns[id] > 0f) { reason = "Thẻ đang hồi chiêu."; return false; }
            if (!CampaignCatalog.Fits(spec.Habitat, layer)) { reason = "Đồng minh này cần tầng còn lại!"; return false; }
            if (Allies[(int)layer, row, col] != null || Trash[(int)layer, row, col] != null)
            { reason = "Ô đã có vật thể. Dọn rác (F) trước."; return false; }
            if (Energy < spec.Cost) { reason = $"Cần {spec.Cost} NL."; return false; }
            return true;
        }

        public bool Plant(int id, Layer layer, int row, int col)
        {
            if (!CanPlant(id, layer, row, col, out string reason))
            {
                if (!string.IsNullOrEmpty(reason)) Say(reason, 2.2f);
                return false;
            }
            var spec = CampaignCatalog.Allies[id];
            Energy -= spec.Cost;
            float rechargeMult = Mathf.Max(0.45f, 1f - progress.UpgradeLevel(5) * 0.15f);
            CardCooldowns[id] = spec.Recharge * rechargeMult;
            Allies[(int)layer, row, col] = new Ally(id, spec.Health, 0f, (float)rng.NextDouble() * 6.28f);

            Vector2 px = CellToPixel(col, row);
            OnSfx?.Invoke(SfxId.Plant);
            OnBurst?.Invoke(layer, px, new Color(0.55f, 0.95f, 0.72f, 1f), 11);
            OnRing?.Invoke(layer, px, new Color(0.6f, 1f, 0.85f, 0.9f), 34f);
            OnFloatText?.Invoke(layer, px - new Vector2(0, 14), spec.Name, new Color(0.7f, 1f, 0.85f, 1f));
            if (spec.Ability == Ability.Income) PlantedIncomeCount++;
            else PlantedFighterCount++;
            if (spec.Reach != Reach.Local) OnAchievement?.Invoke("cross_layer");
            AddEcoProgress(4f, 0);
            Say("Đã triệu hồi " + spec.Name + "!", 2f);
            return true;
        }

        public bool RemoveAlly(Layer layer, int row, int col)
        {
            if (Ended) return false;
            int side = (int)layer;
            var ally = Allies[side, row, col];
            if (ally == null) return false;
            var spec = CampaignCatalog.Allies[ally.Spec];
            int refund = ally.Temporary > 0f ? 0 : spec.Cost / 2;
            Allies[side, row, col] = null;
            if (refund > 0) Energy += refund;
            Vector2 px = CellToPixel(col, row);
            OnSfx?.Invoke(SfxId.Click);
            OnBurst?.Invoke(layer, px, new Color(0.85f, 0.9f, 0.95f, 1f), 7);
            if (refund > 0) OnFloatText?.Invoke(layer, px, $"+{refund} NL", new Color(1f, 0.88f, 0.45f, 1f));
            Say($"Đã tháo {spec.Name} (+{refund} NL).", 2f);
            return true;
        }

        public bool Collect(Layer layer, int row, int col)
        {
            if (Ended || Respawn > 0f) return false;
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste == null) return Refuse("Ô này không có rác.");
            var spec = CampaignCatalog.Trash[waste.Spec];
            Trash[side, row, col] = null;
            int value = spec.Effect == TrashEffect.Float ? 15 : spec.Value;
            Energy += value;
            EnergyBounce = 0.25f;
            CoinsEarned += 2;
            Score += 20 * Mathf.Max(1, ComboCount);
            Vector2 px = CellToPixel(col, row);

            if (spec.Effect == TrashEffect.Filament && Allies[side, row, col] == null)
            {
                Allies[side, row, col] = new Ally(3, CampaignCatalog.Allies[3].Health, 0f, (float)rng.NextDouble() * 6.28f);
                FishRescued++;
                CoinsEarned += 5;
                Score += 75;
                AddEcoProgress(18f, 1);
                OnSfx?.Invoke(SfxId.RescueFish);
                OnBurst?.Invoke(layer, px, new Color(0.4f, 0.95f, 1f, 1f), 16);
                OnRing?.Invoke(layer, px, new Color(0.45f, 1f, 0.95f, 1f), 46f);
                OnFloatText?.Invoke(layer, px - new Vector2(0, 12), "CỨU CÁ! +25", new Color(0.45f, 1f, 0.95f, 1f));
                OnAchievement?.Invoke("first_rescue");
                Say("Đã cắt dây cước! Cá Vệt gia nhập phòng tuyến!", 3.2f);
            }
            else
            {
                AddEcoProgress(10f, 1);
                OnSfx?.Invoke(SfxId.CollectOrb);
                OnBurst?.Invoke(layer, px, new Color(1f, 0.85f, 0.45f, 1f), 9);
                OnFloatText?.Invoke(layer, px, $"+{value} NL", new Color(1f, 0.88f, 0.45f, 1f));
                Say($"Thu gom {spec.Name}: +{value} NL.", 2.2f);
            }
            return true;
        }

        public bool UseTrash(Layer layer, int row, int col)
        {
            if (Ended || Respawn > 0f) return false;
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste == null) return Refuse("Cần đứng trên ô rác để kích hoạt (G).");
            var spec = CampaignCatalog.Trash[waste.Spec];
            Layer opposite = CampaignCatalog.Opposite(layer);
            int other = (int)opposite;
            Vector2 px = CellToPixel(col, row);

            switch (spec.Effect)
            {
                case TrashEffect.Float:
                    return PushFloat(layer, row, col, -1, true);
                case TrashEffect.Filament:
                    return Collect(layer, row, col);
                case TrashEffect.Ballast:
                    if (!Free(opposite, row, col))
                        return Refuse("Ô mặt nước đang bị chiếm. Bấm F để thu.");
                    Allies[other, row, col] = new Ally(1, 14, 28f);
                    OnAchievement?.Invoke("cross_layer");
                    OnFloatText?.Invoke(opposite, px, "PHAO CHẮN!", new Color(0.55f, 1f, 0.85f, 1f));
                    break;
                case TrashEffect.Shell:
                    Allies[side, row, col] = new Ally(1, 12, 26f);
                    OnFloatText?.Invoke(layer, px, "KHIÊN VỎ!", new Color(1f, 0.86f, 0.5f, 1f));
                    break;
                case TrashEffect.Capsule:
                    if (!Free(opposite, row, col))
                        return Refuse("Ô tầng kia bị chiếm.");
                    Trash[other, row, col] = waste;
                    OnFloatText?.Invoke(layer, px, "ĐỔI TẦNG!", new Color(0.6f, 0.9f, 1f, 1f));
                    break;
                case TrashEffect.Mirror:
                case TrashEffect.Vent:
                    if (waste.Active) return Refuse("Đã kích hoạt. Bấm F để tháo.");
                    waste.Active = true;
                    AddEcoProgress(10f, 1);
                    OnSfx?.Invoke(SfxId.Plant);
                    OnRing?.Invoke(layer, px, new Color(0.55f, 1f, 0.95f, 1f), 46f);
                    OnFloatText?.Invoke(layer, px, "LIÊN TẦNG!", new Color(0.55f, 1f, 0.95f, 1f));
                    OnAchievement?.Invoke("cross_layer");
                    Say(spec.Name + " đã kích hoạt kết nối hai tầng!", 3f);
                    return true;
                case TrashEffect.Battery:
                    Shock(layer, row, col, 6);
                    Shock(opposite, row, col, 6);
                    OnSfx?.Invoke(SfxId.Pulse);
                    OnShake?.Invoke(0.22f, 5f);
                    OnAchievement?.Invoke("cross_layer");
                    break;
                case TrashEffect.Spill:
                    HitNear(layer, row, col, 1.4f, 4, true);
                    OnSfx?.Invoke(SfxId.Pulse);
                    OnBurst?.Invoke(layer, px, new Color(1f, 0.55f, 0.25f, 1f), 14);
                    break;
                case TrashEffect.Loom:
                    if (waste.Active) return Refuse("Lưới đã giăng. F để tháo.");
                    waste.Active = true;
                    waste.Cooldown = 20f;
                    AddBeam(opposite, row, col, Columns - 1, new Color(0.5f, 0.95f, 0.85f, 1f), true);
                    OnSfx?.Invoke(SfxId.Plant);
                    OnAchievement?.Invoke("cross_layer");
                    Say("Lưới Cuộn hãm chậm cả hàng đối diện trong 20s!", 3f);
                    return true;
            }
            Trash[side, row, col] = null;
            TrashUsedCount++;
            AddEcoProgress(10f, 1);
            OnSfx?.Invoke(SfxId.PushBottle);
            OnBurst?.Invoke(layer, px, new Color(0.65f, 0.95f, 1f, 1f), 10);
            Say("Đã kích hoạt " + spec.Name + "!", 2.5f);
            return true;
        }

        // Smooth physical sliding bottle launch!
        public bool PushFloat(Layer layer, int row, int col, int direction, bool turbo = false)
        {
            if (Ended || Respawn > 0f || direction == 0) return false;
            int side = (int)layer;
            var waste = Trash[side, row, col];
            if (waste == null || CampaignCatalog.Trash[waste.Spec].Effect != TrashEffect.Float) return false;

            Trash[side, row, col] = null;
            BottlesPushedCount++;
            float speed = (turbo || dashActive > 0f) ? 8.2f : 5.6f;
            SlidingBottles.Add(new SlidingBottle
            {
                Layer = layer,
                Row = row,
                X = col,
                VelX = direction > 0 ? speed : -speed
            });
            Vector2 px = CellToPixel(col, row);
            OnSfx?.Invoke(SfxId.PushBottle);
            OnBurst?.Invoke(layer, px, new Color(0.7f, 0.96f, 1f, 1f), 8);
            Say(direction < 0 ? "Chai Phao lao về Bến Gom!" : "BOWLING RÁC! Chai Phao lao thẳng vào địch!", 2.2f);
            return true;
        }

        public bool TriggerHydroDash(Layer layer, int row, float col, int dirX)
        {
            if (Ended || Respawn > 0f || dashCooldown > 0f) return false;
            dashCooldown = 1.85f;
            dashActive = 0.28f;
            invulnerable = Mathf.Max(invulnerable, 0.45f);
            Vector2 px = CellToPixel(col, row);
            OnSfx?.Invoke(SfxId.Dive);
            OnBurst?.Invoke(layer, px, new Color(0.55f, 0.98f, 1f, 1f), 14);
            OnRing?.Invoke(layer, px, new Color(0.55f, 0.98f, 1f, 0.9f), 40f);
            OnFloatText?.Invoke(layer, px - new Vector2(0, 14), "HYDRO-DASH!", new Color(0.55f, 0.98f, 1f, 1f));

            // Instantly turbo-launch any bottle on current or adjacent cell!
            int c = Mathf.Clamp(Mathf.RoundToInt(col), 0, Columns - 1);
            int pushDir = dirX == 0 ? 1 : dirX;
            if (!PushFloat(layer, row, c, pushDir, true) && c + pushDir >= 0 && c + pushDir < Columns)
                PushFloat(layer, row, c + pushDir, pushDir, true);
            return true;
        }

        public bool RobotPulse(Layer layer, int row, float col)
        {
            if (Ended || Respawn > 0f || pulseCooldown > 0f) return false;
            if (Energy < 15) return Refuse("Bio-Pulse cần 15 NL.");
            Energy -= 15;
            pulseCooldown = PulseCooldownMax;
            int dmg = 5 + progress.UpgradeLevel(2) * 2;
            HitNear(layer, row, col, 1.75f, dmg, true, true);
            int c = Mathf.Clamp(Mathf.RoundToInt(col), 0, Columns - 1);
            for (int x = Mathf.Max(0, c - 1); x <= Mathf.Min(Columns - 1, c + 1); x++)
            {
                var w = Trash[(int)layer, row, x];
                if (w != null && CampaignCatalog.Trash[w.Spec].Effect == TrashEffect.Spill)
                {
                    Trash[(int)layer, row, x] = null;
                    AddEcoProgress(8f, 0);
                }
            }
            Vector2 px = CellToPixel(col, row);
            OnSfx?.Invoke(SfxId.Pulse);
            OnShake?.Invoke(0.22f, 5f);
            OnRing?.Invoke(layer, px, new Color(0.45f, 1f, 0.92f, 1f), 72f);
            OnBurst?.Invoke(layer, px, new Color(0.55f, 1f, 0.95f, 1f), 16);
            OnFloatText?.Invoke(layer, px - new Vector2(0, 18), "BIO-PULSE!", new Color(0.5f, 1f, 0.95f, 1f));
            Say("Xung Bio-Bot! Phá giáp, dọn dầu & đánh chìm địch!", 2.5f);
            return true;
        }

        public bool CollectOrbAt(Layer layer, Vector2 pixelPos)
        {
            for (int i = Orbs.Count - 1; i >= 0; i--)
            {
                var orb = Orbs[i];
                if (orb.Layer != layer || orb.FlyingToBank) continue;
                if (Vector2.Distance(orb.Pos, pixelPos) <= 28f)
                {
                    StartOrbFly(orb);
                    return true;
                }
            }
            return false;
        }

        private void StartOrbFly(BioOrb orb)
        {
            orb.FlyingToBank = true;
            orb.Life = 2f;
            OrbsCollectedCount++;
            OnSfx?.Invoke(SfxId.CollectOrb);
            OnBurst?.Invoke(orb.Layer, orb.Pos, new Color(1f, 0.94f, 0.42f, 1f), 6);
            OnFloatText?.Invoke(orb.Layer, orb.Pos, $"+{Mathf.RoundToInt(orb.Value)}", new Color(1f, 0.94f, 0.45f, 1f));
        }

        public void SpawnOrb(Layer layer, Vector2 startPixel, float targetY, int value, bool popArc = false)
        {
            Orbs.Add(new BioOrb
            {
                Layer = layer,
                Pos = startPixel,
                VelX = popArc ? (float)(rng.NextDouble() * 44.0 - 22.0) : 0f,
                VelY = popArc ? -115f : 15f,
                TargetY = targetY,
                Value = value,
                Life = 12f,
                Bob = (float)rng.NextDouble() * 6.28f
            });
        }

        public void Tick(float delta, Layer robotLayer, int robotRow, float robotColumn)
        {
            if (Ended) return;
            Time += delta;
            Feedback = Mathf.Max(0f, Feedback - delta);
            invulnerable = Mathf.Max(0f, invulnerable - delta);
            pulseCooldown = Mathf.Max(0f, pulseCooldown - delta);
            dashCooldown = Mathf.Max(0f, dashCooldown - delta);
            dashActive = Mathf.Max(0f, dashActive - delta);
            EnergyBounce = Mathf.Max(0f, EnergyBounce - delta);

            if (ComboTimer > 0f)
            {
                ComboTimer = Mathf.Max(0f, ComboTimer - delta);
                if (ComboTimer == 0f) ComboCount = 0;
            }

            if (EcoSurgeTime > 0f)
            {
                EcoSurgeTime = Mathf.Max(0f, EcoSurgeTime - delta);
                WaterClarity = (EcoSurgeTime / 10f) * 100f;
                surgeOrbTimer -= delta;
                if (surgeOrbTimer <= 0f)
                {
                    surgeOrbTimer = 1.6f;
                    Vector2 tgt = CellToPixel(1f + (float)rng.NextDouble() * 6.5f, rng.Next(Rows));
                    SpawnOrb(robotLayer, new Vector2(tgt.x, 74f), tgt.y, 25);
                }
            }
            else
            {
                // Uncleaned oil slicks slowly pollute water clarity
                int slicks = 0;
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Columns; c++)
                    {
                        var w = Trash[(int)Layer.Surface, r, c];
                        if (w != null && CampaignCatalog.Trash[w.Spec].Effect == TrashEffect.Spill) slicks++;
                    }
                if (slicks > 0) WaterClarity = Mathf.Max(5f, WaterClarity - slicks * 0.45f * delta);
            }

            for (int i = 0; i < CardCooldowns.Length; i++)
                if (CardCooldowns[i] > 0f) CardCooldowns[i] = Mathf.Max(0f, CardCooldowns[i] - delta);

            if (Respawn > 0f)
            {
                Respawn = Mathf.Max(0f, Respawn - delta);
                if (Respawn == 0f)
                {
                    RobotHealth = MaxRobotHealth;
                    invulnerable = 2.2f;
                }
            }

            skyOrbTimer -= delta;
            if (skyOrbTimer <= 0f)
            {
                skyOrbTimer = 6.0f;
                Vector2 tgt = CellToPixel(1f + (float)rng.NextDouble() * 6.5f, rng.Next(Rows));
                SpawnOrb(robotLayer, new Vector2(tgt.x, 74f), tgt.y, 25);
            }

            trashDropTimer -= delta;
            if (trashDropTimer <= 0f)
            {
                trashDropTimer = 16f;
                int maxTrash = Endless ? CampaignCatalog.Trash.Length : Mathf.Min(CampaignCatalog.Trash.Length, Level * 2);
                int pick = rng.Next(Mathf.Max(2, maxTrash));
                var tSpec = CampaignCatalog.Trash[pick];
                Layer tLayer = tSpec.Habitat == Habitat.Deep ? Layer.Deep :
                    tSpec.Habitat == Habitat.Surface ? Layer.Surface : (rng.Next(2) == 0 ? Layer.Surface : Layer.Deep);
                AddWaste(pick, tLayer, rng.Next(Rows), 2 + rng.Next(5));
            }

            // Update Bio-Orbs (parabolic pop + fly to HUD Sun Bank!)
            Vector2 bankPx = new Vector2(52f, 28f);
            Vector2 robotPx = CellToPixel(robotColumn, robotRow);
            float magnetPx = MagnetRadius * 58f;
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
                    orb.Pos.x = Mathf.Clamp(orb.Pos.x + orb.VelX * delta, 44f, 556f);
                    orb.VelY += 320f * delta;
                    orb.Pos.y += orb.VelY * delta;
                    if (orb.Pos.y >= orb.TargetY)
                    {
                        orb.Pos.y = orb.TargetY;
                        orb.VelX = 0f;
                        orb.VelY = 0f;
                    }
                }
                if (Respawn <= 0f && orb.Layer == robotLayer)
                {
                    float dist = Vector2.Distance(orb.Pos, robotPx);
                    if (dist < magnetPx)
                    {
                        orb.Pos = Vector2.MoveTowards(orb.Pos, robotPx, 280f * delta);
                        if (dist < 26f) { StartOrbFly(orb); continue; }
                    }
                }
                if (orb.Life <= 0f) Orbs.RemoveAt(i);
            }

            // Update Sliding Bottles (real smooth bowling physics!)
            TickSlidingBottles(delta);

            // Spawn enemy arrivals
            while (nextArrival < arrivals.Count && arrivals[nextArrival].Time <= Time)
            {
                var arrival = arrivals[nextArrival++];
                if (arrival.Wave > Wave)
                {
                    Wave = arrival.Wave;
                    Energy += 25;
                    OnSfx?.Invoke(SfxId.WaveHorn);
                    string banner = (Rules.HasBoss && !Endless && Wave == Rules.Waves)
                        ? "CẢNH BÁO ĐỎ: TÀU XẢ ĐỘC XUẤT HIỆN!"
                        : $"ĐỢT {Wave}{(Endless ? "" : "/" + Rules.Waves)} ĐANG TRÀN TỚI!";
                    OnWaveBanner?.Invoke(banner);
                    if (Endless && Wave >= 5) OnAchievement?.Invoke("endless_5");
                }
                var eSpec = CampaignCatalog.Enemies[arrival.Spec];
                int hp = Endless ? Mathf.RoundToInt(eSpec.Health * (1f + (Wave - 1) * 0.14f)) : eSpec.Health;
                Enemies.Add(new Enemy(arrival.Spec, arrival.Layer, arrival.Row, 8.75f, hp, false, (float)rng.NextDouble() * 6.28f));
            }

            if (Endless && nextArrival >= arrivals.Count && Enemies.Count <= 2)
                ScheduleEndlessWave(endlessWaveScheduled + 1, Time + 5f);

            for (int i = Beams.Count - 1; i >= 0; i--)
            {
                Beams[i].Left -= delta;
                if (Beams[i].Left <= 0f) Beams.RemoveAt(i);
            }

            for (int i = Turbines.Count - 1; i >= 0; i--)
            {
                var tw = Turbines[i];
                tw.X += 7.8f * delta;
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

            for (int i = Enemies.Count - 1; i >= 0; i--)
                TickEnemy(i, delta, robotLayer, robotRow, robotColumn);

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

        private void TickSlidingBottles(float delta)
        {
            for (int i = SlidingBottles.Count - 1; i >= 0; i--)
            {
                var b = SlidingBottles[i];
                b.X += b.VelX * delta;

                // Hit left Eco-Dock -> 2x Energy + Coins + Clarity!
                if (b.X < -0.15f)
                {
                    int bonus = 50 + progress.UpgradeLevel(4) * 15;
                    Energy += bonus;
                    EnergyBounce = 0.28f;
                    BottlesDocked++;
                    CoinsEarned += 5;
                    Score += 50 * Mathf.Max(1, ComboCount);
                    AddEcoProgress(16f, 1);
                    Vector2 px = CellToPixel(0, b.Row);
                    OnSfx?.Invoke(SfxId.Coin);
                    OnBurst?.Invoke(b.Layer, px, new Color(1f, 0.92f, 0.42f, 1f), 16);
                    OnFloatText?.Invoke(b.Layer, px, $"BẾN GOM! +{bonus} NL", new Color(1f, 0.94f, 0.42f, 1f));
                    if (BottlesDocked >= 3) OnAchievement?.Invoke("dock_master");
                    SlidingBottles.RemoveAt(i);
                    continue;
                }

                // Bowling into enemies!
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
                            if (CampaignCatalog.Enemies[e.Spec].Trait != Trait.Anchor &&
                                CampaignCatalog.Enemies[e.Spec].Trait != Trait.Boss)
                                e.X = Mathf.Min(8.7f, e.X + 1.3f);
                            Energy += 20;
                            Score += 50;
                            AddEcoProgress(12f, 1);
                            Vector2 ePx = CellToPixel(e.X, e.Row);
                            OnSfx?.Invoke(SfxId.ArmorCrack);
                            OnShake?.Invoke(0.18f, 4.5f);
                            OnBurst?.Invoke(b.Layer, ePx, new Color(1f, 0.72f, 0.35f, 1f), 14);
                            OnFloatText?.Invoke(b.Layer, ePx - new Vector2(0, 14),
                                b.Hits > 1 ? $"STRIKE x{b.Hits}!" : "BOWLING! -9", new Color(1f, 0.8f, 0.3f, 1f));
                            OnAchievement?.Invoke("bottle_bowler");
                            if (b.Hits >= 2) { SlidingBottles.RemoveAt(i); break; }
                        }
                    }
                    if (i >= SlidingBottles.Count) continue;
                }

                if (b.X > Columns + 0.3f) SlidingBottles.RemoveAt(i);
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
                        waste.Cooldown = 4.6f;
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

            ally.Jammed = Mathf.Max(0f, ally.Jammed - delta);
            float rateMult = (ally.Jammed > 0f ? 0.42f : 1f) * (IsEcoSurge ? 1.95f : 1f);
            if (layer == Layer.Surface && HasUpdraftAt(row, col)) rateMult *= 1.55f;

            ally.Cooldown -= delta * rateMult;
            if (ally.Cooldown > 0f) return;

            var spec = CampaignCatalog.Allies[ally.Spec];
            if (spec.Ability == Ability.Guard) { ally.Cooldown = 2f; return; }

            if (spec.Ability == Ability.Income)
            {
                ally.Cooldown = 7.4f;
                ally.FireAnim = 0.26f;
                Vector2 p = CellToPixel(col, row);
                SpawnOrb(layer, p - new Vector2(0, 10), p.y + 12f, spec.Power, true);
                OnBurst?.Invoke(layer, p, new Color(1f, 0.94f, 0.45f, 1f), 6);
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
                        AddEcoProgress(12f, 0);
                        OnBurst?.Invoke(layer, CellToPixel(x, row), new Color(0.55f, 1f, 0.95f, 1f), 9);
                    }
                }
            }

            bool acted = false;
            if (spec.Reach != Reach.Other) acted |= FireAlly(spec, layer, layer, row, col);
            if (spec.Reach != Reach.Local) acted |= FireAlly(spec, layer, CampaignCatalog.Opposite(layer), row, col);

            if (acted) ally.FireAnim = 0.24f;
            ally.Cooldown = acted
                ? spec.Ability switch
                {
                    Ability.Repair => 2.8f,
                    Ability.Zap => 0.95f,
                    Ability.Pulse => 2.1f,
                    _ => 1.32f
                }
                : 0.35f;
        }

        private bool ActiveWaste(Layer layer, int row, int col, TrashEffect effect)
        {
            var waste = Trash[(int)layer, row, col];
            return waste != null && waste.Active && CampaignCatalog.Trash[waste.Spec].Effect == effect;
        }

        private bool FireAlly(AllySpec spec, Layer from, Layer target, int row, int col)
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
                    Vector2 px = CellToPixel(col, row);
                    OnBurst?.Invoke(target, px, new Color(0.45f, 1f, 0.72f, 1f), 7);
                    OnFloatText?.Invoke(target, px - new Vector2(0, 10), $"+{spec.Power} HP", new Color(0.45f, 1f, 0.72f, 1f));
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
                    if (slowOnly) e.Slow = Mathf.Max(e.Slow, 3.0f);
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

            Projectiles.Add(new Projectile
            {
                Layer = target,
                Row = row,
                X = col + 0.35f,
                Speed = kind == ProjectileKind.Zap ? 7.6f : kind == ProjectileKind.Spear ? 6.8f : 5.6f,
                Damage = spec.Ability == Ability.Slow ? 1 : power,
                PierceLeft = spec.Ability == Ability.Pierce ? 2 : 1,
                Kind = kind,
                Pulse = spec.Ability is Ability.Zap or Ability.Echo or Ability.Reflect,
                Slow = spec.Ability == Ability.Slow,
                Push = spec.Ability == Ability.Pull
            });

            if (from != target)
                OnRing?.Invoke(target, CellToPixel(col, row), new Color(0.5f, 0.95f, 1f, 0.95f), 28f);
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
                        if (p.Slow) e.Slow = Mathf.Max(e.Slow, 2.8f);
                        if (p.Push && CampaignCatalog.Enemies[e.Spec].Trait != Trait.Anchor &&
                            CampaignCatalog.Enemies[e.Spec].Trait != Trait.Boss)
                            e.X = Mathf.Min(8.8f, e.X + 0.55f);

                        Vector2 hitPx = CellToPixel(e.X, e.Row);
                        Color burstCol = p.Kind == ProjectileKind.Zap ? new Color(1f, 0.92f, 0.45f, 1f) :
                            p.Slow ? new Color(0.6f, 0.95f, 1f, 1f) : new Color(0.7f, 1f, 0.72f, 1f);
                        OnBurst?.Invoke(p.Layer, hitPx, burstCol, 6);
                        OnSfx?.Invoke(SfxId.Hit);

                        p.PierceLeft--;
                        if (p.PierceLeft <= 0) { consumed = true; break; }
                    }
                }
                if (consumed) Projectiles.RemoveAt(i);
            }
        }

        private void Damage(Enemy enemy, int amount, bool pulse, bool canSink = false)
        {
            var spec = CampaignCatalog.Enemies[enemy.Spec];
            var trait = spec.Trait;
            if (trait == Trait.Shell && !enemy.Broken && !enemy.Small)
            {
                if (pulse)
                {
                    enemy.Broken = true;
                    OnSfx?.Invoke(SfxId.ArmorCrack);
                    OnFloatText?.Invoke(enemy.Layer, CellToPixel(enemy.X, enemy.Row) - new Vector2(0, 16),
                        "VỠ GIÁP!", new Color(1f, 0.85f, 0.35f, 1f));
                    OnAchievement?.Invoke("armor_breaker");
                }
                else amount = Mathf.Max(1, amount - 2);
            }
            if (!pulse && !enemy.Small && (trait == Trait.Burrow || trait == Trait.Filter || trait == Trait.Phase))
                amount = Mathf.Max(1, amount - 1);

            enemy.Health -= amount;
            enemy.Flash = 0.14f;
            enemy.HurtAnim = 0.16f;

            // Original Water-Column Physics: Heavy Pulse on Surface can SINK adaptable enemies into Deep!
            if (canSink && enemy.Health > 0 && enemy.Layer == Layer.Surface && spec.Habitat == Habitat.Both &&
                trait != Trait.Boss && !enemy.Shifted)
            {
                enemy.Shifted = true;
                enemy.Layer = Layer.Deep;
                enemy.Slow = Mathf.Max(enemy.Slow, 2.2f);
                Vector2 px = CellToPixel(enemy.X, enemy.Row);
                OnRing?.Invoke(Layer.Surface, px, new Color(0.55f, 0.95f, 1f, 1f), 42f);
                OnFloatText?.Invoke(Layer.Surface, px - new Vector2(0, 12), "ĐÁNH CHÌM!", new Color(0.55f, 0.95f, 1f, 1f));
            }
        }

        private void TickEnemy(int index, float delta, Layer heroLayer, int heroRow, float heroCol)
        {
            var enemy = Enemies[index];
            var spec = CampaignCatalog.Enemies[enemy.Spec];
            int col = Mathf.Clamp(Mathf.RoundToInt(enemy.X), 0, Columns - 1);

            if (enemy.Health <= 0)
            {
                Vector2 px = CellToPixel(enemy.X, enemy.Row);
                Enemies.RemoveAt(index);
                Kills++;
                Score += spec.Bounty * 5;
                CoinsEarned += spec.Trait == Trait.Boss ? 50 : (rng.Next(3) == 0 ? 2 : 1);
                AddEcoProgress(spec.Trait == Trait.Boss ? 40f : 4.5f, 0);
                OnBurst?.Invoke(enemy.Layer, px, new Color(1f, 0.65f, 0.45f, 1f), spec.Trait == Trait.Boss ? 30 : 12);
                if (rng.Next(100) < (spec.Trait == Trait.Boss ? 100 : 40))
                    SpawnOrb(enemy.Layer, px - new Vector2(0, 6), px.y + 8f, spec.Trait == Trait.Boss ? 50 : 15, true);
                if (spec.Trait == Trait.Spill) AddWaste(7, enemy.Layer, enemy.Row, col);
                if (spec.Trait == Trait.Split && !enemy.Small)
                {
                    Enemies.Add(new Enemy(enemy.Spec, enemy.Layer, enemy.Row, enemy.X + 0.15f, 5, true, 0f));
                    Enemies.Add(new Enemy(enemy.Spec, enemy.Layer, enemy.Row, enemy.X - 0.15f, 5, true, 3.14f));
                }
                return;
            }

            enemy.Attack = Mathf.Max(0f, enemy.Attack - delta);
            enemy.Slow = Mathf.Max(0f, enemy.Slow - delta);
            enemy.Flash = Mathf.Max(0f, enemy.Flash - delta);
            enemy.BiteAnim = Mathf.Max(0f, enemy.BiteAnim - delta);
            enemy.HurtAnim = Mathf.Max(0f, enemy.HurtAnim - delta);
            enemy.WalkPhase += delta * (enemy.Slow > 0f ? 3.5f : 7.5f);
            enemy.Recovery += delta;

            if (spec.Trait == Trait.Filter && enemy.Recovery >= 4.5f)
            {
                enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + 2);
                enemy.Recovery = 0f;
            }

            if (spec.Trait == Trait.Boss)
            {
                enemy.BossTimer -= delta;
                if (enemy.BossTimer <= 0f)
                {
                    enemy.BossTimer = 7.5f;
                    enemy.Layer = CampaignCatalog.Opposite(enemy.Layer);
                    enemy.Row = rng.Next(Rows);
                    OnRing?.Invoke(enemy.Layer, CellToPixel(enemy.X, enemy.Row), new Color(1f, 0.35f, 0.35f, 1f), 60f);
                    OnShake?.Invoke(0.25f, 5f);
                    int escort = rng.Next(4);
                    Enemies.Add(new Enemy(escort, enemy.Layer, rng.Next(Rows), Mathf.Min(8.5f, enemy.X + 0.4f),
                        CampaignCatalog.Enemies[escort].Health));
                    AddWaste(7, Layer.Surface, enemy.Row, Mathf.Clamp(col, 1, 7));
                    Say("Tàu Xả Độc đổi tầng và thả viện binh!", 2.5f);
                }
            }

            if (spec.Trait == Trait.Ferry && enemy.X < 4.7f && !enemy.Shifted)
            {
                enemy.Shifted = true;
                enemy.Layer = CampaignCatalog.Opposite(enemy.Layer);
                OnRing?.Invoke(enemy.Layer, CellToPixel(enemy.X, enemy.Row), new Color(0.55f, 0.9f, 1f, 1f), 34f);
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
                }
            }

            if (spec.Trait == Trait.Jam || spec.Trait == Trait.Phase)
            {
                var opp = Allies[(int)CampaignCatalog.Opposite(enemy.Layer), enemy.Row, col];
                if (opp != null) opp.Jammed = 1.4f;
            }

            if (Respawn <= 0f && invulnerable <= 0f && enemy.Layer == heroLayer &&
                enemy.Row == heroRow && Mathf.Abs(enemy.X - heroCol) < 0.36f)
            {
                RobotHealth--;
                invulnerable = 1.6f;
                OnShake?.Invoke(0.16f, 4f);
                OnSfx?.Invoke(SfxId.Hit);
                OnFloatText?.Invoke(heroLayer, CellToPixel(heroCol, heroRow) - new Vector2(0, 14), "-1 HP", new Color(1f, 0.45f, 0.45f, 1f));
                if (RobotHealth <= 0)
                {
                    Respawn = 4f;
                    Say("Bio-Bot đang hồi phục tại bến! Hai tầng vẫn tiếp tục.", 4f);
                }
            }

            if (ally != null && Mathf.Abs(enemy.X - col) < 0.48f)
            {
                if (enemy.Attack <= 0f)
                {
                    enemy.Attack = 0.95f;
                    enemy.BiteAnim = 0.25f;
                    ally.HurtAnim = 0.18f;
                    int bite = spec.Trait == Trait.Boss ? 5 : spec.Trait == Trait.Ram ? 3 : 1;
                    ally.Health -= bite;
                    OnBurst?.Invoke(enemy.Layer, CellToPixel(col, enemy.Row), new Color(1f, 0.85f, 0.55f, 1f), 4);
                    var def = CampaignCatalog.Allies[ally.Spec];
                    if (def.Ability == Ability.Guard) Damage(enemy, def.Power, false);
                    if (ally.Health <= 0)
                    {
                        Allies[side, enemy.Row, col] = null;
                        OnBurst?.Invoke(enemy.Layer, CellToPixel(col, enemy.Row), new Color(0.8f, 0.9f, 0.7f, 1f), 10);
                    }
                }
                return;
            }

            float speed = spec.Speed * (enemy.Slow > 0f ? 0.45f : 1f) * (IsEcoSurge ? 0.72f : 1f) * (enemy.Small ? 1.5f : 1f);
            var waste = Trash[side, enemy.Row, col];
            if (spec.Trait == Trait.Skim && waste != null && CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Spill)
                speed *= 1.85f;
            foreach (var peer in Enemies)
            {
                if (peer == enemy || peer.Layer != enemy.Layer || peer.Health <= 0) continue;
                bool school = spec.Trait == Trait.School && peer.Spec == enemy.Spec && peer.Row == enemy.Row;
                if (school || CampaignCatalog.Enemies[peer.Spec].Trait == Trait.Anchor)
                {
                    speed *= 1.25f;
                    break;
                }
            }

            enemy.X -= speed * delta;
            if (enemy.X < -0.25f)
            {
                if (TurbineReady[side, enemy.Row])
                {
                    TurbineReady[side, enemy.Row] = false;
                    Turbines.Add(new TurbineWave { Layer = enemy.Layer, Row = enemy.Row, X = -0.2f });
                    OnSfx?.Invoke(SfxId.Turbine);
                    OnShake?.Invoke(0.32f, 6f);
                    OnFloatText?.Invoke(enemy.Layer, CellToPixel(0.5f, enemy.Row), "TUABIN XẢ LŨ!", new Color(0.45f, 0.95f, 1f, 1f));
                    Say("Tuabin Cửa Đê kích hoạt! Quét sạch hàng!", 3f);
                    return;
                }
                if (enemy.X < -0.58f)
                {
                    Enemies.RemoveAt(index);
                    int loss = spec.Trait == Trait.Boss ? 5 : spec.Trait == Trait.Anchor ? 2 : 1;
                    Integrity = Mathf.Max(0, Integrity - loss);
                    OnShake?.Invoke(0.28f, 6f);
                    OnSfx?.Invoke(SfxId.Lose);
                    Say("Cảnh báo! Rác đã lọt qua Cửa Đê!", 3f);
                }
            }
        }

        private bool HasEnemyInLane(Layer layer, int row, float minCol)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e = Enemies[i];
                if (e.Health > 0 && e.Layer == layer && e.Row == row && e.X >= minCol)
                    return true;
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
            HitNear(layer, row, col, 1.55f, power, true, true);
            AddBeam(layer, row, Mathf.Max(0, col - 1), Mathf.Min(8, col + 1), new Color(0.55f, 0.95f, 1f, 1f), true);
            OnRing?.Invoke(layer, CellToPixel(col, row), new Color(0.55f, 0.95f, 1f, 1f), 46f);
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

        public void StepAutoPilot(TwoLayerBoard board, float delta)
        {
            if (Ended || Respawn > 0f || board.IsTransitioning) return;
            Layer curLayer = board.IsUnderwater ? Layer.Deep : Layer.Surface;
            Vector2Int cell = board.RobotCell;

            if (Orbs.Count > 0)
            {
                BioOrb nearest = null;
                float best = 9999f;
                Vector2 rPx = CellToPixel(board.RobotColumn, cell.y);
                foreach (var o in Orbs)
                {
                    if (o.Layer == curLayer && !o.FlyingToBank)
                    {
                        float d = Vector2.Distance(o.Pos, rPx);
                        if (d < best) { best = d; nearest = o; }
                    }
                }
                if (nearest != null)
                {
                    float col = Mathf.Clamp((nearest.Pos.x - 58f) / 60f, 0f, 8f);
                    float row = Mathf.Clamp((nearest.Pos.y - 109f) / 54f, 0f, 4f);
                    board.MoveRobotToward(CellToLocalWorld(col, row), RobotSpeedBonus);
                }
            }

            var w = Trash[(int)curLayer, cell.y, cell.x];
            if (w != null)
            {
                var eff = CampaignCatalog.Trash[w.Spec].Effect;
                if (eff == TrashEffect.Filament) Collect(curLayer, cell.y, cell.x);
                else if (eff == TrashEffect.Float) PushFloat(curLayer, cell.y, cell.x, -1, true);
                else if (!w.Active && (eff is TrashEffect.Mirror or TrashEffect.Vent or TrashEffect.Loom)) UseTrash(curLayer, cell.y, cell.x);
                else Collect(curLayer, cell.y, cell.x);
            }

            int incomeCount = 0;
            for (int r = 0; r < Rows; r++)
                if (Allies[(int)curLayer, r, 0] != null && Allies[(int)curLayer, r, 0].Spec == 2) incomeCount++;

            if (incomeCount < 3 && Energy >= 50 && CardCooldowns[2] <= 0f)
            {
                for (int r = 0; r < Rows; r++)
                    if (Free(curLayer, r, 0)) { Plant(2, curLayer, r, 0); return; }
            }

            Enemy urgent = null;
            foreach (var e in Enemies)
                if (urgent == null || e.X < urgent.X) urgent = e;

            if (urgent != null)
            {
                if (urgent.Layer != curLayer && urgent.X < 5.5f)
                {
                    board.RequestDive(urgent.Layer == Layer.Deep);
                    return;
                }
                if (urgent.Layer == curLayer)
                {
                    int defender = curLayer == Layer.Surface ? 0 : 3;
                    if (Energy >= 125 && (Endless || Level >= 3) && CardCooldowns[7] <= 0f) defender = 7;
                    for (int c = 1; c <= 3; c++)
                    {
                        if (Free(curLayer, urgent.Row, c) && CanPlant(defender, curLayer, urgent.Row, c, out _))
                        {
                            Plant(defender, curLayer, urgent.Row, c);
                            break;
                        }
                    }
                    if (urgent.X < 2.2f && pulseCooldown <= 0f && Energy >= 15)
                        RobotPulse(curLayer, cell.y, board.RobotColumn);
                }
            }
        }

        private static Vector2 CellToLocalWorld(float col, float row)
        {
            float x = col * (30f / 24f) - 163f / 24f;
            float y = 67f / 24f - (row + 0.5f) * (27f / 24f);
            return new Vector2(x, y);
        }
    }
}
