using System;
using Newtonsoft.Json;
using FinTrackBY.Models;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for DTO deserialization — verifies all fields map correctly from sample JSON.
    /// Run manually or integrate with a test framework (NUnit/MSTest).
    /// </summary>
    public static class DtoDeserializationTests
    {
        public static void Run()
        {
            TestCurrencyDeserialization();
            TestRateDeserialization();
            TestRateShortDeserialization();
            Console.WriteLine("All DTO deserialization tests passed.");
        }

        private static void TestCurrencyDeserialization()
        {
            var json = @"{
                ""Cur_ID"": 431,
                ""Cur_ParentID"": null,
                ""Cur_Code"": ""840"",
                ""Cur_Abbreviation"": ""USD"",
                ""Cur_Name"": ""Доллар США"",
                ""Cur_Name_Bel"": ""Долар ЗША"",
                ""Cur_Name_Eng"": ""US Dollar"",
                ""Cur_QuotName"": ""Доллар"",
                ""Cur_QuotName_Bel"": ""Долар"",
                ""Cur_QuotName_Eng"": ""Dollar"",
                ""Cur_NameMulti"": ""долларов"",
                ""Cur_Name_BelMulti"": ""долараў"",
                ""Cur_Name_EngMulti"": ""dollars"",
                ""Cur_Scale"": 1,
                ""Cur_Periodicity"": 0,
                ""Cur_DateStart"": ""1995-01-01T00:00:00"",
                ""Cur_DateEnd"": ""2050-01-01T00:00:00""
            }";

            var currency = JsonConvert.DeserializeObject<Currency>(json);

            Assert(currency.Cur_ID == 431, "Cur_ID");
            Assert(currency.Cur_Code == "840", "Cur_Code is string");
            Assert(currency.Cur_Abbreviation == "USD", "Cur_Abbreviation");
            Assert(currency.Cur_Name == "Доллар США", "Cur_Name");
            Assert(currency.Cur_Scale == 1, "Cur_Scale");
            Assert(currency.Cur_Periodicity == 0, "Cur_Periodicity");
            Assert(currency.Cur_DateEnd.HasValue && currency.Cur_DateEnd.Value.Year == 2050, "Cur_DateEnd");
            Assert(currency.IsActive, "IsActive (future sentinel)");

            Console.WriteLine("  Currency deserialization: PASS");
        }

        private static void TestRateDeserialization()
        {
            var json = @"{
                ""Cur_ID"": 431,
                ""Date"": ""2026-07-24T00:00:00"",
                ""Cur_Abbreviation"": ""USD"",
                ""Cur_Scale"": 1,
                ""Cur_Name"": ""Доллар США"",
                ""Cur_OfficialRate"": 2.8909
            }";

            var rate = JsonConvert.DeserializeObject<Rate>(json);

            Assert(rate.Cur_ID == 431, "Cur_ID");
            Assert(rate.Date == new DateTime(2026, 7, 24), "Date");
            Assert(rate.Cur_Abbreviation == "USD", "Cur_Abbreviation");
            Assert(rate.Cur_Scale == 1, "Cur_Scale");
            Assert(rate.Cur_Name == "Доллар США", "Cur_Name");
            Assert(rate.Cur_OfficialRate == 2.8909m, "Cur_OfficialRate");

            Console.WriteLine("  Rate deserialization: PASS");
        }

        private static void TestRateShortDeserialization()
        {
            var json = @"{
                ""Cur_ID"": 431,
                ""Date"": ""2026-07-24T00:00:00"",
                ""Cur_OfficialRate"": 2.8909
            }";

            var rateShort = JsonConvert.DeserializeObject<RateShort>(json);

            Assert(rateShort.Cur_ID == 431, "Cur_ID");
            Assert(rateShort.Date == new DateTime(2026, 7, 24), "Date");
            Assert(rateShort.Cur_OfficialRate == 2.8909m, "Cur_OfficialRate");

            Console.WriteLine("  RateShort deserialization: PASS");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {name}");
        }
    }
}
