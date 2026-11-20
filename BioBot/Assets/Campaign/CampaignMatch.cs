using System.Collections.Generic;
using UnityEngine;

namespace BioBot
{
    public sealed class CampaignMatch
    {
        public const int Rows = 5, Columns = 9, MaxIntegrity = 8, MaxRobot = 4;

        public sealed class Ally
        {
            public readonly int Spec;
            public int Health;
            public float Cooldown, Jammed, Temporary;
            public Ally(int spec, int health, float temporary = 0)
            {
                Spec = spec;
                Health = health;
                Temporary = temporary;
            }
        }

        public sealed class Waste
        {
            public readonly int Spec;
            public bool Active;
            public float Cooldown;
            public Waste(int spec) => Spec = spec;
        }

        public sealed class Enemy
        {
            public readonly int Spec;
            public Layer Layer;
            public int Row, Health, MaxHealth;
            public float X, Attack, Slow, Recovery, Flash;
            public bool Shifted, Broken, Small;

            public Enemy(int spec, Layer layer, int row, float x, int health, bool small = false)
            {
                Spec = spec; Layer = layer; Row = row; X = x;
                Health = MaxHealth = health; Small = small;
            }
        }

        public sealed class Effect
        {
            public Layer Layer;
            public int Row;
            public float From, To, Left;
            public bool Pulse;
        }

        private struct Arrival
        {
            public float Time;
            public int Wave, Spec, Row;
            public Layer Layer;
        }

        public readonly Ally[,,] Allies = new Ally[2, Rows, Columns];
        public readonly Waste[,,] Trash = new Waste[2, Rows, Columns];
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Effect> Effects = new List<Effect>();

        private readonly List<Arrival> arrivals = new List<Arrival>();
        private int nextArrival;
        private float income = 3f;
        private float invulnerable;
        private float pulseCooldown;

        public readonly int Level;
        public LevelSpec Rules => CampaignCatalog.Levels[Level - 1];
        public int Energy { get; private set; }
        public int Integrity { get; private set; } = MaxIntegrity;
        public int RobotHealth { get; private set; } = MaxRobot;
        public int Wave { get; private set; }
        public float Time { get; private set; }
        public float Respawn { get; private set; }
        public bool Won { get; private set; }
        public bool Lost => Integrity <= 0;
        public bool Ended => Won || Lost;
        public string Message { get; private set; }
        public float NextAttack => nextArrival < arrivals.Count ? Mathf.Max(0, arrivals[nextArrival].Time - Time) : -1;
        public int NextRow => nextArrival < arrivals.Count ? arrivals[nextArrival].Row : -1;
        public Layer NextLayer => nextArrival < arrivals.Count ? arrivals[nextArrival].Layer : Layer.Surface;
        public float Feedback { get; private set; }

        public CampaignMatch(int level)
        {
            Level = level;
            Energy = Rules.Budget;
            Say(Rules.Lesson, 10f);
            CreateArrivals();
            PlaceStartingTrash();
        }

        private void CreateArrivals()
        {
            var random = new System.Random(1021 + Level * 31);
            float start = 10f;
            var eligible = new List<int>();
            for (int i = 0; i < CampaignCatalog.Enemies.Length; i++)
                if (CampaignCatalog.Enemies[i].Unlock <= Level)
                    eligible.Add(i);

            for (int wave = 1; wave <= Rules.Waves; wave++)
            {
                var introduced = eligible.FindAll(id => CampaignCatalog.Enemies[id].Unlock == Level);
                int count = 2 + Level + wave;
                for (int n = 0; n < count; n++)
                {
                    int spec = n < introduced.Count && wave == 1 ? introduced[n] :
                        eligible[random.Next(eligible.Count)];
                    Habitat habitat = CampaignCatalog.Enemies[spec].Habitat;
                    Layer target = random.Next(100) < Rules.SurfaceWeight ? Layer.Surface : Layer.Deep;
                    if (!CampaignCatalog.Fits(habitat, target))
                        target = CampaignCatalog.Opposite(target);
                    int row = Level == 1 && wave == 1 ? 2 : (n + wave + random.Next(3)) % Rows;
                    arrivals.Add(new Arrival { Time = start + n * 2.4f, Wave = wave, Spec = spec, Row = row, Layer = target });
                }
                start += count * 2.4f + 12f;
            }
        }

