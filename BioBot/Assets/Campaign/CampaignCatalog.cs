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

    public sealed class LevelSpec
    {
        public readonly string Name, Subtitle, Story, Lesson, UnlockSummary;
        public readonly int Budget, Waves, SurfaceWeight, CoinReward;
        public readonly bool HasBoss;

        public LevelSpec(string name, string subtitle, int budget, int waves, int surfaceWeight,
            int coinReward, bool hasBoss, string unlockSummary, string story, string lesson)
        {
            Name = name;
            Subtitle = subtitle;
            Budget = budget;
            Waves = waves;
            SurfaceWeight = surfaceWeight;
            CoinReward = coinReward;
            HasBoss = hasBoss;
            UnlockSummary = unlockSummary;
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

        // 10 Handcrafted Stages with Clear 3-Act Eco-Story & Progressive Unlocks!
        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec(
                "Bến Gom Nắng Mai", "Màn 1 · Hướng dẫn cơ bản", 125, 2, 100, 35, false,
                "Vịt Bắn Bọt, Rùa Khiên, Rái Cá, Cá Xạ Thủ",
                "Chương 1 — Cửa Sông Bình Minh: Đợt rác nhựa đầu mùa tràn vào cửa đê! Hãy triệu hồi Rái Cá tạo năng lượng và Vịt Bắn Bọt bảo vệ mặt nước.",
                "Click chọn thẻ ở thanh trên rồi click ô trống để thả đồng minh. Đẩy Chai Phao sang trái nhận +50 NL!"),
            new LevelSpec(
                "Vịnh Cứu Hộ Đáy Sâu", "Màn 2 · Khám phá 2 tầng", 150, 2, 65, 45, false,
                "Bạch Tuộc Neo (làm chậm Mặt Nước từ Đáy)",
                "Chương 1 — Lặn Xuống Rạn Cỏ: Nhiều Cá Xạ Thủ bị mắc dây cước dưới đáy! Lặn xuống (Q) cắt dây cứu cá và thả Bạch Tuộc Neo hỗ trợ ngược lên Mặt Nước.",
                "Bấm Q (hoặc nút Lặn) để xuống Đáy Biển. Click / F vào Dây Cước để cứu Cá Xạ Thủ miễn phí!"),
            new LevelSpec(
                "Hải Lưu Phá Giáp", "Màn 3 · Tôm Súng & Cò Hồi Máu", 175, 2, 60, 50, false,
                "Tôm Súng (phá giáp), Cò Chữa Lành (hồi máu Đáy)",
                "Chương 1 — Giáp Sắt Lon Kén: Rác kim loại Lon Kén có vỏ cứng chống đòn thường. Tôm Súng dưới đáy và Bio-Pulse (R) sẽ đập vỡ lớp giáp của chúng!",
                "Dùng Tôm Súng hoặc nhấn R (Bio-Pulse) để đập vỡ giáp Lon Kén. Cò Chữa Lành trên mặt sẽ hồi máu cho đồng đội dưới đáy!"),
            new LevelSpec(
                "Eo Gương Dòng Đảo", "Màn 4 · Xuyên tầng & Mảnh Gương", 200, 3, 55, 60, false,
                "Hải Ly Đê Xanh, Cá Ống Phun",
                "Chương 2 — Làn Sóng Ngầm: Bè Hai Mạn biết lặn bất ngờ giữa đường! Hãy thả Cá Ống Phun từ đáy bắn ngược lên mặt nước và Hải Ly bắn xuyên 2 mục tiêu.",
                "Cá Ống Phun tạo cột nước đẩy giúp đồng minh phía trên bắn nhanh +55%! Dùng nút C để soi xuyên 2 tầng."),
            new LevelSpec(
                "Bãi Đá San Hô", "Màn 5 · Bồ Nông & San Hô Gai", 225, 3, 52, 70, false,
                "Bồ Nông Hất Sóng, San Hô Gai",
                "Chương 2 — Tường Đáy Biển: Bầy Lọc Bẩn hồi máu và Mạt Cào tràn qua đáy biển. Dựng pháo đài San Hô Gai dưới đáy và Bồ Nông đẩy lùi trên mặt nước!",
                "Đặt San Hô Gai chắn đầu hàng dưới đáy. Chuột phải (G) vào Mảnh Vỏ hoặc Cục Chì để dựng khiên khẩn cấp!"),
            new LevelSpec(
                "Trạm Xung Thủy Triều", "Màn 6 · Sứa Pha Lê & Lươn Điện", 250, 3, 50, 80, false,
                "Sứa Pha Lê (gương 2 tầng), Lươn Điện (EMP 2 tầng)",
                "Chương 2 — Bão Dầu Đen: Phao Hắc vỡ để lại Váng Dầu, còn Ống Rè gây nhiễu tầng đối diện. Kích nổ Pin Rò (G) và thả Lươn Điện quét sạch cả 2 tầng!",
                "Chuột phải (G) vào Mảnh Gương để Sứa Pha Lê bắn cả 2 tầng! Nhấn Shift (Hydro-Dash) húc bowling Chai Phao!"),
            new LevelSpec(
                "Vịnh Lọc Dầu", "Màn 7 · Hải Âu Lưới & Cá Koi Lọc", 275, 3, 48, 90, false,
                "Hải Âu Lưới, Cặp Cá Hề, Cá Koi Lọc",
                "Chương 3 — Thanh Lọc Đại Dương: Váng dầu loang rộng khiến Độ Trong Nước tụt giảm. Cá Koi Lọc sẽ hút sạch dầu và đẩy Độ Trong Nước lên 100% kích hoạt Dòng Chảy Xanh!",
                "Giữ Độ Trong Nước đạt 100% để kích hoạt DÒNG CHẢY XANH (x2 tốc độ bắn & mưa năng lượng)!"),
            new LevelSpec(
                "Rạn Sứa Phát Sáng", "Màn 8 · Hồng Hạc & Cá Nhồng", 300, 4, 48, 105, false,
                "Hồng Hạc Xung, Cá Nhồng Ngư Lôi",
                "Chương 3 — Sứa Nilon Độc: Đàn Sứa Nilon và Cánh Lướt đổi làn liên tục! Mở Van Khí (G) dưới đáy để cường hóa Hồng Hạc Xung trên mặt nước.",
                "Mở Van Khí (G) ở Đáy Biển để tạo xung định kỳ và tăng cực đại sát thương cho Hồng Hạc Xung ở ô trên!"),
            new LevelSpec(
                "Hải Lưu Đại Thủy", "Màn 9 · Đội Hình Tối Thượng", 325, 4, 50, 120, false,
                "Ếch Bọt Biển (2 dạng), Cua Thợ Máy (sửa 2 tầng)",
                "Chương 3 — Trước Cơn Bão Lớn: Toàn bộ 20 Thủy Thủ Sinh Thái đã hội tụ! Bè Neo và Thùng Húc tiến đánh đồng thời cả Mặt Nước và Đáy Biển.",
                "Ếch Bọt Biển: thả trên Mặt thành đại bác, thả dưới Đáy làm chậm cả hàng! Cua Thợ Máy sửa chữa cả 2 tầng."),
            new LevelSpec(
                "Đê Xanh Cảm Tử", "Màn 10 · ĐẠI CHIẾN SIÊU BOSS", 375, 4, 50, 220, true,
                "HOÀN THÀNH CHIẾN DỊCH & VINH DANH!",
                "CHƯƠNG CUỐI — ĐẠI CHIẾN TÀU XẢ ĐỘC: Siêu tàu rác khổng lồ tiến thẳng vào Cửa Đê! Nó liên tục đổi tầng, xả dầu và thả viện binh. Huy động toàn lực bảo vệ đại dương!",
                "Dựng phòng tuyến ở cả 2 tầng, kích hoạt Dòng Chảy Xanh và dùng Lươn Điện / Hồng Hạc dồn sát thương hạ gục Boss!")
        };

        // 20 100% Original Aquatic / Marine Guardians (NO land plants!)
        public static readonly AllySpec[] Allies =
        {
            // Stage 1 (0..3)
            new AllySpec("Vịt Bắn Bọt", "Bắn tia nước", S, L, Ability.Shot, 50, 6, 1, 2, 4.2f, "Vịt mặt nước phun tia nước ngọt dọc hàng."),
            new AllySpec("Rùa Khiên", "Tường chắn", S, L, Ability.Guard, 50, 22, 1, 1, 9.5f, "Rùa biển mai cứng chặn đứng rác trên Mặt Nước, phản đòn khi bị cắn."),
            new AllySpec("Rái Cá", "Tạo năng lượng", B, L, Ability.Income, 50, 5, 1, 25, 4.8f, "Thả ngọc năng lượng (+25 NL) theo nhịp. Sống được ở cả 2 tầng!"),
            new AllySpec("Cá Xạ Thủ", "Bắn bọt đáy", D, L, Ability.Shot, 50, 7, 1, 2, 4.2f, "Cá dưới Đáy Biển bắn bọt dọc hàng. Cắt Dây Cước để cứu miễn phí!"),
            // Stage 2 (4)
            new AllySpec("Bạch Tuộc Neo", "Hãm Mặt Nước", D, O, Ability.Slow, 75, 9, 2, 3, 6f, "Vươn xúc tu từ Đáy Biển lên giữ chậm quái rác ở hàng Mặt Nước!"),
            // Stage 3 (5..6)
            new AllySpec("Tôm Súng", "Phá giáp", D, L, Ability.Zap, 100, 8, 3, 3, 6.2f, "Búng càng siêu âm siêu nhanh, đập vỡ giáp sắt Lon Kén!"),
            new AllySpec("Cò Chữa Lành", "Hồi máu Đáy", S, O, Ability.Repair, 75, 7, 3, 4, 6.8f, "Lặn mỏ từ Mặt Nước xuống chữa lành đồng minh ở ô Đáy Biển."),
            // Stage 4 (7..8)
            new AllySpec("Hải Ly Đê", "Xuyên 2 mục tiêu", B, L, Ability.Pierce, 125, 9, 4, 3, 7f, "Phóng giáo nước xuyên qua 2 kẻ địch gần nhất. Đặt được ở cả 2 tầng!"),
            new AllySpec("Cá Ống Phun", "Bắn lên Mặt", D, O, Ability.Shot, 100, 8, 4, 3, 6.5f, "Phun vòi nước từ Đáy lên Mặt Nước & tạo cột đẩy +55% tốc độ cho ô trên!"),
            // Stage 5 (9..10)
            new AllySpec("Bồ Nông", "Đẩy lùi sóng", S, L, Ability.Pull, 100, 8, 5, 2, 7.2f, "Vỗ cánh tạo sóng đẩy lùi quái rác trên Mặt Nước."),
            new AllySpec("San Hô Gai", "Pháo đài Đáy", D, L, Ability.Guard, 75, 28, 5, 2, 10.5f, "Rạn san hô siêu bền dưới Đáy Biển, phản gai sát thương cao."),
            // Stage 6 (11..12)
            new AllySpec("Sứa Pha Lê", "Khúc xạ 2 tầng", B, X, Ability.Reflect, 100, 7, 6, 3, 6.8f, "Bắn đồng thời cả 2 tầng khi có Mảnh Gương kích hoạt ở cùng cột!"),
            new AllySpec("Lươn Điện", "Xung EMP 2 tầng", D, X, Ability.Pulse, 150, 9, 6, 4, 8.8f, "Phát xung điện xuyên 2 tầng phá giáp & tạo cột đẩy cho ô trên!"),
            // Stage 7 (13..15)
            new AllySpec("Hải Âu Lưới", "Hãm Đáy Biển", S, O, Ability.Slow, 75, 9, 7, 3, 6.5f, "Thả lưới tảo từ Mặt Nước làm chậm toàn bộ quái rác dưới Đáy!"),
            new AllySpec("Cặp Cá Hề", "Liên kết 2 tầng", B, X, Ability.Link, 125, 9, 7, 4, 7.5f, "Bắn cả 2 tầng khi có bất kỳ đồng minh nào ở ô đối diện!"),
            new AllySpec("Cá Koi Lọc", "Dọn dầu & Lọc", D, L, Ability.Clean, 100, 12, 7, 3, 6.5f, "Tự hút sạch Váng Dầu lân cận, tăng Độ Trong Nước & bắn bọt."),
            // Stage 8 (16..17)
            new AllySpec("Hồng Hạc Xung", "Hỏa lực Van Khí", S, X, Ability.Echo, 175, 9, 8, 5, 9.2f, "Phóng xung xuyên 2 tầng cực mạnh, tăng sát thương khi có Van Khí!"),
            new AllySpec("Cá Nhồng", "Ngư lôi lên Mặt", D, O, Ability.Pierce, 150, 10, 8, 4, 8.2f, "Phóng ngư lôi từ Đáy xuyên 2 quái Mặt Nước & tạo cột đẩy!"),
            // Stage 9 (18..19)
            new AllySpec("Ếch Bọt Biển", "Biến hóa 2 tầng", B, L, Ability.Habitat, 125, 11, 9, 4, 7.2f, "Mặt Nước: đại bác hạng nặng. Đáy Biển: đóng băng làm chậm cả hàng!"),
            new AllySpec("Cua Thợ Máy", "Sửa 2 tầng", D, X, Ability.Repair, 125, 12, 9, 5, 7.8f, "Vá giáp & hồi máu đồng minh ở cả 2 tầng tại cùng vị trí.")
        };

        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt", S, Trait.Skim, 8, .36f, 1, 10, "Lướt cực nhanh khi đi qua Váng Dầu."),
            new EnemySpec("Túi Chộp", S, Trait.School, 7, .43f, 1, 10, "Tăng tốc mạnh khi đi theo bầy cùng hàng."),
            new EnemySpec("Mạt Cào", D, Trait.Burrow, 10, .34f, 2, 12, "Bò sát đáy, giảm sát thương từ đòn thường."),
            new EnemySpec("Lon Kén", B, Trait.Shell, 15, .29f, 3, 15, "Giáp sắt giảm sát thương cho tới khi bị Tôm Súng / Xung phá vỡ."),
            new EnemySpec("Bè Hai Mạn", B, Trait.Ferry, 14, .34f, 4, 18, "Lặn hoặc nổi bất ngờ sang tầng kia ở giữa sân!"),
            new EnemySpec("Lọc Bẩn", D, Trait.Filter, 14, .31f, 5, 16, "Tự hồi máu theo nhịp và kháng bọt yếu."),
            new EnemySpec("Phao Hắc", S, Trait.Spill, 15, .32f, 6, 18, "Vỡ tung để lại Váng Dầu cản đường khi bị hạ."),
            new EnemySpec("Ống Rè", D, Trait.Jam, 16, .30f, 6, 18, "Gây nhiễu làm chậm tốc độ bắn đồng minh ở tầng đối diện."),
            new EnemySpec("Thùng Húc", B, Trait.Ram, 22, .34f, 7, 22, "Đòn húc phá tường cực mạnh vào vật chắn."),
            new EnemySpec("Bầy Rách", B, Trait.Split, 18, .34f, 7, 20, "Tách thành 2 mảnh vi nhựa chạy cực nhanh khi vỡ!"),
            new EnemySpec("Cánh Lướt", B, Trait.Drift, 14, .50f, 8, 20, "Chuyển sang hàng bên cạnh khi gặp phòng thủ."),
            new EnemySpec("Bè Neo", B, Trait.Anchor, 34, .22f, 9, 30, "Kháng đẩy lùi và tăng tốc toàn bộ đồng đội cùng tầng."),
            new EnemySpec("Sứa Nilon", D, Trait.Phase, 19, .37f, 8, 24, "Lấp lánh độc tố, kháng đòn thường và làm chậm đồng minh gần."),
            new EnemySpec("Tàu Xả Độc", B, Trait.Boss, 180, .14f, 10, 150, "SIÊU BOSS: Đổi tầng liên tục, xả dầu và thả quái viện binh!")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao", S, TrashEffect.Float, 20, 1, "Đẩy Trái: +50 NL | Đẩy Phải: BOWLING", "Đẩy sang trái vào bến nhận +50 NL! Đẩy sang phải (hoặc Shift) để bowling húc bay quái!"),
            new TrashSpec("Mảnh Vỏ", B, TrashEffect.Shell, 25, 1, "Click/F: +25 NL | Phải/G: Khiên tạm", "Click (F) thu +25 NL, hoặc Chuột phải (G) dựng ngay Khiên chắn tạm thời."),
            new TrashSpec("Dây Cước", D, TrashEffect.Filament, 25, 2, "Click/F: CỨU CÁ MIỄN PHÍ!", "Click (F) cắt dây GIẢI CỨU Cá Xạ Thủ miễn phí ngay tại ô!"),
            new TrashSpec("Cục Chì", D, TrashEffect.Ballast, 30, 2, "Click/F: +30 NL | Phải/G: Khiên Mặt Nước", "Click (F) thu +30 NL, hoặc Chuột phải (G) phóng phao chắn lên Mặt Nước."),
            new TrashSpec("Mảnh Gương", B, TrashEffect.Mirror, 25, 4, "Phải/G: Dựng Gương 2 Tầng", "Chuột phải (G) dựng gương kết nối đòn bắn Sứa Pha Lê xuyên 2 tầng!"),
            new TrashSpec("Kiện Nổi", B, TrashEffect.Capsule, 30, 4, "Click/F: +30 NL | Phải/G: Đổi tầng", "Chuột phải (G) chuyển kiện sang tầng kia để mở chỗ trống."),
            new TrashSpec("Pin Rò", B, TrashEffect.Battery, 30, 6, "Phải/G: Nổ EMP 2 Tầng!", "Chuột phải (G) kích nổ xung EMP phá giáp & sát thương cả 2 tầng!"),
            new TrashSpec("Váng Dầu", S, TrashEffect.Spill, 20, 6, "Click/F: Dọn dầu | Phải/G: Đốt cháy", "Làm đục nước & tăng tốc Váng Quệt. Click (F) dọn sạch ngay!"),
            new TrashSpec("Van Khí", D, TrashEffect.Vent, 35, 8, "Phải/G: Mở Van Cột Nước", "Chuột phải (G) mở van phun xung 2 tầng & tạo cột đẩy +55% tốc độ cho ô trên!"),
            new TrashSpec("Lưới Cuộn", B, TrashEffect.Loom, 35, 8, "Phải/G: Giăng lưới tầng kia", "Chuột phải (G) giăng lưới làm chậm cả hàng ở tầng đối diện trong 20s!")
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
            new AchievementSpec("first_rescue", "Cứu Tinh Đại Dương", "Cắt dây cước giải cứu Cá Xạ Thủ dưới đáy biển.", 25),
            new AchievementSpec("bottle_bowler", "Vua Bowling Rác", "Đẩy Chai Phao húc trúng kẻ địch đang tiến tới.", 30),
            new AchievementSpec("dock_master", "Tái Chế Chuyên Nghiệp", "Đưa 3 Chai Phao về bến gom trong một màn.", 30),
            new AchievementSpec("cross_layer", "Bậc Thầy Hai Tầng", "Kích hoạt tương tác xuyên tầng (Gương/Van/Bạch Tuộc/Cá Ống).", 30),
            new AchievementSpec("armor_breaker", "Kẻ Phá Giáp", "Phá vỡ lớp vỏ giáp của Lon Kén bằng Tôm Súng / Xung.", 30),
            new AchievementSpec("flawless", "Đê Xanh Bất Khả Xâm Phạm", "Thắng một màn mà không mất điểm Cửa Đê nào.", 40),
            new AchievementSpec("eco_shopper", "Kỹ Sư Nâng Cấp", "Mua nâng cấp đầu tiên tại Xưởng Bio-Bot.", 25),
            new AchievementSpec("boss_slayer", "Hạ Gục Tàu Xả Độc", "Đánh bại Siêu Boss ở màn 10 Đê Xanh Cảm Tử.", 100),
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
