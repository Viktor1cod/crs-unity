using System.Collections.Generic;

namespace CRS.Core.Cars
{
    /// <summary>
    /// Seller phrases in Russian and English.
    /// Temporary in-code table; will move to Unity Localization string tables with the UI.
    /// </summary>
    public static class Phrases
    {
        public static readonly IReadOnlyDictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            //                                   RU                                              EN
            ["private.intro.1"]    = new[] { "Продаю свою машину, один владелец.",              "Selling my own car, one owner." },
            ["private.intro.2"]    = new[] { "Машина в семье с самой покупки.",                 "Family car since day one." },
            ["private.intro.3"]    = new[] { "Продаю в связи с покупкой новой.",                "Selling because I bought a new one." },
            ["reseller.intro.1"]   = new[] { "Отличное состояние, вложений не требует!",        "Excellent condition, nothing to fix!" },
            ["reseller.intro.2"]   = new[] { "Свежепригнанная, всё обслужено.",                 "Just imported, fully serviced." },
            ["reseller.intro.3"]   = new[] { "Без вложений, сел и поехал.",                     "Turn-key, just get in and drive." },
            ["urgent.intro.1"]     = new[] { "Срочно! Переезд, нужны деньги.",                  "Urgent! Moving, need cash." },
            ["urgent.intro.2"]     = new[] { "Срочная продажа, торг уместен.",                  "Quick sale, open to offers." },
            ["claim.no_accidents"] = new[] { "Не бита, не крашена.",                            "Never crashed, never repainted." },
            ["claim.original_mileage"] = new[] { "Пробег родной!",                              "Original mileage!" },
            ["claim.serviced"]     = new[] { "Обслуживание по регламенту.",                     "Serviced on schedule." },
            ["claim.haggle_at_car"] = new[] { "Торг у капота.",                                 "Haggle at the car." },
            ["claim.no_lowballers"] = new[] { "Перекупам не беспокоить.",                       "No resellers please." },
            ["fact.ex_taxi"]       = new[] { "Работала в такси, пробег большой, но всё ездит.", "Used as a taxi, high mileage but runs fine." },
        };

        public static string Get(string key, Language lang)
        {
            if (!Table.TryGetValue(key, out var texts)) return key;
            return texts[lang == Language.Ru ? 0 : 1];
        }
    }
}
