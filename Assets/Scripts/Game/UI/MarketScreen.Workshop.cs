using System.Collections.Generic;
using CRS.Core;
using CRS.Core.Cars;
using CRS.Core.Economy;
using CRS.Core.Repairs;
using CRS.Core.Selling;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CRS.Game.UI
{
    /// <summary>
    /// Garage car overlay: selling, value of the car, repairs, washing, polishing and diagnostics.
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
            var sale = session.SaleFor(owned);
            long marketPrice = SalePricing.PriceAt(car, 0);
            if (sale == null)
                AddCard(detailsContent, owned.ListingId, car, GarageDetails(owned), null,
                    MoneyFormat.Format(valueNow),
                    UiText.Format("sale.list_btn", lang, MoneyFormat.Format(marketPrice)), Theme.Buy, true,
                    () => OnList(owned, marketPrice));
            else
                AddCard(detailsContent, owned.ListingId, car, GarageDetails(owned), null,
                    MoneyFormat.Format(sale.AskingPrice),
                    UiText.Get("sale.unlist", lang), Theme.Tab, true,
                    () => OnUnlist(owned));

            AddSaleSection(owned, sale);

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

        // ---------- selling ----------

        private void AddSaleSection(OwnedCar owned, SaleListing sale)
        {
            AddSection(UiText.Get("section.sale", lang));

            if (sale == null)
            {
                AddLine(UiText.Format("sale.market", lang, MoneyFormat.Format(SalePricing.MarketValue(owned.Car))), Theme.Money);
                AddLine(UiText.Get("sale.hint", lang), Theme.Muted);
                AddPriceButtons(owned);
                return;
            }

            AddLine(UiText.Format("sale.listed", lang, MoneyFormat.Format(sale.AskingPrice)), Theme.Text);
            if (sale.Offers.Count == 0)
            {
                AddLine(UiText.Get("sale.no_offers", lang), Theme.Muted);
                return;
            }

            // Copy: buttons below change the list while it is drawn again.
            foreach (var offer in new List<BuyerOffer>(sale.Offers)) AddBuyerCard(owned, sale, offer);
            AddLine(UiText.Get("sale.offer_note", lang), Theme.Muted);
        }

        /// <summary>Quick prices around the market value, like the haggling buttons.</summary>
        private void AddPriceButtons(OwnedCar owned)
        {
            var prices = new List<long>();
            foreach (int step in SalePricing.PriceSteps)
            {
                long price = SalePricing.PriceAt(owned.Car, step);
                if (!prices.Contains(price)) prices.Add(price);
            }

            // Phones: two rows of three buttons, so the labels stay readable.
            int perRow = compact ? 3 : prices.Count;
            for (int start = 0; start < prices.Count; start += perRow)
            {
                var row = Ui.CreateRect("Prices", detailsContent);
                var layout = Ui.Horizontal(row.gameObject, 10);
                layout.childForceExpandWidth = true;
                for (int i = start; i < prices.Count && i < start + perRow; i++)
                {
                    long price = prices[i];
                    var button = Ui.CreateButton("Price", row, 22, Theme.Tab, () => OnList(owned, price), out var label);
                    label.text = MoneyFormat.Format(price);
                    Ui.Layout(button, minWidth: 100, minHeight: 56);
                }
            }
        }

        /// <summary>One buyer: what was said, then sell / ask for more / decline.</summary>
        private void AddBuyerCard(OwnedCar owned, SaleListing sale, BuyerOffer offer)
        {
            var card = Ui.CreateImage("Buyer", detailsContent, Theme.Card);
            Ui.Vertical(card.gameObject, 8, Ui.Pad(16));

            string opening = UiText.Format(offer.PhraseKey, lang, MoneyFormat.Format(offer.OpeningPrice));
            Ui.CreateText("Opening", card.transform, 22, offer.FoundProblems ? Theme.Warning : Theme.Text,
                FontStyle.Normal, TextAnchor.UpperLeft).text = UiText.Format("sale.buyer", lang, opening);

            foreach (var turn in offer.Turns)
            {
                Ui.CreateText("You", card.transform, 22, Theme.Muted, FontStyle.Normal, TextAnchor.UpperLeft).text =
                    UiText.Format("sale.you", lang, MoneyFormat.Format(turn.Demand));
                string reply = UiText.Format(turn.PhraseKey, lang, MoneyFormat.Format(turn.Price));
                Ui.CreateText("Reply", card.transform, 22,
                    turn.Reaction == BuyerReaction.Agreed ? Theme.Money : Theme.Text,
                    FontStyle.Normal, TextAnchor.UpperLeft).text = UiText.Format("sale.buyer", lang, reply);
            }

            // Sell and decline
            var main = Ui.CreateRect("Main", card.transform);
            Ui.Horizontal(main.gameObject, 10);
            var sell = Ui.CreateButton("Sell", main, 22, Theme.Buy, () => OnSell(owned, offer), out var sellLabel);
            sellLabel.text = UiText.Format("sale.sell_btn", lang, MoneyFormat.Format(offer.Price));
            Ui.Layout(sell, minWidth: 200, preferredWidth: 0, flexibleWidth: 1, minHeight: 56);
            var decline = Ui.CreateButton("Decline", main, 22, Theme.Tab, () => OnDecline(owned, offer), out var declineLabel);
            declineLabel.text = UiText.Get("sale.decline", lang);
            Ui.Layout(decline, minWidth: 140, minHeight: 56);

            // Ask for more: +5%, +10%, +15% of the offer, never above the asking price
            var asks = new List<long>();
            foreach (int step in new[] { 5, 10, 15 })
            {
                double raw = offer.Price * (1 + step / 100.0);
                long ask = PriceMath.RoundToMarket((long)System.Math.Round(raw), CarValuation.MarketStep(raw));
                if (ask > sale.AskingPrice) ask = sale.AskingPrice;
                if (ask > offer.Price && !asks.Contains(ask)) asks.Add(ask);
            }
            if (asks.Count == 0) return;

            var row = Ui.CreateRect("Asks", card.transform);
            var layout = Ui.Horizontal(row.gameObject, 10);
            layout.childForceExpandWidth = true;
            foreach (long amount in asks)
            {
                long ask = amount;
                var button = Ui.CreateButton("Ask", row, 20, Theme.Accent, () => OnAsk(owned, offer, ask), out var label);
                label.text = UiText.Format("sale.ask_btn", lang, MoneyFormat.Format(ask));
                Ui.Layout(button, minWidth: 100, minHeight: 52);
            }
        }

        private void OnList(OwnedCar owned, long price)
        {
            if (session.ListForSale(owned, price) == ListResult.Success)
                ShowToast(UiText.Format("toast.listed", lang, MoneyFormat.Format(price)));
            SaveGame();
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnUnlist(OwnedCar owned)
        {
            if (session.Unlist(owned)) ShowToast(UiText.Get("toast.unlisted", lang));
            SaveGame();
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnAsk(OwnedCar owned, BuyerOffer offer, long amount)
        {
            var sale = session.SaleFor(owned);
            if (sale == null || !sale.Contains(offer) || amount <= offer.Price) return;

            var turn = session.Counter(owned, offer, amount);
            if (turn.Reaction == BuyerReaction.Left)
                ShowToast(UiText.Format("sale.buyer", lang, UiText.Format(turn.PhraseKey, lang, MoneyFormat.Format(turn.Price))));
            SaveGame();
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnDecline(OwnedCar owned, BuyerOffer offer)
        {
            if (session.Decline(owned, offer)) ShowToast(UiText.Get("toast.buyer_left", lang));
            SaveGame();
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }

        private void OnSell(OwnedCar owned, BuyerOffer offer)
        {
            var record = session.Sell(owned, offer);
            if (record == null)
            {
                RedrawDetails(resetScroll: false);
                return;
            }

            SaveGame();
            ShowToast(UiText.Format("toast.sold", lang, record.Car.Title(lang),
                MoneyFormat.Format(record.SalePrice), MoneyFormat.Format(record.Profit)));
            // The car is gone: back to the garage list.
            CloseDetails();
        }

        // ---------- repairs ----------

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
            SaveGame();
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
            SaveGame();
            Redraw(resetScroll: false);
            RedrawDetails(resetScroll: false);
        }
    }
}
