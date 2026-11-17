using System.Collections.Generic;
using System.Globalization;
using CRS.Core.Cars;

namespace CRS.Core
{
    /// <summary>
    /// Interface strings in Russian and English.
    /// Placeholders {0}, {1} are filled by <see cref="Format"/>.
    /// </summary>
    public static class UiText
    {
        public static readonly IReadOnlyDictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            //                              RU                                                        EN
            ["tab.market"]        = new[] { "Объявления",                                             "Listings" },
            ["tab.garage"]        = new[] { "Гараж",                                                  "Garage" },
            ["top.garage"]        = new[] { "Гараж: {0}/{1}",                                         "Garage: {0}/{1}" },
            ["btn.buy"]           = new[] { "Купить",                                                 "Buy" },
            ["lang.switch"]       = new[] { "EN",                                                     "RU" },
            ["toast.bought"]      = new[] { "Куплено: {0} за {1}",                                    "Bought: {0} for {1}" },
            ["toast.no_money"]    = new[] { "Не хватает денег: нужно ещё {0}",                        "Not enough money: {0} short" },
            ["toast.garage_full"] = new[] { "Гараж заполнен",                                         "Your garage is full" },
            ["toast.unavailable"] = new[] { "Объявление уже снято",                                   "This listing is gone" },
            ["garage.empty"]      = new[] { "Гараж пуст. Купите машину на вкладке «Объявления».",     "Your garage is empty. Buy a car on the Listings tab." },
            ["garage.bought_for"] = new[] { "куплена за {0}",                                         "bought for {0}" },
            ["unit.km"]           = new[] { "км",                                                     "km" },
            ["seller.private"]    = new[] { "частник",                                                "private seller" },
            ["seller.reseller"]   = new[] { "перекуп",                                                "reseller" },
            ["seller.urgent"]     = new[] { "срочная продажа",                                        "urgent sale" },
            ["garage.issues"]     = new[] { "известных неисправностей: {0}",                          "known faults: {0}" },
            ["toast.paid"]        = new[] { "Оплачено: {0}",                                          "Paid: {0}" },
            ["toast.seller_refused"] = new[] { "Продавец отказался вам продавать",                    "The seller refused to sell to you" },

            // Listing details
            ["btn.inspect"]       = new[] { "Осмотреть",                                              "Inspect" },
            ["btn.back"]          = new[] { "Назад",                                                  "Back" },
            ["btn.buy_for"]       = new[] { "Купить за {0}",                                          "Buy for {0}" },
            ["section.condition"] = new[] { "Состояние",                                              "Condition" },
            ["section.checks"]    = new[] { "Проверки",                                               "Checks" },
            ["section.deal"]      = new[] { "Торг с продавцом",                                       "Haggling" },
            ["condition.note"]    = new[] { "Так машина выглядит на осмотре. Скрытые проблемы покажет диагностика.",
                                            "This is how the car looks. Diagnostics reveal hidden problems." },
            ["comp.Engine"]       = new[] { "Двигатель",                                              "Engine" },
            ["comp.Transmission"] = new[] { "Коробка передач",                                        "Transmission" },
            ["comp.Suspension"]   = new[] { "Подвеска",                                               "Suspension" },
            ["comp.Body"]         = new[] { "Кузов",                                                  "Body" },
            ["comp.Interior"]     = new[] { "Салон",                                                  "Interior" },
            ["comp.Electrics"]    = new[] { "Электрика",                                              "Electrics" },
            ["severity.1"]        = new[] { "мелкая неисправность",                                   "minor fault" },
            ["severity.2"]        = new[] { "серьёзная проблема",                                     "serious problem" },
            ["severity.3"]        = new[] { "критическая поломка",                                    "critical failure" },
            ["defect.line"]       = new[] { "{0}: {1}, ремонт {2}",                                   "{0}: {1}, repair {2}" },
            ["check.history"]     = new[] { "Проверка по базе: пробег и история",                     "Records check: mileage and history" },
            ["check.history_btn"] = new[] { "Проверить за {0}",                                       "Check for {0}" },
            ["check.diag"]        = new[] { "Диагностика: скрытые неисправности",                     "Diagnostics: hidden faults" },
            ["check.diag_btn"]    = new[] { "Проверить за {0}",                                       "Check for {0}" },
            ["check.done"]        = new[] { "Готово",                                                 "Done" },
            ["history.ok"]        = new[] { "Пробег по базе совпадает: {0} км.",                      "Mileage matches the records: {0} km." },
            ["history.rolled"]    = new[] { "По базе {0} км. Пробег скручен на {1} км!",              "Records say {0} km. Odometer rolled back by {1} km!" },
            ["history.taxi"]      = new[] { "Машина работала в такси.",                               "The car was used as a taxi." },
            ["diag.clean"]        = new[] { "Скрытых неисправностей не найдено.",                     "No hidden faults found." },
            ["diag.found"]        = new[] { "Найдено неисправностей: {0}, ремонт примерно {1}.",      "Hidden faults: {0}, repairs about {1}." },

