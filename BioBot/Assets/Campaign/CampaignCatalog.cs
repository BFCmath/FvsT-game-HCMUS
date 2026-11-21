using System;
using System.Globalization;
using UnityEngine;

namespace BioBot
{
    [Flags]
    public enum Habitat { Surface = 1, Deep = 2, Both = 3 }
    public enum Layer { Surface, Deep }
    public enum TrashEffect { Float, Filament, Ballast, Spill, Battery, Mirror, Shell, Vent, Loom, Capsule, Salvage, Clutter }
    public enum Trait { Skim, Burrow, Ferry, Filter, Spill, Shell, Jam, School, Ram, Split, Drift, Anchor, Phase, Boss, Litter, None }
    public enum Ability { Shot, Guard, Repair, Slow, Zap, Income, Pull, Pulse, Reflect, Link, Pierce, Echo, Habitat, Clean, Swap }
    public enum Reach { Local, Other, Both }

    public sealed class AllySpec
    {
        public readonly string Name, Role, ShortTag, Hint;
        public readonly Habitat Habitat;
        public readonly Reach Reach;
        public readonly Ability Ability;
        public readonly int Cost, Health, Unlock, Power;
        public readonly float Recharge;
        public readonly bool BoostsSurface;

        public AllySpec(string name, string role, string shortTag, Habitat habitat, Reach reach, Ability ability,
            int cost, int health, int unlock, int power, float recharge, string hint, bool boostsSurface = false)
        {
            Name = name;
            Role = role;
            ShortTag = shortTag;
            Habitat = habitat;
            Reach = reach;
            Ability = ability;
            Cost = cost;
            Health = health;
            Unlock = unlock;
            Power = power;
            Recharge = recharge;
            BoostsSurface = boostsSurface;
            Hint = hint;
        }
    }

    public sealed class EnemySpec
    {
        public readonly string Name, Hint;
        public readonly Habitat Habitat;
        public readonly Trait Trait;
        public readonly int Health, Unlock, Bounty;
        public readonly float Speed;

        public int SpawnHealth(Layer layer) =>
            layer == Layer.Deep && Trait != Trait.Boss ? Mathf.RoundToInt(Health * 1.4f) : Health;

        public EnemySpec(string name, Habitat habitat, Trait trait, int health, float speed, int unlock, int bounty, string hint)
        {
            Name = name;
            Habitat = habitat;
            Trait = trait;
            Health = health;
            Speed = speed;
            Unlock = unlock;
            Bounty = bounty;
            Hint = hint;
        }
    }

    public sealed class TrashSpec
    {
        public readonly string Name, Hint, QuickAction;
        public readonly Habitat Habitat;
        public readonly TrashEffect Effect;
        public readonly int Value, Unlock;

        public TrashSpec(string name, Habitat habitat, TrashEffect effect, int value, int unlock, string quickAction, string hint)
        {
            Name = name;
            Habitat = habitat;
            Effect = effect;
            Value = value;
            Unlock = unlock;
            QuickAction = quickAction;
            Hint = hint;
        }
    }

    public sealed class WaveSegment
    {
        public readonly Layer Layer;
        public readonly float Start, FirstGap, LastGap;
        public readonly int LaneOffset;
        public readonly int[] Enemies, LanePattern;
        public readonly bool StartsSwarm;

        public WaveSegment(Layer layer, float start, float firstGap, float lastGap, int laneOffset,
            int[] enemies, bool startsSwarm = false, int[] lanePattern = null)
        {
            Layer = layer;
            Start = start;
            FirstGap = firstGap;
            LastGap = lastGap;
            LaneOffset = laneOffset;
            Enemies = enemies;
            StartsSwarm = startsSwarm;
            LanePattern = lanePattern;
        }
    }

    public sealed class WaveSpec
    {
        public readonly WaveSegment[] Segments;

        public WaveSpec(params WaveSegment[] segments)
        {
            Segments = segments;
        }
    }

    public sealed class LevelSpec
    {
        public readonly string Name, Subtitle, Story, Lesson, ConceptTitle, ConceptBody;
        public readonly int Budget, SurfaceWeight, CoinReward;
        public readonly WaveSpec[] Arrivals;
        private readonly int plannedWaves;
        public int Waves => Arrivals?.Length ?? plannedWaves;
        public readonly bool HasBoss, DeepActive;

        public LevelSpec(string name, string subtitle, int budget, int coinReward, bool deepActive,
            string conceptTitle, string conceptBody, string story, string lesson, params WaveSpec[] arrivals)
            : this(name, subtitle, budget, 0, 0, coinReward, false, deepActive, conceptTitle, conceptBody, story, lesson, arrivals)
        {
        }

        public LevelSpec(string name, string subtitle, int budget, int waves, int surfaceWeight,
            int coinReward, bool hasBoss, bool deepActive, string conceptTitle, string conceptBody, string story, string lesson,
            WaveSpec[] arrivals = null)
        {
            Name = name;
            Subtitle = subtitle;
            Budget = budget;
            plannedWaves = waves;
            Arrivals = arrivals;
            SurfaceWeight = surfaceWeight;
            CoinReward = coinReward;
            HasBoss = hasBoss;
            DeepActive = deepActive;
            ConceptTitle = conceptTitle;
            ConceptBody = conceptBody;
            Story = story;
            Lesson = lesson;
        }
    }

