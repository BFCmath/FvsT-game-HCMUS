# Chiến dịch Bio-Bot

Mỗi tầng có 5 hàng và 9 cột. Bio-Bot đứng ở một tầng tại một thời điểm. Các đối thủ, đồng minh và thiết bị ở tầng kia tiếp tục hoạt động. Bản đồ nhỏ dùng màu cam cho rác, xanh cho đồng minh và đỏ cho địch. Cảnh báo báo tầng và hàng của lượt địch sắp vào.

## Cách chơi

| Phím | Tác dụng |
| --- | --- |
| WASD hoặc mũi tên | Di chuyển trong bàn đang xem |
| Q / E | Lặn hoặc nổi ở cùng hàng và cột |
| 1–4 | Chọn đồng minh trên thanh hiện tại |
| Z / X | Chuyển sang nhóm thẻ trước hoặc tiếp theo |
| Space | Đặt đồng minh vào ô đang đứng |
| F | Thu rác, tháo thiết bị hoặc cắt cước để cứu cá |
| G | Dùng công dụng tại chỗ của loại rác đang đứng |
| R | Xả xung vào địch gần Bio-Bot, tốn một năng lượng |
| Esc | Về danh sách màn |

Cửa đê mất điểm khi địch lọt mép trái. Dọn hết các đợt để mở màn sau. Bio-Bot có sức khỏe riêng, gục khi va địch nhiều lần và hồi ở bến sau bốn giây. Đồng minh cần đúng môi trường, đủ năng lượng và ô trống. Sổ sinh thái trong game có mô tả khả năng, tầng và mốc mở của cả 42 loại thực thể.

## Mở khóa theo màn

| Màn | Mới trong màn | Tình huống cần xử lý |
| --- | --- | --- |
| 1. Bến Gom | 4 cá/cây, Váng Quệt, Túi Chộp, chai và mảnh vỏ | Giữ mặt nước. Chai thu nhanh được 1 năng lượng. Đưa đến bến trái được 6. |
| 2. Vườn Rễ | 4 cá/cây, Mạt Cào, Lon Kén, cước và cục chì | Cứu Cá Vệt ở đáy. Rễ Neo hãm địch mặt nước và cục chì dựng chắn ở ô trên. |
| 3. Dòng Đảo | 4 cá/cây, Bè Hai Mạn, Lọc Bẩn, gương và kiện nổi | Địch đổi tầng giữa đường. Dùng gương xuyên tầng, hoặc chuyển kiện để lấy chỗ trồng. |
| 4. Trạm Xung | 4 cá/cây, Phao Hắc, Ống Rè, Thùng Húc, pin và dầu | Xả pin tháo giáp ở hai tầng. Cặp Tảo cần đồng minh đúng ô đối diện để hỗ trợ nhau. |
| 5. Cửa Đê | 4 cá/cây, Bầy Rách, Cánh Lướt, Bè Neo, van và lưới | Dùng van tăng lực xuyên tầng, giăng lưới để mua thời gian và chặn địch đổi hàng. |

## Vai trò của rác

Các loại rác tạo một quyết định giữa thu năng lượng, mở ô, hoặc giữ hiệu ứng đang có. Thiết bị đang dùng vẫn chiếm ô của nó. Cục chì ở hàng 2 cột 5 chẳng hạn có thể bỏ đi để trồng cây dưới nước, hoặc dùng ngay để dựng vật chắn tạm ở hàng 2 cột 5 mặt nước. Van khí ở dưới có thể tạo xung cả hai tầng và tăng lực cho Lộc Van ở ô trên. Cỏ Gương và Cặp Tảo tận dụng ô tương ứng theo hai cách khác nhau. Phao Hắc để lại váng khi bị hạ, váng làm chậm Bio-Bot và tăng tốc Váng Quệt, còn Cá Lọc có thể dọn váng gần chỗ đứng.

Các file runtime của campaign đặt trong `Assets/Campaign`. A1 giữ scene và clip nộp riêng. Đồ họa campaign là sprite pixel art nguyên bản, mỗi loại có bốn frame cùng hiệu ứng đánh trúng, xung, đường bắn và chuyển tầng được điều khiển trong game.
