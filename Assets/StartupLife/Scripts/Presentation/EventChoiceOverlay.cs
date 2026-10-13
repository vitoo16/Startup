#nullable enable
using System;
using System.Collections.Generic;
using TMPro;
using StartupLife.Integration;
using UnityEngine;
using UnityEngine.UI;

namespace StartupLife.Presentation
{
    // A blocking, localized V4 choice surface. Installed only when the same
    // authoritative V4 gateway is wired into the production command flow.
    // No event identifiers, eligibility rules or choices are authored by UI.
    public sealed class EventChoiceOverlay : MonoBehaviour
    {
        private const float MinTouchHeight = 96f;
        private EventV4CommitGateway gateway = null!;
        private ICommandIdSource commandIds = null!;
        private TMP_FontAsset font = null!;
        private GameObject panel = null!;
        private RectTransform choiceContent = null!;
        private ScrollRect choiceScroll = null!;
        private LocalizedKeyLabel status = null!;
        private LocalizedKeyLabel heading = null!;
        private LocalizedKeyLabel storyBody = null!;
        private EventPendingChoiceSnapshot? pending;
        private string displayedTicket = "";
        private string uncertainCommand = "";
        private string uncertainTicket = "";
        private string uncertainChoice = "";
        private readonly List<ChoiceButton> buttons = new List<ChoiceButton>();

        private sealed class ChoiceButton
        {
            internal readonly Button Button;
            internal readonly LocalizedKeyLabel Label;
            internal string ChoiceId = "";

            internal ChoiceButton(Button button, LocalizedKeyLabel label)
            {
                Button = button;
                Label = label;
            }
        }

