using System.Collections.Generic;
using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Economy;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>
    /// Listing details overlay: condition, paid checks and haggling with the seller.
    /// </summary>
    public sealed partial class MarketScreen
    {
        /// <summary>Discount steps offered as quick buttons, in percent of the asking price.</summary>
        private static readonly int[] OfferSteps = { 5, 10, 15, 20, 25 };

        private GameObject detailsPanel;
        private RectTransform detailsContent;
        private SmoothScrollRect detailsScroll;
        private Text detailsMoneyText, backLabel;
        private Listing openListing;

        private bool IsDetailsOpen => openListing != null;

        private void BuildDetails()
        {
            var panel = Ui.CreateImage("Details", canvasRoot, Theme.Background);
            Ui.Stretch(panel.rectTransform);
            Ui.Vertical(panel.gameObject, 12, new RectOffset(24, 24, 16, 16));

            var header = Ui.CreateRect("Header", panel.transform);
            Ui.Horizontal(header.gameObject, 20);
            Ui.CreateButton("Back", header, 24, Theme.Tab, CloseDetails, out backLabel);
            Ui.Layout(Ui.CreateRect("Spacer", header), flexibleWidth: 1);
            detailsMoneyText = Ui.CreateText("Money", header, 40, Theme.Money, FontStyle.Bold, TextAnchor.MiddleRight);

            CreateScrollList(panel.transform, out detailsScroll, out detailsContent);

            detailsPanel = panel.gameObject;
            detailsPanel.SetActive(false);
        }

        private void OpenDetails(Listing listing)
        {
            openListing = listing;
            detailsPanel.SetActive(true);
            RedrawDetails(resetScroll: true);
        }

        private void CloseDetails()
        {
            openListing = null;
            detailsPanel.SetActive(false);
            Redraw(resetScroll: false);
        }

        private void RedrawDetails(bool resetScroll)
        {
            var listing = openListing;
            if (listing == null) return;

            var car = listing.Car;
            var report = session.ReportFor(listing);
            var deal = session.FindNegotiation(listing);
            long price = session.PriceFor(listing);
            bool refused = deal != null && deal.SellerLeft;
            bool affordable = session.Player.Money >= price && session.Player.HasFreeSlot;

            detailsMoneyText.text = MoneyFormat.Format(session.Player.Money);
            backLabel.text = UiText.Get("btn.back", lang);
            ClearChildren(detailsContent);

            // Car card with the buy button
            AddCard(detailsContent, listing.Id, car, ListingDetails(listing), listing.Description(lang),
                MoneyFormat.Format(price),
                UiText.Format("btn.buy_for", lang, MoneyFormat.Format(price)),
                !refused && affordable ? Theme.Buy : Theme.Disabled, !refused,
                () => OnBuy(listing));

            // Condition
            AddSection(UiText.Get("section.condition", lang));
            foreach (var component in Car.Components)
            {
                int value = report.DiagnosticsDone ? car.TrueCondition(component) : car.VisibleCondition(component);
                AddConditionRow(UiText.Get(UiText.ComponentKey(component), lang), value);
            }
            if (!report.DiagnosticsDone)
            {
                AddLine(UiText.Get("condition.note", lang), Theme.Muted);
            }
            else
            {
                foreach (var defect in car.HiddenDefects)
                    AddLine(UiText.Format("defect.line", lang,
                        UiText.Get(UiText.ComponentKey(defect.Component), lang),
                        UiText.Get("severity." + defect.Severity, lang),
                        MoneyFormat.Format(defect.RepairCost)), Theme.Warning);
            }

            // Paid checks
            AddSection(UiText.Get("section.checks", lang));
            HistoryResult(car, report, out string historyText, out Color historyColor);
            AddCheckRow(UiText.Get("check.history", lang), historyText, historyColor,
                UiText.Format("check.history_btn", lang, MoneyFormat.Format(InspectionPrices.HistoryCheck)),
                report.HistoryChecked, () => OnInspect(listing, session.CheckHistory(listing), InspectionPrices.HistoryCheck));

            long diagnosticsPrice = session.DiagnosticsPrice(listing);
            DiagnosticsResult(car, report, out string diagText, out Color diagColor);
            AddCheckRow(UiText.Get("check.diag", lang), diagText, diagColor,
                UiText.Format("check.diag_btn", lang, MoneyFormat.Format(diagnosticsPrice)),
                report.DiagnosticsDone, () => OnInspect(listing, session.RunDiagnostics(listing), diagnosticsPrice));

            // Haggling
            AddSection(UiText.Get("section.deal", lang));
            if (deal == null || deal.Turns.Count == 0) AddLine(UiText.Get("deal.hint", lang), Theme.Muted);
            if (deal != null)
            {
                foreach (var turn in deal.Turns)
                {
                    AddLine(UiText.Format("deal.you", lang, MoneyFormat.Format(turn.Offer)), Theme.Muted);
                    string reply = UiText.Format(turn.PhraseKey, lang, MoneyFormat.Format(turn.SellerPrice));
                    AddLine(UiText.Format("deal.seller", lang, reply),
                        turn.Reaction == SellerReaction.Left || turn.Reaction == SellerReaction.Insulted ? Theme.Danger : Theme.Text);
                }
            }

            if (refused) AddLine(UiText.Get("deal.refused", lang), Theme.Danger);
            else if (deal != null && deal.HasDeal) AddLine(UiText.Format("deal.agreed", lang, MoneyFormat.Format(price)), Theme.Money);
            else AddOfferButtons(listing, price);

            if (resetScroll) detailsScroll.ScrollToTop();
        }

        // ---------- detail blocks ----------

        private void AddSection(string title)
        {
            var text = Ui.CreateText("Section", detailsContent, 28, Theme.Text, FontStyle.Bold, TextAnchor.LowerLeft);
            text.text = title;
            Ui.Layout(text, minHeight: 52);
        }

        private void AddLine(string text, Color color)
        {
            var line = Ui.CreateText("Line", detailsContent, 22, color, FontStyle.Normal, TextAnchor.UpperLeft);
            line.text = text;
        }

        private void AddConditionRow(string name, int value)
        {
            var row = Ui.CreateRect("Condition", detailsContent);
            Ui.Horizontal(row.gameObject, 16);

            float labelWidth = compact ? 170 : 240;
            var label = Ui.CreateText("Name", row, 22, Theme.Text);
            label.text = name;
            Ui.Layout(label, minWidth: labelWidth, preferredWidth: labelWidth);

            var bar = Ui.CreateImage("Bar", row, Theme.Tab);
            bar.raycastTarget = false;
            Ui.Layout(bar, minWidth: 80, preferredWidth: 0, flexibleWidth: 1, minHeight: 16, preferredHeight: 16);
            var fill = Ui.CreateImage("Fill", bar.transform, Theme.Condition(value));
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var percent = Ui.CreateText("Value", row, 22, Theme.Muted, FontStyle.Normal, TextAnchor.MiddleRight);
            percent.text = value + "%";
            Ui.Layout(percent, minWidth: 70, preferredWidth: 70);
        }

        private void AddCheckRow(string title, string result, Color resultColor, string buttonLabel,
            bool done, UnityAction onClick)
        {
            var row = Ui.CreateImage("Check", detailsContent, Theme.Card);
            Ui.Horizontal(row.gameObject, 16, Ui.Pad(16));

            var texts = Ui.CreateRect("Texts", row.transform);
            Ui.Vertical(texts.gameObject, 4);
            Ui.Layout(texts, minWidth: 160, preferredWidth: 0, flexibleWidth: 1);
            Ui.CreateText("Title", texts, 24, Theme.Text, FontStyle.Bold, TextAnchor.UpperLeft).text = title;
            if (!string.IsNullOrEmpty(result))
                Ui.CreateText("Result", texts, 22, resultColor, FontStyle.Normal, TextAnchor.UpperLeft).text = result;

            var button = Ui.CreateButton("Do", row.transform, 22, done ? Theme.Disabled : Theme.Accent, onClick, out var label);
            label.text = done ? UiText.Get("check.done", lang) : buttonLabel;
            button.interactable = !done;
            Ui.Layout(button, minWidth: 200, minHeight: 56);
        }

        private void AddOfferButtons(Listing listing, long currentPrice)
        {
            var amounts = new List<long>();
            foreach (int step in OfferSteps)
            {
                double raw = listing.AskingPrice * (1 - step / 100.0);
                long amount = PriceMath.RoundToMarket((long)System.Math.Round(raw), CarValuation.MarketStep(raw));
                if (amount < currentPrice && !amounts.Contains(amount)) amounts.Add(amount);
            }

            if (amounts.Count == 0)
            {
                AddLine(UiText.Get("deal.no_room", lang), Theme.Muted);
                return;
            }

            var row = Ui.CreateRect("Offers", detailsContent);
            var layout = Ui.Horizontal(row.gameObject, 10);
            layout.childForceExpandWidth = true;
            foreach (long amount in amounts)
            {
                long offer = amount;
                var button = Ui.CreateButton("Offer", row, 22, Theme.Tab, () => OnOffer(listing, offer), out var label);
                label.text = MoneyFormat.Format(offer);
                Ui.Layout(button, minWidth: 100, minHeight: 56);
            }
        }

        // ---------- check results ----------

        private void HistoryResult(Car car, InspectionReport report, out string text, out Color color)
        {
            text = null;
            color = Theme.Money;
            if (!report.HistoryChecked) return;

            if (car.IsOdometerRolledBack)
            {
                text = UiText.Format("history.rolled", lang, MoneyFormat.Number(car.RealMileage),
                    MoneyFormat.Number(car.RealMileage - car.DisplayedMileage));
                color = Theme.Danger;
            }
            else
            {
                text = UiText.Format("history.ok", lang, MoneyFormat.Number(car.DisplayedMileage));
            }

            if (car.WasTaxi)
            {
                text += " " + UiText.Get("history.taxi", lang);
                if (color != Theme.Danger) color = Theme.Warning;
            }
        }

        private void DiagnosticsResult(Car car, InspectionReport report, out string text, out Color color)
        {
            text = null;
            color = Theme.Money;
            if (!report.DiagnosticsDone) return;

            if (car.HiddenDefects.Count == 0)
            {
                text = UiText.Get("diag.clean", lang);
            }
            else
            {
                text = UiText.Format("diag.found", lang, car.HiddenDefects.Count, MoneyFormat.Format(car.HiddenRepairCost));
                color = Theme.Warning;
            }
        }

        // ---------- detail actions ----------

        private void OnInspect(Listing listing, InspectionResult result, long cost)
        {
            switch (result)
            {
                case InspectionResult.Success:
                    ShowToast(UiText.Format("toast.paid", lang, MoneyFormat.Format(cost)));
                    break;
                case InspectionResult.NotEnoughMoney:
                    ShowToast(UiText.Format("toast.no_money", lang, MoneyFormat.Format(cost - session.Player.Money)));
                    break;
                case InspectionResult.ListingUnavailable:
                    ShowToast(UiText.Get("toast.unavailable", lang));
                    break;
            }
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnOffer(Listing listing, long amount)
        {
            var deal = session.NegotiationFor(listing);
            if (deal.IsClosed) return;
            session.Offer(listing, amount);
            RedrawDetails(resetScroll: false);
        }
    }
}