        private void PlaceStartingTrash()
        {
            AddWaste(0, Layer.Surface, 2, 3);
            AddWaste(1, Layer.Surface, 3, 2);
            if (Level == 1) return;
            AddWaste(2, Layer.Deep, 2, 2);
            AddWaste(3, Layer.Deep, 1, 4);
            if (Level >= 3)
            {
                AddWaste(4, Layer.Deep, 2, 4);
                AddWaste(5, Layer.Surface, 0, 5);
            }
            if (Level >= 4)
            {
                AddWaste(6, Layer.Deep, 3, 3);
                AddWaste(7, Layer.Surface, 2, 6);
            }
            if (Level >= 5)
            {
                AddWaste(8, Layer.Deep, 2, 5);
                AddWaste(9, Layer.Surface, 4, 5);
            }
        }

        private void AddWaste(int id, Layer layer, int row, int column)
        {
            if (Trash[(int)layer, row, column] == null && Allies[(int)layer, row, column] == null)
                Trash[(int)layer, row, column] = new Waste(id);
        }

        public bool Plant(int id, Layer layer, int row, int column)
        {
            if (Ended || Respawn > 0 || id < 0 || id >= CampaignCatalog.Allies.Length) return false;
            var spec = CampaignCatalog.Allies[id];
            if (spec.Unlock > Level) return false;
            if (!CampaignCatalog.Fits(spec.Habitat, layer)) return Refuse("Đồng minh này cần tầng còn lại.");
            if (Allies[(int)layer, row, column] != null || Trash[(int)layer, row, column] != null)
                return Refuse("Ô đã có đồng minh hoặc rác. F để dọn rác.");
            if (Energy < spec.Cost) return Refuse("Chưa đủ năng lượng.");
            Energy -= spec.Cost;
            Allies[(int)layer, row, column] = new Ally(id, spec.Health);
            Say("Đã đặt " + spec.Name + ".");
            return true;
        }

        public bool Collect(Layer layer, int row, int column)
        {
            if (Ended || Respawn > 0) return false;
            int side = (int)layer;
            var waste = Trash[side, row, column];
            if (waste == null) return Refuse("Ô này không có rác.");
            var spec = CampaignCatalog.Trash[waste.Spec];
            Trash[side, row, column] = null;
            int value = spec.Effect == TrashEffect.Float ? 1 : spec.Value;
            Energy += value;
            if (spec.Effect == TrashEffect.Filament && Allies[side, row, column] == null)
            {
                Allies[side, row, column] = new Ally(3, CampaignCatalog.Allies[3].Health);
                Say("Đã cắt cước. Cá Vệt ở lại phòng thủ ô này.");
            }
            else Say($"+{value} năng lượng từ {spec.Name}.");
            return true;
        }

