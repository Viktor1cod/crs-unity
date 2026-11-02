using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>
    /// Main 2D screen: money, garage, the listing board and buying cars.
    /// Builds its own Canvas on game start, no scene setup needed.
    /// The listing details overlay lives in MarketScreen.Details.cs.
    /// </summary>
    public sealed partial class MarketScreen : MonoBehaviour
    {
        private enum Tab { Market, Garage }

        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;
        /// <summary>Below this logical width cards switch to a two-row phone layout.</summary>
        private const float CompactWidth = 1000f;
        private const float ToastSeconds = 2.5f;

        private GameSession session;
        private Language lang;
        private Tab tab = Tab.Market;
        private bool compact;
        private int lastScreenWidth, lastScreenHeight;

        private Transform canvasRoot;
        private Text moneyText, garageText, langLabel, marketTabLabel, garageTabLabel, refreshLabel, toastText;
        private Image marketTabImage, garageTabImage;
        private GameObject refreshButton, toastPanel;
        private RectTransform listContent;
        private SmoothScrollRect scroll;
        private float toastHideAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<MarketScreen>() != null) return;
            var go = new GameObject(nameof(MarketScreen));
            DontDestroyOnLoad(go);
            go.AddComponent<MarketScreen>();
        }

        private void Awake()
        {
            lang = Application.systemLanguage == SystemLanguage.Russian ? Language.Ru : Language.En;
            session = new GameSession(System.Environment.TickCount);
            Ui.LoadFont();
            EnsureEventSystem();
            Build();
            BuildDetails();
            toastPanel.transform.SetAsLastSibling();
            Redraw(resetScroll: true);
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                Redraw(resetScroll: false);
                if (IsDetailsOpen) RedrawDetails(resetScroll: false);
            }
            if (toastPanel.activeSelf && Time.unscaledTime >= toastHideAt) toastPanel.SetActive(false);
        }

        // ---------- construction ----------

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            DontDestroyOnLoad(go);
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasRoot = canvasGo.transform;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = Ui.CreateImage("Background", canvasRoot, Theme.Background);
            Ui.Stretch(background.rectTransform);
            Ui.Vertical(background.gameObject, 12, new RectOffset(24, 24, 16, 16));

            // Row 1: money, garage counter, language
            var top = Ui.CreateRect("TopRow", background.transform);
            Ui.Horizontal(top.gameObject, 20);
            moneyText = Ui.CreateText("Money", top, 40, Theme.Money, FontStyle.Bold);
            garageText = Ui.CreateText("Garage", top, 24, Theme.Muted);
            Ui.Layout(Ui.CreateRect("Spacer", top), flexibleWidth: 1);
            Ui.CreateButton("Language", top, 22, Theme.Tab, ToggleLanguage, out langLabel);

            // Row 2: tabs and refresh
            var tabs = Ui.CreateRect("Tabs", background.transform);
            Ui.Horizontal(tabs.gameObject, 12);
            marketTabImage = (Image)Ui.CreateButton("MarketTab", tabs, 26, Theme.Accent, () => SwitchTab(Tab.Market), out marketTabLabel).targetGraphic;
            garageTabImage = (Image)Ui.CreateButton("GarageTab", tabs, 26, Theme.Tab, () => SwitchTab(Tab.Garage), out garageTabLabel).targetGraphic;
            Ui.Layout(Ui.CreateRect("Spacer", tabs), flexibleWidth: 1);
            refreshButton = Ui.CreateButton("Refresh", tabs, 22, Theme.Tab, OnRefresh, out refreshLabel).gameObject;

            CreateScrollList(background.transform, out scroll, out listContent);

            // Toast at the bottom, above everything
            var toast = Ui.CreateImage("Toast", canvasRoot, Theme.Toast);
            toast.raycastTarget = false;
            var toastRect = toast.rectTransform;
            toastRect.anchorMin = new Vector2(0, 0);
            toastRect.anchorMax = new Vector2(1, 0);
            toastRect.pivot = new Vector2(0.5f, 0);
            toastRect.offsetMin = new Vector2(48, 32);
            toastRect.offsetMax = new Vector2(-48, 104);
            toastText = Ui.CreateText("Text", toast.transform, 24, Theme.Text, FontStyle.Normal, TextAnchor.MiddleCenter);
            Ui.Stretch(toastText.rectTransform, 16, 8, 16, 8);
            toastPanel = toast.gameObject;
            toastPanel.SetActive(false);
        }

        /// <summary>A vertical list that fills the remaining height of its parent column.</summary>
        private static void CreateScrollList(Transform parent, out SmoothScrollRect scrollRect, out RectTransform content)
        {
            var scrollArea = Ui.CreateRect("Scroll", parent);
            Ui.Layout(scrollArea, flexibleHeight: 1);
            scrollRect = scrollArea.gameObject.AddComponent<SmoothScrollRect>();
            var viewport = Ui.CreateImage("Viewport", scrollArea, new Color(0, 0, 0, 0));
            Ui.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Ui.CreateRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            Ui.Vertical(content.gameObject, 12);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.1f;
        }

        // ---------- drawing ----------

        private void Redraw(bool resetScroll)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            compact = LogicalWidth() < CompactWidth;

            var player = session.Player;
            moneyText.text = MoneyFormat.Format(player.Money);
            garageText.text = UiText.Format("top.garage", lang, player.Garage.Count, player.GarageCapacity);
            langLabel.text = UiText.Get("lang.switch", lang);
            marketTabLabel.text = UiText.Get("tab.market", lang);
            garageTabLabel.text = UiText.Get("tab.garage", lang);
            refreshLabel.text = UiText.Get("btn.refresh", lang);
            marketTabImage.color = tab == Tab.Market ? Theme.Accent : Theme.Tab;
            garageTabImage.color = tab == Tab.Garage ? Theme.Accent : Theme.Tab;
            refreshButton.SetActive(tab == Tab.Market);

            ClearChildren(listContent);
            if (tab == Tab.Market)
            {
                if (session.Market.Listings.Count == 0) AddNote(listContent, UiText.Get("market.empty", lang));
                foreach (var listing in session.Market.Listings) AddListingCard(listing);
            }
            else
            {
                if (player.Garage.Count == 0)
                {
                    AddNote(listContent, UiText.Get("garage.empty", lang));
                }
                else
                {
                    foreach (var owned in player.Garage) AddGarageCard(owned);
                    AddNote(listContent, UiText.Get("garage.soon", lang));
                }
            }

            if (resetScroll) scroll.ScrollToTop();
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private string ListingDetails(Listing listing) =>
            $"{MoneyFormat.Number(listing.Car.DisplayedMileage)} {UiText.Get("unit.km", lang)} · " +
            UiText.Get(UiText.SellerKey(listing.Seller), lang);

        private void AddListingCard(Listing listing)
        {
            AddCard(listContent, listing.Id, listing.Car, ListingDetails(listing), listing.Description(lang),
                MoneyFormat.Format(session.PriceFor(listing)),
                UiText.Get("btn.inspect", lang), Theme.Accent, true,
                () => OpenDetails(listing));
        }

        private string GarageDetails(OwnedCar owned)
        {
            string details = $"{MoneyFormat.Number(owned.Car.DisplayedMileage)} {UiText.Get("unit.km", lang)} · " +
                             UiText.Format("garage.bought_for", lang, MoneyFormat.Format(owned.PurchasePrice));
            if (owned.Report.DiagnosticsDone)
                details += " · " + UiText.Format("garage.issues", lang, owned.Car.HiddenDefects.Count);
            return details;
        }

        private void AddGarageCard(OwnedCar owned)
        {
            AddCard(listContent, owned.ListingId, owned.Car, GarageDetails(owned), null,
                MoneyFormat.Format(Workshop.EstimateNow(owned)),
                UiText.Get("btn.workshop", lang), Theme.Accent, true,
                () => OpenWorkshop(owned));
        }

        /// <summary>
        /// Wide: [photo | info | price + button] in one row.
        /// Compact (phones): [photo | info] on top, [price ... button] below.
        /// </summary>
        private void AddCard(Transform parent, int seed, Car car, string details, string description,
            string price, string actionLabel, Color actionColor, bool interactable, UnityAction onAction)
        {
            var card = Ui.CreateImage("Card", parent, Theme.Card);
            Transform row;
            if (compact)
            {
                Ui.Vertical(card.gameObject, 12, Ui.Pad(16));
                row = Ui.CreateRect("Top", card.transform);
                Ui.Horizontal(row.gameObject, 16, null, TextAnchor.UpperLeft);
            }
            else
            {
                Ui.Horizontal(card.gameObject, 20, Ui.Pad(16), TextAnchor.UpperLeft);
                row = card.transform;
            }

            // Photo
            float photoWidth = compact ? 150 : 220;
            float photoHeight = compact ? 90 : 130;
            var frame = Ui.CreateImage("Photo", row, Theme.PhotoBackground);
            frame.raycastTarget = false;
            Ui.Layout(frame, minWidth: photoWidth, preferredWidth: photoWidth, minHeight: photoHeight, preferredHeight: photoHeight);
            var picture = Ui.CreateImage("Car", frame.transform, Color.white);
            picture.sprite = CarSprites.Get(car.Model.Body, seed);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            Ui.Stretch(picture.rectTransform, 10, 10, 10, 10);

            // Title, details, seller text
            var info = Ui.CreateRect("Info", row);
            Ui.Vertical(info.gameObject, 6);
            Ui.Layout(info, minWidth: 160, preferredWidth: 0, flexibleWidth: 1);
            Ui.CreateText("Title", info, 30, Theme.Text, FontStyle.Bold, TextAnchor.UpperLeft).text = car.Title(lang);
            Ui.CreateText("Details", info, 22, Theme.Muted, FontStyle.Normal, TextAnchor.UpperLeft).text = details;
            if (!string.IsNullOrEmpty(description))
                Ui.CreateText("Description", info, 22, Theme.Text, FontStyle.Normal, TextAnchor.UpperLeft).text = description;

            // Price and action
            var actions = Ui.CreateRect("Actions", compact ? card.transform : row);
            if (compact)
            {
                Ui.Horizontal(actions.gameObject, 16);
            }
            else
            {
                Ui.Vertical(actions.gameObject, 12, null, TextAnchor.UpperRight);
                Ui.Layout(actions, minWidth: 220, preferredWidth: 220);
            }

            var priceText = Ui.CreateText("Price", actions, 34, Theme.Money, FontStyle.Bold,
                compact ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            priceText.text = price;
            if (compact) Ui.Layout(priceText, flexibleWidth: 1);

            var button = Ui.CreateButton("Action", actions, 24, actionColor, onAction ?? (() => { }), out var label);
            label.text = actionLabel;
            button.interactable = interactable;
            Ui.Layout(button, minWidth: 180, minHeight: 56);
        }

        private void AddNote(Transform parent, string text)
        {
            var note = Ui.CreateText("Note", parent, 24, Theme.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            note.text = text;
            Ui.Layout(note, minHeight: 80);
        }

        // ---------- actions ----------

        private void OnBuy(Listing listing)
        {
            long price = session.PriceFor(listing);
            switch (session.Buy(listing))
            {
                case PurchaseResult.Success:
                    ShowToast(UiText.Format("toast.bought", lang, listing.Car.Title(lang), MoneyFormat.Format(price)));
                    CloseDetails();
                    break;
                case PurchaseResult.NotEnoughMoney:
                    ShowToast(UiText.Format("toast.no_money", lang, MoneyFormat.Format(price - session.Player.Money)));
                    break;
                case PurchaseResult.GarageFull:
                    ShowToast(UiText.Get("toast.garage_full", lang));
                    break;
                case PurchaseResult.SellerRefused:
                    ShowToast(UiText.Get("toast.seller_refused", lang));
                    break;
                default:
                    ShowToast(UiText.Get("toast.unavailable", lang));
                    break;
            }
        }

        private void OnRefresh()
        {
            session.RefreshMarket();
            ShowToast(UiText.Get("toast.refreshed", lang));
            Redraw(resetScroll: true);
        }

        private void SwitchTab(Tab next)
        {
            tab = next;
            Redraw(resetScroll: true);
        }

        private void ToggleLanguage()
        {
            lang = lang == Language.Ru ? Language.En : Language.Ru;
            Redraw(resetScroll: false);
            if (IsDetailsOpen) RedrawDetails(resetScroll: false);
        }

        private void ShowToast(string text)
        {
            toastText.text = text;
            toastPanel.SetActive(true);
            toastHideAt = Time.unscaledTime + ToastSeconds;
        }

        /// <summary>Canvas width in reference units, as CanvasScaler computes it with match = 0.5.</summary>
        private static float LogicalWidth()
        {
            float scale = Mathf.Sqrt(Screen.width / ReferenceWidth * (Screen.height / ReferenceHeight));
            return scale > 0 ? Screen.width / scale : ReferenceWidth;
        }
    }
}
