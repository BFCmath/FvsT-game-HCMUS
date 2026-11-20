# Kế hoạch triển khai

## Phiên đầu

- [x] Cài và mở Unity Hub trên Mac Apple Silicon.
- [x] Xác nhận phiên bản Editor lớp yêu cầu là Unity 6.3 LTS `6000.3.16f1`.
- [x] Đối chiếu yêu cầu A1 do người dùng gửi với tiêu chí giữa kỳ trong PDF, ghi riêng tình trạng đề A2.
- [x] Ghi bản nháp gameplay Bio-Bot để cùng duyệt qua vài lượt trao đổi.
- [x] Cài Unity Editor `6000.3.16f1` bản ARM64 và kiểm tra bộ cài trong danh sách Editor.
- [x] Xác nhận giấy phép Unity Personal và kiểm tra Editor tạo, mở lại project tạm ở batch mode.
- [x] Hoàn tất hộp thoại Unity Editor Software Terms của `6000.3.16f1` và xác nhận Editor mở project qua giao diện.
- [x] Chốt cấu trúc hai bàn 5 × 9, một trên mặt nước và một dưới nước và chọn hướng Action có máu riêng cho Bio-Bot.
- [x] Chốt cá được giải cứu trở thành đồng minh tại cột tương ứng.
- [ ] Chốt kỹ năng của cá đồng minh, tương tác trồng theo cặp ô và tinh chỉnh chuyển tầng sau khi chơi thử. Các phương án nằm trong [thiết kế gameplay](docs/game-design.md).

## A1 — Màn vườn ươm

- [x] Tạo project `BioBot` từ mẫu Universal 2D trong Unity `6000.3.16f1`, chuẩn bị scene `A1Nursery`.
- [x] Dựng Bio-Bot điều khiển bằng WASD hoặc phím mũi tên và hai animation `Idle` / `Move`.
- [x] Dựng hai bàn 5 × 9 với gợn nước và rong đung đưa. Hiển thị bàn đang chơi cùng bản đồ nhỏ của bàn kia, chuyển tầng với Q / E.
- [x] Dùng `Rigidbody2D` và collider để Bio-Bot đẩy chai dọc mặt nước và bị biên màn chặn lại.
- [x] Kiểm tra trong Play Mode với input bàn phím mô phỏng. Xác minh robot đi các hướng, đổi animation, đẩy chai, dừng ở biên và hai animation môi trường chuyển frame.
- [x] Quay và kiểm tra 30 giây Play Mode trong bố cục Editor thường, giữ Hierarchy rõ. Video thể hiện animation, đi bốn hướng, đẩy chai, chạm biên và đổi tầng.
- [x] Xác nhận nhóm 04 và xuất [Nhom04-A1.mp4](Nhom04-A1.mp4).
- [ ] Người dùng nộp `Nhom04-A1.mp4` lên Moodle.

## Campaign — Năm màn

- [x] Lên danh sách 12 đối thủ, 10 loại rác, 20 đồng minh theo môi trường và cơ chế. Mỗi màn mở thêm 4 đồng minh và các loại rác, đối thủ mới.
- [x] Dựng vòng chơi trong scene campaign riêng. Nối menu, phòng thủ, cảnh báo từ bản đồ nhỏ, mất máu/hồi sinh, trạng thái thắng/thua và lưu mở khóa.
- [x] Dựng animation sprite cho danh sách thực thể và hiệu ứng bắn/xung.
- [x] Kiểm tra thắng cả năm màn bằng mô phỏng, một trường hợp thua, tương tác rác và qua tầng. Xác nhận trong Play Mode rằng thắng màn đầu lưu mở khóa và chuyển được sang màn hai. [Thiết kế chiến dịch](docs/campaign.md)
- [ ] Đối chiếu đề A2 chính thức khi có để xác định nội dung và hình thức nộp.

## Giữa kỳ — Hoàn thiện và đóng gói

- Đưa game về vòng chơi hoàn chỉnh từ menu đến kết quả. Bao phủ các tiêu chí cơ bản trong [yêu cầu bài tập](docs/requirements.md), gồm tối thiểu hai màn hoặc hai chế độ.
- Theo dõi nguồn đồ họa ngay khi thêm asset. Dùng đồ họa đủ điều kiện giữa kỳ cho các màn đưa vào bài nộp.
- Cài module Windows Build Support phù hợp sau khi chốt phiên bản Editor. Kiểm tra trọn bộ build trên máy Windows và kiểm tra project mở lại từ thư mục nộp.
- Chọn poster hoặc trailer. Hoàn thiện báo cáo, demo theo nền tảng, `Readme.txt` và gói `NhomXX-GK.zip` hoặc `.rar`. Kiểm tra hạn nộp trên Moodle.
- Sau khi đạt yêu cầu cơ bản, cân nhắc lần lượt chủ đề môi trường gắn với gameplay, AI đổi làn, cửa hàng, thành tích, autoplay và Android + Web. Chỉ đưa vào lịch các mục nhóm chọn.