        public bool UseTrash(Layer layer, int row, int column)
        {
            if (Ended || Respawn > 0) return false;
            int side = (int)layer;
            var waste = Trash[side, row, column];
            if (waste == null) return Refuse("G cần rác ở ô hiện tại.");
            var spec = CampaignCatalog.Trash[waste.Spec];
            Layer opposite = CampaignCatalog.Opposite(layer);
            int other = (int)opposite;
            switch (spec.Effect)
            {
                case TrashEffect.Float:
                    return PushFloat(layer, row, column, -1);
                case TrashEffect.Filament:
                    return Collect(layer, row, column);
                case TrashEffect.Ballast:
                    if (!Free(opposite, row, column))
                        return Refuse("Ô mặt nước bị chiếm. F để thu cục chì.");
                    Allies[other, row, column] = new Ally(1, 9, 24);
                    break;
                case TrashEffect.Shell:
                    Allies[side, row, column] = new Ally(1, 7, 22);
                    break;
                case TrashEffect.Capsule:
                    if (!Free(opposite, row, column))
                        return Refuse("Ô tầng kia bị chiếm.");
                    Trash[other, row, column] = waste;
                    break;
                case TrashEffect.Mirror:
                case TrashEffect.Vent:
                    if (waste.Active) return Refuse("Đang hoạt động. F để tháo.");
                    waste.Active = true;
                    Say(spec.Name + " đang nối ô giữa hai tầng.");
                    return true;
                case TrashEffect.Battery:
                    Shock(layer, row, column);
                    Shock(opposite, row, column);
                    break;
                case TrashEffect.Spill:
                    HitNear(layer, row, column, 1.3f, 2, true);
                    break;
                case TrashEffect.Loom:
                    if (waste.Active) return Refuse("Lưới đã giăng. F để tháo.");
                    waste.Active = true;
                    waste.Cooldown = 18f;
                    DrawEffect(opposite, row, column, Columns - 1, true);
                    Say("Lưới giữ chậm cả hàng đối diện trong 18 giây.");
                    return true;
            }
            Trash[side, row, column] = null;
            Say(spec.Name + " đã được sử dụng.");
            return true;
        }

        public bool PushFloat(Layer layer, int row, int column, int direction)
        {
            if (Ended || Respawn > 0 || direction == 0) return false;
            int side = (int)layer;
            var waste = Trash[side, row, column];
            if (waste == null || CampaignCatalog.Trash[waste.Spec].Effect != TrashEffect.Float) return false;

            if (column + direction < 0)
            {
                Energy += CampaignCatalog.Trash[waste.Spec].Value * 2;
                Trash[side, row, column] = null;
                Say("Chai vào bến. +6 năng lượng.");
                return true;
            }
            if (column + direction >= Columns || !Free(layer, row, column + direction)) return false;
            Trash[side, row, column + direction] = waste;
            Trash[side, row, column] = null;
            Say("Đưa chai về mép trái để được thưởng đôi. F thu ngay.");
            return true;
        }

        public bool RobotPulse(Layer layer, int row, float column)
        {
            if (Ended || Respawn > 0 || pulseCooldown > 0 || Energy <= 0) return false;
            if (!HasNear(layer, row, column, 1.25f)) return Refuse("Cần địch ở gần robot để phát xung.");
            Energy--;
            pulseCooldown = 2.5f;
            HitNear(layer, row, column, 1.25f, 4, true);
            DrawEffect(layer, row, column - 1, column + 1, true);
            Say("Xung Bio-Bot. Có thể tháo vỏ.");
            return true;
        }

        private bool Free(Layer layer, int row, int column) =>
            Trash[(int)layer, row, column] == null && Allies[(int)layer, row, column] == null;

        private void Say(string message, float duration = 3f)
        {
            Message = message;
            Feedback = duration;
        }

        private bool Refuse(string message)
        {
            Say(message);
            return false;
        }

        public void Tick(float delta, Layer robotLayer, int robotRow, float robotColumn)
        {
            if (Ended) return;
            Time += delta;
            Feedback -= delta;
            invulnerable -= delta;
            pulseCooldown -= delta;

            if (Respawn > 0)
            {
                Respawn = Mathf.Max(0, Respawn - delta);
                if (Respawn == 0)
                {
                    RobotHealth = MaxRobot;
                    invulnerable = 2;
                }
            }

            income -= delta;
            if (income <= 0)
            {
                income += 3f;
                Energy += 2;
            }

            while (nextArrival < arrivals.Count && arrivals[nextArrival].Time <= Time)
            {
                var arrival = arrivals[nextArrival++];
                if (arrival.Wave > Wave)
                {
                    Wave = arrival.Wave;
                    Energy += 4;
                    Say($"Đợt {Wave}/{Rules.Waves}. Kiểm tra bản đồ nhỏ.");
                }
                Enemies.Add(new Enemy(arrival.Spec, arrival.Layer, arrival.Row, 8.65f,
                    CampaignCatalog.Enemies[arrival.Spec].Health));
            }

            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                Effects[i].Left -= delta;
                if (Effects[i].Left <= 0) Effects.RemoveAt(i);
            }

