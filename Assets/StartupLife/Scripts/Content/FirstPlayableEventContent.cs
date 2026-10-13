#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Content
{
    // Authored MVP narrative, independent of Unity and the save writer.
    // Keeping localized copy beside each definition prevents UI/catalog drift.
    // Authored optional rewards are settled only by the verified V3/V4 gateway.
    public static class FirstPlayableEventContent
    {
        private sealed class Choice
        {
            public readonly string Id;
            public readonly string Text;
            public readonly string[] Tags;
            public readonly EventChoiceReward? Reward;

            public Choice(string id, string text, params string[] tags)
            {
                Id = id; Text = text; Tags = tags;
            }

            // Only authored event data supplies an effect; the V4 gateway is
            // still solely responsible for an authenticated payout.
            public Choice(string id, string text, EventChoiceReward reward,
                params string[] tags) : this(id, text, tags)
            {
                Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            }
        }

        private sealed class Story
        {
            public readonly string Id;
            public readonly string Category;
            public readonly int Stage;
            public readonly int Weight;
            public readonly string[] Required;
            public readonly string Title;
            public readonly string Body;
            public readonly Choice[] Choices;

            public Story(string id, string category, int stage, int weight,
                string[] required, string title, string body, params Choice[] choices)
            {
                Id = id; Category = category; Stage = stage; Weight = weight;
                Required = required; Title = title; Body = body; Choices = choices;
            }
        }

        private static readonly Story[] Stories =
        {
            new Story("welcome-neighbor", "life", 1, 12,
                new[] { "character-created" }, "Lời mời từ hàng xóm",
                "Một người hàng xóm rủ bạn tham dự buổi sinh hoạt khu phố vào cuối tuần.",
                new Choice("join", "Đến làm quen mọi người", "event/contact"),
                new Choice("decline", "Ở nhà nghỉ ngơi")),
            new Story("coffee-connection", "life", 1, 8,
                new[] { "event/contact" }, "Cuộc gặp tại quán cà phê",
                "Người quen ở khu phố giới thiệu bạn với một nhóm thường trao đổi ý tưởng làm ăn.",
                new Choice("listen", "Tham gia nghe và trao đổi", "event/networking"),
                new Choice("later", "Hẹn gặp vào dịp khác")),
            new Story("mentor-introduction", "career", 1, 6,
                new[] { "event/networking" }, "Lời khuyên từ người đi trước",
                "Một người nhiều kinh nghiệm đề nghị chia sẻ những bài học khi mới đi làm.",
                new Choice("ask", "Xin một buổi trò chuyện", "event/mentor"),
                new Choice("thank", "Cảm ơn và tiếp tục tự tìm hiểu")),
            new Story("workplace-lunch", "career", 1, 10,
                new[] { "employed" }, "Bữa trưa cùng đồng nghiệp",
                "Đồng nghiệp rủ bạn ăn trưa để kể về các dự án đang triển khai.",
                new Choice("connect", "Ngồi lại và lắng nghe", "event/teamwork"),
                new Choice("rest", "Dành giờ nghỉ cho bản thân")),
            new Story("salary-reflection", "career", 1, 8,
                new[] { "employed" }, "Kế hoạch sau ngày nhận lương",
                "Bạn có dịp nghĩ về mục tiêu tiết kiệm và dự định tương lai.",
                new Choice("plan", "Viết một kế hoạch nhỏ", "event/planning"),
                new Choice("reflect", "Cứ suy nghĩ thêm, chưa quyết định")),
            new Story("weekend-study", "study", 1, 9,
                new[] { "course-active" }, "Nhóm học cuối tuần",
                "Các học viên cùng khóa muốn lập nhóm để chia sẻ kinh nghiệm học tập.",
                new Choice("join", "Tham gia nhóm học", "event/study-group"),
                new Choice("solo", "Tiếp tục học theo nhịp riêng")),
            new Story("study-friend", "study", 1, 7,
                new[] { "event/study-group" }, "Người bạn cùng khóa",
                "Một người trong nhóm gợi ý trao đổi các ghi chú và phương pháp luyện tập.",
                new Choice("share", "Cùng chia sẻ ghi chú", "event/study-circle"),
                new Choice("keep", "Lịch sự từ chối")),
            new Story("first-market-day", "business", 2, 9,
                new[] { "business-active" }, "Phiên chợ địa phương",
                "Một khu chợ gần nhà mời các chủ cơ sở nhỏ đến gặp gỡ khách hàng.",
                new Choice("visit", "Đến tìm hiểu nhu cầu", "event/local-customer"),
                new Choice("skip", "Tập trung vào hoạt động hiện tại")),
            new Story("supplier-chat", "business", 2, 7,
                new[] { "business-active", "event/local-customer" },
                "Cuộc trao đổi với nhà cung cấp",
                "Tại một buổi gặp mặt, bạn được giới thiệu cách xây dựng quan hệ nguồn hàng lâu dài.",
                new Choice("learn", "Lắng nghe và giữ liên lạc", "event/supplier"),
                new Choice("wait", "Chờ đến khi kinh doanh ổn định")),
            new Story("customer-feedback", "business", 2, 8,
                new[] { "business-active" }, "Góp ý từ khách hàng",
                "Một khách hàng chủ động chia sẻ điều họ hài lòng và điều còn bất tiện.",
                new Choice("listen", "Ghi nhận phản hồi", "event/customer-listening"),
                new Choice("later", "Hẹn trao đổi vào lần tới")),
            new Story("founder-community", "life", 1, 9,
                new[] { "unemployed", "character-created" }, "Cộng đồng làm việc tự do",
                "Bạn được mời tham dự buổi gặp của những người đang tìm hướng đi mới.",
                new Choice("attend", "Đến nghe các câu chuyện", "event/founder-peer"),
                new Choice("decline", "Tìm hiểu thêm trước khi tham dự")),
            new Story("pitch-practice", "life", 1, 6,
                new[] { "event/founder-peer" }, "Thử kể ý tưởng của mình",
                "Một người trong cộng đồng đề nghị bạn thử kể ý tưởng trong hai phút.",
                new Choice("practice", "Thử trình bày ý tưởng", "event/pitch-practice"),
                new Choice("observe", "Lắng nghe người khác trình bày")),
            new Story("mentor-follow-up", "career", 1, 7,
                new[] { "event/mentor" }, "Một buổi hẹn với người hướng dẫn",
                "Sau giờ làm, người hướng dẫn hẹn bạn ở quán nước gần văn phòng để trao đổi về cách phát triển nghề nghiệp.",
                new Choice("prepare", "Chuẩn bị câu hỏi trước buổi gặp", "event/mentor-notes"),
                new Choice("listen", "Đến nghe và ghi nhớ lời khuyên")),
            new Story("team-code-review", "career", 1, 8,
                new[] { "employed", "event/teamwork" }, "Buổi chia sẻ trong nhóm",
                "Đồng nghiệp rủ bạn cùng xem lại cách cả nhóm phối hợp và rút kinh nghiệm sau một dự án.",
                new Choice("share", "Chia sẻ một bài học nhỏ", "event/peer-learning"),
                new Choice("observe", "Lắng nghe mọi người trước")),
            new Story("savings-checkin", "life", 1, 8,
                new[] { "event/planning" }, "Cuốn sổ kế hoạch tháng",
                "Khi kiểm tra lại ghi chú, bạn nhận ra đã đến lúc xác định mục tiêu tiết kiệm rõ ràng hơn.",
                new Choice("review", "Rà soát các ưu tiên", "event/budget-awareness"),
                new Choice("postpone", "Để dành cho một buổi khác")),
            new Story("study-presentation", "study", 1, 7,
                new[] { "event/study-circle" }, "Buổi chia sẻ cuối khóa",
                "Nhóm học đề nghị mỗi người kể một điều hữu ích họ đã tự khám phá trong quá trình luyện tập.",
                new Choice("present", "Tình nguyện chia sẻ", "event/study-presenter"),
                new Choice("attend", "Đến nghe phần trình bày của bạn học")),
            new Story("service-improvement", "business", 2, 8,
                new[] { "business-active", "event/customer-listening" },
                "Một ý tưởng cải thiện dịch vụ",
                "Sau khi đọc phản hồi, bạn có cơ hội bàn cùng cộng sự về cách phục vụ thuận tiện hơn.",
                new Choice("discuss", "Ghi lại một hướng cải thiện", "event/service-ideas"),
                new Choice("consider", "Chờ thêm ý kiến từ khách hàng")),
            new Story("supplier-follow-up", "business", 2, 6,
                new[] { "business-active", "event/supplier" },
                "Giữ liên lạc với nhà cung cấp",
                "Một đối tác gửi lời mời tham quan nơi đóng gói và giao hàng để hiểu quy trình làm việc.",
                new Choice("visit", "Sắp xếp buổi tham quan", "event/supplier-insight"),
                new Choice("later", "Hẹn vào ngày ít bận hơn")),
            new Story("pitch-feedback", "life", 1, 7,
                new[] { "event/pitch-practice" }, "Lời góp ý cho ý tưởng",
                "Một người trong buổi gặp gửi bạn vài câu hỏi để giúp ý tưởng trở nên dễ hiểu hơn.",
                new Choice("revise", "Chỉnh cách trình bày", "event/pitch-feedback"),
                new Choice("reflect", "Ghi lại để suy nghĩ tiếp")),
            new Story("community-collaboration", "life", 1, 6,
                new[] { "event/networking", "event/founder-peer" },
                "Lời mời hỗ trợ cộng đồng",
                "Nhóm làm việc tự do rủ bạn cùng chuẩn bị một buổi chia sẻ kinh nghiệm cho những người mới bắt đầu.",
                new Choice("help", "Góp sức chuẩn bị chương trình", "event/community-trust"),
                new Choice("observe", "Đến dự để học hỏi trước")),
            // New authored-release events do not alter the archived first 20.
            new Story("community-starter-grant", "life", 1, 5,
                new[] { "event/pitch-feedback" },
                "Khoản hỗ trợ thử ý tưởng",
                "Sau buổi trao đổi, một quỹ cộng đồng đề nghị hỗ trợ một khoản nhỏ để bạn thử nghiệm ý tưởng kinh doanh.",
                new Choice("accept", "Nhận 100.000 ₫ hỗ trợ thử nghiệm",
                    new EventChoiceReward(100_000L, 0), "event/starter-grant"),
                new Choice("decline", "Cảm ơn và tự chuẩn bị nguồn vốn")),
            new Story("peer-coaching", "career", 1, 6,
                new[] { "employed", "event/teamwork" },
                "Hướng dẫn đồng nghiệp mới",
                "Trưởng nhóm mời bạn chỉ dẫn một đồng nghiệp mới cách phối hợp trong công việc.",
                new Choice("mentor", "Đứng ra hướng dẫn (+60 XP nghề)",
                    new EventChoiceReward(0, 60), "event/peer-coach"),
                new Choice("observe", "Cùng theo dõi, chưa đứng ra hướng dẫn")),
            new Story("local-promotion-print", "business", 2, 6,
                new[] { "business-active", "event/customer-listening" },
                "Thử một bảng giới thiệu nhỏ",
                "Một khách hàng gợi ý bạn in bảng giới thiệu gọn để mọi người dễ biết sản phẩm và cách liên hệ.",
                new Choice("print", "Chi 40.000 ₫ để thử in bảng giới thiệu",
                    new EventChoiceReward(-40_000L, 0), "event/promotion-material"),
                new Choice("wait", "Để dành tiền, chưa in lúc này")),
            new Story("mentor-workshop-materials", "career", 1, 6,
                new[] { "employed", "event/mentor-notes" },
                "Tài liệu cho buổi hướng dẫn",
                "Người hướng dẫn mời bạn tham gia buổi trao đổi ngắn trong giờ nghỉ. Bạn có thể góp tiền in tài liệu thực hành.",
                new Choice("contribute", "Góp 25.000 ₫, nhận 45 XP nghề",
                    new EventChoiceReward(-25_000L, 45), "event/mentor-workshop"),
                new Choice("listen", "Đến nghe và ghi chép, không mua tài liệu")),
            new Story("supplier-quality-sample", "business", 2, 6,
                new[] { "business-active", "event/supplier-insight" },
                "Thử mẫu đóng gói mới",
                "Nhà cung cấp cho bạn xem mẫu hộp và túi giao hàng phù hợp với sản phẩm. Bạn được chọn mua một bộ mẫu để đánh giá.",
                new Choice("sample", "Chi 70.000 ₫ để thử bộ mẫu",
                    new EventChoiceReward(-70_000L, 0), "event/packaging-sample"),
                new Choice("compare", "Ghi lại thông tin và so sánh sau")),
            new Story("customer-trust-repair", "business", 2, 6,
                new[] { "business-active", "event/customer-listening" },
                "Đơn hàng cần chăm sóc lại",
                "Một khách hàng phản ánh khâu giao nhận chưa thuận tiện. Bạn có thể hỗ trợ chi phí đổi trả hoặc trực tiếp hẹn tìm cách xử lý.",
                new Choice("compensate", "Chi 55.000 ₫ hỗ trợ đổi trả",
                    new EventChoiceReward(-55_000L, 0), "event/customer-care-complete"),
                new Choice("follow-up", "Liên hệ để tìm giải pháp không tốn phí",
                    "event/customer-care-followup")),
            // A free recovery path must lead somewhere; it may not silently
            // turn the player's customer-service choice into a dead end.
            new Story("customer-care-follow-up", "business", 2, 7,
                new[] { "business-active", "event/customer-care-followup" },
                "Cuộc hẹn giải quyết phản hồi",
                "Sau khi bạn liên hệ, khách hàng đồng ý trao đổi rõ hơn về khâu giao nhận. Cả hai có cơ hội thống nhất cách xử lý phù hợp.",
                new Choice("agree", "Thống nhất cách giao nhận lần sau",
                    "event/customer-care-agreed"),
                new Choice("review", "Ghi lại góp ý để điều chỉnh quy trình",
                    "event/customer-care-reviewed")),
            new Story("supplier-sample-review", "business", 2, 6,
                new[] { "business-active", "event/packaging-sample" },
                "Đánh giá bộ mẫu đóng gói",
                "Bạn xem lại bộ mẫu đã đặt và nhận thấy có vài lựa chọn phù hợp với cách giao hàng hiện tại.",
                new Choice("select", "Ghi nhận kiểu đóng gói phù hợp",
                    "event/packaging-reviewed"),
                new Choice("revise", "Nhờ nhà cung cấp điều chỉnh thêm",
                    "event/packaging-revision")),
            new Story("mentor-workshop-reflection", "career", 1, 6,
                new[] { "employed", "event/mentor-workshop" },
                "Ứng dụng bài học tại nơi làm việc",
                "Sau buổi trao đổi, bạn có dịp cùng đồng nghiệp nhìn lại một tình huống phối hợp trong dự án.",
                new Choice("share", "Chia sẻ ghi chú thực hành",
                    "event/workshop-notes-shared"),
                new Choice("reflect", "Tự ôn lại bài học",
                    "event/workshop-self-review")),
            // Significant but recoverable setbacks: a paid response is never
            // mandatory, and the zero-cost response starts an authored recovery arc.
            new Story("delivery-incident", "business", 2, 5,
                new[] { "business-active", "event/customer-care-reviewed" },
                "Đơn hàng bị giao chậm",
                "Một đơn hàng đến tay khách trễ hơn dự kiến. Khách muốn biết cách cơ sở xử lý để họ có thể tiếp tục yên tâm đặt hàng.",
                new Choice("support", "Chi 90.000 ₫ hỗ trợ giao lại",
                    new EventChoiceReward(-90_000L, 0), "event/delivery-support"),
                new Choice("coordinate", "Gọi khách và hẹn phương án xử lý",
                    "event/delivery-recovery-contact")),
            new Story("delivery-incident-follow-up", "business", 2, 6,
                new[] { "business-active", "event/delivery-recovery-contact" },
                "Xác nhận phương án giao hàng",
                "Sau cuộc gọi, bạn có thể phối hợp lại lịch giao với khách hoặc rà soát bước nhận đơn để hạn chế trễ hẹn.",
                new Choice("schedule", "Thống nhất lại thời điểm giao",
                    "event/delivery-recovery-agreed"),
                new Choice("review", "Rà soát cách nhận và chuyển đơn",
                    "event/delivery-process-reviewed")),
            new Story("supplier-backorder", "business", 2, 5,
                new[] { "business-active", "event/packaging-reviewed" },
                "Lô hàng đóng gói đến muộn",
                "Nhà cung cấp báo thiếu một số mẫu đóng gói đã chọn. Bạn cân nhắc đặt bộ thay thế hoặc tìm nguồn khác để tránh phụ thuộc vào một đối tác.",
                new Choice("reserve", "Chi 120.000 ₫ đặt bộ mẫu thay thế",
                    new EventChoiceReward(-120_000L, 0), "event/supply-alternative-reserved"),
                new Choice("source", "Trao đổi với nguồn hàng khác, chưa đặt tiền",
                    "event/supply-recovery-contact")),
            new Story("supplier-backorder-follow-up", "business", 2, 6,
                new[] { "business-active", "event/supply-recovery-contact" },
                "Tìm phương án nguồn hàng dự phòng",
                "Một đơn vị khác gửi thông tin về quy cách và lịch giao. Bạn có thể so sánh trước khi quyết định có đặt hàng hay không.",
                new Choice("compare", "So sánh quy cách và điều kiện giao",
                    "event/supply-options-compared"),
                new Choice("defer", "Theo dõi thêm, không cam kết mua",
                    "event/supply-options-deferred"))
        };

        private static readonly IReadOnlyList<GameEventDefinition> DefinitionsValue = BuildDefinitions();
        private static readonly IReadOnlyList<KeyValuePair<string, string>> VietnameseValue = BuildVietnamese();

        public static IReadOnlyList<GameEventDefinition> Definitions => DefinitionsValue;
        public static IReadOnlyList<KeyValuePair<string, string>> VietnameseEntries => VietnameseValue;
        public static IReadOnlyList<EventCategoryPolicy> Policies { get; } =
            Array.AsReadOnly(new[]
            {
                new EventCategoryPolicy("life", 2, 7, 2),
                new EventCategoryPolicy("career", 3, 7, 1),
                new EventCategoryPolicy("study", 3, 7, 1),
                new EventCategoryPolicy("business", 3, 7, 1)
            });

        private static IReadOnlyList<GameEventDefinition> BuildDefinitions()
        {
            var definitions = Stories.Select(x => new GameEventDefinition(
                x.Id, x.Category, 2026, x.Stage, x.Weight, 0, 1, 1, 0,
                x.Required, Array.Empty<string>(),
                x.Choices.Select(choice => new EventChoiceDefinition(
                    choice.Id, ChoiceKey(x.Id, choice.Id), x.Id + "-" + choice.Id,
                    choice.Tags, choice.Reward)),
                TitleKey(x.Id), BodyKey(x.Id))).ToArray();
            if (definitions.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
                throw new InvalidOperationException("Duplicate event story IDs.");
            ValidateAuthoredPaths(definitions);
            return Array.AsReadOnly(definitions);
        }

        // Authoring-only integrity check. Runtime eligibility still belongs
        // exclusively to the receipt-verified V4 journal/selector. A tag chain
        // must have a reachable source, and no paid decision may trap a run.
        // This is a coarse dependency check: employment and business state
        // gates are deliberately left to AuthoritativeEventContext.
        // Public to the engine-free content checks; this validates authored
        // dependencies, not player state or V4 journal replay.
        public static void ValidateAuthoredPaths(IReadOnlyList<GameEventDefinition> definitions)
        {
            if (definitions == null || definitions.Any(x => x == null))
                throw new ArgumentException("Event authoring requires non-null definitions.");
            foreach (var entry in definitions)
                if (entry.Choices.Any(choice => choice.Reward != null) &&
                    entry.Choices.All(choice => choice.Reward != null))
                    throw new InvalidOperationException(
                        "A financial event must offer a no-cost resolution: " + entry.Id);

            // A single-occurrence story has mutually exclusive choices.
            // A later story requiring tags granted ONLY by opposite choices of
            // that story can never be offered, even though both tags appear in
            // the naive fixed-point union below.
            var producers = new Dictionary<string, HashSet<GameEventDefinition>>(
                StringComparer.Ordinal);
            foreach (var source in definitions)
                foreach (var choice in source.Choices)
                    foreach (var tag in choice.GrantedTags)
                    {
                        if (!producers.TryGetValue(tag, out var owners))
                            producers.Add(tag, owners = new HashSet<GameEventDefinition>());
                        owners.Add(source);
                    }
            foreach (var entry in definitions)
            {
                var prerequisites = entry.RequiredTags.Where(tag =>
                    tag.StartsWith("event/", StringComparison.Ordinal)).ToArray();
                for (var i = 0; i < prerequisites.Length; i++)
                    for (var j = i + 1; j < prerequisites.Length; j++)
                    {
                        if (!producers.TryGetValue(prerequisites[i], out var left) ||
                            !producers.TryGetValue(prerequisites[j], out var right) ||
                            left.Count != 1 || right.Count != 1)
                            continue;
                        var origin = left.First();
                        if (!right.Contains(origin) || origin.MaxOccurrences != 1)
                            continue;
                        if (!origin.Choices.Any(choice =>
                            choice.GrantedTags.Contains(prerequisites[i]) &&
                            choice.GrantedTags.Contains(prerequisites[j])))
                            throw new InvalidOperationException(
                                "Event requires mutually exclusive outcomes: " + entry.Id);
                    }
            }

            var availableTags = new HashSet<string>(StringComparer.Ordinal);
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            bool changed;
            do
            {
                changed = false;
                foreach (var entry in definitions)
                {
                    if (reachable.Contains(entry.Id) ||
                        entry.RequiredTags.Any(tag => tag.StartsWith("event/",
                            StringComparison.Ordinal) && !availableTags.Contains(tag)))
                        continue;
                    reachable.Add(entry.Id);
                    foreach (var choice in entry.Choices)
                        foreach (var tag in choice.GrantedTags)
                            availableTags.Add(tag);
                    changed = true;
                }
            } while (changed);

            var inaccessible = definitions.Where(entry => !reachable.Contains(entry.Id))
                .Select(entry => entry.Id).ToArray();
            if (inaccessible.Length != 0)
                throw new InvalidOperationException("Unreachable event authoring path: " +
                    string.Join(", ", inaccessible));
        }

        private static IReadOnlyList<KeyValuePair<string, string>> BuildVietnamese()
        {
            var entries = new List<KeyValuePair<string, string>>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var story in Stories)
            {
                Add(entries, keys, TitleKey(story.Id), story.Title);
                Add(entries, keys, BodyKey(story.Id), story.Body);
                foreach (var choice in story.Choices)
                    Add(entries, keys, ChoiceKey(story.Id, choice.Id), choice.Text);
            }
            return entries.AsReadOnly();
        }

        private static void Add(List<KeyValuePair<string, string>> entries,
            ISet<string> keys, string key, string value)
        {
            if (!keys.Add(key) || string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Duplicate or empty event localization.");
            entries.Add(new KeyValuePair<string, string>(key, value));
        }

        private static string TitleKey(string id) => "event.story." + id + ".title";
        private static string BodyKey(string id) => "event.story." + id + ".body";
        private static string ChoiceKey(string id, string choice) =>
            "event.story." + id + ".choice." + choice;
    }
}
