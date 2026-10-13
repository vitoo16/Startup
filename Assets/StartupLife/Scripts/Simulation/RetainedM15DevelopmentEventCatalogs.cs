#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Simulation
{
    // Immutable authoring deltas for pinned developer V4 checkpoints created
    // before the current post-29-story roster. These are retained data, NOT a second event
    // selector, reward executor or replay engine. Do not source old text from
    // FirstPlayableEventContent or live Unity StringTables.
    //
    // Frozen sources:
    // 22 stories: fb3067c07bcb65382ce0f308578a1124257f5eda
    // 23 stories: b385148dd4e62f7554fb17554b623ea1d87b1fac
    // 26 stories: 8d56ee240759d027cd35a5c8ea546117f9561703
    // 29 stories: debcb79e89de3b3e572cd00e25e7c5d8b5809a29
    public static class RetainedM15DevelopmentEventCatalogs
    {
        private static readonly GameEventDefinition[] AddedDefinitions =
        {
            Event("community-starter-grant", "life", 1, 5,
                new[] { "event/pitch-feedback" },
                Choice("community-starter-grant", "accept", new EventChoiceReward(100_000L, 0),
                    "event/starter-grant"),
                Choice("community-starter-grant", "decline")),
            Event("peer-coaching", "career", 1, 6,
                new[] { "employed", "event/teamwork" },
                Choice("peer-coaching", "mentor", new EventChoiceReward(0, 60),
                    "event/peer-coach"),
                Choice("peer-coaching", "observe")),
            Event("local-promotion-print", "business", 2, 6,
                new[] { "business-active", "event/customer-listening" },
                Choice("local-promotion-print", "print", new EventChoiceReward(-40_000L, 0),
                    "event/promotion-material"),
                Choice("local-promotion-print", "wait")),
            Event("mentor-workshop-materials", "career", 1, 6,
                new[] { "employed", "event/mentor-notes" },
                Choice("mentor-workshop-materials", "contribute",
                    new EventChoiceReward(-25_000L, 45), "event/mentor-workshop"),
                Choice("mentor-workshop-materials", "listen")),
            Event("supplier-quality-sample", "business", 2, 6,
                new[] { "business-active", "event/supplier-insight" },
                Choice("supplier-quality-sample", "sample",
                    new EventChoiceReward(-70_000L, 0), "event/packaging-sample"),
                Choice("supplier-quality-sample", "compare")),
            Event("customer-trust-repair", "business", 2, 6,
                new[] { "business-active", "event/customer-listening" },
                Choice("customer-trust-repair", "compensate",
                    new EventChoiceReward(-55_000L, 0), "event/customer-care-complete"),
                Choice("customer-trust-repair", "follow-up", null,
                    "event/customer-care-followup")),
            Event("customer-care-follow-up", "business", 2, 7,
                new[] { "business-active", "event/customer-care-followup" },
                Choice("customer-care-follow-up", "agree", null,
                    "event/customer-care-agreed"),
                Choice("customer-care-follow-up", "review", null,
                    "event/customer-care-reviewed")),
            Event("supplier-sample-review", "business", 2, 6,
                new[] { "business-active", "event/packaging-sample" },
                Choice("supplier-sample-review", "select", null,
                    "event/packaging-reviewed"),
                Choice("supplier-sample-review", "revise", null,
                    "event/packaging-revision")),
            Event("mentor-workshop-reflection", "career", 1, 6,
                new[] { "employed", "event/mentor-workshop" },
                Choice("mentor-workshop-reflection", "share", null,
                    "event/workshop-notes-shared"),
                Choice("mentor-workshop-reflection", "reflect", null,
                    "event/workshop-self-review"))
        };

        // Each historical event has exactly four retained Vietnamese keys:
        // title, body, first choice and second choice.
        private static readonly KeyValuePair<string, string>[] AddedVietnamese =
        {
            Text("community-starter-grant", "title", "Khoản hỗ trợ thử ý tưởng"),
            Text("community-starter-grant", "body", "Sau buổi trao đổi, một quỹ cộng đồng đề nghị hỗ trợ một khoản nhỏ để bạn thử nghiệm ý tưởng kinh doanh."),
            Text("community-starter-grant", "choice.accept", "Nhận 100.000 ₫ hỗ trợ thử nghiệm"),
            Text("community-starter-grant", "choice.decline", "Cảm ơn và tự chuẩn bị nguồn vốn"),
            Text("peer-coaching", "title", "Hướng dẫn đồng nghiệp mới"),
            Text("peer-coaching", "body", "Trưởng nhóm mời bạn chỉ dẫn một đồng nghiệp mới cách phối hợp trong công việc."),
            Text("peer-coaching", "choice.mentor", "Đứng ra hướng dẫn (+60 XP nghề)"),
            Text("peer-coaching", "choice.observe", "Cùng theo dõi, chưa đứng ra hướng dẫn"),
            Text("local-promotion-print", "title", "Thử một bảng giới thiệu nhỏ"),
            Text("local-promotion-print", "body", "Một khách hàng gợi ý bạn in bảng giới thiệu gọn để mọi người dễ biết sản phẩm và cách liên hệ."),
            Text("local-promotion-print", "choice.print", "Chi 40.000 ₫ để thử in bảng giới thiệu"),
            Text("local-promotion-print", "choice.wait", "Để dành tiền, chưa in lúc này"),
            Text("mentor-workshop-materials", "title", "Tài liệu cho buổi hướng dẫn"),
            Text("mentor-workshop-materials", "body", "Người hướng dẫn mời bạn tham gia buổi trao đổi ngắn trong giờ nghỉ. Bạn có thể góp tiền in tài liệu thực hành."),
            Text("mentor-workshop-materials", "choice.contribute", "Góp 25.000 ₫, nhận 45 XP nghề"),
            Text("mentor-workshop-materials", "choice.listen", "Đến nghe và ghi chép, không mua tài liệu"),
            Text("supplier-quality-sample", "title", "Thử mẫu đóng gói mới"),
            Text("supplier-quality-sample", "body", "Nhà cung cấp cho bạn xem mẫu hộp và túi giao hàng phù hợp với sản phẩm. Bạn được chọn mua một bộ mẫu để đánh giá."),
            Text("supplier-quality-sample", "choice.sample", "Chi 70.000 ₫ để thử bộ mẫu"),
            Text("supplier-quality-sample", "choice.compare", "Ghi lại thông tin và so sánh sau"),
            Text("customer-trust-repair", "title", "Đơn hàng cần chăm sóc lại"),
            Text("customer-trust-repair", "body", "Một khách hàng phản ánh khâu giao nhận chưa thuận tiện. Bạn có thể hỗ trợ chi phí đổi trả hoặc trực tiếp hẹn tìm cách xử lý."),
            Text("customer-trust-repair", "choice.compensate", "Chi 55.000 ₫ hỗ trợ đổi trả"),
            Text("customer-trust-repair", "choice.follow-up", "Liên hệ để tìm giải pháp không tốn phí"),
            Text("customer-care-follow-up", "title", "Cuộc hẹn giải quyết phản hồi"),
            Text("customer-care-follow-up", "body", "Sau khi bạn liên hệ, khách hàng đồng ý trao đổi rõ hơn về khâu giao nhận. Cả hai có cơ hội thống nhất cách xử lý phù hợp."),
            Text("customer-care-follow-up", "choice.agree", "Thống nhất cách giao nhận lần sau"),
            Text("customer-care-follow-up", "choice.review", "Ghi lại góp ý để điều chỉnh quy trình"),
            Text("supplier-sample-review", "title", "Đánh giá bộ mẫu đóng gói"),
            Text("supplier-sample-review", "body", "Bạn xem lại bộ mẫu đã đặt và nhận thấy có vài lựa chọn phù hợp với cách giao hàng hiện tại."),
            Text("supplier-sample-review", "choice.select", "Ghi nhận kiểu đóng gói phù hợp"),
            Text("supplier-sample-review", "choice.revise", "Nhờ nhà cung cấp điều chỉnh thêm"),
            Text("mentor-workshop-reflection", "title", "Ứng dụng bài học tại nơi làm việc"),
            Text("mentor-workshop-reflection", "body", "Sau buổi trao đổi, bạn có dịp cùng đồng nghiệp nhìn lại một tình huống phối hợp trong dự án."),
            Text("mentor-workshop-reflection", "choice.share", "Chia sẻ ghi chú thực hành"),
            Text("mentor-workshop-reflection", "choice.reflect", "Tự ôn lại bài học")
        };

        public static IReadOnlyList<EventCatalogSnapshot> Create(EventCatalogSnapshot released)
        {
            if (released == null || released.Definitions.Count != 20 ||
                released.VietnameseText.Count != 80 ||
                AddedDefinitions.Length != 9 || AddedVietnamese.Length != 36)
                throw new ArgumentException("The frozen first-playable archive is required.");

            var counts = new[] { 2, 3, 6, 9 };
            var versions = new EventCatalogSnapshot[counts.Length];
            for (var i = 0; i < counts.Length; i++)
            {
                var count = counts[i];
                versions[i] = new EventCatalogSnapshot(
                    released.Definitions.Concat(AddedDefinitions.Take(count)),
                    released.Policies,
                    released.VietnameseText.Concat(AddedVietnamese.Take(count * 4)));
            }
            return Array.AsReadOnly(versions);
        }

        private static GameEventDefinition Event(string id, string category, int stage,
            int weight, string[] required, params EventChoiceDefinition[] choices) =>
            new GameEventDefinition(id, category, 2026, stage, weight, 0, 1, 1, 0,
                required, Array.Empty<string>(), choices,
                "event.story." + id + ".title", "event.story." + id + ".body");

        private static EventChoiceDefinition Choice(string eventId, string choiceId,
            EventChoiceReward? reward = null, params string[] tags) =>
            new EventChoiceDefinition(choiceId,
                "event.story." + eventId + ".choice." + choiceId,
                eventId + "-" + choiceId, tags, reward);

        private static KeyValuePair<string, string> Text(string id, string suffix, string value) =>
            new KeyValuePair<string, string>("event.story." + id + "." + suffix, value);
    }
}
