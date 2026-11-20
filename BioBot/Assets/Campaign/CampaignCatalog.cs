using System;
using UnityEngine;

namespace BioBot
{
    [Flags]
    public enum Habitat { Surface = 1, Deep = 2, Both = 3 }
    public enum Layer { Surface, Deep }
    public enum TrashEffect { Float, Filament, Ballast, Spill, Battery, Mirror, Shell, Vent, Loom, Capsule }
    public enum Trait { Skim, Burrow, Ferry, Filter, Spill, Shell, Jam, School, Ram, Split, Drift, Anchor, Phase, Boss }
    public enum Ability { Shot, Guard, Repair, Slow, Zap, Income, Pull, Pulse, Reflect, Link, Pierce, Echo, Habitat, Clean }
    public enum Reach { Local, Other, Both }

    public sealed class AllySpec
    {
        public readonly string Name, Role, Hint;
        public readonly Habitat Habitat;
        public readonly Reach Reach;
        public readonly Ability Ability;
        public readonly int Cost, Health, Unlock, Power;
        public readonly float Recharge;

        public AllySpec(string name, string role, Habitat habitat, Reach reach, Ability ability,
            int cost, int health, int unlock, int power, float recharge, string hint)
        {
            Name = name;
            Role = role;
            Habitat = habitat;
            Reach = reach;
            Ability = ability;
            Cost = cost;
            Health = health;
            Unlock = unlock;
            Power = power;
            Recharge = recharge;
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
        public readonly string Name, Hint;
        public readonly Habitat Habitat;
        public readonly TrashEffect Effect;
        public readonly int Value, Unlock;

        public TrashSpec(string name, Habitat habitat, TrashEffect effect, int value, int unlock, string hint)
        {
            Name = name;
            Habitat = habitat;
            Effect = effect;
            Value = value;
            Unlock = unlock;
            Hint = hint;
        }
    }

    public sealed class LevelSpec
    {
        public readonly string Name, Subtitle, Story, Lesson;
        public readonly int Budget, Waves, SurfaceWeight, CoinReward;
        public readonly bool HasBoss;