            for (int side = 0; side < 2; side++)
                for (int row = 0; row < Rows; row++)
                    for (int column = 0; column < Columns; column++)
                        TickTile((Layer)side, row, column, delta);

            for (int i = Enemies.Count - 1; i >= 0; i--)
                TickEnemy(i, delta, robotLayer, robotRow, robotColumn);
            if (nextArrival == arrivals.Count && Enemies.Count == 0 && !Lost)
                Won = true;
        }

        private void TickTile(Layer layer, int row, int column, float delta)
        {
            int side = (int)layer;
            var waste = Trash[side, row, column];
            if (waste != null && waste.Active && CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Loom)
            {
                waste.Cooldown -= delta;
                foreach (var enemy in Enemies)
                    if (enemy.Layer != layer && enemy.Row == row && enemy.X >= column - .5f)
                        enemy.Slow = Mathf.Max(enemy.Slow, .7f);
                if (waste.Cooldown <= 0) Trash[side, row, column] = null;
            }
            if (waste != null && waste.Active && CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Vent)
            {
                waste.Cooldown -= delta;
                if (waste.Cooldown <= 0)
                {
                    Shock(layer, row, column);
                    Shock(CampaignCatalog.Opposite(layer), row, column);
                    waste.Cooldown = 5;
                }
            }
            var ally = Allies[side, row, column];
            if (ally == null) return;
            if (ally.Temporary > 0)
            {
                ally.Temporary -= delta;
                if (ally.Temporary <= 0)
                {
                    Allies[side, row, column] = null;
                    return;
                }
            }
            ally.Jammed -= delta;
            ally.Cooldown -= delta * (ally.Jammed > 0 ? .35f : 1f);
            if (ally.Cooldown > 0) return;

            var spec = CampaignCatalog.Allies[ally.Spec];
            ally.Cooldown = spec.Ability switch
            {
                Ability.Income => 5f,
                Ability.Repair => 2.6f,
                Ability.Zap => .85f,
                Ability.Pulse => 2.3f,
                _ => 1.35f
            };
            if (spec.Ability == Ability.Income)
            {
                Energy += spec.Power;
                return;
            }
            if (spec.Ability == Ability.Guard) return;
            if (spec.Ability == Ability.Clean)
            {
                for (int x = Mathf.Max(0, column - 1); x <= Mathf.Min(Columns - 1, column + 1); x++)
                {
                    var nearby = Trash[side, row, x];
                    if (nearby != null && CampaignCatalog.Trash[nearby.Spec].Effect == TrashEffect.Spill)
                        Trash[side, row, x] = null;
                }
            }
            if (spec.Reach != Reach.Other) ApplyAlly(spec, layer, layer, row, column);
            if (spec.Reach != Reach.Local) ApplyAlly(spec, layer, CampaignCatalog.Opposite(layer), row, column);
        }

        private bool ActiveWaste(Layer layer, int row, int column, TrashEffect effect)
        {
            var waste = Trash[(int)layer, row, column];
            return waste != null && waste.Active && CampaignCatalog.Trash[waste.Spec].Effect == effect;
        }

