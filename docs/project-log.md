# Nhật ký dự án

## 20/11/2026 — Phiên đầu

- Workspace bắt đầu từ repo Git rỗng. Tạo tài liệu để tiếp tục qua các phiên. Chưa tạo scene, tài nguyên hay mã Unity.
- Cài Unity Hub bản Apple Silicon từ CDN Unity, kiểm tra chữ ký Unity Technologies SF và mở Hub. Tải `~/Downloads/Unity-6000.3.29f1.pkg` và xác minh chữ ký của bộ cài. Chưa cài Editor vì đề giữa kỳ dẫn chiếu phiên bản thống nhất trên lớp mà chưa ghi số cụ thể.
- Nguồn yêu cầu gồm thông báo A1 do người dùng chép nguyên văn và PDF `MiniProject2D-CNTN23.pdf` gồm năm trang. Bản ý tưởng Bio-Bot có lộ trình A2 dự kiến. Chờ đề A2 gốc để chốt mốc đó.
- Ghi lại một giả thuyết gameplay để cùng duyệt. Cây tự giữ làn, robot chạy để đặt cây và nhặt năng lượng, cú húc giúp cứu làn hoặc tận dụng bẫy. Tần suất cú húc, nhịp sóng và thao tác trồng cây vẫn mở.
- Phiên tiếp theo ưu tiên hỏi về phiên bản Unity của lớp và đối chiếu tình huống gameplay mẫu. Sau khi có phiên bản Editor, xác nhận mở được trước khi tạo project A1.

## 20/11/2026 — Xác nhận phiên bản và thiết kế hai tầng

- Người dùng xác nhận lớp yêu cầu Unity 6.3 LTS `6000.3.16f1`. Đã cài bản Apple Silicon vào `/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app`. Unity CLI nhận diện đúng phiên bản, xác minh file Editor và kiến trúc ARM64.
- Editor xác nhận phiên bản `6000.3.16f1` khi chạy `-version`. Lần thử mở project tạm dừng vì chưa có giấy phép Unity hợp lệ. Đăng nhập Hub, kích hoạt giấy phép, rồi thử mở lại.
- Người dùng chọn kiểu Action số 1 từ bản ý tưởng thứ hai. Bio-Bot có máu riêng và có thể bị hất lùi, gục, rồi hồi sinh.
- Thay bố cục năm hàng đề xuất bằng hai hàng cùng cột, trên mặt nước và dưới mặt nước. Đầu màn chỉ một hàng chịu tấn công, sau đó cả hai có thể chịu áp lực. Robot dọn các loại rác bằng tương tác riêng và làm công việc sinh thái dưới nước, ví dụ cứu cá, để nhận lợi ích phòng thủ. Cây hoặc sinh vật có thể ảnh hưởng ô tương ứng ở tầng kia.
- Còn mở ý nghĩa cụ thể của phần thưởng cứu cá, cách chuyển tầng và nhịp xuất hiện đợt tấn công dưới nước. Bản thiết kế ghi một đoạn chơi thử để cùng đối chiếu.

## 20/11/2026 — Cá đồng minh và kiểm tra lại Unity

- Người dùng xác nhận cá được cứu ở lại làm đồng minh và hỗ trợ phòng thủ tại cột tương ứng. Kỹ năng cụ thể của cá sẽ được chọn khi thử vòng chơi hai tầng.
- Kiểm tra lại sau khi người dùng bấm đồng ý điều khoản. Editor đúng phiên bản vẫn khởi chạy, nhưng lần tạo project tạm dừng với thông báo chưa có giấy phép hợp lệ. Unity CLI cũng chưa thấy phiên đăng nhập hoặc giấy phép đang hoạt động. Mở lại Hub để người dùng đăng nhập và kiểm tra mục Licenses.
- Người dùng gửi ảnh đã đăng nhập Hub. Kiểm tra mới xác nhận Unity Personal đang hoạt động. Tạo và mở lại project tạm với `6000.3.16f1` ở batch mode đều hoàn thành import và thoát thành công. Lần mở giao diện nhận giấy phép và đang dừng ở hộp thoại Unity Editor Software Terms dành cho Editor.

## 20/11/2026 — Project A1

