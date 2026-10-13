#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Content;

namespace StartupLife.Presentation
{
    // Keyed source copy; the existing Unity Editor sync writes actual StringTables.
    // Authored event-specific choice labels remain in the event catalog (M15-T02).
    public static class EventLocalizationCatalog
    {
        public static IReadOnlyList<KeyValuePair<string, string>> Entries { get; } =
            Array.AsReadOnly(new[]
            {
                new KeyValuePair<string, string>("event.choice.title", "Sự kiện cần quyết định"),
                new KeyValuePair<string, string>("event.choice.prompt", "Chọn một phương án để tiếp tục."),
                new KeyValuePair<string, string>("event.choice.retry", "Lưu chưa chắc chắn. Nhấn lại cùng phương án để thử lại."),
                new KeyValuePair<string, string>("event.choice.changed", "Trạng thái đã thay đổi. Hãy kiểm tra và chọn lại."),
                new KeyValuePair<string, string>("event.choice.unavailable", "Chưa thể kiểm tra dữ liệu sự kiện. Vui lòng thử lại sau."),
                new KeyValuePair<string, string>("event.choice.insufficient_funds", "Chưa đủ tiền cho lựa chọn có chi phí. Bạn vẫn có thể chọn phương án khác."),
                new KeyValuePair<string, string>("event.choice.reward_unavailable", "Một phương án hiện chưa khả dụng. Hãy chọn phương án khác.")
            }.Concat(FirstPlayableEventContent.VietnameseEntries).ToArray());
    }
}