        public LevelSpec(string name, string subtitle, int budget, int waves, int surfaceWeight,
            int coinReward, bool hasBoss, string story, string lesson)
        {
            Name = name;
            Subtitle = subtitle;
            Budget = budget;
            Waves = waves;
            SurfaceWeight = surfaceWeight;
            CoinReward = coinReward;
            HasBoss = hasBoss;
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
        public const int TotalAtlasRows = 44;
        private const Habitat S = Habitat.Surface;
        private const Habitat D = Habitat.Deep;
        private const Habitat B = Habitat.Both;
        private const Reach L = Reach.Local;
        private const Reach O = Reach.Other;
        private const Reach X = Reach.Both;

        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec(
                "Bến Gom Nắng Mai", "Mở đầu · Mặt nước", 125, 2, 100, 35, false,
                "Nhật ký Đê Xanh #01: Đợt rác nhựa đầu mùa đang trôi vào cửa sông. Hãy dẫn chai về bến gom và dựng hàng cây phòng thủ!",
                "Click chọn thẻ rồi click ô để trồng (hoặc Space). Đẩy Chai Phao về mép trái nhận x2 năng lượng, hoặc đẩy phải để húc quái!"),
            new LevelSpec(
                "Vườn Rễ Biển Sâu", "Khám phá hai tầng", 150, 2, 65, 45, false,
                "Nhật ký Đê Xanh #02: Dưới đáy sông có cá bị mắc dây cước! Bấm Q để lặn xuống cứu cá và trồng Rễ Neo hỗ trợ lên mặt nước.",
                "Q lặn / E nổi / Tab đổi tầng. Nhấn F trên Dây Cước để giải cứu Cá Vệt. Rễ Neo ở tầng dưới làm chậm địch trên mặt nước!"),
            new LevelSpec(
                "Eo Gương Dòng Đảo", "Xuyên tầng & Phản xạ", 175, 3, 55, 55, false,
                "Nhật ký Đê Xanh #03: Bè Hai Mạn có thể lặn bất ngờ giữa đường! Hãy dựng Mảnh Gương và Cá Ống để kiểm soát cả hai tầng.",
                "Bấm G (hoặc chuột phải) trên Mảnh Gương để kết nối đòn bắn xuyên tầng. Dùng Kiện Nổi đổi tầng để mở chỗ trồng!"),
            new LevelSpec(
                "Trạm Xung Thủy Triều", "Xung điện & Phá giáp", 200, 3, 50, 65, false,
                "Nhật ký Đê Xanh #04: Lon Kén bọc giáp và Phao Hắc tràn dầu đã xuất hiện. Pin Rò và Cá Mạch sẽ bẻ gãy lớp giáp của chúng!",
                "G trên Pin Rò kích nổ EMP phá giáp cả hai tầng. Cá Lọc tự dọn váng dầu. Nhấn R để Bio-Bot phát xung cứu nguy!"),
            new LevelSpec(
                "Cửa Đê Hắc Ám", "Liên kết đồng đội", 225, 3, 50, 75, false,
                "Nhật ký Đê Xanh #05: Bầy Rách và Cánh Lướt đổi làn liên tục! Hãy kết hợp Cặp Tảo, Lá Chài và Van Khí xuyên tầng.",
                "Mở Van Khí (G) để kích xung định kỳ cả hai tầng và cường hóa Lộc Van ở ô đối diện!"),
            new LevelSpec(
                "Rạn San Hô Sứa Độc", "Sứa Nilon & Thùng Húc", 250, 4, 48, 90, false,
                "Nhật ký Đê Xanh #06: Sứa Nilon phát sáng đang len lỏi qua rạn san hô cùng Thùng Húc siêu nặng. Giữ chắc tường Rong Xi!",
                "Sứa Nilon có thể né đòn thường và làm choáng. Dùng đòn Xung (Cá Gõ, Cá Mạch, Lộc Van) để khắc chế!"),
            new LevelSpec(
                "Hải Lưu Đại Thủy", "Tổng lực hai tầng", 275, 4, 50, 110, false,
                "Nhật ký Đê Xanh #07: Thủy triều rác dâng cao từ cả hai tầng cùng lúc! Hãy giữ nhịp Bụi Bạc và tận dụng mọi công cụ rác.",
                "Bật AI Tự Lái (phím P) nếu muốn Bio-Bot tự hỗ trợ thu hạt và tuần tra, hoặc tự tay điều khiển để tối ưu combo!"),
            new LevelSpec(
                "Đê Xanh Cảm Tử", "ĐẠI CHIẾN BOSS", 325, 4, 50, 200, true,
                "Nhật ký Đê Xanh #08: CẢNH BÁO ĐỎ! Siêu Tàu Xả Độc đang tiến thẳng vào Cửa Đê! Huy động toàn bộ hệ sinh thái đánh bại nó!",
                "Tàu Xả Độc đổi tầng, xả dầu và gọi viện binh! Dựng phòng tuyến cả hai tầng và dồn sát thương xuyên tầng để hạ gục Boss!")
        };

