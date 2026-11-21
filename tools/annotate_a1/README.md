# Chú thích video A1

Công cụ xuất MP4 H.264 bằng Swift, AVFoundation và Core Animation trên macOS, dùng Swift có sẵn trong Xcode hoặc Xcode Command Line Tools. Dải chú thích cao 96 px nằm dưới toàn bộ khung Unity Editor. Công cụ giữ âm thanh trong khoảng thời gian của hình nếu video có tiếng.

Từ thư mục gốc của dự án, thay các đường dẫn trong lệnh bằng vị trí bản quay Play Mode và vị trí muốn lưu.

```sh
tools/annotate_a1/annotate_video.sh "/path/to/raw-play-mode.mov" tools/annotate_a1/a1_captions.json "/path/to/export/Nhom04-A1.mp4"
```

Đặt đường dẫn đầu ra trong một thư mục đã tồn tại và chọn tên tệp còn trống. Công cụ từ chối ghi đè tệp có sẵn.

## Chỉnh mốc cho bản quay mới

Mở `a1_captions.json` và đặt `start`, `end` theo số giây tính từ đầu video. Ví dụ, `4.2` là 4,2 giây. Mỗi chú thích hiện từ `start` đến ngay trước `end`. Xếp các mục theo thứ tự thời gian và giữ các khoảng không chồng lấn. Đặt `end` của mục trước bằng `start` của mục sau để chuyển chú thích liền mạch. Nếu có khoảng trống, dải dưới sẽ để trống. Các mốc vượt thời lượng sẽ dừng ở cuối video.

Sửa `tag` (ví dụ `1/5`), `title` và `subtitle` để khớp với thao tác trong clip. Giữ mỗi trường trên một dòng. Mốc mẫu dành cho video A1 hiện có khoảng 30 giây. Pha vật lý nhắc lại cú đẩy chai tại giây thứ 5 vì thao tác này xuất hiện trong pha di chuyển của bản quay mẫu.
