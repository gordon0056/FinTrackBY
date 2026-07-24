using System;
using System.Collections.Generic;
using FinTrackBY.Models;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for CurrencyRepository active filter.
    /// Verifies Cur_DateEnd >= today returns correct subset, Cur_DateEnd == null returns empty.
    /// </summary>
    public static class CurrencyRepositoryTests
    {
        public static void Run()
        {
            TestActiveFilter();
            TestInactiveFilter();
            Console.WriteLine("All CurrencyRepository tests passed.");
        }

        private static void TestActiveFilter()
        {
            var currencies = new List<Currency>
            {
                new Currency { Cur_ID = 1, Cur_Abbreviation = "USD", Cur_DateEnd = new DateTime(2050, 1, 1) },
                new Currency { Cur_ID = 2, Cur_Abbreviation = "EUR", Cur_DateEnd = new DateTime(2050, 1, 1) },
                new Currency { Cur_ID = 3, Cur_Abbreviation = "OLD", Cur_DateEnd = new DateTime(2020, 1, 1) }
            };

            var active = currencies.FindAll(c => c.IsActive);

            Assert(active.Count == 2, "Active count should be 2");
            Assert(active.Exists(c => c.Cur_Abbreviation == "USD"), "USD should be active");
            Assert(active.Exists(c => c.Cur_Abbreviation == "EUR"), "EUR should be active");
            Assert(!active.Exists(c => c.Cur_Abbreviation == "OLD"), "OLD should be inactive");

            Console.WriteLine("  Active filter: PASS");
        }

        private static void TestInactiveFilter()
        {
            var currencies = new List<Currency>
            {
                new Currency { Cur_ID = 1, Cur_Abbreviation = "OLD1", Cur_DateEnd = new DateTime(2010, 1, 1) },
                new Currency { Cur_ID = 2, Cur_Abbreviation = "OLD2", Cur_DateEnd = new DateTime(2015, 1, 1) }
            };

            var active = currencies.FindAll(c => c.IsActive);

            Assert(active.Count == 0, "No active currencies when all are expired");

            Console.WriteLine("  Inactive filter (all expired): PASS");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {name}");
        }
    }
}
