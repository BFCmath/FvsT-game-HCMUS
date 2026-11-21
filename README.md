# Bio-Bot: Đê Xanh Cảm Tử

Game phòng thủ theo làn bằng Unity. Người chơi điều khiển Bio-Bot bảo vệ bờ đê trên hai bàn 5 × 11 hiển thị đồng thời. Robot thu gom rác, cứu cá và đặt đồng minh. Một số đồng minh và thiết bị hỗ trợ ô tương ứng ở tầng kia.

## Trạng thái

- Đã đọc đề và thống nhất hướng gameplay hai tầng cùng Bio-Bot có máu riêng.
- Unity Hub và Unity 6.3 LTS `6000.3.16f1` bản ARM64 đã được cài trên Mac Apple Silicon. Đây là phiên bản lớp yêu cầu.
- Giấy phép Unity Personal đang hoạt động. Đã chấp nhận Unity Editor Software Terms và xác nhận giao diện Editor mở được project tạm.
- [BioBot](BioBot/) là project Unity tạo từ mẫu Universal 2D. Scene [A1Nursery](BioBot/Assets/Scenes/A1Nursery.unity) có hai bàn 5 × 9, Bio-Bot, chai nổi và cảnh vật chuyển động.
- Mở **BioBot** bằng `6000.3.16f1`, chọn `Assets/Scenes/A1Nursery.unity` và bấm **Play**. Nhấp vào tab Game để nhận phím, dùng `WASD` hoặc phím mũi tên để đi cùng tầng. Từ điểm xuất phát, đi phải để đẩy chai, bấm `Q` để lặn và `E` để nổi.
- Bản chơi thử A1 đã qua kiểm tra Play Mode. Video nộp bài của nhóm 04 đã sẵn sàng. [Nhom04-A1.mp4](Nhom04-A1.mp4)

## Chơi chiến dịch

Mở scene `BioBotCampaign` trong Unity 6.3 LTS và bấm **Play**. Chọn nhiệm vụ, xem chỉ dẫn rồi bấm **Triển khai**. Chiến dịch có 4 nhiệm vụ. Màn 01 có 2 đợt và thường mất khoảng 1 phút 20 giây. Các màn 02–04 có 3 đợt, với màn cuối mất khoảng 3 phút 10 giây. Mỗi tầng có 5 hàng × 11 cột. Từ màn 02, cả hai tầng hoạt động song song. [Scene BioBotCampaign](BioBot/Assets/Scenes/BioBotCampaign.unity)

Dùng **WASD** hoặc phím mũi tên để bơi, **Q** để chuyển qua lại giữa mặt nước và đáy biển. Chọn thẻ bằng chuột hoặc **1–5**, đặt bằng chuột hoặc **Space**. Bơi tới ô rác và **giữ F** để dựng khiên, thu gom, thả cá hoặc dọn Rác Cản. Chạm vào Chai Phao để đẩy.

Cả bàn của tầng bị đe dọa sáng đỏ với tam giác **Nguy Hiểm** khi địch đầu tiên xuất hiện ở tầng kia hoặc khi địch vượt qua 5 cột ngoài cùng bên phải. Cảnh báo hiện trong 4 giây. Làm theo nhắc phím dưới tam giác để bấm **Q**, chuyển tầng và tắt cảnh báo ngay.

**Rái Cá** tạo hạt +20 năng lượng mỗi 14 giây. Hạt tự nhiên cho +15 năng lượng mỗi 24 giây sau hạt đầu ở giây 13. **Can Đội Nắp** xuất hiện từ đợt cuối màn 01. Dồn hai Vịt cùng hàng, đẩy Chai Phao vào địch hoặc dùng **Rùa Khiên** để cầm chân từ màn 02. Rùa Khiên chỉ hấp thụ sát thương. [Cách chơi, các nhiệm vụ và đồ họa pixel](docs/campaign.md)

Thắng màn 03 để mở màn 04 và thẻ **Mực Kéo Tầng**. Mực là bẫy dùng một lần để đổi tầng địch. **Súng Rác** bắn **Rác Cản** vào ô trống trên tầng đang bơi. Giữ **F** trong 0,5 giây để mở lại ô.

## Tiếp tục ở phiên sau

| Tệp | Nội dung |
| --- | --- |
| [PLAN.md](PLAN.md) | Việc đã làm, việc tiếp theo và điều kiện hoàn thành từng mốc. |
| [Yêu cầu bài tập](docs/requirements.md) | Yêu cầu A1, tình trạng A2 và tiêu chí giữa kỳ, kèm nguồn. |
| [Thiết kế gameplay](docs/game-design.md) | Cách hiểu hiện tại, tình huống chơi và các lựa chọn cần chốt. |
| [Chiến dịch](docs/campaign.md) | Cách chơi bốn màn, mở khóa và vai trò của rác. |
| [Nhật ký dự án](docs/project-log.md) | Ghi nhận theo phiên và các quyết định cần giữ khi tiếp tục. |

Khi tiếp tục, đọc `PLAN.md` và mục mới nhất trong nhật ký trước. Cập nhật trạng thái, các quyết định đã chốt và việc tiếp theo ngay trong phiên có thay đổi.
