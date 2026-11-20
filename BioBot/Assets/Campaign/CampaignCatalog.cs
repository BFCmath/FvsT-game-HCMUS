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
        public readonly string Name, Subtitle, Story, Lesson, ConceptTitle, ConceptBody;
        public readonly int Budget, Waves, SurfaceWeight, CoinReward;
        public readonly bool HasBoss, DeepActive;

        public LevelSpec(string name, string subtitle, int budget, int waves, int surfaceWeight,
            int coinReward, bool hasBoss, bool deepActive, string conceptTitle, string conceptBody, string story, string lesson)
        {
            Name = name;
            Subtitle = subtitle;
            Budget = budget;
            Waves = waves;
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
        public const int TotalAtlasRows = 44;
        private const Habitat S = Habitat.Surface;
        private const Habitat D = Habitat.Deep;
        private const Habitat B = Habitat.Both;
        private const Reach L = Reach.Local;
        private const Reach O = Reach.Other;
        private const Reach X = Reach.Both;

        // 10 Gradual Stages — Stage 1 has ONLY Rái Cá & Vịt!
        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec(
                "Bến Gom Nắng Mai", "Màn 1 · Chỉ Rái Cá & Vịt", 100, 2, 100, 35, false, false,
                "KHÁI NIỆM MỚI: RÁI CÁ & VỊT BẮN BỌT",
                "• RÁI CÁ (☀ +25 NL): Tự tạo Hạt Vàng Năng Lượng. Hạt rơi tự nhiên rất ít nên hãy đặt 2-3 Rái Cá trước!\n• VỊT BẮN BỌT (● Bắn Mặt): Bắn tiêu diệt quái rác trên Mặt Nước.\n• Lái Bio-Bot (WASD) đẩy Chai Phao sang trái nhận +50 NL!",
                "Chương 1 — Cửa Sông Bình Minh: Đợt rác đầu tiên trôi vào mặt bể. Hãy dùng Rái Cá tạo năng lượng và Vịt Bắn Bọt bảo vệ bờ đê!",
                "Màn 1 chỉ có 2 thẻ: Đặt Rái Cá ở cột trái tạo Năng Lượng, sau đó đặt Vịt Bắn Bọt chặn quái rác!"),
            new LevelSpec(
                "Bức Tường & Đáy Sâu", "Màn 2 · Rùa Khiên & Đáy Biển (Q)", 150, 2, 70, 45, false, true,
                "KHÁI NIỆM MỚI: RÙA KHIÊN & ĐÁY BIỂN NGỘP NƯỚC",
                "• RÙA KHIÊN (🛡 Chắn): Mai siêu cứng chặn quái rác.\n• Bấm Q để LẶN xuống Đáy Biển! Quái dưới đáy ô nhiễm nặng nên TRÂU HƠN +40% MÁU.\n• Bạn CÓ THỂ đặt Vịt hoặc Rùa xuống Đáy khi khẩn cấp, nhưng chúng không phải loài sống ở đáy nên sẽ NGỘP NƯỚC & mất máu dần!",
                "Chương 1 — Thử Thách Đáy Sâu: Đáy bể đã mở! Chưa có sinh vật đáy bản địa — hãy dùng Rùa Khiên & Vịt lặn xuống cầm cự tạm thời!",
                "Bấm Q để lặn/nổi. Vịt & Rùa đặt xuống Đáy sẽ mất máu dần (O₂↓) — hãy giữ nhịp cho đến khi giải cứu được sinh vật đáy ở Màn 3!"),
            new LevelSpec(
                "Cá Xạ Thủ Đáy Biển", "Màn 3 · Sinh vật đáy bản địa", 175, 2, 58, 55, false, true,
                "KHÁI NIỆM MỚI: CÁ XẠ THỦ (BẮN ĐẨY LÙI) & BẠCH TUỘC",
                "• CÁ XẠ THỦ (75 NL · 💥 Bắn Đẩy): Sống bản địa dưới Đáy Biển (không bị ngộp nước, KHÔNG thể đặt lên Mặt Nước!). Đạn bọt cực mạnh + ĐẨY LÙI quái trâu dưới đáy!\n• Click cắt DÂY CƯỚC dưới đáy để cứu miễn phí 1 Cá Xạ Thủ!\n• BẠCH TUỘC (❄ Chậm ↑): Đặt dưới Đáy làm chậm quái Mặt Nước!",
                "Chương 1 — Chúa Tể Đáy Sâu: Cá Xạ Thủ bản địa đã sẵn sàng! Không còn lo ngộp nước — sức bắn đẩy lùi của chúng là khắc tinh của quái đáy trâu bò!",
                "Bấm Q lặn xuống cứu Cá Xạ Thủ từ Dây Cước. Cá Xạ Thủ (75 NL) bắn mạnh & đẩy lùi quái đáy, nhưng không sống được trên Mặt Nước!"),
            new LevelSpec(
                "Hải Lưu Phá Giáp", "Màn 4 · Lon Kén & Tôm Súng", 175, 3, 55, 65, false, true,
                "KHÁI NIỆM MỚI: GIÁP SẮT & TÔM SÚNG PHÁ GIÁP",
                "• LON KÉN có vỏ sắt giảm sát thương từ đòn thường.\n• TÔM SÚNG (⚡ Phá Giáp) dưới Đáy hoặc bấm R (Bio-Pulse) sẽ đập vỡ giáp của chúng!\n• CÒ HỒI MÁU (✚ Hồi ↓) đặt trên Mặt sẽ chữa lành đồng đội ngay ô dưới Đáy!",
                "Chương 2 — Giáp Kim Loại: Rác kim loại Lon Kén đã chìm xuống đáy và nổi trên mặt. Tôm Súng và Cò Hồi Máu gia nhập đội hình!",
                "Dùng Tôm Súng hoặc nhấn R (Bio-Pulse) để vỡ giáp Lon Kén. Cò Hồi Máu trên Mặt chữa lành cho đồng minh dưới Đáy!"),
            new LevelSpec(
                "Eo Gương Dòng Đảo", "Màn 5 · Cột Nước & Hải Ly", 200, 3, 55, 75, false, true,
                "KHÁI NIỆM MỚI: CỘT NƯỚC ĐẨY +55% & BÈ HAI MẠN",
                "• BÈ HAI MẠN biết tự lặn/nổi đổi tầng ở giữa bể!\n• CÁ ỐNG PHUN (▲ Bắn ↑) dưới Đáy vừa bắn lên Mặt Nước vừa tạo Cột Nước Đẩy tăng +55% tốc độ cho ô ngay trên nó!\n• HẢI LY ĐÊ (» Xuyên 2): Bắn xuyên 2 quái, sống được cả 2 tầng!",
                "Chương 2 — Làn Sóng Ngầm: Quái rác bắt đầu chuyển tầng bất ngờ. Hãy tận dụng Cá Ống Phun và Hải Ly Đê kiểm soát cả hai mặt bể!",
                "Đặt Cá Ống Phun dưới Đáy ngay bên dưới Vịt/Hải Ly ở Mặt Nước để kích hoạt Cột Nước Đẩy!"),
            new LevelSpec(
                "Bãi Đá San Hô", "Màn 6 · Bồ Nông & San Hô Gai", 225, 3, 50, 85, false, true,
                "KHÁI NIỆM MỚI: SAN HÔ GAI & LỌC BẨN HỒI MÁU",
                "• LỌC BẨN dưới Đáy tự hồi máu và cực trâu.\n• SAN HÔ GAI (🛡 Gai Đáy): Pháo đài máu siêu dày dưới Đáy, phản gai sát thương cao.\n• BỒ NÔNG (« Đẩy Lùi): Vỗ cánh hất lùi quái trên Mặt Nước!",
                "Chương 2 — Rạn San Hô: Quái đáy ngày càng nặng và lì đòn. Hãy dựng tường San Hô Gai dưới đáy để chặn đứng chúng!",
                "Đặt San Hô Gai ở Đáy để giữ chân quái trâu +40% máu. Bồ Nông đẩy lùi trên Mặt Nước!"),
            new LevelSpec(
                "Trạm Xung Thủy Triều", "Màn 7 · Sứa Pha Lê & Lươn Điện", 250, 3, 50, 95, false, true,
                "KHÁI NIỆM MỚI: LƯƠN ĐIỆN EMP 2 TẦNG & VÁNG DẦU",
                "• PHAO HẮC vỡ tung để lại Váng Dầu làm đục nước.\n• LƯƠN ĐIỆN (⚡ EMP 2T): Phóng điện xuyên cả Đáy lẫn Mặt Nước!\n• Chuột phải vào PIN RÒ để kích nổ EMP phá giáp cả 2 tầng!",
                "Chương 3 — Bão Dầu Đen: Dầu loang và Ống Rè gây nhiễu tấn công đồng loạt. Huy động Sứa Pha Lê và Lươn Điện phản công!",
                "Bấm Q linh hoạt để dọn dầu, kích nổ Pin Rò và thả Lươn Điện quét sạch cả 2 tầng!"),
            new LevelSpec(
                "Vịnh Lọc Dầu", "Màn 8 · Cá Koi Lọc & Dòng Chảy Xanh", 275, 4, 48, 110, false, true,
                "KHÁI NIỆM MỚI: THANH LỌC ĐỘ TRONG NƯỚC 100%",
                "• CÁ KOI LỌC (✦ Lọc Dầu) tự hút Váng Dầu lân cận và đẩy thanh ĐỘ TRONG NƯỚC lên 100%.\n• Khi Độ Trong Nước đạt 100%, DÒNG CHẢY XANH kích hoạt 10s: x2 tốc độ bắn & mưa năng lượng!",
                "Chương 3 — Thanh Lọc Đại Dương: Thùng Húc và Bầy Rách tràn vào. Cá Koi Lọc, Hải Âu Lưới và Cặp Cá Hề sẵn sàng!",
                "Giữ Đáy sạch và đẩy Độ Trong Nước lên 100% để kích hoạt Dòng Chảy Xanh!"),
            new LevelSpec(
                "Rạn Sứa Phát Sáng", "Màn 9 · Đội Hình Tối Thượng", 325, 4, 48, 130, false, true,
                "KHÁI NIỆM MỚI: HỒNG HẠC, CÁ NHỒNG, ẾCH & CUA THỢ",
                "• Toàn bộ 20 Sinh Vật Biển đã mở khóa!\n• Mở VAN KHÍ (chuột phải) dưới Đáy để buff cực đại cho Hồng Hạc Xung!\n• Ếch Bọt đổi dạng theo tầng, Cua Thợ Máy sửa cả 2 tầng!",
                "Chương 3 — Trước Giờ G: Bè Neo, Sứa Nilon và Cánh Lướt tổng tiến công cả hai mặt bể 3D!",
                "Kết hợp Van Khí + Hồng Hạc + Cua Thợ Máy để giữ vững cả 2 tầng!"),
            new LevelSpec(
                "Đê Xanh Cảm Tử", "Màn 10 · ĐẠI CHIẾN SIÊU BOSS", 375, 4, 50, 240, true, true,
                "CẢNH BÁO ĐỎ: SIÊU TÀU XẢ ĐỘC!",
                "• TÀU XẢ ĐỘC có lượng máu khổng lồ, liên tục lặn/nổi giữa Mặt Nước và Đáy Biển, xả dầu và thả viện binh!\n• Dùng Q bám theo tầng của Boss, kích hoạt Dòng Chảy Xanh và dồn hỏa lực 2 tầng tiêu diệt nó!",
                "CHƯƠNG CUỐI: Cuộc chiến quyết định số phận Đê Xanh! Huy động toàn bộ đội quân đại dương đánh bại Tàu Xả Độc!",
                "Theo dõi bóng mờ của Boss khi nó đổi tầng, bấm Q bám sát và dồn hỏa lực xuyên tầng!")
        };

        // 20 100% Aquatic Guardians with gradual Stage 1..9 unlock curve!
        public static readonly AllySpec[] Allies =
        {
            // Stage 1 (0..1): ONLY Rái Cá & Vịt Bắn Bọt!
            new AllySpec("Rái Cá", "Tạo năng lượng", "☀ +25 NL", B, L, Ability.Income, 50, 6, 1, 25, 4.8f, "Tự thả Hạt Vàng +25 NL. Sống khỏe ở cả 2 tầng!"),
            new AllySpec("Vịt Bắn Bọt", "Bắn tia nước", "● Bắn Mặt", S, L, Ability.Shot, 50, 7, 1, 2, 4.2f, "Bắn dọc hàng Mặt Nước. Có thể đặt khẩn cấp xuống Đáy nhưng sẽ ngộp nước & mất máu dần!"),
            // Stage 2 (2): Rùa Khiên!
            new AllySpec("Rùa Khiên", "Tường chắn", "🛡 Chắn", S, L, Ability.Guard, 50, 24, 2, 1, 9.5f, "Mai rùa siêu cứng chặn quái. Đặt xuống Đáy sẽ bị ngộp nước & mất máu dần!"),
            // Stage 3 (3..4): Native Deep — Cá Xạ Thủ (Special Knockback + Stronger + 75 NL!) & Bạch Tuộc!
            new AllySpec("Cá Xạ Thủ", "Bắn đẩy Đáy", "💥 Bắn Đẩy", D, L, Ability.Shot, 75, 10, 3, 3, 5.2f, "Bản địa Đáy Biển (KHÔNG sống trên Mặt!). Đạn bọt mạnh + ĐẨY LÙI quái trâu dưới đáy!"),
            new AllySpec("Bạch Tuộc", "Hãm Mặt Nước", "❄ Chậm ↑", D, O, Ability.Slow, 75, 9, 3, 3, 6.0f, "Đặt dưới Đáy: vươn xúc tu lên làm chậm quái rác ở Mặt Nước!"),
            // Stage 4 (5..6)
            new AllySpec("Tôm Súng", "Phá giáp", "⚡ Phá Giáp", D, L, Ability.Zap, 100, 9, 4, 3, 6.0f, "Búng càng siêu âm dưới Đáy, đập vỡ giáp sắt Lon Kén!"),
            new AllySpec("Cò Hồi Máu", "Hồi máu Đáy", "✚ Hồi ↓", S, O, Ability.Repair, 75, 8, 4, 4, 6.5f, "Đặt trên Mặt: lặn mỏ xuống hồi máu cho đồng minh ô Đáy bên dưới!"),
            // Stage 5 (7..8)
            new AllySpec("Hải Ly Đê", "Xuyên 2 quái", "» Xuyên 2", B, L, Ability.Pierce, 125, 9, 5, 3, 6.8f, "Phóng giáo nước xuyên 2 quái rác gần nhất. Sống được cả 2 tầng!"),
            new AllySpec("Cá Ống Phun", "Bắn lên Mặt", "▲ Bắn ↑", D, O, Ability.Shot, 100, 9, 5, 3, 6.2f, "Đặt dưới Đáy: phun lên Mặt Nước & tạo Cột Nước Đẩy +55% tốc độ cho ô trên!"),
            // Stage 6 (9..10)
            new AllySpec("Bồ Nông", "Đẩy lùi sóng", "« Đẩy Lùi", S, L, Ability.Pull, 100, 8, 6, 2, 7.0f, "Vỗ cánh tạo sóng đẩy lùi quái rác trên Mặt Nước."),
            new AllySpec("San Hô Gai", "Tường Đáy", "🛡 Gai Đáy", D, L, Ability.Guard, 75, 32, 6, 2, 10f, "Pháo đài san hô siêu bền dưới Đáy Biển, phản gai sát thương cao."),
            // Stage 7 (11..12)
            new AllySpec("Sứa Pha Lê", "Gương 2 tầng", "⇄ 2 Tầng", B, X, Ability.Reflect, 100, 8, 7, 3, 6.5f, "Bắn cả 2 tầng khi có Mảnh Gương kích hoạt ở cùng cột!"),
            new AllySpec("Lươn Điện", "Xung EMP", "⚡ EMP 2T", D, X, Ability.Pulse, 150, 10, 7, 4, 8.5f, "Phát xung điện phá giáp cả 2 tầng & tạo cột nước đẩy cho ô trên!"),
            // Stage 8 (13..15)
            new AllySpec("Hải Âu Lưới", "Hãm Đáy", "❄ Chậm ↓", S, O, Ability.Slow, 75, 9, 8, 3, 6.2f, "Đặt trên Mặt: thả lưới tảo làm chậm toàn bộ quái dưới Đáy!"),
            new AllySpec("Cặp Cá Hề", "Liên kết", "⇄ Ghép Đôi", B, X, Ability.Link, 125, 9, 8, 4, 7.2f, "Bắn cả 2 tầng khi có đồng minh ở ô đối diện!"),
            new AllySpec("Cá Koi Lọc", "Dọn dầu", "✦ Lọc Dầu", D, L, Ability.Clean, 100, 12, 8, 3, 6.2f, "Hút sạch Váng Dầu lân cận, tăng Độ Trong Nước & bắn bọt."),
            // Stage 9 (16..19)
            new AllySpec("Hồng Hạc", "Xung Van Khí", "★ Xung 2T", S, X, Ability.Echo, 175, 9, 9, 5, 9.0f, "Phóng xung xuyên 2 tầng cực mạnh, tăng sát thương khi có Van Khí!"),
            new AllySpec("Cá Nhồng", "Ngư lôi ↑", "» Xuyên ↑", D, O, Ability.Pierce, 150, 10, 9, 4, 8.0f, "Phóng ngư lôi từ Đáy xuyên 2 quái Mặt Nước & tạo cột đẩy!"),
            new AllySpec("Ếch Bọt", "2 Dạng", "★ Biến Hình", B, L, Ability.Habitat, 125, 11, 9, 4, 7.0f, "Mặt Nước: đại bác hạng nặng. Đáy Biển: làm chậm cả hàng!"),
            new AllySpec("Cua Thợ Máy", "Sửa 2 tầng", "✚ Sửa 2T", D, X, Ability.Repair, 125, 12, 9, 5, 7.5f, "Vá giáp & hồi máu đồng minh ở cả 2 tầng tại cùng cột.")
        };

        // Turned speed back up for exciting, balanced challenge on the 11-column 3D pool!
        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt", S, Trait.Skim, 8, .36f, 1, 10, "Quái mặt nước cơ bản, tăng tốc khi qua Váng Dầu."),
            new EnemySpec("Túi Chộp", S, Trait.School, 7, .43f, 2, 10, "Bơi nhanh hơn khi đi theo bầy cùng hàng."),
            new EnemySpec("Mạt Cào", D, Trait.Burrow, 11, .34f, 2, 12, "Bò sát Đáy Biển, trâu hơn +40% máu & giảm đòn thường."),
            new EnemySpec("Lon Kén", B, Trait.Shell, 16, .30f, 4, 15, "Giáp sắt giảm sát thương cho tới khi bị Tôm Súng / Xung phá vỡ."),
            new EnemySpec("Bè Hai Mạn", B, Trait.Ferry, 15, .34f, 5, 18, "Lặn hoặc nổi bất ngờ sang tầng kia ở giữa bể!"),
            new EnemySpec("Lọc Bẩn", D, Trait.Filter, 16, .31f, 6, 16, "Quái đáy siêu lì, tự hồi máu theo nhịp."),
            new EnemySpec("Phao Hắc", S, Trait.Spill, 16, .33f, 7, 18, "Vỡ tung để lại Váng Dầu cản đường khi bị hạ."),
            new EnemySpec("Ống Rè", D, Trait.Jam, 17, .30f, 7, 18, "Gây nhiễu làm chậm đồng minh ở tầng đối diện."),
            new EnemySpec("Thùng Húc", B, Trait.Ram, 24, .34f, 8, 22, "Đòn húc mạnh phá tường chắn."),
            new EnemySpec("Bầy Rách", B, Trait.Split, 18, .35f, 8, 20, "Tách thành 2 mảnh vi nhựa chạy nhanh khi vỡ!"),
            new EnemySpec("Cánh Lướt", B, Trait.Drift, 15, .48f, 9, 20, "Chuyển sang hàng bên cạnh khi gặp phòng thủ."),
            new EnemySpec("Bè Neo", B, Trait.Anchor, 36, .23f, 9, 30, "Kháng đẩy lùi và tăng tốc đồng đội cùng tầng."),
            new EnemySpec("Sứa Nilon", D, Trait.Phase, 20, .37f, 9, 24, "Lấp lánh độc tố dưới Đáy, kháng đòn thường."),
            new EnemySpec("Tàu Xả Độc", B, Trait.Boss, 200, .14f, 10, 150, "SIÊU BOSS: Đổi tầng liên tục, xả dầu và thả viện binh!")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao", S, TrashEffect.Float, 20, 1, "Đẩy Trái: +50 NL | Đẩy Phải: BOWLING", "Đẩy sang trái vào bến nhận +50 NL! Đẩy sang phải húc bay quái!"),
            new TrashSpec("Mảnh Vỏ", B, TrashEffect.Shell, 25, 2, "Click: +25 NL | Chuột phải: Khiên", "Click thu +25 NL, hoặc Chuột phải dựng ngay Khiên chắn tạm thời."),
            new TrashSpec("Dây Cước", D, TrashEffect.Filament, 25, 3, "Click: CỨU CÁ MIỄN PHÍ!", "Lặn xuống (Q) rồi click cắt dây GIẢI CỨU Cá Xạ Thủ miễn phí!"),
            new TrashSpec("Cục Chì", D, TrashEffect.Ballast, 30, 3, "Click: +30 NL | Chuột phải: Khiên Mặt", "Click thu +30 NL, hoặc Chuột phải phóng phao chắn lên Mặt Nước."),
            new TrashSpec("Mảnh Gương", B, TrashEffect.Mirror, 25, 5, "Chuột phải: Dựng Gương 2 Tầng", "Chuột phải dựng gương kết nối đòn bắn Sứa Pha Lê xuyên 2 tầng!"),
            new TrashSpec("Kiện Nổi", B, TrashEffect.Capsule, 30, 5, "Click: +30 NL | Chuột phải: Đổi tầng", "Chuột phải chuyển kiện sang tầng kia để mở chỗ trống."),
            new TrashSpec("Pin Rò", B, TrashEffect.Battery, 30, 7, "Chuột phải: Nổ EMP 2 Tầng!", "Chuột phải kích nổ xung EMP phá giáp & sát thương cả 2 tầng!"),
            new TrashSpec("Váng Dầu", S, TrashEffect.Spill, 20, 7, "Click: Dọn dầu | Chuột phải: Đốt cháy", "Làm đục nước & tăng tốc Váng Quệt. Click dọn sạch ngay!"),
            new TrashSpec("Van Khí", D, TrashEffect.Vent, 35, 9, "Chuột phải: Mở Van Cột Nước", "Chuột phải mở van phun xung 2 tầng & tạo cột đẩy +55% tốc độ cho ô trên!"),
            new TrashSpec("Lưới Cuộn", B, TrashEffect.Loom, 35, 9, "Chuột phải: Giăng lưới tầng kia", "Chuột phải giăng lưới làm chậm cả hàng ở tầng đối diện trong 20s!")
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
            new AchievementSpec("cross_layer", "Bậc Thầy Hai Tầng", "Kích hoạt tương tác xuyên tầng (Gương/Van/Bạch Tuộc/Cá Ống).", 30),
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