        public static readonly AllySpec[] Allies =
        {
            // Level 1 (0..3)
            new AllySpec("Mầm Vỗ", "Bắn hạt", S, L, Ability.Shot, 50, 6, 1, 2, 4.5f, "Bắn hạt nước ngọt dọc hàng mặt nước."),
            new AllySpec("Đệm Bấc", "Khiên chắn", S, L, Ability.Guard, 50, 20, 1, 1, 10f, "Bức tường cỏ bấc siêu bền, phản sát thương khi bị cắn."),
            new AllySpec("Bụi Bạc", "Tạo năng lượng", B, L, Ability.Income, 50, 5, 1, 25, 5f, "Thả hạt Bio-Orb (+25 NL) theo nhịp, đặt được ở cả hai tầng."),
            new AllySpec("Cá Vệt", "Bắn bọt", D, L, Ability.Shot, 50, 7, 1, 2, 4.5f, "Cá dưới nước bắn bọt dọc hàng. Cắt cước sẽ cứu miễn phí!"),
            // Level 2 (4..6)
            new AllySpec("Rễ Neo", "Hãm xuyên tầng", D, O, Ability.Slow, 75, 8, 2, 3, 6f, "Rễ dưới nước vươn lên làm chậm địch ở hàng tương ứng trên mặt!"),
            new AllySpec("Cá Gõ", "Phá giáp", D, L, Ability.Zap, 100, 8, 2, 3, 6.5f, "Bắn xung siêu âm nhanh, phá vỡ vỏ giáp Lon Kén."),
            new AllySpec("Sen Bơm", "Hồi máu tầng dưới", S, O, Ability.Repair, 75, 7, 2, 4, 7f, "Truyền dưỡng chất chữa lành đồng minh ở ô tương ứng dưới nước."),
            // Level 3 (7..9)
            new AllySpec("Sậy Nặng", "Xuyên 2 mục tiêu", B, L, Ability.Pierce, 125, 8, 3, 3, 7f, "Bắn giáo nước xuyên qua 2 kẻ địch gần nhất trong hàng."),
            new AllySpec("Cá Ống", "Bắn lên mặt nước", D, O, Ability.Shot, 100, 8, 3, 3, 6.5f, "Phóng tia nước từ tầng đáy lên tiêu diệt địch mặt nước."),
            new AllySpec("Bèo Kéo", "Đẩy lùi", S, L, Ability.Pull, 100, 8, 3, 2, 7.5f, "Đẩy lùi kẻ địch bằng sóng bèo, phá nhịp tiến công."),
            // Level 4 (10..12)
            new AllySpec("Rong Xi", "Tường gai đáy", D, L, Ability.Guard, 75, 26, 4, 2, 11f, "Pháo đài san hô siêu cứng dưới đáy, phản gai cực mạnh."),
            new AllySpec("Cỏ Gương", "Khúc xạ 2 tầng", B, X, Ability.Reflect, 100, 7, 4, 3, 7f, "Bắn cả hai tầng nếu có Mảnh Gương kích hoạt tại ô tương ứng."),
            new AllySpec("Cá Mạch", "Xung điện 2 tầng", D, X, Ability.Pulse, 150, 9, 4, 4, 9f, "Phát xung EMP gây sát thương và phá giáp cả hai tầng cùng hàng!"),
            // Level 5 (13..15)
            new AllySpec("Lá Chài", "Lưới hãm tầng dưới", S, O, Ability.Slow, 75, 9, 5, 3, 6.5f, "Thả rễ chài từ mặt nước làm chậm toàn bộ địch tầng dưới."),
            new AllySpec("Cặp Tảo", "Liên kết đồng minh", B, X, Ability.Link, 125, 9, 5, 4, 7.5f, "Bắn cường hóa cả hai tầng khi có đồng minh ở ô đối diện."),
            new AllySpec("Cá Lọc", "Dọn dầu & Bắn bọt", D, L, Ability.Clean, 100, 12, 5, 3, 6.5f, "Tự động hút sạch Váng Dầu lân cận và bắn bọt liên tục."),
            // Level 6 (16..17)
            new AllySpec("Lộc Van", "Hỏa lực Van Khí", S, X, Ability.Echo, 175, 9, 6, 5, 9.5f, "Bắn xung xuyên tầng cực mạnh, tăng sát thương khi ghép Van Khí."),
            new AllySpec("Cá Dẫn", "Ngư lôi lên mặt", D, O, Ability.Pierce, 150, 10, 6, 4, 8.5f, "Phóng ngư lôi xuyên 2 kẻ địch mặt nước từ dưới đáy."),
            // Level 7 (18..19)
            new AllySpec("Cỏ Bọt", "Biến hóa theo tầng", B, L, Ability.Habitat, 125, 11, 7, 4, 7.5f, "Đặt trên mặt: đại bác hạng nặng. Đặt dưới đáy: làm chậm cả hàng!"),
            new AllySpec("Cá Thợ", "Sửa chữa 2 tầng", D, X, Ability.Repair, 125, 12, 7, 5, 8f, "Cua thợ máy sửa chữa đồng minh ở cả hai tầng tại cùng ô.")
        };

        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt", S, Trait.Skim, 8, .36f, 1, 10, "Lướt cực nhanh khi đi qua Váng Dầu."),
            new EnemySpec("Túi Chộp", S, Trait.School, 7, .44f, 1, 10, "Tăng tốc mạnh khi đi theo bầy cùng hàng."),
            new EnemySpec("Mạt Cào", D, Trait.Burrow, 10, .34f, 2, 12, "Bò sát đáy, giảm sát thương từ đòn thường."),
            new EnemySpec("Lon Kén", B, Trait.Shell, 15, .29f, 2, 15, "Giáp sắt giảm sát thương cho tới khi bị Xung phá vỡ."),
            new EnemySpec("Bè Hai Mạn", B, Trait.Ferry, 14, .34f, 3, 18, "Lặn hoặc nổi bất ngờ sang tầng kia ở giữa sân!"),
            new EnemySpec("Lọc Bẩn", D, Trait.Filter, 14, .31f, 3, 16, "Tự hồi máu theo nhịp và kháng bọt yếu."),
            new EnemySpec("Phao Hắc", S, Trait.Spill, 15, .32f, 4, 18, "Vỡ tung để lại Váng Dầu cản đường khi bị hạ."),
            new EnemySpec("Ống Rè", D, Trait.Jam, 16, .30f, 4, 18, "Gây nhiễu làm chậm tốc độ bắn đồng minh ở tầng đối diện."),
            new EnemySpec("Thùng Húc", B, Trait.Ram, 22, .34f, 5, 22, "Đòn húc phá tường cực mạnh vào vật chắn."),
            new EnemySpec("Bầy Rách", B, Trait.Split, 18, .34f, 5, 20, "Tách thành 2 mảnh vi nhựa chạy cực nhanh khi vỡ!"),
            new EnemySpec("Cánh Lướt", B, Trait.Drift, 14, .50f, 6, 20, "Chuyển sang hàng bên cạnh khi gặp phòng thủ."),
            new EnemySpec("Bè Neo", B, Trait.Anchor, 34, .22f, 6, 30, "Kháng đẩy lùi và tăng tốc toàn bộ đồng đội cùng tầng."),
            new EnemySpec("Sứa Nilon", D, Trait.Phase, 19, .37f, 6, 24, "Lấp lánh độc tố, kháng đòn thường và làm chậm đồng minh gần."),
            new EnemySpec("Tàu Xả Độc", B, Trait.Boss, 160, .14f, 8, 120, "SIÊU BOSS: Đổi tầng liên tục, xả dầu và thả quái viện binh!")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao", S, TrashEffect.Float, 20, 1, "Đẩy trái vào bến nhận +50 NL! Đẩy phải để bowling húc lùi địch!"),
            new TrashSpec("Mảnh Vỏ", B, TrashEffect.Shell, 25, 1, "F thu +25 NL, hoặc G dựng ngay Tường Chắn tạm thời."),
            new TrashSpec("Dây Cước", D, TrashEffect.Filament, 25, 2, "F cắt dây GIẢI CỨU Cá Vệt miễn phí ngay tại ô! Để lâu sẽ cản Bio-Bot."),
            new TrashSpec("Cục Chì", D, TrashEffect.Ballast, 30, 2, "F thu +30 NL, hoặc G phóng phao chắn lên ô Mặt Nước tương ứng."),
            new TrashSpec("Mảnh Gương", B, TrashEffect.Mirror, 25, 3, "G dựng gương kết nối đòn bắn Cỏ Gương xuyên hai tầng!"),
            new TrashSpec("Kiện Nổi", B, TrashEffect.Capsule, 30, 3, "G đổi tầng để mở chỗ trồng, hoặc F thu +30 NL."),
            new TrashSpec("Pin Rò", B, TrashEffect.Battery, 30, 4, "G kích nổ xung EMP phá giáp & sát thương cả hai tầng!"),
            new TrashSpec("Váng Dầu", S, TrashEffect.Spill, 20, 4, "Làm chậm Bio-Bot & tăng tốc Váng Quệt. G gạt bùng cháy hoặc F dọn!"),
            new TrashSpec("Van Khí", D, TrashEffect.Vent, 35, 5, "G mở van phun xung xuyên tầng định kỳ và buff Lộc Van!"),
            new TrashSpec("Lưới Cuộn", B, TrashEffect.Loom, 35, 5, "G giăng lưới làm chậm toàn bộ hàng ở tầng đối diện trong 18s!")
        };

