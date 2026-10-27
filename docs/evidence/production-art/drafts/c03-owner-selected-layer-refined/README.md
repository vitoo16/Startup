# C03 Owner-Selected Visual — Source-Faithful 21-Layer Refined Draft

**Status:** `DRAFT / ART CORRECTION REQUIRED / NOT UNITY-ANIMATION-READY / NOT APPROVED`

## 1. Nguồn ảnh chuẩn C03 & SHA-256 gốc
- **Tệp nguồn chuẩn:** `Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png`
- **Kích thước:** `1122×1402` RGBA
- **SHA-256 gốc:** `1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`

## 2. Mục đích bộ refined
Bộ refined này sửa chữa lỗi phân bổ layer (layer ownership defect) trong bản cutout ban đầu (`c03-owner-selected-layer-study`), trong đó 14.025 pixel foreground nhìn thấy được đã bị gán nhầm vào `torso` dưới dạng fallback.

Toàn bộ quá trình phân bổ lại được thực hiện tự động bằng thuật toán lân cận trên mặt nạ ngữ nghĩa (nearest semantic mask reassignment) thông qua script `scripts/Refine-C03SelectedLayerOwnership.py`, đã được xác minh độc lập qua GitHub Actions run 39745979598.

## 3. Các lỗi layer đã sửa
- **Tái phân bổ chính xác 14.025 pixel fallback:**
  - `upper_arm_l`: +3.950 px
  - `forearm_l`: +4.876 px
  - `head`: +2.275 px
  - `lower_leg_r`: +724 px
  - `foot_l`: +689 px
  - `hand_l`: +663 px
  - `upper_leg_r`: +388 px
  - `torso`: +329 px (thuộc vùng thân hợp lệ)
  - `lower_leg_l`: +117 px
  - `foot_r`: +14 px
- **Sửa bounding box của `torso`:**
  - Trước: `[308, 481, 790, 1256]` (vươn sâu xuống chân/giày)
  - Sau: `[438, 661, 790, 947]` (giới hạn chuẩn xác ở thân trên)
- **Bảo toàn 100% composite hiển thị:**
  - Ghép chồng 21 layer refined cho ra ảnh trùng khớp từng pixel (`raw_pixel_diff = False`) so với composite gốc đã kiểm chứng.
  - Không thay đổi màu sắc bất kỳ pixel nào.
- **Giữ nguyên quan hệ che phủ túi/tay:**
  - `hand_r` (11.040 pixel) nằm hoàn toàn phía sau `accessory_front` và `masks/bag_body.png`.

## 4. Cấu trúc tệp
- `layers/*.png`: 21 semantic RGBA layers (`1122×1402`)
- `layers/masks/bag_body.png`: Mặt nạ thân túi trước
- `C03-refined-layer-ownership-composite.png`: Ảnh ghép kiểm chứng 21 layer
- `C03-refined-ownership-before-after-stress.png`: Ảnh so sánh biến dạng khớp trước/sau khi sửa
- `C03-refined-layer-ownership-receipt.json`: Biên lai kỹ thuật chứa SHA-256 từng tệp và thông số audit
- `README.md`: Tài liệu hướng dẫn và trạng thái này

## 5. Giới hạn còn lại — Chưa sẵn sàng cho Unity Runtime / Production Animation
1. **Hidden anatomy:** Đây là bản cutout độ phân giải cao từ tranh minh họa phẳng (opaque 2D illustration). Các phần cơ thể bị che khuất (khuỷu tay, cổ tay, mắt cá, khớp gối, chân tóc phía sau mặt) chưa được vẽ mở rộng (occluded anatomy chưa author). Khi xoay khớp ở biên độ lớn sẽ xuất hiện vệt rách/khoảng hở (seam gaps).
2. **Chưa phải native pixel art:** Không phải pixel art vẽ tay 128×160 / 32 PPU.
3. **Pivots & Rig:** 16 pivot points hiện tại là ước lượng hình học phục vụ stress test, chưa được tác giả nghệ thuật căn chỉnh và phê duyệt cho gameplay runtime.
4. **Trạng thái:** DRAFT nội bộ phục vụ review nghệ thuật và kỹ thuật. KHÔNG được tham chiếu vào `Art/Approved`, `FirstPlayable.unity`, runtime prefabs hay hệ thống gameplay.

---
**Technical Owner:** GPT-6  
**Pipeline Review:** Senior Unity Technical Artist + Pixel Art Pipeline Engineer  

## 6. Unity Editor QA — dùng bản refined đã merge

Lệnh kiểm tra QA (không tích hợp runtime):

`Startup Life → Art → C03 → Build selected-owner refined overlap-pivot QA rig (NOT motion ready)`

