using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for CurrencyConverter - cross-rate and BYN bridge calculations.
    /// </summary>
    [TestClass]
    public class CurrencyConverterTests
    {
        private const int UsdId = 431;
        private const int EurId = 440;
        private const int RubId = 298;
        private const int BynId = -1;

        private readonly CurrencyConverter _converter = new CurrencyConverter();
        private readonly DateTime _date = new DateTime(2026, 7, 24);

        [TestMethod]
        public void Convert_BynToByn_ReturnsSameAmount()
        {
            var result = _converter.Convert(BynId, BynId, 123.45m, _date, Rates());

            Assert.AreEqual(123.45m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_BynToCurrency_DividesByRate()
        {
            // 100 BYN / (2.89 BYN per USD) = 34.6020...
            var result = _converter.Convert(BynId, UsdId, 100m, _date, Rates());

            Assert.AreEqual(100m / 2.89m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_CurrencyToByn_MultipliesByRate()
        {
            // 100 USD * 2.89 BYN per USD = 289 BYN
            var result = _converter.Convert(UsdId, BynId, 100m, _date, Rates());

            Assert.AreEqual(289m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_CrossRate_GoesThroughByn()
        {
            // byn_per_unit_USD = 2.89, byn_per_unit_EUR = 3.35
            // 100 * 2.89 / 3.35 = 86.2686...
            var result = _converter.Convert(UsdId, EurId, 100m, _date, Rates());

            Assert.AreEqual(100m * 2.89m / 3.35m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_UsesScaleForScaledCurrency()
        {
            // byn_per_unit_RUB = 350.12 / 100 = 3.5012
            // 10000 * 3.5012 / 2.89 = 12114.8789...
            var result = _converter.Convert(RubId, UsdId, 10000m, _date, Rates());

            Assert.AreEqual(10000m * (350.12m / 100) / 2.89m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_TakesLatestRateNotAfterRequestedDate()
        {
            var rates = new List<CurrencyRateRecord>
            {
                Record(UsdId, new DateTime(2026, 7, 20), 2.80m, 1),
                Record(UsdId, new DateTime(2026, 7, 24), 2.89m, 1),
                Record(UsdId, new DateTime(2026, 7, 27), 3.10m, 1),
                Record(EurId, new DateTime(2026, 7, 21), 3.30m, 1),
                Record(EurId, new DateTime(2026, 7, 24), 3.35m, 1)
            };

            var result = _converter.Convert(UsdId, EurId, 100m, new DateTime(2026, 7, 22), rates);

            // Newest rates on or before 2026-07-22 are 2.80 for USD and 3.30 for EUR
            Assert.AreEqual(100m * 2.80m / 3.30m, result.Value, 0.0001m);
        }

        [TestMethod]
        public void Convert_ReturnsNullWhenRateMissing()
        {
            var rates = new List<CurrencyRateRecord> { Record(UsdId, _date, 2.89m, 1) };

            var result = _converter.Convert(UsdId, EurId, 100m, _date, rates);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Convert_ReturnsNullWhenRateIsNull()
        {
            var rates = new List<CurrencyRateRecord>
            {
                Record(UsdId, _date, null, 1),
                Record(EurId, _date, 3.35m, 1)
            };

            var result = _converter.Convert(UsdId, EurId, 100m, _date, rates);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Convert_ReturnsNullWhenTargetRateIsZero()
        {
            var rates = new List<CurrencyRateRecord>
            {
                Record(UsdId, _date, 2.89m, 1),
                Record(EurId, _date, 0m, 1)
            };

            var result = _converter.Convert(UsdId, EurId, 100m, _date, rates);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Convert_ReturnsNullWhenNoRecordIsOldEnough()
        {
            var rates = new List<CurrencyRateRecord>
            {
                Record(UsdId, new DateTime(2026, 7, 25), 2.89m, 1),
                Record(EurId, new DateTime(2026, 7, 24), 3.35m, 1)
            };

            var result = _converter.Convert(UsdId, EurId, 100m, _date, rates);

            Assert.IsNull(result);
        }

        private static List<CurrencyRateRecord> Rates()
        {
            return new List<CurrencyRateRecord>
            {
                Record(UsdId, new DateTime(2026, 7, 24), 2.89m, 1, "USD"),
                Record(EurId, new DateTime(2026, 7, 24), 3.35m, 1, "EUR"),
                Record(RubId, new DateTime(2026, 7, 24), 350.12m, 100, "RUB")
            };
        }

        private static CurrencyRateRecord Record(int curId, DateTime date, decimal? rate, int scale, string abbreviation = null)
        {
            return new CurrencyRateRecord
            {
                CurId = curId,
                Date = date,
                OfficialRate = rate,
                Scale = scale,
                Abbreviation = abbreviation
            };
        }
    }
}