        public static readonly UpgradeSpec[] Upgrades =
        {
            new UpgradeSpec("Vỏ Hợp Kim", "+1 máu tối đa Bio-Bot mỗi cấp.", 40, 3),
            new UpgradeSpec("Chân Vịt Turbo", "+15% tốc độ bơi Bio-Bot mỗi cấp.", 45, 3),
            new UpgradeSpec("Tụ Xung Thủy Lực", "+2 sát thương & giảm hồi chiêu Bio-Pulse (R).", 50, 3),
            new UpgradeSpec("Nam Châm Sinh Học", "Tăng tầm tự hút hạt Bio-Orb cho Bio-Bot.", 40, 3),
            new UpgradeSpec("Bến Tái Chế", "+15 NL khởi đầu & +15 NL khi đẩy Chai vào bến.", 55, 3),
            new UpgradeSpec("Hạt Giống Siêu Tốc", "Giảm 15% thời gian hồi thẻ đồng minh mỗi cấp.", 60, 3)
        };

        public static readonly AchievementSpec[] Achievements =
        {
            new AchievementSpec("first_win", "Người Gác Đê", "Hoàn thành màn đầu tiên Bến Gom Nắng Mai.", 25),
            new AchievementSpec("first_rescue", "Cứu Tinh Đại Dương", "Cắt dây cước giải cứu Cá Vệt dưới đáy nước.", 25),
            new AchievementSpec("bottle_bowler", "Vua Bowling Rác", "Đẩy Chai Phao húc trúng kẻ địch đang tiến tới.", 30),
            new AchievementSpec("dock_master", "Tái Chế Chuyên Nghiệp", "Đưa 3 Chai Phao về bến gom trong một màn.", 30),
            new AchievementSpec("cross_layer", "Bậc Thầy Hai Tầng", "Kích hoạt tương tác xuyên tầng (Gương/Van/Rễ/Cá Ống).", 30),
            new AchievementSpec("armor_breaker", "Kẻ Phá Giáp", "Phá vỡ lớp vỏ giáp của Lon Kén bằng xung điện.", 30),
            new AchievementSpec("flawless", "Đê Xanh Bất Khả Xâm Phạm", "Thắng một màn mà không mất điểm Cửa Đê nào.", 40),
            new AchievementSpec("eco_shopper", "Kỹ Sư Nâng Cấp", "Mua nâng cấp đầu tiên tại Xưởng Bio-Bot.", 25),
            new AchievementSpec("boss_slayer", "Hạ Gục Tàu Xả Độc", "Đánh bại Siêu Boss ở màn 8 Đê Xanh Cảm Tử.", 100),
            new AchievementSpec("endless_5", "Bền Bỉ Vô Tận", "Vượt qua Đợt 5 trong chế độ Sinh Tồn Vô Tận.", 60)
        };

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
