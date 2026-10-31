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
        };

        public static string Get(string key, Language lang)
        {
            if (!Table.TryGetValue(key, out var texts)) return key;
            return texts[lang == Language.Ru ? 0 : 1];
        }

        public static string Format(string key, Language lang, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Get(key, lang), args);

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
