# Bio-Bot: Đê Xanh Cảm Tử

## Tổng quan và trạng thái chiến dịch

Bio-Bot là game phòng thủ theo làn kiểu Plants vs. Zombies trên hai tầng nước, mỗi tầng có **5 hàng × 11 cột**. Người chơi bơi giữa mặt nước và đáy biển để bảo vệ bờ đê, thu gom rác, cứu cá và bố trí đồng minh.

| Màn | Trọng tâm |
| --- | --- |
| 1 · Bến Gom Nắng Mai | Dùng **Rái Cá** tạo năng lượng và **Vịt Bắn Bọt** giữ làn. **Can Đội Nắp** xuất hiện ở đợt cuối. |
| 2 · Bức Tường & Đáy Sâu | Mở tầng đáy và dùng **Rùa Khiên** chặn địch. |
| 3 · Cá Xạ Thủ Đáy Biển | Cứu **Cá Xạ Thủ** và phòng thủ cả hai tầng. |
| 4 · Hải Lưu Đổi Tầng | Dùng **Mực Kéo Tầng** chuyển địch sang tầng kia. **Súng Rác** bắn Rác Cản vào ô trống. |

Cả bốn màn đều chơi được và mở khóa tuần tự. Màn 1 có hai đợt trên mặt nước. Màn 2–4 có ba đợt, với cả hai tầng hoạt động song song. [Chi tiết chiến dịch](docs/campaign.md)

## Bắt đầu nhanh cho đồng đội

1. Clone repo và vào thư mục vừa tải.

   ```sh
   git clone https://github.com/BFCmath/FvsT-game-HCMUS.git
   cd FvsT-game-HCMUS
   ```

2. Cài **Unity Hub** và **Unity 6.3 LTS (`6000.3.16f1`)**. Đây là bản chính xác lớp yêu cầu. [ProjectVersion.txt](BioBot/ProjectSettings/ProjectVersion.txt)
3. Trong Unity Hub, chọn **Add** và trỏ đến thư mục [BioBot/](BioBot/). Mở project bằng Editor vừa cài và đợi Unity import xong.
4. Trong cửa sổ **Project**, mở [BioBotCampaign.unity](BioBot/Assets/Scenes/BioBotCampaign.unity) từ `Assets/Scenes/`. Bấm **Play**, nhấp vào **Game View** để nhận phím, rồi chọn màn và bấm **Triển khai**.

### Điều khiển chiến dịch

| Phím hoặc thao tác | Tác dụng |
| --- | --- |
| `WASD` hoặc phím mũi tên | Bơi trong tầng hiện tại. |
| `Q` | Lặn hoặc nổi tại cùng ô khi tầng đáy đã mở. |
| `1`–`5` hoặc nhấp vào thẻ | Chọn đồng minh đã mở khóa. |
| Nhấp trái hoặc `Space` | Đặt đồng minh ở ô vừa nhấp hoặc ô Bio-Bot đang đứng. |
| Giữ `F` tại ô rác | Thu gom, dựng khiên, thả cá hoặc dọn Rác Cản tùy loại rác. |
| Bơi chạm **Chai Phao** | Đẩy chai về bến trái để lấy năng lượng hoặc sang phải để húc địch. |
| `Shift` | Lướt theo hướng đang nhìn. |
| `V` | Bật hoặc tắt chế độ gỡ đồng minh. Nhấp trái hoặc bấm `Space` để gỡ. |
| `P` | Bật hoặc tắt AI hỗ trợ ở tầng hiện tại. |
| `Esc` | Tạm dừng, tiếp tục hoặc đóng cửa sổ đang mở. |

**Bản A1** dùng scene [A1Nursery.unity](BioBot/Assets/Scenes/A1Nursery.unity) với bàn 5 × 9 mỗi tầng, `Q` để lặn và `E` để nổi. [Video A1 của nhóm 04](Nhom04-A1.mp4)

## Hướng dẫn cho AI Agents

### Đường dẫn và cách chạy

