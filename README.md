# BogoTV - Bộ gõ Tiếng Việt cho Windows

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-WPF-239120?logo=csharp&logoColor=white" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/License-MIT-green" />
  <img src="https://img.shields.io/badge/Version-1.0.0-blue" />
</p>

Phần mềm gõ tiếng Việt cho Windows, hỗ trợ các kiểu gõ **Telex**, **VNI**, **VIQR** và nhiều bảng mã (Unicode, TCVN3, VNI, VPS, VISCII). Sử dụng **Global Keyboard Hook** để bắt và xử lý phím toàn hệ thống.

---

## Tính năng

- **Bắt phím toàn hệ thống** (Global Keyboard Hook - `WH_KEYBOARD_LL`)
- **3 kiểu gõ**: Telex (mặc định), VNI, VIQR
- **5 bảng mã**: Unicode (mặc định), TCVN3, VNI, VPS, VISCII
- **Gõ tiếng Việt mọi ứng dụng**: Notepad, Word, trình duyệt, chat, v.v.
- **System Tray**: ẩn cửa sổ xuống khay hệ thống, bật/tắt nhanh
- **Phím tắt**: `Ctrl + .` để bật/tắt bộ gõ
- **Lưu cài đặt**: tự động ghi vào `%LOCALAPPDATA%\BogoTV\settings.xml`
- **Xử lý dấu thông minh**: giữ dấu khi đổi nguyên âm, gõ dấu ở cấp âm tiết

---

## Kiến trúc dự án

```
bogotv/
├── BogoTV.sln                          # Visual Studio Solution
├── BogoTV/
│   ├── BogoTV.csproj                   # Project file (.NET 8.0 WPF + WinForms)
│   ├── app.manifest                    # UAC manifest
│   ├── App.xaml                        # WPF Application entry
│   ├── App.xaml.cs                     # Khởi tạo engine, hook, tray icon
│   ├── MainWindow.xaml                 # GUI chính
│   ├── MainWindow.xaml.cs             # Logic GUI
│   ├── NotifyIconController.cs         # System Tray icon + context menu
│   ├── Engine/
│   │   ├── VietnameseEngine.cs         # Engine xử lý Telex/VNI/VIQR
│   │   └── InputSimulator.cs           # Gửi phím ảo (SendInput API)
│   ├── Hook/
│   │   └── GlobalKeyboardHook.cs       # WH_KEYBOARD_LL hook
│   ├── Models/
│   │   └── AppSettings.cs              # Lưu/đọc cài đặt XML
│   └── Views/
│       ├── HelpWindow.xaml(.cs)        # Popup hướng dẫn
│       └── AboutWindow.xaml(.cs)       # Popup thông tin app
```

---

## Bảng điều khiển (GUI)

| Thành phần | Mô tả |
|---|---|
| Dropdown **Bảng mã** | Unicode, TCVN3, VNI, VPS, VISCII (chọn sẵn Unicode) |
| Dropdown **Kiểu gõ** | Telex, VNI, VIQR (chọn sẵn Telex) |
| Nút **Đóng** | Ẩn cửa sổ xuống System Tray |
| Nút **Kết thúc** | Thoát hoàn toàn ứng dụng |
| Nút **Mở rộng** | Hiển thị/ẩn tùy chọn nâng cao |
| Nút **Mặc định** | Khôi phục cài đặt gốc (Unicode + Telex) |
| Nút **Hướng dẫn sử dụng** | Mở popup quy tắc gõ |
| Nút **Thông tin app** | Mở popup phiên bản, tác giả (About) |
| Nút **Bật/Tắt bộ gõ** | Toggle engine on/off |
| Trạng thái | Chỉ báo xanh (đang hoạt động) / đỏ (đã tắt) |

---

## Quy tắc gõ

### Kiểu Telex (mặc định)

#### Dấu thanh

