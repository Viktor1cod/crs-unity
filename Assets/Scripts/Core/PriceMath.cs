using System;

namespace CRS.Core
{
    /// <summary>
    /// Базовая денежная математика сделок. Деньги храним в целых числах (long),
    /// чтобы не накапливались ошибки округления float.
    /// Логика не зависит от Unity, поэтому её удобно покрывать тестами.
    /// </summary>
    public static class PriceMath
    {
        /// <summary>Цена после скидки. percent — от 0 до 100.</summary>
        public static long ApplyDiscount(long price, double percent)
        {
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            return (long)Math.Round(price * (1.0 - percent / 100.0), MidpointRounding.AwayFromZero);
        }

        /// <summary>Чистая прибыль сделки: продажа минус покупка и вложения.</summary>
        public static long Profit(long buyPrice, long investments, long sellPrice)
        {
            return sellPrice - buyPrice - investments;
        }

        /// <summary>
        /// Округление до «рыночной» цены, как в настоящих объявлениях:
        /// 487 300 → 490 000 при шаге 10 000.
        /// </summary>
        public static long RoundToMarket(long price, long step)
        {
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
            return (long)Math.Round((double)price / step, MidpointRounding.AwayFromZero) * step;
        }
    }
}
