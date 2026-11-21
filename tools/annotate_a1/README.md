# Chú thích video A1

Công cụ dùng Swift, AVFoundation và Core Animation trên macOS để xuất MP4 H.264. Khung đỏ, nhãn và mũi tên được vẽ trực tiếp lên bản quay Unity Editor. Video giữ kích thước khung hình theo hướng hiển thị của bản gốc và giữ phần âm thanh đi cùng hình.

Từ thư mục gốc của dự án, chạy lệnh sau với Swift từ Xcode hoặc Xcode Command Line Tools.

```sh
tools/annotate_a1/annotate_video.sh "/path/to/raw-play-mode.mov" tools/annotate_a1/a1_captions.json "/path/to/export/Nhom04-A1.mp4"
```

Thư mục đầu ra phải tồn tại. Chọn tên tệp còn trống vì công cụ từ chối ghi đè.

## Đặt chú thích theo bản quay

Các mốc và vị trí mẫu khớp với bố cục Editor của `Nhom04-A1.mp4` (cảnh A1 Nursery). Khi quay `A1Playground` hoặc di chuyển các tab Unity, hãy cập nhật cả mốc thời gian lẫn vị trí các khung và mũi tên.

- `start` và `end` tính bằng giây từ đầu video. Ví dụ, pha từ `4.2` đến `11.7` hiện từ giây 4,2 đến ngay trước giây 11,7. Xếp các pha theo thứ tự và tránh chồng lấn. Khoảng trống hiển thị nguyên video.
- `tag` và `title` tạo tiêu đề pha ở góc trên phải. Dùng `headerPos` nếu cần dời mép trên trái của tiêu đề, chẳng hạn để tránh che Inspector.
- `callouts` chứa các khung và nhãn cần hiện trong pha. `rect` là `[x, y, w, h]`. `label` là dòng chữ trong nhãn đỏ. Khi có nhãn, `labelPos` đặt mép trên trái của nhãn, còn `arrowTo` tùy chọn đặt đầu mũi tên.
- Mọi tọa độ đều tính trên **toàn bộ khung hình** từ góc trên trái. `x` tăng sang phải, `y` tăng xuống dưới, và các giá trị nằm trong khoảng 0 đến 1. Nhãn tự co theo chiều rộng khung hình và dịch vào trong nếu chạm mép.

Ví dụ một khung quanh đối tượng, kèm nhãn ở bên phải.

```json
{
  "rect": [0.242, 0.349, 0.047, 0.062],
  "label": "Đứng yên · Idle",
  "labelPos": [0.302, 0.423],
  "arrowTo": [0.285, 0.386]
}
```