- Sau khi người dùng chấp nhận Unity Editor Software Terms, project tạm mở qua giao diện và hoàn thành import.
- Tạo project `BioBot` bằng mẫu Universal 2D từ Unity `6000.3.16f1`. Đặt tên scene khởi đầu `A1Nursery`, giữ GUID của scene và đưa vào cấu hình build. Thêm quy tắc Git cho cache và dữ liệu cục bộ của Unity.
- Mở `BioBot` bằng giao diện Editor, xác nhận dự án hoàn thành import và biên dịch, gồm scene `A1Nursery`.
- Scene hiện dùng camera trực giao và đèn 2D từ mẫu. Mốc tiếp theo là dựng mặt cắt hai tầng cùng Bio-Bot, chuyển động, animation và va chạm cho A1.

## 20/11/2026 — Bản chơi thử A1

- Dựng khung cảnh mặt cắt bờ đê, Bio-Bot, chai nổi, gợn nước và rong bằng sprite pixel art tự vẽ cho project. Sprite import dạng Point, dùng vật liệu Unlit để giữ màu. Tắt SRP Batcher trên asset URP của project vì lần kiểm tra render trên Mac cho thấy sprite lấy nhầm texture khi bật.
- `BioBotMovement` đọc phím từ Input System, chuẩn hóa chuyển động chéo và đặt vận tốc qua `Rigidbody2D` trong `FixedUpdate`. Animator của robot chuyển giữa hai clip `Idle` và `Move`. Chai là vật thể động khóa trục Y. Collider biên giới hạn vùng chơi.
- Chạy Play Mode trên bản sao bằng Unity `6000.3.16f1`. Kiểm tra phím mũi tên và WASD, đổi trạng thái animation, đổi sprite của gợn nước/rong, đẩy chai, va chạm biên và dừng khi nhả phím. Đưa scene và asset đã kiểm tra vào project. Bài nộp A1 còn thiếu video quay trong Editor.

## 20/11/2026 — Hai bàn 5 × 9 và bàn phím

- Người dùng làm rõ mỗi tầng là một bàn 5 × 9. Chuyển A1 sang góc nhìn từ trên xuống, một bàn lớn và bản đồ nhỏ của tầng kia. Hai bàn nằm trong cùng scene, giữ vật thể và tọa độ hàng/cột khi đổi camera.
- Thêm Q lặn, E nổi và nút đổi tầng. Chuyển tầng có hiệu ứng tối dần và giữ ô tương ứng. Bản đồ nhỏ có viền ô tương ứng và hiện vị trí chai khi đang ở dưới nước.
- Chuyển di chuyển sang action Player/Move sẵn có của Unity Input System. Kiểm tra Play Mode bằng bàn phím mô phỏng xác nhận WASD, phím mũi tên, trạng thái animation, va chạm biên, chai nổi và chuyển tầng Q/E. Video A1 còn chờ quay.

## 20/11/2026 — Video nộp A1 nhóm 04

- Quay `A1Nursery` trong Play Mode của Unity Editor bằng input bàn phím mô phỏng. Giữ Hierarchy mở, Game view không phóng to. Kiểm tra khung hình thấy animation nhân vật, gợn nước, rong, đẩy chai, chạm biên và chuyển tầng.
- Xuất `Nhom04-A1.mp4`, dài 30,04 giây, 2412 × 1486, H.264. Đã giải mã thử toàn bộ file. Người dùng tự nộp Moodle.

## 20/11/2026 — Chiến dịch Bio-Bot

- Tạo goal cho chiến dịch Unity. Chọn năm màn, mỗi màn mở bốn đồng minh, hai loại rác và thêm đối thủ. Scene `BioBotCampaign` vào đầu danh sách build; scene và video A1 được lưu riêng.
- Tách dữ liệu danh sách, mô phỏng chiến đấu và giao diện. Bản đồ nhỏ hiển thị các thực thể còn hoạt động ở tầng kia. Xử lý rác dùng hai thao tác thu và tận dụng, với phần thưởng khác nhau và hiệu ứng chuyển tầng theo ô tương ứng.
- Vẽ 42 sprite thực thể, mỗi sprite có bốn frame. Bổ sung chuyển động khi nhận đòn, tia xung và ngắm theo hàng. Toàn bộ danh sách có trong Sổ sinh thái trong game.
- Mô phỏng tuần tự thắng năm màn, kiểm tra trường hợp thua không phòng thủ, đủ 12 loại địch xuất hiện, đủ 10 cách xử lý rác và các tương tác qua tầng. Chạy kiểm tra Play Mode riêng cho phím điều khiển và đặt đồng minh đúng môi trường.
- Xác nhận mở scene campaign và hiển thị menu trong Unity Editor. Kiểm tra Play Mode trên hồ sơ thử riêng xác nhận màn hai bị khóa lúc đầu, thắng màn một lưu tiến độ và Enter chuyển được sang màn hai.
