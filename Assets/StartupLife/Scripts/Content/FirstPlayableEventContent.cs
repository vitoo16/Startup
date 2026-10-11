#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Content
{
    // Authored MVP narrative, independent of Unity and the save writer.
    // Keeping localized copy beside each definition prevents UI/catalog drift;
    // no money, XP or career claims are silently synthesized by event choices.
    public static class FirstPlayableEventContent
    {
        private sealed class Choice
        {
            public readonly string Id;
            public readonly string Text;
            public readonly string[] Tags;

            public Choice(string id, string text, params string[] tags)
            {
                Id = id; Text = text; Tags = tags;
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
                new Choice("observe", "Lắng nghe người khác trình bày"))
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
                    choice.Tags)),
                TitleKey(x.Id), BodyKey(x.Id))).ToArray();
            if (definitions.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
                throw new InvalidOperationException("Duplicate event story IDs.");
            return Array.AsReadOnly(definitions);
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