| Phím | Dấu | Ví dụ |
|---|---|---|
| `s` | Sắc | `as` → á |
| `f` | Huyền | `af` → à |
| `r` | Hỏi | `ar` → ả |
| `x` | Ngã | `ax` → ã |
| `j` | Nặng | `aj` → ạ |
| `z` | Bỏ dấu | `az` → a |

#### Nguyên âm đặc biệt

| Phím | Kết quả | Phím | Kết quả |
|---|---|---|---|
| `aw` | ă (mơ) | `aa` → â (mũ) | â |
| `ow` | ơ (mơ) | `oo` → ô (mũ) | ô |
| `uw` | ư (mơ) | `ee` → ê (mũ) | ê |
| `dd` | đ (gạch) | | |

#### Ví dụ

```
teets    → tiết      (ee → ê, s → sắc)
nguwowif → người     (uw → ư, ow → ơ, f → huyền)
ddax     → đã        (dd → đ, x → ngã)
hoacs    → hóa       (o → ơ, s → sắc)
```

### Kiểu VNI

| Phím | Dấu | Phím | Nguyên âm |
|---|---|---|---|
| `1` | Sắc | `8` | ă |
| `2` | Huyền | `6` | â / ê / ô |
| `3` | Hỏi | `7` | ơ / ư |
| `4` | Ngã | `9` | đ |
| `5` | Nặng | | |
| `0` | Bỏ dấu | | |

### Kiểu VIQR

| Phím | Dấu | Phím | Nguyên âm |
|---|---|---|---|
| `'` | Sắc | `^` | â / ê / ô |
| `` ` `` | Huyền | `+` | ă / ơ / ư |
| `?` | Hỏi | | |
| `~` | Ngã | | |
| `.` | Nặng | | |
| `-` | Bỏ dấu | | |

---

## Yêu cầu hệ thống

- **OS**: Windows 10 1607+ hoặc Windows 11
- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **IDE** (tùy chọn): Visual Studio 2022 17.8+ hoặc VS Code + C# Dev Kit

---

## Biên dịch và chạy thử

### Cách 1: .NET CLI

```bash
cd bogotv
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

### Cách 2: Visual Studio

1. Mở `BogoTV.sln`
2. Chọn `Release` | `Any CPU`
3. Nhấn **F5** (Debug) hoặc **Ctrl+F5** (Run)

### Cách 3: MSBuild

```bash
msbuild BogoTV.sln /p:Configuration=Release /p:Platform="AnyCPU"
```

**Output:** `BogoTV/bin/Release/net8.0-windows/BogoTV.exe`

---

## Kiểm thử

1. Khởi động `BogoTV.exe` → cửa sổ bảng điều khiển hiện ra
2. Mở Notepad hoặc bất kỳ ứng dụng nào
3. Gõ `teets` → hiển thị `tiết`
4. Gõ `nguwowif` → hiển thị `người`
5. Gõ `ddax` → hiển thị `đã`
6. Nhấn `Ctrl + .` để bật/tắt bộ gõ
7. Nút **Đóng** → ẩn xuống khay hệ thống
8. Nút **Kết thúc** → thoát hoàn toàn

---

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Ngôn ngữ | C# (.NET 8.0) |
| GUI Framework | WPF (Windows Presentation Foundation) |
| System Tray | Windows Forms (`NotifyIcon`) |
| Keyboard Hook | Win32 API (`SetWindowsHookEx`, `WH_KEYBOARD_LL`) |
| Gửi phím ảo | Win32 API (`SendInput`, `KEYEVENTF_UNICODE`) |
| Lưu cài đặt | XML (`XDocument`) |
| Kiến trúc | MVVM-ready, DI-friendly |

---

## Tác giả

**System Developer**

- Email: huyztlong@gmail.com
- GitHub: [@huyzpka-commits](https://github.com/huyzpka-commits)

---

## License

Distributed under the **MIT License**.

Copyright (c) 2026 BogoTV