| Thành phần | Đường dẫn tính từ gốc repo |
| --- | --- |
| Project Unity | [`BioBot/`](BioBot/) |
| Scene chiến dịch | [`BioBot/Assets/Scenes/BioBotCampaign.unity`](BioBot/Assets/Scenes/BioBotCampaign.unity) |
| Scene A1 | [`BioBot/Assets/Scenes/A1Nursery.unity`](BioBot/Assets/Scenes/A1Nursery.unity) |

Chạy từ gốc repo. Các lệnh dùng đường dẫn cài đặt mặc định của Unity Hub.

**macOS**

```sh
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -projectPath "$PWD/BioBot"
```

**Windows (PowerShell)**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -projectPath (Join-Path (Get-Location) 'BioBot')
```

Trong Editor, mở scene chiến dịch, bấm **Play** và nhấp vào **Game View** trước khi gửi phím `WASD`, `Q`, `1`–`5`, `Space` hoặc `F`. Kiểm tra màn 1 nhận phím di chuyển và đặt được đồng minh. Từ màn 2, kiểm tra `Q` đổi tầng. Ở màn 4, thử giữ `F` để dọn Rác Cản do Súng Rác tạo ra.

**Mô phỏng headless.** `CampaignMatch` là logic C# độc lập với scene và vòng đời `MonoBehaviour`. Để kiểm tra luật và cân bằng ngoài Editor, biên dịch harness cùng `CampaignMatch.cs`, `CampaignCatalog.cs` và `CampaignProgress.cs` bằng Roslyn đi kèm Unity (`dotnet csc.dll`), nhắm các assembly tương thích Mono, rồi chạy harness bằng `mono`. Harness có thể dùng stub cho API `UnityEngine` mà mã gọi, như `Mathf`, các kiểu vector và API lưu tiến độ. Khởi tạo match theo màn, gọi `Tick` với bước thời gian cố định và kiểm tra kết quả.

### Bản đồ mã nguồn

| Tệp | Trách nhiệm |
| --- | --- |
| [`CampaignCatalog.cs`](BioBot/Assets/Campaign/CampaignCatalog.cs) | Nguồn chuẩn cho cấu hình màn, các đợt `WaveSpec` và từng tốp địch, đồng minh, địch và rác. |
| [`CampaignMatch.cs`](BioBot/Assets/Campaign/CampaignMatch.cs) | Mô phỏng và luật chơi bằng C#. |
| [`BioBotCampaign.cs`](BioBot/Assets/Campaign/BioBotCampaign.cs) và [`BioBotCampaignPresentation.cs`](BioBot/Assets/Campaign/BioBotCampaignPresentation.cs) | Tích hợp Unity, xử lý input, HUD và render. |
| [`build_campaign_pixels.py`](BioBot/ArtSource/build_campaign_pixels.py) | Script Pillow tạo `TidalRoster.png` và `TidalSilhouettes.png`. |

### Quy tắc khi sửa chiến dịch

- Đặt khác biệt giữa các màn trong cấu hình `CampaignCatalog.cs`. Không thêm nhánh `if (Level == ...)` để ghi đè luật riêng cho từng màn.
- Khi đổi dữ liệu hoặc luật chơi, đồng bộ `CampaignCatalog.cs`, **Sổ Sinh Thái** trong game và [`docs/campaign.md`](docs/campaign.md).
- Khi tiếp tục công việc, đọc [PLAN.md](PLAN.md) và mục mới nhất trong [nhật ký dự án](docs/project-log.md).

## Tài liệu dự án

| Tệp | Nội dung |
| --- | --- |
| [PLAN.md](PLAN.md) | Tiến độ, việc tiếp theo và tiêu chí hoàn thành. |
| [Yêu cầu bài tập](docs/requirements.md) | Yêu cầu A1, A2 và tiêu chí giữa kỳ. |
| [Thiết kế gameplay](docs/game-design.md) | Cơ chế chơi và các lựa chọn thiết kế. |
| [Chiến dịch](docs/campaign.md) | Các màn, đợt địch, mở khóa và đồ họa pixel. |
| [Nhật ký dự án](docs/project-log.md) | Quyết định và ghi nhận theo phiên. |