        private void ApplyAlly(AllySpec spec, Layer from, Layer target, int row, int column)
        {
            if (from != target && spec.Ability == Ability.Reflect &&
                !ActiveWaste(from, row, column, TrashEffect.Mirror) && !ActiveWaste(target, row, column, TrashEffect.Mirror))
                return;
            if (from != target && spec.Ability == Ability.Link && Allies[(int)target, row, column] == null)
                return;
            if (spec.Ability == Ability.Repair)
            {
                var patient = Allies[(int)target, row, column];
                if (patient != null && patient.Health < CampaignCatalog.Allies[patient.Spec].Health)
                {
                    patient.Health = Mathf.Min(patient.Health + spec.Power, CampaignCatalog.Allies[patient.Spec].Health);
                    DrawEffect(target, row, column, column + .2f, true);
                }
                return;
            }

            int hits = spec.Ability == Ability.Pierce ? 2 : 1;
            bool wide = spec.Ability == Ability.Pulse || (spec.Ability == Ability.Habitat && from == Layer.Deep);
            bool pulse = spec.Ability is Ability.Pulse or Ability.Zap or Ability.Echo;
            int power = spec.Power;
            if (spec.Ability == Ability.Echo && (ActiveWaste(from, row, column, TrashEffect.Vent) ||
                ActiveWaste(target, row, column, TrashEffect.Vent)))
                power += 3;
            var struck = new HashSet<Enemy>();
            for (int n = 0; n < (wide ? Enemies.Count : hits); n++)
            {
                var enemy = FirstEnemy(target, row, column, struck);
                if (enemy == null) break;
                struck.Add(enemy);
                if (spec.Ability == Ability.Pull && CampaignCatalog.Enemies[enemy.Spec].Trait != Trait.Anchor)
                    enemy.X = Mathf.Min(8.8f, enemy.X + .4f);
                if (spec.Ability == Ability.Slow || (spec.Ability == Ability.Habitat && from == Layer.Deep))
                    enemy.Slow = Mathf.Max(enemy.Slow, spec.Power * .7f);
                else
                    Damage(enemy, power, pulse);
                DrawEffect(target, row, column, enemy.X, pulse || spec.Ability == Ability.Slow);
            }
        }

        private Enemy FirstEnemy(Layer layer, int row, float column, HashSet<Enemy> struck)
        {
            Enemy first = null;
            foreach (var enemy in Enemies)
            {
                if (enemy.Health <= 0 || enemy.Layer != layer || enemy.Row != row || enemy.X < column - .25f) continue;
                if (struck.Contains(enemy)) continue;
                if (first == null || enemy.X < first.X) first = enemy;
            }
            return first;
        }

        private bool HasNear(Layer layer, int row, float column, float distance)
        {
            foreach (var enemy in Enemies)
                if (enemy.Health > 0 && enemy.Layer == layer && enemy.Row == row && Mathf.Abs(enemy.X - column) <= distance)
                    return true;
            return false;
        }

        private void HitNear(Layer layer, int row, float column, float range, int power, bool pulse)
        {
            foreach (var enemy in Enemies)
                if (enemy.Health > 0 && enemy.Layer == layer && enemy.Row == row && Mathf.Abs(enemy.X - column) <= range)
                    Damage(enemy, power, pulse);
        }

        private void Shock(Layer layer, int row, int column)
        {
            HitNear(layer, row, column, 1.45f, 4, true);
            DrawEffect(layer, row, Mathf.Max(0, column - 1), Mathf.Min(8, column + 1), true);
        }

        private void DrawEffect(Layer layer, int row, float from, float to, bool pulse) =>
            Effects.Add(new Effect { Layer = layer, Row = row, From = from, To = to, Left = .20f, Pulse = pulse });

        private static void Damage(Enemy enemy, int amount, bool pulse)
        {
            var trait = CampaignCatalog.Enemies[enemy.Spec].Trait;
            if (trait == Trait.Shell && !enemy.Broken && !enemy.Small)
            {
                if (pulse) enemy.Broken = true;
                else amount = Mathf.Max(1, amount - 2);
            }
            if (!pulse && !enemy.Small && (trait == Trait.Burrow || trait == Trait.Filter))
                amount = Mathf.Max(1, amount - 1);
            enemy.Health -= amount;
            enemy.Flash = .14f;
        }