Builder `C03OwnerSelectedCutoutRigQA` đọc **PNG refined của chính thư mục này**
và SHA-256 từ `C03-refined-layer-ownership-receipt.json`, đồng thời dùng
`C03-selected-overlap-pivot-guide-QA.json` của thư mục này cho 10 pivot
hiệu chỉnh; 6 pivot và topology gốc được giữ từ guide study cũ.
Nó đối chiếu ảnh C03 owner-selected chuẩn, nguồn/refined receipt,
21 semantic slots và 16 khớp trước khi copy hay Unity import.

Prefab QA được tạo riêng tại:

`Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-Owner-Refined-Calibration-QA/C03_OWNER_SELECTED_REFINED_OVERLAP_PIVOT_QA.prefab`

Không ghi đè `Generated-V3-Quarantine` hay prefab cutout QA đời đầu;
cũng không có Animator, clip, `Art/Approved` hoặc gameplay binding.
Không ghi rằng Unity chạy PASS khi chưa có Console/Hierarchy/screenshot thật.

Git LFS integrity gate cho nguồn đã commit:
`python scripts/Verify-C03CommittedRefined.py` (Pillow 11.3).
GitHub Actions: `.github/workflows/c03-committed-refined-qa.yml`.
Gate chỉ xác minh ảnh, checksum và bảo toàn composite, **không duyệt mỹ thuật**.


## 7. Tối ưu QA import và kiểm tra khớp trong Unity

**PNG refined nguồn đã commit trong Git LFS không thay đổi**. Khi dựng prefab QA,
`C03RefinedSpriteCropper` lấy alpha bounding box + padding 2 pixel,
giữ nguyên toàn bộ texel, và `C03OwnerSelectedCutoutRigQA` bù offset tâm
texture vào từng `SpriteRenderer`. Kiểm tra từ 22 ảnh RGBA nguồn:

- Full-canvas 22 textures: xấp xỉ **132,0 MiB** nếu lưu RGBA32 chưa nén.
- Alpha-bounded 22 textures: xấp xỉ **5,3 MiB** RGBA32 chưa nén.
- Tiết kiệm xấp xỉ **96%** trong tính toán diện tích texture, chưa bao gồm
  Unity memory overhead, GPU padding, batching hay đo profiler trên thiết bị.
- `scripts/Verify-C03RefinedQaCrop.py` và workflow
  `.github/workflows/c03-committed-refined-qa.yml` kiểm chứng vị trí crop và
  composite tĩnh khớp từng pixel. Đây không phải chứng nhận Unity runtime.

**Hai lệnh trên Unity Editor:**

1. `Startup Life → Art → C03 → Build selected-owner refined overlap-pivot QA rig (NOT motion ready)` —
   import 21 layer refined crop trong prefab QA riêng, vẫn 16 khớp, không Animator.
2. `Startup Life → Art → C03 → Preview refined overlap-pivot rotation (QA only)` —
   mở cửa sổ preview Scene **cách ly**, chỉnh từng joint trong giới hạn ±20°
   để quan sát rách ở mép áo, tóc, tay/túi và cổ chân. Đóng cửa sổ sẽ cleanup
   đối tượng preview; không ghi scene/prefab, không tạo animation clip.

**Cảnh báo:** Những nét rách/joint thiếu hình khi xoay vẫn còn. Không
được sử dụng công cụ preview làm bằng chứng đủ điều kiện animation.
Unity Editor/PlayMode và device capture vẫn chưa được chạy ở phiên này.

## 8. Pivot QA đo từ ảnh C03 thật (ngày 27/10/2026)

Dữ liệu duy nhất dành cho prefab QA là
`C03-selected-overlap-pivot-guide-QA.json`. Từ đúng 21 PNG refined
1122×1402 đã xác minh SHA-256, giao nhau alpha của 10 cặp layer
được đo để lấy tâm; hệ thống hierarchy 16 khớp và thứ tự 21 layer
giữ nguyên. Hai pivot phải được sửa thêm 1 pixel lưới native so với
bản đề xuất đầu tiên: `wrist_r = [91, 110]`,
`hip_r = [79, 113]`. Những pivot còn lại không đổi sau phép đo.

- `python scripts/Verify-C03OverlapPivotGuide.py` kiểm đúng **tọa độ
  làm tròn**, số pixel giao nhau và quan hệ 16 bone; không cho sai số ±1.
- `python scripts/Review-C03OverlapPivots.py --output-dir <empty-dir>`
  so sánh các pose tay/chân trước/sau từ cùng PNG, xuất ảnh và báo cáo QA.
- `.github/workflows/c03-committed-refined-qa.yml` tự kiểm hai script
  với Git LFS và upload ảnh so sánh để review.

**Đây là vị trí khớp theo overlap hình học, KHÔNG phải bằng chứng
khớp với giải phẫu khi chạy animation.** Bộ underpaint ở
`scripts/Stage-C03HiddenSeamUnderpaint.py` hiện chỉ là
ứng viên QA chưa được duyệt và không thay thế art refined đã merge.

**Unity Editor và device vẫn chưa được chạy tại phiên này.**
