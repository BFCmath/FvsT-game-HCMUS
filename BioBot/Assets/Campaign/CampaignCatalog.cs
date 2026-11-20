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
        public readonly string Name, Role, ShortTag, Hint;
        public readonly Habitat Habitat;
        public readonly Reach Reach;
        public readonly Ability Ability;
        public readonly int Cost, Health, Unlock, Power;
        public readonly float Recharge;

        public AllySpec(string name, string role, string shortTag, Habitat habitat, Reach reach, Ability ability,
            int cost, int health, int unlock, int power, float recharge, string hint)
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
        public readonly bool HasBoss, DeepActive;

        public LevelSpec(string name, string subtitle, int budget, int waves, int surfaceWeight,
            int coinReward, bool hasBoss, bool deepActive, string unlockSummary, string story, string lesson)
        {
            Name = name;
            Subtitle = subtitle;
            Budget = budget;
            Waves = waves;
            SurfaceWeight = surfaceWeight;
            CoinReward = coinReward;
            HasBoss = hasBoss;
            DeepActive = deepActive;
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

        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec(
                "Bến Gom Nắng Mai", "Màn 1 · Làm quen Mặt Nước", 150, 2, 100, 35, false, false,
                "Rái Cá (+25 NL), Vịt Bắn Bọt, Rùa Khiên",
                "Chương 1 — Cửa Sông Bình Minh: Đợt rác đầu mùa đang trôi chậm vào mặt nước. Hãy thả Rái Cá tạo Năng Lượng và Vịt Bắn Bọt bảo vệ bờ đê!",
                "Chỉ cần chơi ở Mặt Nước (nửa trên): 1) Thả Rái Cá lấy NL, 2) Thả Vịt Bắn Bọt chặn rác, 3) Click Hạt Vàng!"),
            new LevelSpec(
                "Vịnh Cứu Hộ Đáy Sâu", "Màn 2 · Mở khóa Đáy Biển", 175, 2, 65, 45, false, true,
                "Cá Xạ Thủ (bắn Đáy), Bạch Tuộc Neo (làm chậm Mặt từ Đáy)",
                "Chương 1 — Khám Phá Đáy Biển: Nửa dưới màn hình là Đáy Biển! Hãy click vào Dây Cước ở Đáy để cứu Cá Xạ Thủ và thả Bạch Tuộc Neo hỗ trợ ngược lên Mặt Nước.",
                "Bạn nhìn thấy CẢ 2 TẦNG cùng lúc! Click trực tiếp vào ô ở Mặt Nước (trên) hoặc Đáy Biển (dưới) để thả sinh vật."),
            new LevelSpec(
                "Hải Lưu Phá Giáp", "Màn 3 · Phá giáp & Hồi máu", 200, 2, 60, 50, false, true,
                "Tôm Súng (phá giáp), Cò Chữa Lành (hồi máu Đáy)",
                "Chương 1 — Giáp Sắt Lon Kén: Lon Kén có vỏ sắt chống đòn thường. Thả Tôm Súng dưới Đáy hoặc bấm R (Bio-Pulse) để đập vỡ giáp của chúng!",
                "Tôm Súng phá giáp cực nhanh. Đặt Cò Chữa Lành ở Mặt Nước ngay phía trên đồng đội Đáy để hồi máu xuyên tầng!"),
            new LevelSpec(
                "Eo Gương Dòng Đảo", "Màn 4 · Cột nước đẩy +55%", 225, 3, 55, 60, false, true,
                "Hải Ly Đê (xuyên 2 quái), Cá Ống Phun (bắn lên Mặt)",
                "Chương 2 — Làn Sóng Ngầm: Bè Hai Mạn có thể lặn bất ngờ giữa đường! Cá Ống Phun ở Đáy vừa bắn lên Mặt Nước vừa tăng +55% tốc độ cho đồng minh ngay phía trên.",
                "Thử đặt Cá Ống Phun ở Đáy ngay dưới Vịt Bắn Bọt ở Mặt Nước để kích hoạt Cột Nước Đẩy!"),
            new LevelSpec(
                "Bãi Đá San Hô", "Màn 5 · Bồ Nông & San Hô Gai", 250, 3, 52, 70, false, true,
                "Bồ Nông (đẩy lùi), San Hô Gai (tường chắn Đáy)",
                "Chương 2 — Tường Đáy Biển: Lọc Bẩn hồi máu và Mạt Cào tràn qua Đáy Biển. Dựng pháo đài San Hô Gai dưới Đáy và Bồ Nông đẩy lùi trên Mặt Nước!",
                "Đặt San Hô Gai chắn đầu hàng dưới Đáy. Chuột phải vào Mảnh Vỏ hoặc Cục Chì để dựng khiên khẩn cấp!"),
            new LevelSpec(
                "Trạm Xung Thủy Triều", "Màn 6 · Sứa Pha Lê & Lươn Điện", 275, 3, 50, 80, false, true,
                "Sứa Pha Lê (khúc xạ 2 tầng), Lươn Điện (EMP 2 tầng)",
                "Chương 2 — Bão Dầu Đen: Phao Hắc vỡ để lại Váng Dầu. Kích nổ Pin Rò (chuột phải) và thả Lươn Điện quét sạch cả 2 tầng cùng lúc!",
                "Chuột phải vào Mảnh Gương để Sứa Pha Lê bắn cả 2 tầng. Bấm Shift (Hydro-Dash) húc Chai Phao bowling quái!"),
            new LevelSpec(
                "Vịnh Lọc Dầu", "Màn 7 · Thanh lọc Đại Dương", 300, 3, 48, 90, false, true,
                "Hải Âu Lưới, Cặp Cá Hề, Cá Koi Lọc dầu",
                "Chương 3 — Dòng Chảy Xanh: Cá Koi Lọc hút sạch Váng Dầu và tăng Độ Trong Nước lên 100% để kích hoạt Dòng Chảy Xanh (x2 tốc độ bắn)!",
                "Theo dõi thanh Độ Trong Nước bên phải: đạt 100% sẽ bùng nổ Dòng Chảy Xanh trong 10 giây!"),
            new LevelSpec(
                "Rạn Sứa Phát Sáng", "Màn 8 · Hồng Hạc & Cá Nhồng", 325, 4, 48, 105, false, true,
                "Hồng Hạc Xung, Cá Nhồng Ngư Lôi",
                "Chương 3 — Sứa Nilon Độc: Đàn Sứa Nilon và Cánh Lướt đổi làn liên tục! Mở Van Khí (chuột phải) ở Đáy để cường hóa Hồng Hạc Xung ở Mặt Nước.",
                "Mở Van Khí ở Đáy Biển để tạo xung định kỳ cả 2 tầng và buff sát thương cho Hồng Hạc Xung!"),
            new LevelSpec(
                "Hải Lưu Đại Thủy", "Màn 9 · Đội Hình Tối Thượng", 350, 4, 50, 120, false, true,
                "Ếch Bọt Biển (2 dạng), Cua Thợ Máy (sửa 2 tầng)",
                "Chương 3 — Trước Cơn Bão Lớn: Toàn bộ 20 Sinh Vật Biển đã hội tụ! Bè Neo và Thùng Húc tiến đánh đồng thời cả 2 tầng.",
                "Ếch Bọt Biển: thả trên Mặt thành đại bác, thả dưới Đáy làm chậm cả hàng! Cua Thợ Máy sửa cả 2 tầng."),
            new LevelSpec(
                "Đê Xanh Cảm Tử", "Màn 10 · ĐẠI CHIẾN SIÊU BOSS", 400, 4, 50, 220, true, true,
                "HOÀN THÀNH CHIẾN DỊCH!",
                "CHƯƠNG CUỐI — SIÊU TÀU XẢ ĐỘC: Con tàu rác khổng lồ đang tiến vào Cửa Đê! Nó liên tục đổi giữa Mặt Nước và Đáy Biển. Huy động toàn lực đánh bại nó!",
                "Quan sát Boss đổi tầng ở màn hình 2 tầng, dựng phòng tuyến cả trên lẫn dưới và dồn hỏa lực hạ gục Boss!")
        };

        // 20 100% Aquatic Guardians with instant-read ShortTags!
        public static readonly AllySpec[] Allies =
        {
            // Stage 1 (0..2) — super clean 3-card starter deck!
            new AllySpec("Rái Cá", "Tạo năng lượng", "☀ +25 NL", B, L, Ability.Income, 50, 6, 1, 25, 4.5f, "Giống Hoa Hướng Dương: tự thả Hạt Vàng +25 Năng Lượng. Đặt được ở cả 2 tầng!"),
            new AllySpec("Vịt Bắn Bọt", "Bắn tia nước", "● Bắn Mặt", S, L, Ability.Shot, 50, 7, 1, 2, 4.0f, "Phun tia nước dọc hàng Mặt Nước để tiêu diệt quái rác."),
            new AllySpec("Rùa Khiên", "Tường chắn", "🛡 Chắn", S, L, Ability.Guard, 50, 24, 1, 1, 9.0f, "Mai rùa siêu cứng chặn đứng quái rác ở Mặt Nước, phản đòn khi bị cắn."),
            // Stage 2 (3..4) — Deep unlocks!
            new AllySpec("Cá Xạ Thủ", "Bắn bọt đáy", "● Bắn Đáy", D, L, Ability.Shot, 50, 8, 2, 2, 4.0f, "Bắn bọt dọc hàng Đáy Biển. Click cắt Dây Cước sẽ cứu miễn phí!"),
            new AllySpec("Bạch Tuộc", "Hãm Mặt Nước", "❄ Chậm ↑", D, O, Ability.Slow, 75, 9, 2, 3, 5.8f, "Đặt dưới Đáy: vươn xúc tu lên làm chậm quái rác ở Mặt Nước!"),
            // Stage 3 (5..6)
            new AllySpec("Tôm Súng", "Phá giáp", "⚡ Phá Giáp", D, L, Ability.Zap, 100, 8, 3, 3, 6.0f, "Búng càng siêu âm siêu nhanh dưới Đáy, đập vỡ giáp sắt Lon Kén!"),
            new AllySpec("Cò Hồi Máu", "Hồi máu Đáy", "✚ Hồi ↓", S, O, Ability.Repair, 75, 8, 3, 4, 6.5f, "Đặt trên Mặt: lặn mỏ xuống hồi máu cho đồng minh ngay phía dưới Đáy!"),
            // Stage 4 (7..8)
            new AllySpec("Hải Ly Đê", "Xuyên 2 quái", "» Xuyên 2", B, L, Ability.Pierce, 125, 9, 4, 3, 6.8f, "Phóng giáo nước xuyên qua 2 quái rác gần nhất. Đặt được cả 2 tầng!"),
            new AllySpec("Cá Ống Phun", "Bắn lên Mặt", "▲ Bắn ↑", D, O, Ability.Shot, 100, 8, 4, 3, 6.2f, "Đặt dưới Đáy: phun vòi nước lên Mặt Nước & buff +55% tốc độ ô phía trên!"),
            // Stage 5 (9..10)
            new AllySpec("Bồ Nông", "Đẩy lùi sóng", "« Đẩy Lùi", S, L, Ability.Pull, 100, 8, 5, 2, 7.0f, "Vỗ cánh tạo sóng đẩy lùi quái rác trên Mặt Nước."),
            new AllySpec("San Hô Gai", "Tường Đáy", "🛡 Gai Đáy", D, L, Ability.Guard, 75, 30, 5, 2, 10f, "Rạn san hô siêu bền dưới Đáy Biển, phản gai sát thương cao."),
            // Stage 6 (11..12)
            new AllySpec("Sứa Pha Lê", "Gương 2 tầng", "⇄ 2 Tầng", B, X, Ability.Reflect, 100, 8, 6, 3, 6.5f, "Bắn cả 2 tầng khi có Mảnh Gương kích hoạt ở cùng cột!"),
            new AllySpec("Lươn Điện", "Xung EMP", "⚡ EMP 2T", D, X, Ability.Pulse, 150, 9, 6, 4, 8.5f, "Phát xung điện phá giáp cả 2 tầng & tạo cột nước đẩy cho ô trên!"),
            // Stage 7 (13..15)
            new AllySpec("Hải Âu Lưới", "Hãm Đáy", "❄ Chậm ↓", S, O, Ability.Slow, 75, 9, 7, 3, 6.2f, "Đặt trên Mặt: thả lưới tảo làm chậm toàn bộ quái dưới Đáy!"),
            new AllySpec("Cặp Cá Hề", "Liên kết", "⇄ Ghép Đôi", B, X, Ability.Link, 125, 9, 7, 4, 7.2f, "Bắn cả 2 tầng khi có bất kỳ đồng minh nào ở ô đối diện!"),
            new AllySpec("Cá Koi Lọc", "Dọn dầu", "✦ Lọc Dầu", D, L, Ability.Clean, 100, 12, 7, 3, 6.2f, "Tự hút sạch Váng Dầu lân cận, tăng Độ Trong Nước & bắn bọt."),
            // Stage 8 (16..17)
            new AllySpec("Hồng Hạc", "Xung Van Khí", "★ Xung 2T", S, X, Ability.Echo, 175, 9, 8, 5, 9.0f, "Phóng xung xuyên 2 tầng cực mạnh, tăng sát thương khi có Van Khí!"),
            new AllySpec("Cá Nhồng", "Ngư lôi ↑", "» Xuyên ↑", D, O, Ability.Pierce, 150, 10, 8, 4, 8.0f, "Phóng ngư lôi từ Đáy xuyên 2 quái Mặt Nước & tạo cột đẩy!"),
            // Stage 9 (18..19)
            new AllySpec("Ếch Bọt", "2 Dạng", "★ Biến Hình", B, L, Ability.Habitat, 125, 11, 9, 4, 7.0f, "Mặt Nước: đại bác hạng nặng. Đáy Biển: đóng băng làm chậm cả hàng!"),
            new AllySpec("Cua Thợ Máy", "Sửa 2 tầng", "✚ Sửa 2T", D, X, Ability.Repair, 125, 12, 9, 5, 7.5f, "Vá giáp & hồi máu đồng minh ở cả 2 tầng tại cùng vị trí.")
        };

        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt", S, Trait.Skim, 7, .22f, 1, 10, "Trôi chậm trên Mặt Nước, tăng tốc khi qua Váng Dầu."),
            new EnemySpec("Túi Chộp", S, Trait.School, 6, .25f, 1, 10, "Đi theo bầy cùng hàng sẽ bơi nhanh hơn một chút."),
            new EnemySpec("Mạt Cào", D, Trait.Burrow, 9, .21f, 2, 12, "Bò sát Đáy Biển, đỡ một phần đòn thường."),
            new EnemySpec("Lon Kén", B, Trait.Shell, 14, .18f, 3, 15, "Giáp sắt giảm sát thương cho tới khi bị Tôm Súng / Xung phá vỡ."),
            new EnemySpec("Bè Hai Mạn", B, Trait.Ferry, 13, .21f, 4, 18, "Lặn hoặc nổi bất ngờ sang tầng kia ở giữa sân!"),
            new EnemySpec("Lọc Bẩn", D, Trait.Filter, 13, .19f, 5, 16, "Tự hồi máu theo nhịp và kháng bọt yếu."),
            new EnemySpec("Phao Hắc", S, Trait.Spill, 14, .20f, 6, 18, "Vỡ tung để lại Váng Dầu cản đường khi bị hạ."),
            new EnemySpec("Ống Rè", D, Trait.Jam, 15, .19f, 6, 18, "Gây nhiễu làm chậm đồng minh ở tầng đối diện."),
            new EnemySpec("Thùng Húc", B, Trait.Ram, 20, .21f, 7, 22, "Đòn húc mạnh vào tường chắn."),
            new EnemySpec("Bầy Rách", B, Trait.Split, 16, .21f, 7, 20, "Tách thành 2 mảnh vi nhựa khi vỡ!"),
            new EnemySpec("Cánh Lướt", B, Trait.Drift, 13, .28f, 8, 20, "Chuyển sang hàng bên cạnh khi gặp phòng thủ."),
            new EnemySpec("Bè Neo", B, Trait.Anchor, 32, .14f, 9, 30, "Kháng đẩy lùi và tăng tốc đồng đội cùng tầng."),
            new EnemySpec("Sứa Nilon", D, Trait.Phase, 18, .22f, 8, 24, "Lấp lánh độc tố, kháng đòn thường và làm chậm đồng minh gần."),
            new EnemySpec("Tàu Xả Độc", B, Trait.Boss, 170, .09f, 10, 150, "SIÊU BOSS: Đổi tầng liên tục, xả dầu và thả viện binh!")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao", S, TrashEffect.Float, 25, 1, "Đẩy Trái: +50 NL | Đẩy Phải: BOWLING", "Đẩy sang trái vào bến nhận +50 NL! Đẩy sang phải để bowling húc bay quái!"),
            new TrashSpec("Mảnh Vỏ", B, TrashEffect.Shell, 25, 1, "Click: +25 NL | Chuột phải: Khiên", "Click thu +25 NL, hoặc Chuột phải dựng ngay Khiên chắn tạm thời."),
            new TrashSpec("Dây Cước", D, TrashEffect.Filament, 25, 2, "Click: CỨU CÁ MIỄN PHÍ!", "Click cắt dây GIẢI CỨU Cá Xạ Thủ miễn phí ngay tại ô!"),
            new TrashSpec("Cục Chì", D, TrashEffect.Ballast, 30, 2, "Click: +30 NL | Chuột phải: Khiên Mặt", "Click thu +30 NL, hoặc Chuột phải phóng phao chắn lên Mặt Nước."),
            new TrashSpec("Mảnh Gương", B, TrashEffect.Mirror, 25, 4, "Chuột phải: Dựng Gương 2 Tầng", "Chuột phải dựng gương kết nối đòn bắn Sứa Pha Lê xuyên 2 tầng!"),
            new TrashSpec("Kiện Nổi", B, TrashEffect.Capsule, 30, 4, "Click: +30 NL | Chuột phải: Đổi tầng", "Chuột phải chuyển kiện sang tầng kia để mở chỗ trống."),
            new TrashSpec("Pin Rò", B, TrashEffect.Battery, 30, 6, "Chuột phải: Nổ EMP 2 Tầng!", "Chuột phải kích nổ xung EMP phá giáp & sát thương cả 2 tầng!"),
            new TrashSpec("Váng Dầu", S, TrashEffect.Spill, 20, 6, "Click: Dọn dầu | Chuột phải: Đốt cháy", "Làm đục nước & tăng tốc Váng Quệt. Click dọn sạch ngay!"),
            new TrashSpec("Van Khí", D, TrashEffect.Vent, 35, 8, "Chuột phải: Mở Van Cột Nước", "Chuột phải mở van phun xung 2 tầng & tạo cột đẩy +55% tốc độ cho ô trên!"),
            new TrashSpec("Lưới Cuộn", B, TrashEffect.Loom, 35, 8, "Chuột phải: Giăng lưới tầng kia", "Chuột phải giăng lưới làm chậm cả hàng ở tầng đối diện trong 20s!")
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

        // Sprite row index in RosterV2.png (kept consistent after swapping Rái Cá to slot 0!)
        public static readonly int[] AllySpriteRow =
        {
            2, 0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
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
