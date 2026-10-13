#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
        private const float RevealDurationSeconds = 0.24f;
        private const float InitialScale = 0.97f;
        private CanvasGroup revealGroup = null!;
        private RectTransform panelRect = null!;
        private bool revealing;
        private float revealElapsed;
        private EventV4CommitGateway gateway = null!;
        private ICommandIdSource commandIds = null!;
        private Action refreshCommittedState = null!;
        private TMP_FontAsset font = null!;
        private GameObject panel = null!;
        private GameObject scrim = null!;
        private Image scrimImage = null!;
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
        private ChoiceButton reloadButton = null!;

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
            // Reuse the host's authoritative gameplay/finance readouts. The
            // event overlay must not maintain a shadow cash/XP projection.
            overlay.refreshCommittedState = host.Refresh;
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
                    uncertainTicket = "";
                    uncertainChoice = "";
                    reloadButton.Button.gameObject.SetActive(false);
                    panel.SetActive(false);
                    scrim.SetActive(false);
                    ResetReveal();
                    return;
                }

                pending = current;
                reloadButton.Button.gameObject.SetActive(false);
                DisplayEventText(heading, current,
                    current.TitleKey.Length > 0 ? current.TitleKey : "event.choice.title");
                DisplayEventText(storyBody, current,
                    current.BodyKey.Length > 0 ? current.BodyKey : "event.choice.prompt");
                scrim.SetActive(true);
                scrim.transform.SetAsLastSibling();
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
                if (displayedTicket != current.TicketId)
                {
                    displayedTicket = current.TicketId;
                    // A transient failed read must not discard an ambiguous
                    // resolution ID for the very same pending ticket.
                    if (uncertainTicket != current.TicketId)
                        uncertainCommand = "";
                    BindChoices(current);
                    // A newly offered story must begin at its heading, not at the
                    // previous ticket's scroll offset.
                    choiceScroll.StopMovement();
                    choiceScroll.verticalNormalizedPosition = 1f;
                    BeginReveal();
                }
                status.SetKey(uncertainCommand.Length > 0 &&
                    uncertainTicket == current.TicketId
                        ? "event.choice.retry" : ChoiceGuidanceKey(current));
                SetButtonsEnabled(true);
            }
            catch (Exception error)
            {
                // An unverified checkpoint never silently dismisses a choice.
                Debug.LogWarning("Pending event checkpoint unavailable: " + error.Message);
                scrim.SetActive(true);
                scrim.transform.SetAsLastSibling();
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
                ResetReveal();
                pending = null;
                displayedTicket = "";
                // Do not expose stale authored copy or choices from a previous
                // ticket when the authoritative V4 checkpoint is unavailable.
                heading.SetKey("event.choice.title");
                storyBody.SetKey("event.choice.prompt");
                foreach (var button in buttons)
                    button.Button.gameObject.SetActive(false);
                reloadButton.Button.gameObject.SetActive(true);
                choiceScroll.StopMovement();
                choiceScroll.verticalNormalizedPosition = 1f;
                status.SetKey("event.choice.unavailable");
                SetButtonsEnabled(false);
            }
        }

        // Cosmetic reveal only: authoritative pending and CAS writes are never
        // delayed by a scene animation. The CanvasGroup intercepts touches
        // beneath the modal, but choices cannot be clicked until reveal ends.
        private void BeginReveal()
        {
            revealing = true;
            revealElapsed = 0f;
            revealGroup.alpha = 0.15f;
            scrimImage.color = new Color(0.01f, 0.02f, 0.05f, 0f);
            revealGroup.interactable = false;
            revealGroup.blocksRaycasts = true;
            panelRect.localScale = Vector3.one * InitialScale;
        }

        private void LateUpdate()
        {
            if (!revealing || panel == null || !panel.activeInHierarchy) return;
            revealElapsed += Time.unscaledDeltaTime;
            var normalized = Mathf.Clamp01(revealElapsed / RevealDurationSeconds);
            var eased = normalized * normalized * (3f - 2f * normalized);
            revealGroup.alpha = Mathf.Lerp(0.15f, 1f, eased);
            scrimImage.color = new Color(0.01f, 0.02f, 0.05f, 0.72f * eased);
            panelRect.localScale = Vector3.one * Mathf.Lerp(InitialScale, 1f, eased);
            if (normalized < 1f) return;
            revealing = false;
            revealGroup.interactable = true;
        }

        private void ResetReveal()
        {
            revealing = false;
            revealElapsed = 0f;
            if (revealGroup == null) return;
            revealGroup.alpha = 1f;
            if (scrimImage != null)
                scrimImage.color = new Color(0.01f, 0.02f, 0.05f, 0.72f);
            revealGroup.interactable = true;
            revealGroup.blocksRaycasts = true;
            if (panelRect != null) panelRect.localScale = Vector3.one;
        }

        private static string ChoiceGuidanceKey(EventPendingChoiceSnapshot current)
        {
            var reasons = current.UnavailableChoiceReasons.Values;
            if (reasons.Contains("event.choice.insufficient_funds"))
                return "event.choice.insufficient_funds";
            if (reasons.Contains("event.choice.after_shift_required"))
                return "event.choice.after_shift_required";
            if (reasons.Contains("event.choice.time_unavailable"))
                return "event.choice.time_unavailable";
            return current.UnavailableChoiceReasons.Count > 0
                ? "event.choice.reward_unavailable" : "event.choice.prompt";
        }

        private void Resolve(string choiceId)
        {
            if (pending == null || pending.UnavailableChoiceReasons.ContainsKey(choiceId))
                return;
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
                        // A reward may have changed the wallet, Career XP and
                        // promotion. Refresh the whole screen after the V4 CAS,
                        // not only the dialog that initiated the choice.
                        refreshCommittedState();
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
                // The CAS may have committed before an exception surfaced.
                // Re-read V4 authority; keep the exact command ID for any
                // still-pending matching ticket. Do not mint another command.
                Refresh();
                if (pending == null && !IsOpen)
                {
                    // The write may have succeeded despite the transport
                    // exception. Reconcile all committed finance/career views.
                    refreshCommittedState();
                }
                else if (pending != null && pending.TicketId == selected.TicketId)
                {
                    status.SetKey("event.choice.retry");
                    SetButtonsEnabled(true);
                }
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
                        (pending == null || !pending.UnavailableChoiceReasons.ContainsKey(button.ChoiceId)) &&
                        (uncertainCommand.Length == 0 || button.ChoiceId == uncertainChoice);
        }

        private void Build()
        {
            // Full-screen raycast scrim prevents touch-through at the inset
            // edges. It has no close action: V4 still requires real resolution.
            scrim = Node("EventChoiceModalScrim", transform,
                typeof(Image), typeof(LayoutElement));
            scrimImage = scrim.GetComponent<Image>();
            scrimImage.raycastTarget = true;
            scrim.GetComponent<LayoutElement>().ignoreLayout = true;
            var scrimRect = (RectTransform)scrim.transform;
            scrimRect.anchorMin = Vector2.zero;
            scrimRect.anchorMax = Vector2.one;
            scrimRect.offsetMin = scrimRect.offsetMax = Vector2.zero;

            panel = Node("EventChoiceOverlay", transform,
                typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement),
                typeof(CanvasGroup));
            revealGroup = panel.GetComponent<CanvasGroup>();
            panelRect = (RectTransform)panel.transform;
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

            // Reload is read-only and never dismisses a required decision.
            // It remains reachable when the verified checkpoint cannot load.
            reloadButton = CreateChoiceButton();
            reloadButton.Button.gameObject.name = "EventChoiceReload";
            reloadButton.Label.SetKey("event.choice.reload");
            reloadButton.Button.onClick.AddListener(Refresh);
            reloadButton.Button.gameObject.SetActive(false);

            panel.SetActive(false);
            scrim.SetActive(false);
            ResetReveal();
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
