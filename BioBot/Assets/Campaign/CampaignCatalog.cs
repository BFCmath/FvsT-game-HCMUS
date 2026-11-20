using System;
using UnityEngine;

namespace BioBot
{
    [Flags]
    public enum Habitat { Surface = 1, Deep = 2, Both = 3 }
    public enum Layer { Surface, Deep }
    public enum TrashEffect { Float, Filament, Ballast, Spill, Battery, Mirror, Shell, Vent, Loom, Capsule }
    public enum Trait { Skim, Burrow, Ferry, Filter, Spill, Shell, Jam, School, Ram, Split, Drift, Anchor }
    public enum Ability { Shot, Guard, Repair, Slow, Zap, Income, Pull, Pulse, Reflect, Link, Pierce, Echo, Habitat, Clean }
    public enum Reach { Local, Other, Both }

    public sealed class AllySpec
    {
        public readonly string Name, Hint;
        public readonly Habitat Habitat;
        public readonly Reach Reach;
        public readonly Ability Ability;
        public readonly int Cost, Health, Unlock, Power;

        public AllySpec(string name, Habitat habitat, Reach reach, Ability ability, int cost, int health, int unlock, int power, string hint)
        {
            Name = name;
            Habitat = habitat;
            Reach = reach;
            Ability = ability;
            Cost = cost;
            Health = health;
            Unlock = unlock;
            Power = power;
            Hint = hint;
        }
    }

    public sealed class EnemySpec
    {
        public readonly string Name, Hint;
        public readonly Habitat Habitat;
        public readonly Trait Trait;
        public readonly int Health, Unlock;
        public readonly float Speed;

        public EnemySpec(string name, Habitat habitat, Trait trait, int health, float speed, int unlock, string hint)
        {
            Name = name;
            Habitat = habitat;
            Trait = trait;
            Health = health;
            Speed = speed;
            Unlock = unlock;
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
        public readonly string Name, Lesson;
        public readonly int Budget, Waves, SurfaceWeight;

        public LevelSpec(string name, int budget, int waves, int surfaceWeight, string lesson)
        {
            Name = name;
            Budget = budget;
            Waves = waves;
            SurfaceWeight = surfaceWeight;
            Lesson = lesson;
        }
    }

    public static class CampaignCatalog
    {
        private const Habitat S = Habitat.Surface;
        private const Habitat D = Habitat.Deep;
        private const Habitat B = Habitat.Both;
        private const Reach L = Reach.Local;
        private const Reach O = Reach.Other;
        private const Reach X = Reach.Both;

        public static readonly LevelSpec[] Levels =
        {
            new LevelSpec("Bến Gom", 15, 2, 100, "Dẫn chai vào sát bến, hoặc thu gom tại ô. Đặt cây sớm ở hàng có cảnh báo."),
            new LevelSpec("Vườn Rễ", 22, 2, 65, "Cắt cước và cứu cá dưới nước. Thử Rễ Neo chặn từ tầng dưới sang mặt nước."),
            new LevelSpec("Dòng Đảo", 29, 3, 55, "Kiện nổi có thể đổi tầng. Dùng gương để nâng đường bắn qua ô tương ứng."),
            new LevelSpec("Trạm Xung", 34, 3, 48, "Pin có thể thu hồi hoặc xả xung. Ghép cây cùng cột để giữ hai tầng."),
            new LevelSpec("Cửa Đê", 40, 3, 50, "Gỡ lưới, xử lý van và ngăn các Bè Neo ở cả hai tầng.")
        };

