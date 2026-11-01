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
            ["btn.refresh"]       = new[] { "Новые объявления",                                       "New listings" },
            ["btn.buy"]           = new[] { "Купить",                                                 "Buy" },
            ["btn.sell_soon"]     = new[] { "Продажа скоро",                                          "Selling soon" },
            ["lang.switch"]       = new[] { "EN",                                                     "RU" },
            ["toast.bought"]      = new[] { "Куплено: {0} за {1}",                                    "Bought: {0} for {1}" },
            ["toast.no_money"]    = new[] { "Не хватает денег: нужно ещё {0}",                        "Not enough money: {0} short" },
            ["toast.garage_full"] = new[] { "Гараж заполнен",                                         "Your garage is full" },
            ["toast.unavailable"] = new[] { "Объявление уже снято",                                   "This listing is gone" },
            ["toast.refreshed"]   = new[] { "На доске новые объявления",                              "Fresh listings on the board" },
            ["market.empty"]      = new[] { "Объявлений нет. Нажмите «Новые объявления».",            "No listings. Tap \"New listings\"." },
            ["garage.empty"]      = new[] { "Гараж пуст. Купите машину на вкладке «Объявления».",     "Your garage is empty. Buy a car on the Listings tab." },
            ["garage.bought_for"] = new[] { "куплена за {0}",                                         "bought for {0}" },
            ["garage.soon"]       = new[] { "Осмотр, ремонт и продажа появятся в следующих версиях.", "Inspection, repairs and selling are coming next." },
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
