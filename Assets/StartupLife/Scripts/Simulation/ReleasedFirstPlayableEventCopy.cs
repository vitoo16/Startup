#nullable enable
using System;
using System.Collections.Generic;

namespace StartupLife.Simulation
{
    // Frozen Vietnamese string copy captured from the first-playable release at
    // 4f07870e. This is deliberately independent of the current Unity StringTable
    // and FirstPlayableEventContent: future copy edits must not rewrite old stories.
    // Add new released snapshots rather than modifying these values.
    public static class ReleasedFirstPlayableEventCopy
    {
        public static IReadOnlyList<KeyValuePair<string, string>> Vietnamese { get; } =
            Array.AsReadOnly(new[]
            {
                new KeyValuePair<string, string>("event.story." + "welcome-neighbor" + ".title", "Lời mời từ hàng xóm"),
                new KeyValuePair<string, string>("event.story." + "welcome-neighbor" + ".body", "Một người hàng xóm rủ bạn tham dự buổi sinh hoạt khu phố vào cuối tuần."),
                new KeyValuePair<string, string>("event.story." + "welcome-neighbor" + ".choice." + "join", "Đến làm quen mọi người"),
                new KeyValuePair<string, string>("event.story." + "welcome-neighbor" + ".choice." + "decline", "Ở nhà nghỉ ngơi"),
                new KeyValuePair<string, string>("event.story." + "coffee-connection" + ".title", "Cuộc gặp tại quán cà phê"),
                new KeyValuePair<string, string>("event.story." + "coffee-connection" + ".body", "Người quen ở khu phố giới thiệu bạn với một nhóm thường trao đổi ý tưởng làm ăn."),
                new KeyValuePair<string, string>("event.story." + "coffee-connection" + ".choice." + "listen", "Tham gia nghe và trao đổi"),
                new KeyValuePair<string, string>("event.story." + "coffee-connection" + ".choice." + "later", "Hẹn gặp vào dịp khác"),
                new KeyValuePair<string, string>("event.story." + "mentor-introduction" + ".title", "Lời khuyên từ người đi trước"),
                new KeyValuePair<string, string>("event.story." + "mentor-introduction" + ".body", "Một người nhiều kinh nghiệm đề nghị chia sẻ những bài học khi mới đi làm."),
                new KeyValuePair<string, string>("event.story." + "mentor-introduction" + ".choice." + "ask", "Xin một buổi trò chuyện"),
                new KeyValuePair<string, string>("event.story." + "mentor-introduction" + ".choice." + "thank", "Cảm ơn và tiếp tục tự tìm hiểu"),
                new KeyValuePair<string, string>("event.story." + "workplace-lunch" + ".title", "Bữa trưa cùng đồng nghiệp"),
                new KeyValuePair<string, string>("event.story." + "workplace-lunch" + ".body", "Đồng nghiệp rủ bạn ăn trưa để kể về các dự án đang triển khai."),
                new KeyValuePair<string, string>("event.story." + "workplace-lunch" + ".choice." + "connect", "Ngồi lại và lắng nghe"),
                new KeyValuePair<string, string>("event.story." + "workplace-lunch" + ".choice." + "rest", "Dành giờ nghỉ cho bản thân"),
                new KeyValuePair<string, string>("event.story." + "salary-reflection" + ".title", "Kế hoạch sau ngày nhận lương"),
                new KeyValuePair<string, string>("event.story." + "salary-reflection" + ".body", "Bạn có dịp nghĩ về mục tiêu tiết kiệm và dự định tương lai."),
                new KeyValuePair<string, string>("event.story." + "salary-reflection" + ".choice." + "plan", "Viết một kế hoạch nhỏ"),
                new KeyValuePair<string, string>("event.story." + "salary-reflection" + ".choice." + "reflect", "Cứ suy nghĩ thêm, chưa quyết định"),
                new KeyValuePair<string, string>("event.story." + "weekend-study" + ".title", "Nhóm học cuối tuần"),
                new KeyValuePair<string, string>("event.story." + "weekend-study" + ".body", "Các học viên cùng khóa muốn lập nhóm để chia sẻ kinh nghiệm học tập."),
                new KeyValuePair<string, string>("event.story." + "weekend-study" + ".choice." + "join", "Tham gia nhóm học"),
                new KeyValuePair<string, string>("event.story." + "weekend-study" + ".choice." + "solo", "Tiếp tục học theo nhịp riêng"),
                new KeyValuePair<string, string>("event.story." + "study-friend" + ".title", "Người bạn cùng khóa"),
                new KeyValuePair<string, string>("event.story." + "study-friend" + ".body", "Một người trong nhóm gợi ý trao đổi các ghi chú và phương pháp luyện tập."),
                new KeyValuePair<string, string>("event.story." + "study-friend" + ".choice." + "share", "Cùng chia sẻ ghi chú"),
                new KeyValuePair<string, string>("event.story." + "study-friend" + ".choice." + "keep", "Lịch sự từ chối"),
                new KeyValuePair<string, string>("event.story." + "first-market-day" + ".title", "Phiên chợ địa phương"),
                new KeyValuePair<string, string>("event.story." + "first-market-day" + ".body", "Một khu chợ gần nhà mời các chủ cơ sở nhỏ đến gặp gỡ khách hàng."),
                new KeyValuePair<string, string>("event.story." + "first-market-day" + ".choice." + "visit", "Đến tìm hiểu nhu cầu"),
                new KeyValuePair<string, string>("event.story." + "first-market-day" + ".choice." + "skip", "Tập trung vào hoạt động hiện tại"),
                new KeyValuePair<string, string>("event.story." + "supplier-chat" + ".title", "Cuộc trao đổi với nhà cung cấp"),
                new KeyValuePair<string, string>("event.story." + "supplier-chat" + ".body", "Tại một buổi gặp mặt, bạn được giới thiệu cách xây dựng quan hệ nguồn hàng lâu dài."),
                new KeyValuePair<string, string>("event.story." + "supplier-chat" + ".choice." + "learn", "Lắng nghe và giữ liên lạc"),
                new KeyValuePair<string, string>("event.story." + "supplier-chat" + ".choice." + "wait", "Chờ đến khi kinh doanh ổn định"),
                new KeyValuePair<string, string>("event.story." + "customer-feedback" + ".title", "Góp ý từ khách hàng"),
                new KeyValuePair<string, string>("event.story." + "customer-feedback" + ".body", "Một khách hàng chủ động chia sẻ điều họ hài lòng và điều còn bất tiện."),
                new KeyValuePair<string, string>("event.story." + "customer-feedback" + ".choice." + "listen", "Ghi nhận phản hồi"),
                new KeyValuePair<string, string>("event.story." + "customer-feedback" + ".choice." + "later", "Hẹn trao đổi vào lần tới"),
                new KeyValuePair<string, string>("event.story." + "founder-community" + ".title", "Cộng đồng làm việc tự do"),
                new KeyValuePair<string, string>("event.story." + "founder-community" + ".body", "Bạn được mời tham dự buổi gặp của những người đang tìm hướng đi mới."),
                new KeyValuePair<string, string>("event.story." + "founder-community" + ".choice." + "attend", "Đến nghe các câu chuyện"),
                new KeyValuePair<string, string>("event.story." + "founder-community" + ".choice." + "decline", "Tìm hiểu thêm trước khi tham dự"),
                new KeyValuePair<string, string>("event.story." + "pitch-practice" + ".title", "Thử kể ý tưởng của mình"),
                new KeyValuePair<string, string>("event.story." + "pitch-practice" + ".body", "Một người trong cộng đồng đề nghị bạn thử kể ý tưởng trong hai phút."),
                new KeyValuePair<string, string>("event.story." + "pitch-practice" + ".choice." + "practice", "Thử trình bày ý tưởng"),
                new KeyValuePair<string, string>("event.story." + "pitch-practice" + ".choice." + "observe", "Lắng nghe người khác trình bày"),
                new KeyValuePair<string, string>("event.story." + "mentor-follow-up" + ".title", "Một buổi hẹn với người hướng dẫn"),
                new KeyValuePair<string, string>("event.story." + "mentor-follow-up" + ".body", "Sau giờ làm, người hướng dẫn hẹn bạn ở quán nước gần văn phòng để trao đổi về cách phát triển nghề nghiệp."),
                new KeyValuePair<string, string>("event.story." + "mentor-follow-up" + ".choice." + "prepare", "Chuẩn bị câu hỏi trước buổi gặp"),
                new KeyValuePair<string, string>("event.story." + "mentor-follow-up" + ".choice." + "listen", "Đến nghe và ghi nhớ lời khuyên"),
                new KeyValuePair<string, string>("event.story." + "team-code-review" + ".title", "Buổi chia sẻ trong nhóm"),
                new KeyValuePair<string, string>("event.story." + "team-code-review" + ".body", "Đồng nghiệp rủ bạn cùng xem lại cách cả nhóm phối hợp và rút kinh nghiệm sau một dự án."),
                new KeyValuePair<string, string>("event.story." + "team-code-review" + ".choice." + "share", "Chia sẻ một bài học nhỏ"),
                new KeyValuePair<string, string>("event.story." + "team-code-review" + ".choice." + "observe", "Lắng nghe mọi người trước"),
                new KeyValuePair<string, string>("event.story." + "savings-checkin" + ".title", "Cuốn sổ kế hoạch tháng"),
                new KeyValuePair<string, string>("event.story." + "savings-checkin" + ".body", "Khi kiểm tra lại ghi chú, bạn nhận ra đã đến lúc xác định mục tiêu tiết kiệm rõ ràng hơn."),
                new KeyValuePair<string, string>("event.story." + "savings-checkin" + ".choice." + "review", "Rà soát các ưu tiên"),
                new KeyValuePair<string, string>("event.story." + "savings-checkin" + ".choice." + "postpone", "Để dành cho một buổi khác"),
                new KeyValuePair<string, string>("event.story." + "study-presentation" + ".title", "Buổi chia sẻ cuối khóa"),
                new KeyValuePair<string, string>("event.story." + "study-presentation" + ".body", "Nhóm học đề nghị mỗi người kể một điều hữu ích họ đã tự khám phá trong quá trình luyện tập."),
                new KeyValuePair<string, string>("event.story." + "study-presentation" + ".choice." + "present", "Tình nguyện chia sẻ"),
                new KeyValuePair<string, string>("event.story." + "study-presentation" + ".choice." + "attend", "Đến nghe phần trình bày của bạn học"),
                new KeyValuePair<string, string>("event.story." + "service-improvement" + ".title", "Một ý tưởng cải thiện dịch vụ"),
                new KeyValuePair<string, string>("event.story." + "service-improvement" + ".body", "Sau khi đọc phản hồi, bạn có cơ hội bàn cùng cộng sự về cách phục vụ thuận tiện hơn."),
                new KeyValuePair<string, string>("event.story." + "service-improvement" + ".choice." + "discuss", "Ghi lại một hướng cải thiện"),
                new KeyValuePair<string, string>("event.story." + "service-improvement" + ".choice." + "consider", "Chờ thêm ý kiến từ khách hàng"),
                new KeyValuePair<string, string>("event.story." + "supplier-follow-up" + ".title", "Giữ liên lạc với nhà cung cấp"),
                new KeyValuePair<string, string>("event.story." + "supplier-follow-up" + ".body", "Một đối tác gửi lời mời tham quan nơi đóng gói và giao hàng để hiểu quy trình làm việc."),
                new KeyValuePair<string, string>("event.story." + "supplier-follow-up" + ".choice." + "visit", "Sắp xếp buổi tham quan"),
                new KeyValuePair<string, string>("event.story." + "supplier-follow-up" + ".choice." + "later", "Hẹn vào ngày ít bận hơn"),
                new KeyValuePair<string, string>("event.story." + "pitch-feedback" + ".title", "Lời góp ý cho ý tưởng"),
                new KeyValuePair<string, string>("event.story." + "pitch-feedback" + ".body", "Một người trong buổi gặp gửi bạn vài câu hỏi để giúp ý tưởng trở nên dễ hiểu hơn."),
                new KeyValuePair<string, string>("event.story." + "pitch-feedback" + ".choice." + "revise", "Chỉnh cách trình bày"),
                new KeyValuePair<string, string>("event.story." + "pitch-feedback" + ".choice." + "reflect", "Ghi lại để suy nghĩ tiếp"),
                new KeyValuePair<string, string>("event.story." + "community-collaboration" + ".title", "Lời mời hỗ trợ cộng đồng"),
                new KeyValuePair<string, string>("event.story." + "community-collaboration" + ".body", "Nhóm làm việc tự do rủ bạn cùng chuẩn bị một buổi chia sẻ kinh nghiệm cho những người mới bắt đầu."),
                new KeyValuePair<string, string>("event.story." + "community-collaboration" + ".choice." + "help", "Góp sức chuẩn bị chương trình"),
                new KeyValuePair<string, string>("event.story." + "community-collaboration" + ".choice." + "observe", "Đến dự để học hỏi trước")
            });
    }
}