            // Haggling
            ["deal.hint"]         = new[] { "Предложите свою цену. Найденные неисправности — хороший аргумент для скидки.",
                                            "Make an offer. Faults you found are a good argument for a discount." },
            ["deal.you"]          = new[] { "Вы: предлагаю {0}",                                      "You: I offer {0}" },
            ["deal.seller"]       = new[] { "Продавец: {0}",                                          "Seller: {0}" },
            ["deal.accept"]       = new[] { "По рукам, {0}!",                                         "Deal, {0}!" },
            ["deal.counter.1"]    = new[] { "Мало. Давай за {0}.",                                    "Too low. Make it {0}." },
            ["deal.counter.2"]    = new[] { "Несерьёзно. За {0} забирай.",                            "Come on. {0} and it's yours." },
            ["deal.counter.3"]    = new[] { "Могу уступить до {0}.",                                  "I can go down to {0}." },
            ["deal.counter_leverage"] = new[] { "Ну да, есть косяки... Ладно, {0}.",                  "Yeah, it has issues... Fine, {0}." },
            ["deal.insulted"]     = new[] { "Ты издеваешься? Меньше {0} даже не предлагай.",          "Are you kidding? Don't offer less than {0}." },
            ["deal.final"]        = new[] { "Последняя цена — {0}. Больше не уступлю.",               "Final price: {0}. Not a buck less." },
            ["deal.left"]         = new[] { "Всё, передумал продавать тебе. Удачи.",                  "That's it, I'm not selling to you. Bye." },
            ["deal.agreed"]       = new[] { "Договорились на {0}. Можно покупать.",                   "Agreed at {0}. You can buy now." },
            ["deal.refused"]      = new[] { "Продавец больше не хочет с вами разговаривать.",         "The seller won't talk to you anymore." },
            ["deal.no_room"]      = new[] { "Цена уже минимальная, дальше торговаться некуда.",       "The price is as low as it gets." },

