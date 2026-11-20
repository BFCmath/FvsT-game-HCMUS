# Thiết kế gameplay Bio-Bot

**Trạng thái** · Hai tầng, sức khỏe riêng của Bio-Bot và cá đồng minh đã được đưa vào campaign. [Thiết kế chiến dịch](campaign.md) ghi vòng chơi, điều khiển, cách mở khóa và những tương tác đã triển khai.

## Hướng đã thống nhất

- Sân chơi có hai tầng, mỗi tầng gồm 5 hàng × 9 cột. Giai đoạn đầu, đợt tấn công chỉ nhắm tầng mặt nước. Về sau, cả hai tầng đều có thể bị tấn công.
- Bio-Bot di chuyển giữa hai tầng để phòng thủ, dọn rác, giải cứu hoặc làm các công việc sinh thái. Những việc này có ích cho phòng tuyến, chẳng hạn cấp năng lượng để trồng thêm hoặc tạo lợi ích cho một hàng.
- Rác có vai trò trong cả việc dọn dẹp lẫn phòng thủ. Từng loại cần hành vi vật lý, cách xử lý và hệ quả dễ nhận ra.
- Các ô trên và dưới có thể hỗ trợ nhau. Cây có thể đổi vai trò theo tầng đặt hoặc hỗ trợ một cây ở ô tương ứng. Cá sau khi được cứu ở lại làm đồng minh và hỗ trợ phòng thủ tại cột tương ứng.
- Chọn hướng Action số 1 trong bản ý tưởng. Bio-Bot có máu riêng, chạm quái bị mất máu và hất lùi, hết máu thì hồi sinh sau một quãng ngắn tại bờ đê. Mốc 5 giây là giá trị đề xuất để thử. Máu Bio-Bot và độ bền phòng tuyến phục vụ hai mục đích khác nhau.

## Bố cục 5 × 9 × 2

Hiển thị toàn bộ bàn 5 × 9 đang chơi, nhìn từ trên xuống. Bản đồ nhỏ bên phải hiển thị 5 × 9 ô của tầng kia và đánh dấu ô tương ứng với vị trí Bio-Bot. Hai tầng dùng chung hệ tọa độ hàng và cột. Ví dụ, lặn từ hàng 3 cột 4 đưa Bio-Bot đến hàng 3 cột 4 dưới nước.

- `WASD` và phím mũi tên điều khiển di chuyển liên tục trong mặt phẳng của tầng, qua các ô.
- `Q` lặn và `E` nổi. Có nút đổi tầng trên giao diện. Khoảng chuyển hình ngắn giữ nguyên vị trí tương ứng và vật thể của từng tầng.
- Màu nền và nhãn phân biệt hai tầng. Bản đồ nhỏ hiện vị trí chai trên mặt nước khi người chơi đang lặn. Khi có đợt tấn công, bản đồ nhỏ có thể hiển thị làn nguy hiểm để người chơi quyết định lúc nào cần chuyển tầng.

## Một đoạn chơi để đối chiếu

1. Đầu màn, rác chỉ trôi vào bàn mặt nước. Bio-Bot kéo một chai nổi về điểm gom và nhận năng lượng, rồi trồng cây mặt nước để chặn đợt rác tiếp theo.
2. Khi cây đang giữ bàn mặt nước, Bio-Bot lặn xuống gỡ một vòng nhựa mắc vào cá. Cá ở lại làm đồng minh ở cột đó. Một hành vi để thử là cá làm chậm rác đi qua ô dưới cùng cột.
3. Sau đó rác bắt đầu tràn vào cả hai tầng. Người chơi có thể lặn để cứu cá hoặc củng cố tầng dưới trong lúc cây giữ tầng trên. Nếu trì hoãn quá lâu, tầng dưới chịu áp lực. Nếu tiếp xúc với quái, robot mất máu, bị hất lùi và có thể mất một khoảng thời gian hoạt động.

Vòng chơi thử là đọc áp lực ở hai tầng, chọn một công việc gần nhất có ích, nhận phần thưởng hoặc thay đổi tình trạng sân, rồi dùng lợi thế đó để củng cố phòng tuyến. Cây tạo thời gian để robot xuống nước. Công việc dưới nước tạo lợi thế để robot quay lên giữ hàng trên. Cú húc vẫn là một cách xử lý vật thể và can thiệp khi cần.

## Ví dụ để thử

| Tình huống | Hành động của Bio-Bot | Hệ quả có thể kiểm chứng |
| --- | --- | --- |
| Chai nổi trôi về bờ | Dùng lực đẩy hoặc dẫn chai vào điểm gom. | Dọn đường ở hàng trên và nhận năng lượng để trồng. |
| Vòng nhựa mắc vào cá | Lặn xuống, đến gần và gỡ. | Cá được giải cứu và trở thành đồng minh phòng thủ tại cột tương ứng. |
| Lon nặng chìm | Canh vị trí và đẩy lon ra khỏi ô dưới. | Mở lại ô đặt cây dưới nước, đồng thời khôi phục cơ hội ghép với ô trên. |
| Cây có hai vai trò | Trồng cùng một loại ở một trong hai tầng. | Trên mặt nước cây bắn dọc hàng. Dưới nước cây giảm tốc rác đi qua ô, hoặc tiếp sức cho cây phía trên cùng cột. Hai biến thể dưới nước là lựa chọn thử. |

## Quyết định còn mở

- Chốt kỹ năng của cá đồng minh. Thử cho cá làm chậm rác đi qua ô dưới. Một kỹ năng khác có thể hỗ trợ cây ở ô trên cùng cột.
- Thử tầng mặt nước là nơi chịu đợt đầu, tầng dưới có công việc sinh thái ngay từ đầu. Cần chọn mốc bắt đầu tạo áp lực ở cả hai tầng.
- Thử cấp năng lượng khi đưa rác về điểm gom. Thời gian hiệu lực và các tương tác khác của cá đồng minh còn mở.
- Chơi thử phím chuyển tầng Q / E và chọn phím tương tác, trồng cây. Tên game và thông số sức khỏe, hồi sinh, chi phí cây vẫn tạm thời.

## Phạm vi thử đầu tiên

A1 dùng hai bàn 5 × 9 và điều khiển liên tục trong mỗi bàn để thử chuyển động, hai animation robot, hai animation cảnh vật và va chạm. Chai nổi chỉ dịch chuyển theo chiều ngang trong bản thử. Công việc cứu cá, sức khỏe, đặt cây, hệ thống đợt và hỗ trợ giữa tầng thuộc bản thử gameplay tiếp theo. Đề A2 chính thức sẽ quyết định phần nào của bản thử được đưa vào bài nộp A2.
