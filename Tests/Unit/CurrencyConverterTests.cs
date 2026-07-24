using System;
using System.Collections.Generic;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for CurrencyConverter — BYN bridge calculation.
    /// </summary>
    public static class CurrencyConverterTests
    {
        public static void Run()
        {
            TestCrossRateCalculation();
            TestNullHandling();
            TestScaleHandling();
            Console.WriteLine("All CurrencyConverter tests passed.");
        }

        private static void TestCrossRateCalculation()
        {
            var converter = new CurrencyConverter();
            var rates = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord { CurId = 1, Date = new DateTime(2026, 7, 24), OfficialRate = 2.89m, Scale = 1, Abbreviation = "USD" },
                new CurrencyRateRecord { CurId = 2, Date = new DateTime(2026, 7, 24), OfficialRate = 3.35m, Scale = 1, Abbreviation = "EUR" }
            };

            // Convert 100 USD to EUR via BYN bridge
            var result = converter.Convert(1, 2, 100, new DateTime(2026, 7, 24), rates);

            // byn_per_unit_USD = 2.89 / 1 = 2.89
            // byn_per_unit_EUR = 3.35 / 1 = 3.35
            // amount_EUR = 100 * 2.89 / 3.35 = 86.268...
            var expected = 100 * 2.89m / 3.35m;
            var tolerance = 0.01m;

            Assert(result.HasValue, "Result should have value");
            Assert(Math.Abs(result.Value - expected) < tolerance, "Cross-rate calculation");

            Console.WriteLine("  Cross-rate calculation: PASS");
        }

        private static void TestNullHandling()
        {
            var converter = new CurrencyConverter();
            var rates = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord { CurId = 1, Date = new DateTime(2026, 7, 24), OfficialRate = null, Scale = 1, Abbreviation = "USD" },
                new CurrencyRateRecord { CurId = 2, Date = new DateTime(2026, 7, 24), OfficialRate = 3.35m, Scale = 1, Abbreviation = "EUR" }
            };

            var result = converter.Convert(1, 2, 100, new DateTime(2026, 7, 24), rates);
            Assert(!result.HasValue, "Null rate should produce null result");

            Console.WriteLine("  Null handling: PASS");
        }

        private static void TestScaleHandling()
        {
            var converter = new CurrencyConverter();
            var rates = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord { CurId = 1, Date = new DateTime(2026, 7, 24), OfficialRate = 350.12m, Scale = 100, Abbreviation = "RUB" },
                new CurrencyRateRecord { CurId = 2, Date = new DateTime(2026, 7, 24), OfficialRate = 2.89m, Scale = 1, Abbreviation = "USD" }
            };

            // Convert 10000 RUB to USD
            // byn_per_unit_RUB = 350.12 / 100 = 3.5012
            // byn_per_unit_USD = 2.89 / 1 = 2.89
            // amount_USD = 10000 * 3.5012 / 2.89 = 12114.87...
            var result = converter.Convert(1, 2, 10000, new DateTime(2026, 7, 24), rates);

            var expected = 10000 * (350.12m / 100) / 2.89m;
            var tolerance = 0.01m;

            Assert(result.HasValue, "Result should have value");
            Assert(Math.Abs(result.Value - expected) < tolerance, "Scale handling");

            Console.WriteLine("  Scale handling: PASS");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {name}");
        }
    }
}