            // Workshop
            ["btn.workshop"]      = new[] { "Открыть",                                                "Open" },
            ["section.value"]     = new[] { "Стоимость",                                              "Value" },
            ["value.now"]         = new[] { "Стоимость сейчас: {0}",                                  "Value now: {0}" },
            ["value.now_note"]    = new[] { "Без диагностики оценка по внешнему виду: скрытые проблемы не учтены.",
                                            "Without diagnostics this is a visual estimate: hidden problems are not counted." },
            ["value.after"]       = new[] { "После ремонта всего за {0}: {1}",                        "After repairing everything for {0}: {1}" },
            ["value.invested"]    = new[] { "Вложено {0}: покупка {1} и ремонт {2}",                  "Invested {0}: purchase {1}, repairs {2}" },
            ["section.repair"]    = new[] { "Ремонт узлов",                                           "Repairs" },
            ["repair.btn"]        = new[] { "Починить за {0}",                                        "Fix for {0}" },
            ["repair.ok"]         = new[] { "В порядке",                                              "Fine" },
            ["section.care"]      = new[] { "Уход и проверки",                                        "Detailing and checks" },
            ["care.wash"]         = new[] { "Мойка: +2% к цене",                                      "Wash: +2% to value" },
            ["care.polish"]       = new[] { "Полировка: +5% к цене",                                  "Polish: +5% to value" },
            ["care.btn"]          = new[] { "Сделать за {0}",                                         "Do it for {0}" },
            ["toast.repaired"]    = new[] { "Отремонтировано: {0} за {1}",                            "Repaired: {0} for {1}" },
            ["toast.hidden_fixed"] = new[] { "Механик нашёл и устранил скрытую проблему. {0}: всего {1}",
                                             "The mechanic found and fixed a hidden problem. {0}: total {1}" },
            ["toast.hidden_found"] = new[] { "Механик нашёл скрытую проблему. {0}: полный ремонт стоит {1}",
                                             "The mechanic found a hidden problem. {0}: full repair costs {1}" },
            ["toast.done"]        = new[] { "Готово: {0}",                                            "Done: {0}" },
            // Reputation and claims
            ["top.reputation"]    = new[] { "Репутация {0}",                                          "Reputation {0}" },
            ["claim.title"]       = new[] { "Претензия: {0}",                                         "Claim: {0}" },
            ["claim.reason.odometer"] = new[] { "Покупатель проверил машину по базе: пробег скручен. Требует вернуть {0}.",
                                                "The buyer checked the records: the odometer was rolled back. Wants {0} back." },
            ["claim.reason.defects"] = new[] { "У покупателя вылезли скрытые неисправности. Требует вернуть {0}.",
                                               "Hidden faults showed up for the buyer. Wants {0} back." },
            ["claim.reason.both"] = new[] { "Пробег скручен, и вылезли скрытые неисправности. Покупатель требует {0}.",
                                            "Rolled-back odometer and hidden faults. The buyer wants {0}." },
            ["claim.deadline"]    = new[] { "Ответить до конца дня {0}, иначе это сочтут отказом.",   "Answer by the end of day {0}, or it counts as a refusal." },
            ["claim.pay"]         = new[] { "Вернуть {0} (репутация {1})",                            "Pay {0} (reputation {1})" },
            ["claim.refuse"]      = new[] { "Отказать (репутация {0})",                               "Refuse (reputation {0})" },
            ["toast.claims"]      = new[] { "Покупатели вернулись с претензиями: {0}. Смотрите в гараже.", "Buyers came back with claims: {0}. See the garage." },
            ["toast.claims_expired"] = new[] { "Претензии без ответа: {0}. Репутация упала.",         "Unanswered claims: {0}. Reputation dropped." },
            ["toast.claim_paid"]  = new[] { "Вы вернули покупателю {0}",                              "You paid the buyer {0}" },
            ["toast.claim_refused"] = new[] { "Вы отказали. Об этом узнают другие покупатели.",       "You refused. Other buyers will hear about it." },
            ["sale.mileage_title"] = new[] { "Пробег в объявлении",                                   "Mileage in the ad" },
            ["sale.mileage_real"] = new[] { "Реальный по базе: {0} км. Дешевле, но честно.",          "Real, from the records: {0} km. Cheaper, but honest." },
            ["sale.mileage_odometer"] = new[] { "Как на одометре: {0} км. Дороже, но покупатель может вернуться с претензией.",
                                                "As on the odometer: {0} km. More money, but the buyer may come back with a claim." },
            ["sale.mileage_to_real"] = new[] { "Указать реальный",                                    "State the real one" },
            ["sale.mileage_to_odometer"] = new[] { "Умолчать",                                        "Keep quiet" },

            // Saves
            ["btn.restart"]       = new[] { "Начать новую игру",                                      "Start a new game" },
            ["btn.restart_confirm"] = new[] { "Стереть прогресс? Нажмите ещё раз",                    "Erase progress? Tap again" },
            ["toast.restarted"]   = new[] { "Новая игра",                                             "New game" },

            // Days
            ["top.day"]           = new[] { "День {0}",                                               "Day {0}" },
            ["btn.next_day"]      = new[] { "Следующий день",                                         "Next day" },
            ["toast.next_day"]    = new[] { "День {0}. Аренда гаража: {1}. Новых покупателей: {2}",   "Day {0}. Garage rent: {1}. New buyers: {2}" },
            ["market.empty"]      = new[] { "Объявлений нет. Нажмите «Следующий день».",              "No listings. Tap \"Next day\"." },