    public sealed class UpgradeSpec
    {
        public readonly string Name, Desc;
        public readonly int BaseCost, MaxLevel;

        public UpgradeSpec(string name, string desc, int baseCost, int maxLevel)
        {
            Name = name;
            Desc = desc;
            BaseCost = baseCost;
            MaxLevel = maxLevel;
        }

        public int CostAt(int level) => BaseCost * (level + 1);
    }

    public sealed class AchievementSpec
    {
        public readonly string Id, Title, Desc;
        public readonly int RewardCoins;

        public AchievementSpec(string id, string title, string desc, int rewardCoins)
        {
            Id = id;
            Title = title;
            Desc = desc;
            RewardCoins = rewardCoins;
        }
    }

    public static class CampaignCatalog
    {
        public const int TotalAtlasRows = 48;
        public const int PlayableStageCount = 4;
        public const int RetiredPulseUpgrade = 2;
        public const int BossId = 13;
        private const int Skimmer = 0, Bag = 1, Crawler = 2, Ferry = 4, Litterer = 14, Capped = 15;
        internal static readonly int[] ArrivalRows = { 2, 1, 3, 0, 4 };
        public static readonly int[] PlayableEnemyIds = { Skimmer, Capped, Bag, Crawler, Ferry, Litterer };
        public const int EnergySalvageId = 10;
        public const int ClutterId = 11;
        public static readonly int[] PlayableTrashIds = { 0, 1, EnergySalvageId, 2, 3, ClutterId };
        public static readonly int[] PlayableAchievementIds = { 0, 1, 2, 3, 4, 6, 7, 9 };
        private const Habitat S = Habitat.Surface;
        private const Habitat D = Habitat.Deep;
        private const Habitat B = Habitat.Both;
        private const Reach L = Reach.Local;
        private const Reach O = Reach.Other;
        private const Reach X = Reach.Both;

        internal static readonly (int Spec, Layer Layer, int Row, int Col, int LastStage)[] StartingTrash =
        {
            (0, Layer.Surface, 2, 4, int.MaxValue),
            (1, Layer.Surface, 3, 3, int.MaxValue),
            (EnergySalvageId, Layer.Deep, 3, 4, int.MaxValue),
            (1, Layer.Deep, 1, 4, 2),
            (2, Layer.Deep, 2, 3, int.MaxValue),
            (3, Layer.Deep, 1, 5, int.MaxValue),
            (4, Layer.Deep, 2, 5, int.MaxValue),
            (5, Layer.Surface, 0, 6, int.MaxValue),
            (6, Layer.Deep, 3, 4, int.MaxValue),
            (7, Layer.Surface, 2, 7, int.MaxValue),
            (8, Layer.Deep, 2, 6, int.MaxValue),
            (9, Layer.Surface, 4, 6, int.MaxValue)
        };

