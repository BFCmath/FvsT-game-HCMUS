# Bio-Bot: Đê Xanh Cảm Tử

Tên tạm cho một game phòng thủ theo làn 2D bằng Unity. Người chơi điều khiển robot bảo vệ bờ đê trên hai bàn 5 × 9, một trên mặt nước và một dưới nước. Bàn đang chơi hiển thị lớn, bàn kia xuất hiện trên bản đồ nhỏ. Robot dọn rác và trồng cây. Cá được cứu trở thành đồng minh tại cột tương ứng. Cây có thể hỗ trợ ô tương ứng ở tầng kia.

## Trạng thái

- Đã đọc đề và thống nhất hướng gameplay hai tầng cùng Bio-Bot có máu riêng.
- Unity Hub và Unity 6.3 LTS `6000.3.16f1` bản ARM64 đã được cài trên Mac Apple Silicon. Đây là phiên bản lớp yêu cầu.
- Giấy phép Unity Personal đang hoạt động. Đã chấp nhận Unity Editor Software Terms và xác nhận giao diện Editor mở được project tạm.
- [BioBot](BioBot/) là project Unity tạo từ mẫu Universal 2D. Scene [A1Nursery](BioBot/Assets/Scenes/A1Nursery.unity) có hai bàn 5 × 9, Bio-Bot, chai nổi và cảnh vật chuyển động.
- Mở **BioBot** bằng `6000.3.16f1`, chọn `Assets/Scenes/A1Nursery.unity` và bấm **Play**. Nhấp vào tab Game để nhận phím, dùng `WASD` hoặc phím mũi tên để đi cùng tầng. Từ điểm xuất phát, đi phải để đẩy chai, bấm `Q` để lặn và `E` để nổi.
- Bản chơi thử A1 đã qua kiểm tra Play Mode. Video nộp bài của nhóm 04 đã sẵn sàng. [Nhom04-A1.mp4](Nhom04-A1.mp4)

## Chơi chiến dịch

Mở [BioBotCampaign](BioBot/Assets/Scenes/BioBotCampaign.unity) trong Unity và bấm **Play**, sau đó nhấp vào Game. Bến Gom mở đầu chiến dịch. Hoàn thành màn để mở màn tiếp theo và nhóm đồng minh mới.

`WASD` hoặc phím mũi tên để chạy, `Q/E` để đổi tầng, `1–4` chọn thẻ, `Space` đặt, `Z/X` lật nhóm thẻ. `F` thu rác hoặc cứu cá, `G` dùng công dụng tại chỗ, `R` phát xung gần robot. Mở **Sổ sinh thái** để xem công dụng và tầng của từng loại. [Cách chơi và mở khóa](docs/campaign.md)

## Tiếp tục ở phiên sau

| Tệp | Nội dung |
| --- | --- |
| [PLAN.md](PLAN.md) | Việc đã làm, việc tiếp theo và điều kiện hoàn thành từng mốc. |
| [Yêu cầu bài tập](docs/requirements.md) | Yêu cầu A1, tình trạng A2 và tiêu chí giữa kỳ, kèm nguồn. |
| [Thiết kế gameplay](docs/game-design.md) | Cách hiểu hiện tại, tình huống chơi và các lựa chọn cần chốt. |
| [Chiến dịch](docs/campaign.md) | Cách chơi năm màn, mở khóa và vai trò của rác. |
| [Nhật ký dự án](docs/project-log.md) | Ghi nhận theo phiên và các quyết định cần giữ khi tiếp tục. |

Khi tiếp tục, đọc `PLAN.md` và mục mới nhất trong nhật ký trước. Cập nhật trạng thái, các quyết định đã chốt và việc tiếp theo ngay trong phiên có thay đổi.