        private void TickEnemy(int index, float delta, Layer heroLayer, int heroRow, float heroColumn)
        {
            var enemy = Enemies[index];
            var spec = CampaignCatalog.Enemies[enemy.Spec];
            int column = Mathf.Clamp(Mathf.RoundToInt(enemy.X), 0, Columns - 1);
            if (enemy.Health <= 0)
            {
                Enemies.RemoveAt(index);
                Energy++;
                if (spec.Trait == Trait.Spill) AddWaste(7, enemy.Layer, enemy.Row, column);
                if (spec.Trait == Trait.Split && !enemy.Small)
                    Enemies.Add(new Enemy(enemy.Spec, enemy.Layer, enemy.Row, enemy.X, 4, true));
                return;
            }

            enemy.Attack -= delta;
            enemy.Slow -= delta;
            enemy.Flash -= delta;
            enemy.Recovery += delta;
            if (spec.Trait == Trait.Filter && enemy.Recovery >= 5)
            {
                enemy.Health = Mathf.Min(enemy.MaxHealth, enemy.Health + 1);
                enemy.Recovery = 0;
            }

            if (spec.Trait == Trait.Ferry && enemy.X < 4.7f && !enemy.Shifted)
            {
                enemy.Shifted = true;
                enemy.Layer = CampaignCatalog.Opposite(enemy.Layer);
                DrawEffect(enemy.Layer, enemy.Row, enemy.X - .25f, enemy.X + .25f, true);
            }

            int side = (int)enemy.Layer;
            var ally = Allies[side, enemy.Row, column];
            if (spec.Trait == Trait.Drift && !enemy.Shifted && ally != null)
            {
                int adjacent = enemy.Row == Rows - 1 ? enemy.Row - 1 : enemy.Row + 1;
                if (Allies[side, adjacent, column] == null)
                {
                    enemy.Row = adjacent;
                    enemy.Shifted = true;
                    ally = null;
                }
            }
            if (spec.Trait == Trait.Jam)
            {
                var opposite = Allies[(int)CampaignCatalog.Opposite(enemy.Layer), enemy.Row, column];
                if (opposite != null) opposite.Jammed = 1.2f;
            }

            if (Respawn <= 0 && invulnerable <= 0 && enemy.Layer == heroLayer &&
                enemy.Row == heroRow && Mathf.Abs(enemy.X - heroColumn) < .32f)
            {
                RobotHealth--;
                invulnerable = 1.5f;
                if (RobotHealth <= 0)
                {
                    Respawn = 4;
                    Say("Bio-Bot đang hồi phục. Hai tầng vẫn tiếp tục.", 4);
                }
            }

            if (ally != null && Mathf.Abs(enemy.X - column) < .48f)
            {
                if (enemy.Attack <= 0)
                {
                    enemy.Attack = .95f;
                    ally.Health -= spec.Trait == Trait.Ram ? 3 : 1;
                    var defense = CampaignCatalog.Allies[ally.Spec];
                    if (defense.Ability == Ability.Guard) Damage(enemy, defense.Power, false);
                    if (ally.Health <= 0) Allies[side, enemy.Row, column] = null;
                }
                return;
            }

            float speed = spec.Speed * (enemy.Slow > 0 ? .46f : 1f) * (enemy.Small ? 1.55f : 1f);
            var waste = Trash[side, enemy.Row, column];
            if (spec.Trait == Trait.Skim && waste != null && CampaignCatalog.Trash[waste.Spec].Effect == TrashEffect.Spill)
                speed *= 1.9f;
            foreach (var peer in Enemies)
            {
                if (peer == enemy || peer.Layer != enemy.Layer || peer.Health <= 0) continue;
                bool school = spec.Trait == Trait.School && peer.Spec == enemy.Spec && peer.Row == enemy.Row;
                if (school || CampaignCatalog.Enemies[peer.Spec].Trait == Trait.Anchor)
                {
                    speed *= 1.27f;
                    break;
                }
            }
            enemy.X -= speed * delta;
            if (enemy.X < -.55f)
            {
                Enemies.RemoveAt(index);
                Integrity = Mathf.Max(0, Integrity - (spec.Trait == Trait.Anchor ? 2 : 1));
                Say("Rác đã lọt qua cửa đê.");
            }
        }
    }
}
