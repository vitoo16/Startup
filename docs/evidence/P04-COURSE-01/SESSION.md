# P04-COURSE-01 — Chọn khóa học thật trong Life Dashboard

**Ngày:** 2026-11-03 · **Owner:** GPT-6 Technical Director · **Baseline:** `a32291067b51a519e934935f31f5cbb3cd3cfd37` · **Lane:** `main`.

## Vấn đề gameplay

Catalog có nhiều khóa học (`communication-basics`, `problem-course`) nhưng tab Kỹ năng trong `LifeDashboardOverlay` chỉ hiển thị danh sách và nút mua bị gắn cứng vào `communication-basics`. Người chơi không thể mua khóa Giải quyết vấn đề từ bảng quản lý dù `SimulationEngine.Purchase` đã hỗ trợ mọi khóa được khai báo trong nội dung.

## Thực thi

1. `CourseEnrollmentPolicy.cs` tạo **read-model only** dùng danh sách Course/Skill/Finance từ `LifeDashboardProjection`. Phản ánh đúng thứ tự kiểm của `SimulationEngine.Purchase`: một khóa đang active, prerequisite, target level, nợ, tiền. Khi restore, khóa đang học được chọn lại theo snapshot; selection tạm trong UI, không ghi save.
2. `StudyActionController.PurchaseCourse(courseId)` gọi lại **cùng** `FirstPlayableFlow.PurchaseCourse` / command idempotent đã có; không thêm giao dịch hay bộ trừ tiền khác. Wrapper `PurchaseCommunicationCourse` được giữ cho UnityEvent cũ.
3. `LifeScreenViewController.PurchaseCourse(courseId)` kiểm tra ID thuộc catalog trước khi ủy quyền, giữ wrapper cho scene cũ.
4. Tab Kỹ năng tự tạo các nút **Chọn** theo `model.Courses`, cache/reuse trong mỗi lần refresh, hiển thị khóa được chọn và lý do bị khóa; nút Đăng ký dùng ID khóa đã chọn. Nút học 60 phút tiếp tục dùng simulation clamp hiện có. Không chỉnh phần UI khác.
5. `tools/SimulationChecks/Program.cs` bổ sung các case mô phỏng: chọn khác default, thiếu tiền, nợ, đạt target, prerequisite, pending active, mua khóa khác thực tế; replay/save schema giữ nguyên.
6. `tools/tests/test_course_dashboard_wiring.py` kiểm tra source join và việc nút UI không quay về hardcode; Engine-free CI chạy test này.

## Quy tắc và QA

- **Không thay đổi** `SimulationEngine`, `GameState`, save version, balance, ContentCatalog, economics, frozen replay, `Art/Approved`, C03, scene/prefab.
- Engine authoritative cho mọi quyết định mua khóa; UI chỉ đưa gợi ý không phải cam kết thanh toán. Nút disabled có thể trở nên stale; command vẫn phải reject an toàn.
- **Unity Editor/PlayMode, screenshot và điện thoại:** cố ý hoãn theo yêu cầu owner. Không chứng nhận visual QA hoặc compile Unity khi không chạy.
- Code kỹ năng: `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `ui-ugui`, `accessibility` (touch target, status labels), `unity-cli` (xác định QA deferred).
- **Tests:** `tools/SimulationChecks` + static course UI join trong Engine-free CI. Xem verdict trên GitHub của commit chứa SESSION; không kế thừa PASS của SHA cũ.
- **Visual evidence:** NONE (Unity/deferred by owner).
- **Save impact:** NONE. Selected course ID không được persist; authoritative active course vẫn được restore từ save.
- **Known limitations:** Scene legacy vẫn có nút mua Giao tiếp để không phá UnityEvent; UI mới của dashboard mới chọn theo catalog. Cần Unity compile, scale 200%, một-tay 9:16 và screenshot khi mở lại QA. Danh sách course có thể cần virtualize khi catalog tăng lên rất lớn.
- **Files changed:** `Assets/StartupLife/Scripts/Application/CourseEnrollmentPolicy.cs[.meta]`, `Presentation/{StudyActionController,LifeScreenViewController,LifeDashboardOverlay}.cs`, `tools/SimulationChecks/Program.cs`, `tools/tests/test_course_dashboard_wiring.py`, `.github/workflows/engine-free-ci.yml`, file này.
- **Commit:** xem `main` HEAD sau khi ghi nhận evidence.