        public static EventChoiceOverlay Install(LifeScreenViewController host,
            EventV4CommitGateway gateway, ICommandIdSource commandIds, TMP_Text fontSource)
        {
            if (host == null || gateway == null || commandIds == null ||
                fontSource == null || fontSource.font == null)
                throw new ArgumentException("Pending event UI needs an existing life view, font and V4 commands.");
            var overlay = host.GetComponent<EventChoiceOverlay>();
            if (overlay == null) overlay = host.gameObject.AddComponent<EventChoiceOverlay>();
            if (overlay.panel == null)
            {
                overlay.font = fontSource.font;
                overlay.Build();
            }
            overlay.gateway = gateway;
            overlay.commandIds = commandIds;
            overlay.Refresh();
            return overlay;
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Refresh()
        {
            if (panel == null || gateway == null) return;
            try
            {
                var current = gateway.ReadPendingChoice();
                if (current == null)
                {
                    pending = null;
                    displayedTicket = "";
                    uncertainCommand = "";
                    panel.SetActive(false);
                    return;
                }

                pending = current;
                DisplayEventText(heading, current,
                    current.TitleKey.Length > 0 ? current.TitleKey : "event.choice.title");
                DisplayEventText(storyBody, current,
                    current.BodyKey.Length > 0 ? current.BodyKey : "event.choice.prompt");
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
                if (displayedTicket != current.TicketId)
                {
                    displayedTicket = current.TicketId;
                    uncertainCommand = "";
                    BindChoices(current);
                    // A newly offered story must begin at its heading, not at the
                    // previous ticket's scroll offset.
                    choiceScroll.StopMovement();
                    choiceScroll.verticalNormalizedPosition = 1f;
                }
                status.SetKey(uncertainCommand.Length > 0 &&
                    uncertainTicket == current.TicketId
                        ? "event.choice.retry" : "event.choice.prompt");
                SetButtonsEnabled(true);
            }
            catch (Exception error)
            {
                // An unverified checkpoint never silently dismisses a choice.
                Debug.LogWarning("Pending event checkpoint unavailable: " + error.Message);
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
                pending = null;
                status.SetKey("event.choice.unavailable");
                SetButtonsEnabled(false);
            }
        }

        private void Resolve(string choiceId)
        {
            if (pending == null) return;
            var selected = pending;
            var sameUncertain = uncertainCommand.Length != 0 &&
                uncertainTicket == selected.TicketId && uncertainChoice == choiceId;
            var id = sameUncertain ? uncertainCommand : commandIds.Next("event-choice");
            if (!sameUncertain)
            {
                uncertainCommand = id;
                uncertainTicket = selected.TicketId;
                uncertainChoice = choiceId;
            }
            SetButtonsEnabled(false);
            try
            {
                var result = gateway.Resolve(selected.RunId, id, selected.Revision,
                    selected.TicketId, choiceId);
                switch (result.Status)
                {
                    case EventCommitStatus.Committed:
                    case EventCommitStatus.AlreadyCommitted:
                        uncertainCommand = "";
                        Refresh();
                        break;
                    case EventCommitStatus.RecoveryRequired:
                    case EventCommitStatus.PersistenceFailed:
                        status.SetKey("event.choice.retry");
                        SetButtonsEnabled(true);
                        break;
                    default:
                        uncertainCommand = "";
                        Refresh();
                        if (pending != null) status.SetKey("event.choice.changed");
                        break;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Pending event resolution unavailable: " + error.Message);
                status.SetKey("event.choice.unavailable");
                SetButtonsEnabled(false);
            }
        }

        private void BindChoices(EventPendingChoiceSnapshot choice)
        {
            for (var i = 0; i < choice.Choices.Count; i++)
            {
                if (i == buttons.Count) buttons.Add(CreateChoiceButton());
                var slot = buttons[i];
                var id = choice.Choices[i].Id;
                slot.ChoiceId = id;
                slot.Button.gameObject.SetActive(true);
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => Resolve(id));
                DisplayEventText(slot.Label, choice, choice.Choices[i].LabelKey);
            }
            for (var i = choice.Choices.Count; i < buttons.Count; i++)
                buttons[i].Button.gameObject.SetActive(false);
        }

        private static void DisplayEventText(LocalizedKeyLabel label,
            EventPendingChoiceSnapshot choice, string key)
        {
            if (choice.ArchivedVietnamese.TryGetValue(key, out var original))
                label.SetFrozenVietnamese(original);
            else label.SetKey(key);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (var button in buttons)
                if (button.Button.gameObject.activeSelf)
                    button.Button.interactable = enabled &&
                        (uncertainCommand.Length == 0 || button.ChoiceId == uncertainChoice);
        }

        private void Build()
        {
            panel = Node("EventChoiceOverlay", transform,
                typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            panel.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.03f, 0.03f);
            rect.anchorMax = new Vector2(0.97f, 0.97f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var column = panel.GetComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(22, 22, 22, 22);
            column.spacing = 16;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            heading = LocalizedKeyLabel.EnsureFor(
                TextNode("Title", panel.transform, 40, 76));
            status = LocalizedKeyLabel.EnsureFor(
                TextNode("ChoiceStatus", panel.transform, 28, 110));

            var eventScroll = MobileScrollViewFactory.Create(panel.transform,
                "EventChoiceScroll", new Color(0.12f, 0.18f, 0.25f, 0.95f),
                new RectOffset(12, 12, 14, 14), 14f, TextAnchor.UpperCenter, 35f);
            choiceContent = (RectTransform)eventScroll.Content.transform;
            choiceScroll = eventScroll.Scroll;
            var content = eventScroll.Content;

            var storyText = TextNode("EventStory", content.transform, 29, 190);
            // The scroll content must grow to the full localized story height.
            // A fixed preferred height with Ellipsis silently hides long events.
            storyText.overflowMode = TextOverflowModes.Overflow;
            storyText.GetComponent<LayoutElement>().preferredHeight = -1;
            storyBody = LocalizedKeyLabel.EnsureFor(storyText);

            panel.SetActive(false);
        }

        private ChoiceButton CreateChoiceButton()
        {
            var go = Node("EventChoiceButton", choiceContent,
                typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = new Color(0.16f, 0.45f, 0.50f);
            var size = go.GetComponent<LayoutElement>();
            size.minHeight = MinTouchHeight;
            size.preferredHeight = 120;
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var label = TextNode("ChoiceLabel", go.transform, 30, 120);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16, 6);
            rect.offsetMax = new Vector2(-16, -6);
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            return new ChoiceButton(button, LocalizedKeyLabel.EnsureFor(label));
        }

        private TMP_Text TextNode(string name, Transform parent, float size, float height)
        {
            var go = Node(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private static GameObject Node(string name, Transform parent, params Type[] types)
        {
            var components = new Type[types.Length + 1];
            components[0] = typeof(RectTransform);
            Array.Copy(types, 0, components, 1, types.Length);
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
