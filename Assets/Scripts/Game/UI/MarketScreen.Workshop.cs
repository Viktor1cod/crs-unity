using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>
    /// Garage workshop overlay: value of the car, repairs, washing, polishing and diagnostics.
    /// Shares the details panel with the listing screen.
    /// </summary>
    public sealed partial class MarketScreen
    {
        private void OpenWorkshop(OwnedCar owned)
        {
            openOwned = owned;
            openListing = null;
            detailsPanel.SetActive(true);
            RedrawDetails(resetScroll: true);
        }

        private void RedrawWorkshop(bool resetScroll)
        {
            var owned = openOwned;
            var car = owned.Car;
            var player = session.Player;

            detailsMoneyText.text = MoneyFormat.Format(player.Money);
            backLabel.text = UiText.Get("btn.back", lang);
            ClearChildren(detailsContent);

            long valueNow = Workshop.EstimateNow(owned);
            AddCard(detailsContent, owned.ListingId, car, GarageDetails(owned), null,
                MoneyFormat.Format(valueNow),
                UiText.Get("btn.sell_soon", lang), Theme.Disabled, false, null);

            // Value
            AddSection(UiText.Get("section.value", lang));
            AddLine(UiText.Format("value.now", lang, MoneyFormat.Format(valueNow)), Theme.Money);
            if (!owned.Report.DiagnosticsDone) AddLine(UiText.Get("value.now_note", lang), Theme.Muted);
            long fullRepair = Workshop.FullRepairQuote(owned);
            if (fullRepair > 0)
                AddLine(UiText.Format("value.after", lang, MoneyFormat.Format(fullRepair),
                    MoneyFormat.Format(Workshop.EstimateAfterRepairs(owned))), Theme.Text);
            AddLine(UiText.Format("value.invested", lang, MoneyFormat.Format(owned.TotalCost),
                MoneyFormat.Format(owned.PurchasePrice), MoneyFormat.Format(owned.Invested)), Theme.Muted);

            // Repairs
            AddSection(UiText.Get("section.repair", lang));
            foreach (var component in Car.Components)
            {
                var part = component;
                bool canRepair = Workshop.CanRepair(owned, part);
                long quote = Workshop.Quote(owned, part);
                int shown = owned.KnowsDefects(part) ? car.TrueCondition(part) : car.VisibleCondition(part);
                AddRepairRow(UiText.Get(UiText.ComponentKey(part), lang), shown,
                    canRepair ? UiText.Format("repair.btn", lang, MoneyFormat.Format(quote)) : UiText.Get("repair.ok", lang),
                    canRepair, player.Money >= quote, () => OnRepair(owned, part));
            }
            foreach (var defect in car.HiddenDefects)
            {
                if (!owned.KnowsDefects(defect.Component)) continue;
                AddLine(UiText.Format("defect.line", lang,
                    UiText.Get(UiText.ComponentKey(defect.Component), lang),
                    UiText.Get("severity." + defect.Severity, lang),
                    MoneyFormat.Format(defect.RepairCost)), Theme.Warning);
            }

            // Detailing and checks
            AddSection(UiText.Get("section.care", lang));
            AddCheckRow(UiText.Get("care.wash", lang), null, Theme.Money,
                UiText.Format("care.btn", lang, MoneyFormat.Format(RepairPricing.WashPrice)),
                car.IsWashed, () => OnCare(UiText.Get("care.wash", lang), Workshop.Wash(player, owned), RepairPricing.WashPrice));
            long polishPrice = RepairPricing.PolishPrice(car);
            AddCheckRow(UiText.Get("care.polish", lang), null, Theme.Money,
                UiText.Format("care.btn", lang, MoneyFormat.Format(polishPrice)),
                car.IsPolished, () => OnCare(UiText.Get("care.polish", lang), Workshop.Polish(player, owned), polishPrice));

            long diagnosticsPrice = Workshop.DiagnosticsPrice(owned);
            DiagnosticsResult(car, owned.Report, out string diagText, out Color diagColor);
            AddCheckRow(UiText.Get("check.diag", lang), diagText, diagColor,
                UiText.Format("check.diag_btn", lang, MoneyFormat.Format(diagnosticsPrice)),
                owned.Report.DiagnosticsDone,
                () => OnCare(UiText.Get("check.diag", lang), Workshop.Diagnose(player, owned), diagnosticsPrice));

            if (resetScroll) detailsScroll.ScrollToTop();
        }

        private void AddRepairRow(string name, int value, string buttonLabel, bool canRepair, bool affordable,
            UnityAction onClick)
        {
            var row = Ui.CreateRect("Repair", detailsContent);
            Ui.Horizontal(row.gameObject, 16);

            float labelWidth = compact ? 150 : 240;
            var label = Ui.CreateText("Name", row, 22, Theme.Text);
            label.text = name;
            Ui.Layout(label, minWidth: labelWidth, preferredWidth: labelWidth);

            var bar = Ui.CreateImage("Bar", row, Theme.Tab);
            bar.raycastTarget = false;
            Ui.Layout(bar, minWidth: 60, preferredWidth: 0, flexibleWidth: 1, minHeight: 16, preferredHeight: 16);
            var fill = Ui.CreateImage("Fill", bar.transform, Theme.Condition(value));
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var percent = Ui.CreateText("Value", row, 22, Theme.Muted, FontStyle.Normal, TextAnchor.MiddleRight);
            percent.text = value + "%";
            Ui.Layout(percent, minWidth: 64, preferredWidth: 64);

            Color color = !canRepair ? Theme.Disabled : affordable ? Theme.Accent : Theme.Tab;
            var button = Ui.CreateButton("Fix", row, 20, color, onClick, out var buttonText);
            buttonText.text = buttonLabel;
            button.interactable = canRepair;
            Ui.Layout(button, minWidth: compact ? 170 : 220, minHeight: 52);
        }

        private void OnRepair(OwnedCar owned, CarComponent component)
        {
            var player = session.Player;
            string part = UiText.Get(UiText.ComponentKey(component), lang);
            long moneyBefore = player.Money;
            long quote = Workshop.Quote(owned, component);

            switch (Workshop.Repair(player, owned, component))
            {
                case RepairResult.Success:
                    ShowToast(UiText.Format("toast.repaired", lang, part, MoneyFormat.Format(moneyBefore - player.Money)));
                    break;
                case RepairResult.FixedHiddenProblem:
                    ShowToast(UiText.Format("toast.hidden_fixed", lang, part, MoneyFormat.Format(moneyBefore - player.Money)));
                    break;
                case RepairResult.HiddenProblemFound:
                    ShowToast(UiText.Format("toast.hidden_found", lang, part,
                        MoneyFormat.Format(Workshop.Quote(owned, component))));
                    break;
                case RepairResult.NotEnoughMoney:
                    ShowToast(UiText.Format("toast.no_money", lang, MoneyFormat.Format(quote - player.Money)));
                    break;
            }
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnCare(string what, RepairResult result, long price)
        {
            switch (result)
            {
                case RepairResult.Success:
                    ShowToast(UiText.Format("toast.done", lang, what));
                    break;
                case RepairResult.NotEnoughMoney:
                    ShowToast(UiText.Format("toast.no_money", lang, MoneyFormat.Format(price - session.Player.Money)));
                    break;
            }
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }
    }
}