        // Gaps compress from FirstGap to LastGap within a segment. Each segment cycles through the
        // five lanes from LaneOffset, with an optional order for stacking lanes. Staggered
        // segments on both layers share one swarm cue.
        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec(
                "Bến Gom Nắng Mai", "Rái Cá và Vịt Bắn Bọt", 100, 35, false,
                "KHÁI NIỆM MỚI: RÁI CÁ & VỊT BẮN BỌT",
                "• Đặt Rái Cá ở cột trái để tạo hạt vàng. Mỗi hạt cho 20 năng lượng.\n• Dùng Vịt Bắn Bọt để giữ các hàng có địch. Cảnh báo bật khi địch vượt qua 5 cột ngoài cùng bên phải.\n• Đẩy Chai Phao về bến trái lấy 50 NL hoặc đẩy phải để húc địch. Dồn hai Vịt cùng hàng để hạ Can Đội Nắp cuối màn.",
                "Chương 1 — Cửa Sông Bình Minh: Đợt rác đầu tiên trôi vào mặt bể. Hãy dùng Rái Cá tạo năng lượng và Vịt Bắn Bọt bảo vệ bờ đê!",
                "Màn 1 chỉ có 2 thẻ: Đặt Rái Cá ở cột trái tạo Năng Lượng, sau đó đặt Vịt Bắn Bọt chặn quái rác!",
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 9, 5.5f, 5.5f, 0,
                        new[] { Skimmer, Skimmer, Skimmer, Skimmer, Skimmer }),
                    new WaveSegment(Layer.Surface, 33, 2.8f, 1.2f, 1,
                        new[] { Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 49, .60f, .60f, 1,
                        new[] { Capped, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer },
                        startsSwarm: true, lanePattern: new[] { 0, 1, 0, 2, 3, 4, 0 }),
                    new WaveSegment(Layer.Surface, 56, .55f, .55f, 1,
                        new[] { Skimmer, Skimmer, Capped, Skimmer, Skimmer, Skimmer, Skimmer },
                        startsSwarm: true, lanePattern: new[] { 0, 0, 2, 3, 4, 1, 0 }),
                    new WaveSegment(Layer.Surface, 63, .50f, .50f, 1,
                        new[] { Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer, Skimmer },
                        startsSwarm: true, lanePattern: new[] { 0, 0, 1, 2, 0, 3, 4, 4 }))),
            new LevelSpec(
                "Bức Tường & Đáy Sâu", "Rùa Khiên và tầng đáy", 150, 45, true,
                "KHÁI NIỆM MỚI: RÙA KHIÊN & ĐÁY BIỂN NGỘP NƯỚC",
                "• Rùa Khiên chặn địch để tuyến sau bắn. Khi thấy tam giác Nguy Hiểm, bấm Q để kiểm tra tầng kia.\n• Giữ F tại Mảnh Vỏ để dựng khiên, hoặc tại Phế Liệu Vàng để lấy năng lượng.\n• Vịt và Rùa đặt dưới đáy mất 1 HP mỗi 4,2 giây do thiếu oxy.",
                "Chương 1 — Thử Thách Đáy Sâu: Đáy bể đã mở! Chưa có sinh vật đáy bản địa — hãy dùng Rùa Khiên & Vịt lặn xuống cầm cự tạm thời!",
                "Bấm Q để lặn hoặc nổi. Vịt & Rùa đặt xuống Đáy sẽ mất máu dần (O₂↓) — hãy giữ nhịp cho đến khi giải cứu được sinh vật đáy ở Màn 3!",
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 10, 5.2f, 5.2f, 0,
                        new[] { Skimmer, Skimmer, Bag, Skimmer, Bag }),
                    new WaveSegment(Layer.Deep, 33, 0, 0, 0, new[] { Crawler })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 38, 2.8f, 1.3f, 1,
                        new[] { Skimmer, Skimmer, Bag, Capped, Skimmer, Bag, Skimmer }),
                    new WaveSegment(Layer.Deep, 45, 18, 20, 1, new[] { Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 74, 1.9f, .8f, 3,
                        new[] { Skimmer, Bag, Skimmer, Capped, Skimmer, Bag, Skimmer })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 94, .65f, .65f, 0,
                        new[] { Skimmer, Bag, Skimmer, Capped, Skimmer, Bag, Skimmer }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 98.4f, 0, 0, 4, new[] { Crawler }),
                    new WaveSegment(Layer.Surface, 102, .6f, .6f, 2,
                        new[] { Bag, Skimmer, Bag, Skimmer, Skimmer, Bag, Skimmer }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 106.2f, 0, 0, 1, new[] { Crawler }),
                    new WaveSegment(Layer.Surface, 110, .55f, .55f, 4,
                        new[] { Skimmer, Bag, Skimmer, Capped, Skimmer, Bag, Skimmer, Bag }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 114.4f, 0, 0, 2, new[] { Crawler }))),
            new LevelSpec(
                "Cá Xạ Thủ Đáy Biển", "Cứu Cá Xạ Thủ dưới đáy", 175, 55, true,
                "CÁ XẠ THỦ & BÈ HAI MẠN",
                "• Giữ [F] tại Dây Cước dưới đáy để Cá Xạ Thủ gia nhập ngay tại ô đó.\n• Cá Xạ Thủ làm choáng và đẩy nhẹ với mỗi phát thứ ba.\n• Bè Hai Mạn khựng lại rồi lặn ở cột thứ ba tính từ mép phải.",
                "Chương 1 — Chúa Tể Đáy Sâu: Giải cứu Cá Xạ Thủ để giữ các làn đáy trước khi Bè Hai Mạn lặn xuống.",
                "Bấm Q lặn xuống cứu Cá Xạ Thủ từ Dây Cước. Cẩn thận Bè Hai Mạn sẽ lặn xuống đáy ngay ở cột thứ 3 từ bên phải!",
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 10, 5.4f, 5.4f, 0,
                        new[] { Skimmer, Skimmer, Bag, Skimmer, Bag, Skimmer }),
                    new WaveSegment(Layer.Deep, 32, 15, 15, 1, new[] { Crawler, Crawler })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 50, 2.8f, 1.2f, 1,
                        new[] { Bag, Skimmer, Ferry, Skimmer, Bag, Capped, Skimmer }),
                    new WaveSegment(Layer.Deep, 55, 4, 1.5f, 2, new[] { Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 81, 2.2f, .9f, 2,
                        new[] { Bag, Ferry, Skimmer, Capped, Bag, Skimmer }),
                    new WaveSegment(Layer.Deep, 91, 2.7f, 1.2f, 0,
                        new[] { Crawler, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 106, 1, 1, 4, new[] { Bag, Skimmer })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 119, .75f, .75f, 0,
                        new[] { Bag, Capped, Skimmer, Bag, Ferry }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 119.35f, .75f, .75f, 2,
                        new[] { Crawler, Crawler, Crawler, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 126, .75f, .75f, 2,
                        new[] { Skimmer, Bag, Skimmer, Bag, Bag, Skimmer }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 126.35f, .75f, .75f, 4,
                        new[] { Crawler, Crawler, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 133, .75f, .75f, 4,
                        new[] { Bag, Skimmer, Capped, Ferry, Bag }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 133.35f, .75f, .75f, 1,
                        new[] { Crawler, Crawler, Crawler, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 140, .75f, .75f, 1,
                        new[] { Skimmer, Bag, Skimmer, Bag, Skimmer, Bag }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 140.35f, .75f, .75f, 3,
                        new[] { Crawler, Crawler, Crawler, Crawler, Crawler }))),
            new LevelSpec(
                "Hải Lưu Đổi Tầng", "Mực Kéo Tầng và Súng Rác", 175, 65, true,
                "MỰC KÉO TẦNG & SÚNG RÁC",
                "• Đặt Mực Kéo Tầng trên đường địch để kéo xuống đáy hoặc đẩy lên mặt nước. Mực biến mất sau một lần kích hoạt.\n• Súng Rác bắn Rác Cản vào giữa bàn. Bơi đến ô bị chặn và giữ [F] để dọn.",
                "Súng Rác bắn rác vào cả hai tầng. Dùng Mực Kéo Tầng chuyển địch sang hàng cùng vị trí ở tầng có hỏa lực.",
                "Chọn Mực bằng [5]. Đặt Mực trước địch hoặc bơi tới Rác Cản và giữ [F] để mở lại ô.",
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 10, 5.3f, 5.3f, 0,
                        new[] { Skimmer, Litterer, Bag, Skimmer, Bag, Litterer, Skimmer }),
                    new WaveSegment(Layer.Deep, 26, 12, 14, 0, new[] { Crawler, Litterer, Crawler })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 50, 2.8f, 1.2f, 1,
                        new[] { Bag, Ferry, Litterer, Skimmer, Capped, Litterer, Skimmer }),
                    new WaveSegment(Layer.Deep, 55, 3.2f, 1.2f, 1,
                        new[] { Crawler, Litterer, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 77, 2.3f, .9f, 3,
                        new[] { Litterer, Bag, Skimmer, Litterer, Skimmer, Capped, Ferry }),
                    new WaveSegment(Layer.Deep, 83, 2.8f, .8f, 3,
                        new[] { Crawler, Litterer, Crawler, Litterer }),
                    new WaveSegment(Layer.Surface, 103, 2.0f, .8f, 0,
                        new[] { Ferry, Litterer, Skimmer, Bag, Skimmer }),
                    new WaveSegment(Layer.Surface, 123, 1, 1, 2, new[] { Bag, Skimmer })),
                new WaveSpec(
                    new WaveSegment(Layer.Surface, 135, .72f, .72f, 0,
                        new[] { Litterer, Bag, Skimmer, Capped, Ferry, Skimmer }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 135.35f, .72f, .72f, 2,
                        new[] { Crawler, Litterer, Crawler, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 142, .72f, .72f, 2,
                        new[] { Bag, Skimmer, Litterer, Ferry, Skimmer, Bag }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 142.35f, .72f, .72f, 4,
                        new[] { Crawler, Crawler, Litterer, Crawler, Crawler, Crawler }),
                    new WaveSegment(Layer.Surface, 149, .68f, .68f, 4,
                        new[] { Capped, Skimmer, Litterer, Bag, Ferry, Skimmer }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 149.34f, .68f, .68f, 1,
                        new[] { Crawler, Crawler, Crawler, Crawler, Litterer, Crawler }),
                    new WaveSegment(Layer.Surface, 156, .65f, .65f, 1,
                        new[] { Litterer, Bag, Skimmer, Litterer, Ferry, Skimmer, Bag }, startsSwarm: true),
                    new WaveSegment(Layer.Deep, 156.32f, .65f, .65f, 3,
                        new[] { Crawler, Litterer, Crawler, Crawler, Litterer, Crawler }))),
            new LevelSpec(
                "Eo Gương Dòng Đảo", "Cột nước và Hải Ly", 200, 3, 55, 75, false, true,
                "KHÁI NIỆM MỚI: CỘT NƯỚC ĐẨY +55% & BÈ HAI MẠN",
                "• Bè Hai Mạn có thể lặn hoặc nổi giữa đường. Chuẩn bị phòng tuyến ở cả hai tầng.\n• Cá Ống Phun tăng 55% tốc độ ra đòn cho đồng minh ngay bên trên.\n• Hải Ly Đê bắn xuyên hai địch và sống được ở cả hai tầng.",
                "Chương 2 — Làn Sóng Ngầm: Quái rác bắt đầu chuyển tầng bất ngờ. Hãy tận dụng Cá Ống Phun và Hải Ly Đê kiểm soát cả hai mặt bể!",
                "Đặt Cá Ống Phun dưới Đáy ngay bên dưới Vịt/Hải Ly ở Mặt Nước để kích hoạt Cột Nước Đẩy!"),
            new LevelSpec(
                "Bãi Đá San Hô", "Bồ Nông và San Hô Gai", 225, 3, 50, 85, false, true,
                "KHÁI NIỆM MỚI: SAN HÔ GAI & LỌC BẨN HỒI MÁU",
                "• Lọc Bẩn dưới đáy tự hồi máu. Dùng San Hô Gai để giữ chân và phản sát thương.\n• Bồ Nông đẩy lùi địch ở mặt nước.",
                "Chương 2 — Rạn San Hô: Quái đáy ngày càng nặng và lì đòn. Hãy dựng tường San Hô Gai dưới đáy để chặn đứng chúng!",
                "Đặt San Hô Gai ở Đáy để giữ chân địch xuất hiện dưới nước với +40% HP. Bồ Nông đẩy lùi trên Mặt Nước!"),
            new LevelSpec(
                "Trạm Xung Thủy Triều", "Sứa Pha Lê và Lươn Điện", 250, 3, 50, 95, false, true,
                "KHÁI NIỆM MỚI: LƯƠN ĐIỆN EMP 2 TẦNG & VÁNG DẦU",
                "• Phao Hắc để lại váng dầu khi bị hạ. Hãy dọn dầu để giữ độ trong.\n• Lươn Điện và Pin Rò gây sát thương xuyên tầng. Bấm phải vào Pin Rò để phá giáp địch gần đó.",
                "Chương 3 — Bão Dầu Đen: Dầu loang và Ống Rè gây nhiễu tấn công đồng loạt. Huy động Sứa Pha Lê và Lươn Điện phản công!",
                "Dùng Q để đổi tầng, dọn dầu, kích nổ Pin Rò và thả Lươn Điện quét sạch cả 2 tầng!"),
            new LevelSpec(
                "Vịnh Lọc Dầu", "Cá Koi Lọc và váng dầu", 275, 4, 48, 110, false, true,
                "KHÁI NIỆM MỚI: THANH LỌC ĐỘ TRONG NƯỚC 100%",
                "• Cá Koi Lọc dọn váng dầu gần nó.\n• Dọn váng dầu để Bio-Bot di chuyển dễ dàng.",
                "Chương 3 — Thanh Lọc Đại Dương: Thùng Húc và Bầy Rách tràn vào. Cá Koi Lọc, Hải Âu Lưới và Cặp Cá Hề sẵn sàng!",
                "Dùng Cá Koi Lọc để dọn váng dầu ở gần."),
            new LevelSpec(
                "Rạn Sứa Phát Sáng", "Mở toàn bộ đội hình", 325, 4, 48, 130, false, true,
                "KHÁI NIỆM MỚI: HỒNG HẠC, CÁ NHỒNG, ẾCH & CUA THỢ",
                "• Toàn bộ 20 đồng minh đã mở.\n• Bấm phải vào Van Khí dưới đáy để tiếp sức cho Hồng Hạc.\n• Ếch Bọt có đòn khác nhau theo tầng. Cua Thợ Máy sửa đồng minh ở cả hai tầng.",
                "Chương 3 — Trước Giờ G: Bè Neo, Sứa Nilon và Cánh Lướt tổng tiến công cả hai mặt bể 3D!",
                "Kết hợp Van Khí + Hồng Hạc + Cua Thợ Máy để giữ vững cả 2 tầng!"),
            new LevelSpec(
                "Đê Xanh Cảm Tử", "Tàu Xả Độc", 375, 4, 50, 240, true, true,
                "CẢNH BÁO ĐỎ: SIÊU TÀU XẢ ĐỘC!",
                "• Tàu Xả Độc đổi tầng, xả dầu và thả viện binh. Theo dõi thanh máu khi chạm trán tàu.\n• Theo sát bằng Q và phối hợp hỏa lực xuyên tầng.",
                "CHƯƠNG CUỐI: Cuộc chiến quyết định số phận Đê Xanh! Huy động toàn bộ đội quân đại dương đánh bại Tàu Xả Độc!",
                "Theo dõi tàu khi nó đổi tầng, bám sát với Q rồi dồn hỏa lực xuyên tầng!")
        };

        // Cards are ordered by unlock stage.
        public static readonly AllySpec[] Allies =
        {
            // Stage 1 (0..1): ONLY Rái Cá & Vịt Bắn Bọt!
            new AllySpec("Rái Cá", "Tạo năng lượng", "☀ +20 NL", B, L, Ability.Income, 50, 6, 1, 20, 4.8f, $"Tạo hạt +20 NL mỗi {CampaignMatch.IncomeInterval.ToString("0.#", CultureInfo.GetCultureInfo("vi-VN"))} giây. Sống được ở cả hai tầng."),
            new AllySpec("Vịt Bắn Bọt", "Bắn tia nước", "● Bắn Mặt", S, L, Ability.Shot, 50, 7, 1, 2, 4.2f, "Bắn dọc hàng Mặt Nước. Có thể đặt khẩn cấp xuống Đáy nhưng sẽ ngộp nước & mất máu dần!"),
            // Stage 2 (2): Rùa Khiên!
            new AllySpec("Rùa Khiên", "Tường chắn", "🛡 Chắn", S, L, Ability.Guard, 50, 24, 2, 0, 15f, "Chặn địch bằng mai cứng, không gây sát thương. Đặt ở đáy sẽ mất máu do thiếu oxy."),
            // Stage 3: native deep defender.
            new AllySpec("Cá Xạ Thủ", "Bắn choáng Đáy", "💥 Bắn Choáng", D, L, Ability.Shot, 100, 3, 3, 3, 10f, "Sống ở đáy. Mỗi phát thứ ba đẩy nhẹ và làm choáng."),
            new AllySpec("Mực Kéo Tầng", "Bẫy đổi tầng", "⇅ Đổi tầng", B, L, Ability.Swap, 5, 1, 4, 2, 0.5f,
                "Bẫy dùng một lần, gây 2 sát thương và choáng 0,6 giây khi chạm địch. Đổi tầng địch thường khi đáy biển mở."),
            // Reserved allies.
            new AllySpec("Tôm Súng", "Phá giáp", "⚡ Phá Giáp", D, L, Ability.Zap, 100, 9, 5, 3, 6.0f, "Búng càng siêu âm dưới Đáy, đập vỡ giáp sắt Lon Kén!"),
            new AllySpec("Cò Hồi Máu", "Hồi máu Đáy", "✚ Hồi ↓", S, O, Ability.Repair, 75, 8, 5, 4, 6.5f, "Đặt trên Mặt: lặn mỏ xuống hồi máu cho đồng minh ô Đáy bên dưới!"),
            new AllySpec("Hải Ly Đê", "Xuyên 2 quái", "» Xuyên 2", B, L, Ability.Pierce, 125, 9, 5, 3, 6.8f, "Phóng giáo nước xuyên 2 quái rác gần nhất. Sống được cả 2 tầng!"),
            new AllySpec("Cá Ống Phun", "Bắn lên Mặt", "▲ Bắn ↑", D, O, Ability.Shot, 100, 9, 5, 3, 6.2f, "Đặt dưới Đáy: phun lên Mặt Nước & tạo Cột Nước Đẩy +55% tốc độ cho ô trên!", boostsSurface: true),
            new AllySpec("Bồ Nông", "Đẩy lùi sóng", "« Đẩy Lùi", S, L, Ability.Pull, 100, 8, 6, 2, 7.0f, "Vỗ cánh tạo sóng đẩy lùi quái rác trên Mặt Nước."),
            new AllySpec("San Hô Gai", "Tường Đáy", "🛡 Gai Đáy", D, L, Ability.Guard, 75, 32, 6, 2, 10f, "Pháo đài san hô siêu bền dưới Đáy Biển, phản gai sát thương cao."),
            new AllySpec("Sứa Pha Lê", "Gương 2 tầng", "⇄ 2 Tầng", B, X, Ability.Reflect, 100, 8, 7, 3, 6.5f, "Bắn cả 2 tầng khi có Mảnh Gương kích hoạt ở cùng cột!"),
            new AllySpec("Lươn Điện", "Xung EMP", "⚡ EMP 2T", D, X, Ability.Pulse, 150, 10, 7, 4, 8.5f, "Phát xung điện phá giáp cả 2 tầng & tạo cột nước đẩy cho ô trên!", boostsSurface: true),
            new AllySpec("Hải Âu Lưới", "Hãm Đáy", "❄ Chậm ↓", S, O, Ability.Slow, 75, 9, 8, 3, 6.2f, "Đặt trên Mặt: thả lưới tảo làm chậm toàn bộ quái dưới Đáy!"),
            new AllySpec("Cặp Cá Hề", "Liên kết", "⇄ Ghép Đôi", B, X, Ability.Link, 125, 9, 8, 4, 7.2f, "Bắn cả 2 tầng khi có đồng minh ở ô đối diện!"),
            new AllySpec("Cá Koi Lọc", "Dọn dầu", "✦ Lọc Dầu", D, L, Ability.Clean, 100, 12, 8, 3, 6.2f, "Hút sạch Váng Dầu lân cận, tăng Độ Trong Nước & bắn bọt."),
            new AllySpec("Hồng Hạc", "Xung Van Khí", "★ Xung 2T", S, X, Ability.Echo, 175, 9, 9, 5, 9.0f, "Phóng xung xuyên 2 tầng cực mạnh, tăng sát thương khi có Van Khí!"),
            new AllySpec("Cá Nhồng", "Ngư lôi ↑", "» Xuyên ↑", D, O, Ability.Pierce, 150, 10, 9, 4, 8.0f, "Phóng ngư lôi từ Đáy xuyên 2 quái Mặt Nước & tạo cột đẩy!", boostsSurface: true),
            new AllySpec("Ếch Bọt", "2 Dạng", "★ Biến Hình", B, L, Ability.Habitat, 125, 11, 9, 4, 7.0f, "Mặt Nước: đại bác hạng nặng. Đáy Biển: làm chậm cả hàng!"),
            new AllySpec("Cua Thợ Máy", "Sửa 2 tầng", "✚ Sửa 2T", D, X, Ability.Repair, 125, 12, 9, 5, 7.5f, "Vá giáp & hồi máu đồng minh ở cả 2 tầng tại cùng cột.")
        };

        // Turned speed back up for exciting, balanced challenge on the 11-column 3D pool!
        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt", S, Trait.Skim, 8, .36f, 1, 10, "Quái mặt nước cơ bản, tăng tốc khi qua Váng Dầu."),
            new EnemySpec("Túi Chộp", S, Trait.School, 5, .60f, 2, 10, "Bơi nhanh hơn khi đi theo bầy cùng hàng."),
            new EnemySpec("Mạt Cào", D, Trait.Burrow, 11, .25f, 2, 12, "Bò sát đáy biển. Được cộng 40% HP khi xuất hiện dưới nước và giảm sát thương từ đòn thường."),
            new EnemySpec("Lon Kén", B, Trait.Shell, 16, .30f, 5, 15, "Giáp sắt giảm sát thương cho tới khi bị Tôm Súng / Xung phá vỡ."),
            new EnemySpec("Bè Hai Mạn", B, Trait.Ferry, 14, .34f, 3, 18, "Khựng lại ở cột thứ ba từ phải rồi đổi tầng, giữ nguyên HP."),
            new EnemySpec("Lọc Bẩn", D, Trait.Filter, 16, .31f, 6, 16, "Quái đáy siêu lì, tự hồi máu theo nhịp."),
            new EnemySpec("Phao Hắc", S, Trait.Spill, 16, .33f, 7, 18, "Vỡ tung để lại Váng Dầu cản đường khi bị hạ."),
            new EnemySpec("Ống Rè", D, Trait.Jam, 17, .30f, 7, 18, "Gây nhiễu làm chậm đồng minh ở tầng đối diện."),
            new EnemySpec("Thùng Húc", B, Trait.Ram, 24, .34f, 8, 22, "Đòn húc mạnh phá tường chắn."),
            new EnemySpec("Bầy Rách", B, Trait.Split, 18, .35f, 8, 20, "Tách thành 2 mảnh vi nhựa chạy nhanh khi vỡ!"),
            new EnemySpec("Cánh Lướt", B, Trait.Drift, 15, .48f, 9, 20, "Chuyển sang hàng bên cạnh khi gặp phòng thủ."),
            new EnemySpec("Bè Neo", B, Trait.Anchor, 36, .23f, 9, 30, "Kháng đẩy lùi và tăng tốc đồng đội cùng tầng."),
            new EnemySpec("Sứa Nilon", D, Trait.Phase, 20, .37f, 9, 24, "Lấp lánh độc tố dưới Đáy, kháng đòn thường."),
            new EnemySpec("Tàu Xả Độc", B, Trait.Boss, 200, .14f, 10, 150, "SIÊU BOSS: Đổi tầng liên tục, xả dầu và thả viện binh!"),
            new EnemySpec("Súng Rác", B, Trait.Litter, 6, 0.20f, 4, 14,
                "Cứ đi 2 ô, bắn Rác Cản vào cột 4–7 tính từ trái trên cùng tầng, ưu tiên hàng đang bơi."),
            new EnemySpec("Can Đội Nắp", S, Trait.None, 24, .46f, 1, 16,
                "Đội can nhựa dày. Có từ đợt cuối màn 1. Dồn hai Vịt, húc bằng Chai Phao hoặc chặn bằng Rùa.")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao", S, TrashEffect.Float, 0, 1, "Đẩy Trái: +50 NL | Đẩy Phải: BOWLING", "Đẩy sang trái vào bến nhận +50 NL! Đẩy sang phải húc bay quái!"),
            new TrashSpec("Mảnh Vỏ", B, TrashEffect.Shell, 0, 2, "Giữ [F] để dựng khiên", "Dựng một khiên vỏ tại chỗ trong 28 giây."),
            new TrashSpec("Dây Cước", D, TrashEffect.Filament, 0, 3, "Giữ [F] để thả cá", "Lặn đến Dây Cước và giữ F. Cá Xạ Thủ gia nhập tại ô đó."),
            new TrashSpec("Cục Chì", D, TrashEffect.Ballast, 0, 3, "Giữ [F] để dựng khiên mặt nước", "Dựng khiên trong 30 giây nếu ô mặt nước tương ứng còn trống."),
            new TrashSpec("Mảnh Gương", B, TrashEffect.Mirror, 20, 5, "Chuột phải: Dựng Gương 2 Tầng", "Chuột phải dựng gương kết nối đòn bắn Sứa Pha Lê xuyên 2 tầng!"),
            new TrashSpec("Kiện Nổi", B, TrashEffect.Capsule, 20, 5, "Chuột phải · Đổi tầng", "Chuột phải chuyển kiện sang tầng kia để mở chỗ trống."),
            new TrashSpec("Pin Rò", B, TrashEffect.Battery, 20, 7, "Chuột phải: Nổ EMP 2 Tầng!", "Chuột phải kích nổ xung EMP phá giáp & sát thương cả 2 tầng!"),
            new TrashSpec("Váng Dầu", S, TrashEffect.Spill, 15, 7, "Chuột phải · Đốt cháy", "Làm đục nước và tăng tốc Váng Quệt. Giữ F tại chỗ để dọn."),
            new TrashSpec("Van Khí", D, TrashEffect.Vent, 25, 9, "Chuột phải: Mở Van Cột Nước", "Chuột phải mở van phun xung 2 tầng & tạo cột đẩy +55% tốc độ cho ô trên!"),
            new TrashSpec("Lưới Cuộn", B, TrashEffect.Loom, 25, 9, "Chuột phải: Giăng lưới tầng kia", "Chuột phải giăng lưới làm chậm cả hàng ở tầng đối diện trong 20s!"),
            new TrashSpec("Phế Liệu Vàng", B, TrashEffect.Salvage, 20, 2, "Giữ [F] để lấy năng lượng", "Thu gom để nhận +20 NL."),
            new TrashSpec("Rác Cản", B, TrashEffect.Clutter, 0, 4, "Giữ [F] để dọn rác",
                "Súng Rác bắn vào ô trống. Chặn đặt đồng minh. Giữ F 0,5 giây để dọn, không có thưởng.")
        };

        public static readonly UpgradeSpec[] Upgrades =
        {
            new UpgradeSpec("Vỏ Hợp Kim", "+1 máu tối đa Bio-Bot mỗi cấp.", 40, 3),
            new UpgradeSpec("Chân Vịt Turbo", "+15% tốc độ bơi Bio-Bot mỗi cấp.", 45, 3),
            new UpgradeSpec("Tụ Xung Thủy Lực", "+2 sát thương & giảm hồi chiêu Bio-Pulse (R).", 50, 3),
            new UpgradeSpec("Nam Châm Sinh Học", "Tăng tầm tự hút hạt năng lượng cho Bio-Bot.", 40, 3),
            new UpgradeSpec("Bến Tái Chế", "+15 NL khởi đầu & +15 NL khi đẩy Chai vào bến.", 55, 3),
            new UpgradeSpec("Triệu Hồi Siêu Tốc", "Giảm 15% thời gian hồi thẻ đồng minh mỗi cấp.", 60, 3)
        };

        public static readonly AchievementSpec[] Achievements =
        {
            new AchievementSpec("first_win", "Người Gác Đê", "Hoàn thành màn đầu tiên Bến Gom Nắng Mai.", 25),
            new AchievementSpec("first_rescue", "Cứu Tinh Đại Dương", "Lặn xuống cắt dây cước cứu Cá Xạ Thủ dưới Đáy.", 25),
            new AchievementSpec("bottle_bowler", "Vua Bowling Rác", "Đẩy Chai Phao húc trúng kẻ địch đang tiến tới.", 30),
            new AchievementSpec("dock_master", "Tái Chế Chuyên Nghiệp", "Đưa 3 Chai Phao về bến gom trong một màn.", 30),
            new AchievementSpec("cross_layer", "Bậc Thầy Hai Tầng", "Kích hoạt tương tác xuyên tầng (Khiên, Gương hoặc Van).", 30),
            new AchievementSpec("armor_breaker", "Kẻ Phá Giáp", "Phá vỡ lớp vỏ giáp của Lon Kén bằng Tôm Súng / Xung.", 30),
            new AchievementSpec("flawless", "Đê Xanh Bất Khả Xâm Phạm", "Thắng một màn mà không mất điểm Cửa Đê nào.", 40),
            new AchievementSpec("eco_shopper", "Kỹ Sư Nâng Cấp", "Mua nâng cấp đầu tiên tại Xưởng Bio-Bot.", 25),
            new AchievementSpec("boss_slayer", "Hạ Gục Tàu Xả Độc", "Đánh bại Siêu Boss ở màn 10 Đê Xanh Cảm Tử.", 100),
            new AchievementSpec("endless_5", "Bền Bỉ Vô Tận", "Vượt qua Đợt 5 trong chế độ Sinh Tồn Vô Tận.", 60)
        };

        public static readonly int[] AllySpriteRow =
        {
            2, 0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
        };

        public static int EnemySpriteRow(int id) => id switch { Litterer => 45, Capped => 47, _ => 20 + id };
        public static int TrashSpriteRow(int id) => id == ClutterId ? 46 : 34 + id;

        public static bool Fits(Habitat habitat, Layer layer) =>
            (habitat & (layer == Layer.Surface ? S : D)) != 0;

        public static Layer Opposite(Layer layer) => layer == Layer.Surface ? Layer.Deep : Layer.Surface;

        public static int UnlockedAllyCount(int level)
        {
            int count = 0;
            for (int i = 0; i < Allies.Length; i++)
                if (Allies[i].Unlock <= level) count++;
            return count;
        }
    }
}