            // Selling
            ["section.sale"]      = new[] { "Продажа",                                                "Selling" },
            ["sale.market"]       = new[] { "Рыночная цена такой машины: {0}",                        "Market price for a car like this: {0}" },
            ["sale.hint"]         = new[] { "Выберите цену. Дешевле рынка — покупатели придут быстрее, дороже — придётся ждать.",
                                            "Pick a price. Below the market buyers come faster, above it you will have to wait." },
            ["sale.list_btn"]     = new[] { "Выставить за {0}",                                       "List for {0}" },
            ["sale.unlist"]       = new[] { "Снять с продажи",                                        "Take off sale" },
            ["sale.listed"]       = new[] { "В продаже за {0}. Чтобы сменить цену, снимите с продажи и выставьте заново.",
                                            "On sale for {0}. To change the price, take it off sale and list it again." },
            ["sale.no_offers"]    = new[] { "Предложений пока нет. Покупатели приходят с новым днём.", "No offers yet. Buyers come with the new day." },
            ["sale.on_sale"]      = new[] { "в продаже за {0}",                                       "on sale for {0}" },
            ["sale.offers"]       = new[] { "предложений: {0}",                                       "offers: {0}" },
            ["sale.buyer"]        = new[] { "Покупатель: {0}",                                        "Buyer: {0}" },
            ["sale.you"]          = new[] { "Вы: хочу {0}",                                           "You: I want {0}" },
            ["sale.sell_btn"]     = new[] { "Продать за {0}",                                         "Sell for {0}" },
            ["sale.ask_btn"]      = new[] { "Просить {0}",                                            "Ask {0}" },
            ["sale.decline"]      = new[] { "Отказать",                                               "Decline" },
            ["sale.offer_note"]   = new[] { "Предложения действуют до конца дня.",                    "Offers are valid until the end of the day." },
            ["buyer.regular.1"]   = new[] { "Машина нравится. Дам {0}.",                              "I like the car. I'll give {0}." },
            ["buyer.regular.2"]   = new[] { "Ищу как раз такую. {0} устроит?",                        "Just what I'm looking for. Will {0} do?" },
            ["buyer.regular.3"]   = new[] { "Готов забрать сегодня за {0}.",                          "I can take it today for {0}." },
            ["buyer.bargainer.1"] = new[] { "Заберу прямо сейчас, деньги с собой. {0}.",              "I'll take it right now, cash in hand. {0}." },
            ["buyer.bargainer.2"] = new[] { "За такую больше {0} никто не даст.",                     "Nobody will give more than {0} for this one." },
            ["buyer.bargainer.3"] = new[] { "Вложений много. {0} — и по рукам.",                      "It needs a lot of work. {0} and we have a deal." },
            ["buyer.expert.clean"] = new[] { "Проверил по базе и на подъёмнике — машина честная. Дам {0}.",
                                             "Checked the records and looked underneath: an honest car. I'll give {0}." },
            ["buyer.expert.found"] = new[] { "Я её проверил: с машиной не всё чисто. Больше {0} она не стоит.",
                                             "I checked it: this car has a history. It's not worth more than {0}." },
            ["buyer.agreed"]      = new[] { "Ладно, {0}. Беру!",                                      "Fine, {0}. I'll take it!" },
            ["buyer.raised.1"]    = new[] { "Дорого. Могу дать {0}.",                                 "Too much. I can give {0}." },
            ["buyer.raised.2"]    = new[] { "Нет, столько не дам. {0}.",                              "No, not that much. {0}." },
            ["buyer.final"]       = new[] { "{0} — моя последняя цена.",                              "{0} is my final price." },
            ["buyer.left"]        = new[] { "За такие деньги найду другую. Всего доброго.",           "For that money I'll find another one. Goodbye." },
            ["toast.listed"]      = new[] { "Объявление размещено: {0}",                              "Ad posted: {0}" },
            ["toast.unlisted"]    = new[] { "Машина снята с продажи",                                 "The car is off sale" },
            ["toast.sold"]        = new[] { "Продано: {0} за {1}. Прибыль: {2}",                      "Sold: {0} for {1}. Profit: {2}" },
            ["toast.buyer_left"]  = new[] { "Покупатель ушёл",                                        "The buyer left" },
        };

        public static string Get(string key, Language lang)
        {
            if (!Table.TryGetValue(key, out var texts)) return key;
            return texts[lang == Language.Ru ? 0 : 1];
        }

        public static string Format(string key, Language lang, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Get(key, lang), args);

        public static string ComponentKey(CarComponent component) => "comp." + component;

        public static string SellerKey(SellerType seller)
        {
            switch (seller)
            {
                case SellerType.Reseller: return "seller.reseller";
                case SellerType.Urgent: return "seller.urgent";
                default: return "seller.private";
            }
        }
    }
}
