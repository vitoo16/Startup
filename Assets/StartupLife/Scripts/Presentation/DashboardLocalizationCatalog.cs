#nullable enable
using System;
using System.Collections.Generic;

namespace StartupLife.Presentation
{
    // One Vietnamese source-copy manifest. Unity Editor owns StringTable assets.
    public static class DashboardLocalizationCatalog
    {
        private static readonly KeyValuePair<string,string>[] Source =
        {
            new KeyValuePair<string,string>("event.choice.title", "Sự kiện cần quyết định"),
            new KeyValuePair<string,string>("event.choice.prompt", "Chọn một phương án để tiếp tục."),
            new KeyValuePair<string,string>("event.choice.retry", "Lưu chưa chắc chắn. Nhấn lại cùng phương án để thử lại."),
            new KeyValuePair<string,string>("event.choice.changed", "Trạng thái đã thay đổi. Hãy kiểm tra và chọn lại."),
            new KeyValuePair<string,string>("event.choice.unavailable", "Chưa thể kiểm tra dữ liệu sự kiện. Vui lòng thử lại sau."),
            new KeyValuePair<string,string>("dashboard.tab.career", "Nghề nghiệp"),
            new KeyValuePair<string,string>("dashboard.tab.skills", "Kỹ năng"),
            new KeyValuePair<string,string>("dashboard.tab.finance", "Tài chính"),
            new KeyValuePair<string,string>("dashboard.tab.business", "Kinh doanh"),
            new KeyValuePair<string,string>("dashboard.tab.profile", "Hồ sơ"),
            new KeyValuePair<string,string>("dashboard.tab.career.short", "Nghề"),
            new KeyValuePair<string,string>("dashboard.tab.finance.short", "Tiền"),
            new KeyValuePair<string,string>("dashboard.tab.business.short", "Cửa hàng"),
            new KeyValuePair<string,string>("dashboard.open", "Tổng quan"),
            new KeyValuePair<string,string>("dashboard.close", "Đóng"),
            new KeyValuePair<string,string>("dashboard.career.current", "Công việc hiện tại"),
            new KeyValuePair<string,string>("dashboard.career.accept", "Nhận việc lập trình"),
            new KeyValuePair<string,string>("dashboard.career.work", "Đi làm"),
            new KeyValuePair<string,string>("dashboard.career.resign", "Nghỉ việc"),
            new KeyValuePair<string,string>("dashboard.career.history", "Lịch sử nghề nghiệp"),
            new KeyValuePair<string,string>("dashboard.skills.progress", "Tiến độ kỹ năng"),
            new KeyValuePair<string,string>("dashboard.skills.courses", "Các khóa học"),
            new KeyValuePair<string,string>("dashboard.skills.purchase", "Mua khóa Giao tiếp"),
            new KeyValuePair<string,string>("dashboard.skills.study", "Học 60 phút"),
            new KeyValuePair<string,string>("dashboard.finance.heading", "Dòng tiền và kết quả đã ghi nhận"),
            new KeyValuePair<string,string>("dashboard.finance.arrears", "Các khoản nợ"),
            new KeyValuePair<string,string>("dashboard.business.heading", "Danh mục kinh doanh"),
            new KeyValuePair<string,string>("dashboard.business.manage", "Mở quản lý doanh nghiệp"),
            new KeyValuePair<string,string>("dashboard.profile.heading", "Thông tin hành trình"),
            new KeyValuePair<string,string>("dashboard.profile.help", "Bạn có thể làm việc, học kỹ năng và khởi nghiệp theo tốc độ của mình."),
            new KeyValuePair<string,string>("dashboard.settings.heading", "Cài đặt hiển thị"),
            new KeyValuePair<string,string>("dashboard.settings.smaller", "Chữ nhỏ hơn"),
            new KeyValuePair<string,string>("dashboard.settings.larger", "Chữ lớn hơn"),
            new KeyValuePair<string,string>("dashboard.guide.heading", "Hướng dẫn hành trình"),
            new KeyValuePair<string,string>("dashboard.guide.previous", "Bước trước"),
            new KeyValuePair<string,string>("dashboard.guide.next", "Bước tiếp"),
            new KeyValuePair<string,string>("dashboard.guide.done", "Đã hiểu"),
            new KeyValuePair<string,string>("dashboard.guide.replay", "Xem lại hướng dẫn"),
            new KeyValuePair<string,string>("dashboard.guide.completed-title", "Đã xem hướng dẫn"),
            new KeyValuePair<string,string>("dashboard.guide.completed-body", "Bạn có thể xem lại hướng dẫn bất kỳ lúc nào."),
            new KeyValuePair<string,string>("dashboard.guide.skills.title", "Kỹ năng có thể luyện miễn phí"),
            new KeyValuePair<string,string>("dashboard.guide.skills.body", "Đi làm và trải nghiệm có thể tăng kỹ năng miễn phí. Bạn không cần mua khóa học để bắt đầu học hỏi."),
            new KeyValuePair<string,string>("dashboard.guide.study.title", "Khóa học cần học phí"),
            new KeyValuePair<string,string>("dashboard.guide.study.body", "Khóa học có giá và yêu cầu riêng. Xem học phí, thời lượng và trình độ trước khi mua; học mới tiêu tốn thời gian."),
            new KeyValuePair<string,string>("dashboard.guide.compatibility.title", "Chọn kinh doanh phù hợp"),
            new KeyValuePair<string,string>("dashboard.guide.compatibility.body", "Một số nghề có thể kinh doanh thêm buổi tối. Quầy cà phê cần thời gian trực tiếp ban ngày và không phù hợp khi còn làm toàn thời gian."),
            new KeyValuePair<string,string>("dashboard.guide.time.title", "Lịch được phân bổ tự động"),
            new KeyValuePair<string,string>("dashboard.guide.time.body", "Game tự chia thời gian cho công việc và các doanh nghiệp. Xem thời gian phân bổ trước khi mở thêm; không cần tự xếp lịch từng giờ."),
            new KeyValuePair<string,string>("dashboard.guide.resign.title", "Nghỉ việc không bắt buộc khởi nghiệp"),
            new KeyValuePair<string,string>("dashboard.guide.resign.body", "Nghỉ việc giúp giải phóng thời gian, nhưng không bắt buộc mở doanh nghiệp. Bạn có thể học, tìm việc lại hoặc khởi nghiệp khi đã sẵn sàng."),
        };
        private static readonly Dictionary<string,string> Reverse = BuildReverse();
        public static IReadOnlyList<KeyValuePair<string,string>> Entries { get; } =
            Array.AsReadOnly(Source);

        public static bool TryFindKey(string fallback, out string key)
        {
            if (fallback == null) { key = ""; return false; }
            return Reverse.TryGetValue(fallback, out key!);
        }

        private static Dictionary<string,string> BuildReverse()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var reverse = new Dictionary<string,string>(StringComparer.Ordinal);
            foreach (var item in Source)
            {
                if (string.IsNullOrWhiteSpace(item.Key) ||
                    string.IsNullOrWhiteSpace(item.Value) || !seen.Add(item.Key))
                    throw new InvalidOperationException("Invalid dashboard localization key.");
                if (!reverse.ContainsKey(item.Value)) reverse.Add(item.Value, item.Key);
            }
            return reverse;
        }
    }
}