        public static readonly AllySpec[] Allies =
        {
            new AllySpec("Mầm Vỗ", S,L,Ability.Shot, 4,5,1,2,"Bắn hạt dọc hàng mặt nước."),
            new AllySpec("Đệm Bấc", S,L,Ability.Guard, 3,12,1,1,"Chặn váng rác ở mặt nước, phản một sát thương khi bị cắn."),
            new AllySpec("Bụi Bạc", B,L,Ability.Income, 3,5,1,1,"Tích năng lượng theo nhịp, ở tầng nào cũng dùng được."),
            new AllySpec("Cá Vệt", D,L,Ability.Shot, 4,6,1,2,"Cá dưới nước bắn bọt dọc hàng. Cá được cứu sẽ vào chỗ cước."),
            new AllySpec("Rễ Neo", D,O,Ability.Slow, 5,7,2,2,"Rễ dưới nước làm chậm địch ở hàng tương ứng trên mặt."),
            new AllySpec("Cá Gõ", D,L,Ability.Zap, 5,6,2,3,"Đánh nhanh, có thể tháo lớp vỏ."),
            new AllySpec("Sen Bơm", S,O,Ability.Repair, 4,5,2,3,"Chữa đồng minh ở ô tương ứng dưới nước."),
            new AllySpec("Sậy Nặng", B,L,Ability.Pierce, 6,6,2,2,"Xuyên qua hai đối thủ gần nhất trong hàng."),
            new AllySpec("Cá Ống", D,O,Ability.Shot, 5,6,3,3,"Bắn lên hàng đối diện từ dưới nước."),
            new AllySpec("Bèo Kéo", S,L,Ability.Pull, 5,6,3,2,"Đẩy địch lùi một đoạn, có lực khi rác đi qua."),
            new AllySpec("Rong Xi", D,L,Ability.Guard, 4,17,3,2,"Neo đáy chắc và phản đòn mạnh."),
            new AllySpec("Cỏ Gương", B,X,Ability.Reflect, 5,5,3,2,"Bắn hai tầng nếu có gương trong một trong hai ô tương ứng."),
            new AllySpec("Cá Mạch", D,X,Ability.Pulse, 7,7,4,3,"Phát xung gây sát thương trong cả hai hàng cùng số."),
            new AllySpec("Lá Chài", S,O,Ability.Slow, 5,7,4,3,"Hãm địch dưới nước từ mặt nước."),
            new AllySpec("Cặp Tảo", B,X,Ability.Link, 6,7,4,3,"Bắn qua tầng nếu có đồng minh ở ô ghép."),
            new AllySpec("Cá Lọc", D,L,Ability.Clean, 5,10,4,2,"Dọn váng dầu gần ô đứng và bắn bọt."),
            new AllySpec("Lộc Van", S,X,Ability.Echo, 8,7,5,4,"Xung xuyên tầng với công suất cao nhờ van ở ô ghép."),
            new AllySpec("Cá Dẫn", D,O,Ability.Pierce, 7,8,5,4,"Xuyên hai địch mặt nước từ đáy."),
            new AllySpec("Cỏ Bọt", B,L,Ability.Habitat, 6,9,5,3,"Trên mặt bắn mạnh. Dưới nước làm chậm cả hàng."),
            new AllySpec("Cá Thợ", D,X,Ability.Repair, 6,9,5,4,"Vá đồng minh ở hai ô cùng hàng và cột.")
        };

        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("Váng Quệt",S,Trait.Skim,7,.37f,1,"Lướt nhanh qua dầu."),
            new EnemySpec("Túi Chộp",S,Trait.School,6,.45f,1,"Tăng tốc khi có đồng loại cùng hàng."),
            new EnemySpec("Mạt Cào",D,Trait.Burrow,8,.35f,2,"Đi thấp, đỡ đòn hạt thường."),
            new EnemySpec("Lon Kén",B,Trait.Shell,11,.30f,2,"Vỏ giảm sát thương cho tới khi gặp xung hoặc bị đập."),
            new EnemySpec("Bè Hai Mạn",B,Trait.Ferry,12,.34f,3,"Đổi tầng một lần giữa đường, giữ nguyên hàng."),
            new EnemySpec("Lọc Bẩn",D,Trait.Filter,11,.32f,3,"Hấp thu bọt yếu và hồi máu theo nhịp."),
            new EnemySpec("Phao Hắc",S,Trait.Spill,13,.33f,4,"Bị hạ để lại váng dầu trên ô."),
            new EnemySpec("Ống Rè",D,Trait.Jam,14,.30f,4,"Làm đồng minh ô đối diện bắn chậm khi chạy qua."),
            new EnemySpec("Thùng Húc",B,Trait.Ram,17,.35f,4,"Đòn cắn mạnh khi gặp tường chắn."),
            new EnemySpec("Bầy Rách",B,Trait.Split,15,.35f,5,"Vỡ ra một sinh vật nhỏ khi bị hạ."),
            new EnemySpec("Cánh Lướt",B,Trait.Drift,12,.53f,5,"Đổi sang hàng kề khi sắp gặp phòng thủ."),
            new EnemySpec("Bè Neo",B,Trait.Anchor,29,.23f,5,"Kháng đẩy lùi và tăng tốc đồng đội cùng tầng.")
        };

        public static readonly TrashSpec[] Trash =
        {
            new TrashSpec("Chai Phao",S,TrashEffect.Float,3,1,"Đẩy vào bến nhận gấp đôi. Thu ngay chỉ nhận phần nhỏ."),
            new TrashSpec("Mảnh Vỏ",B,TrashEffect.Shell,2,1,"Thu lấy năng lượng hoặc chuyển thành vật chắn tạm."),
            new TrashSpec("Dây Cước",D,TrashEffect.Filament,2,2,"Cắt dây để cứu cá nếu ô còn trống. Để lâu sẽ giữ chậm robot."),
            new TrashSpec("Cục Chì",D,TrashEffect.Ballast,3,2,"Thu dọn ô đáy hoặc neo vật chắn lên ô mặt nước tương ứng."),
            new TrashSpec("Mảnh Gương",B,TrashEffect.Mirror,2,3,"Dựng để dẫn đòn bắn qua tầng, hoặc thu lấy năng lượng."),
            new TrashSpec("Kiện Nổi",B,TrashEffect.Capsule,3,3,"Chuyển sang ô tương ứng tầng kia để mở chỗ đặt cây."),
            new TrashSpec("Pin Rò",B,TrashEffect.Battery,2,4,"Thu pin hoặc xả xung tháo giáp ở hai ô tương ứng."),
            new TrashSpec("Váng Dầu",S,TrashEffect.Spill,2,4,"Váng cản robot nhưng giúp Váng Quệt tăng tốc. Gạt để dọn vùng."),
            new TrashSpec("Van Khí",D,TrashEffect.Vent,3,5,"Giữ lại và mở van tạo xung xuyên tầng mỗi vài giây."),
            new TrashSpec("Lưới Cuộn",B,TrashEffect.Loom,3,5,"Giăng sang tầng đối diện làm chậm hàng, hoặc gỡ lấy năng lượng.")
        };

        public static bool Fits(Habitat habitat, Layer layer) =>
            (habitat & (layer == Layer.Surface ? S : D)) != 0;

        public static Layer Opposite(Layer layer) => layer == Layer.Surface ? Layer.Deep : Layer.Surface;
    }
}
