using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using FinTrackBY.Models;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for NBRB DTO deserialization - verifies all fields map correctly from sample JSON.
    /// </summary>
    [TestClass]
    public class DtoDeserializationTests
    {
        [TestMethod]
        public void Currency_MapsEveryFieldFromNbrbJson()
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

            Assert.IsNotNull(currency);
            Assert.AreEqual(431, currency.Cur_ID);
            Assert.IsNull(currency.Cur_ParentID);
            Assert.AreEqual("840", currency.Cur_Code);
            Assert.AreEqual("USD", currency.Cur_Abbreviation);
            Assert.AreEqual("Доллар США", currency.Cur_Name);
            Assert.AreEqual("Долар ЗША", currency.Cur_Name_Bel);
            Assert.AreEqual("US Dollar", currency.Cur_Name_Eng);
            Assert.AreEqual("Доллар", currency.Cur_QuotName);
            Assert.AreEqual("Долар", currency.Cur_QuotName_Bel);
            Assert.AreEqual("Dollar", currency.Cur_QuotName_Eng);
            Assert.AreEqual("долларов", currency.Cur_NameMulti);
            Assert.AreEqual("долараў", currency.Cur_Name_BelMulti);
            Assert.AreEqual("dollars", currency.Cur_Name_EngMulti);
            Assert.AreEqual(1, currency.Cur_Scale);
            Assert.AreEqual(0, currency.Cur_Periodicity);
            Assert.AreEqual(new DateTime(1995, 1, 1), currency.Cur_DateStart);
            Assert.AreEqual(new DateTime(2050, 1, 1), currency.Cur_DateEnd.Value);
            Assert.IsTrue(currency.IsActive, "Future Cur_DateEnd sentinel means the currency is active");
        }

        [TestMethod]
        public void Currency_TreatsMissingDateEndAsActive()
        {
            var json = @"{ ""Cur_ID"": 440, ""Cur_Abbreviation"": ""EUR"" }";

            var currency = JsonConvert.DeserializeObject<Currency>(json);

            Assert.IsNull(currency.Cur_DateEnd);
            Assert.IsTrue(currency.IsActive);
        }

        [TestMethod]
        public void Rate_MapsEveryFieldFromNbrbJson()
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

            Assert.IsNotNull(rate);
            Assert.AreEqual(431, rate.Cur_ID);
            Assert.AreEqual(new DateTime(2026, 7, 24), rate.Date);
            Assert.AreEqual("USD", rate.Cur_Abbreviation);
            Assert.AreEqual(1, rate.Cur_Scale);
            Assert.AreEqual("Доллар США", rate.Cur_Name);
            Assert.AreEqual(2.8909m, rate.Cur_OfficialRate.Value, 0.0001m);
        }

        [TestMethod]
        public void Rate_MapsNullOfficialRate()
        {
            var json = @"{ ""Cur_ID"": 431, ""Date"": ""2026-07-24T00:00:00"", ""Cur_OfficialRate"": null }";

            var rate = JsonConvert.DeserializeObject<Rate>(json);

            Assert.IsNull(rate.Cur_OfficialRate);
        }

        [TestMethod]
        public void RateShort_MapsEveryFieldFromNbrbJson()
        {
            var json = @"{
                ""Cur_ID"": 431,
                ""Date"": ""2026-07-24T00:00:00"",
                ""Cur_OfficialRate"": 2.8909
            }";

            var rateShort = JsonConvert.DeserializeObject<RateShort>(json);

            Assert.IsNotNull(rateShort);
            Assert.AreEqual(431, rateShort.Cur_ID);
            Assert.AreEqual(new DateTime(2026, 7, 24), rateShort.Date);
            Assert.AreEqual(2.8909m, rateShort.Cur_OfficialRate.Value, 0.0001m);
        }

        [TestMethod]
        public void CurrencyRateRecord_DoesNotSerializeChangePercent()
        {
            var record = new CurrencyRateRecord
            {
                CurId = 431,
                Date = new DateTime(2026, 7, 24),
                Abbreviation = "USD",
                Name = "Доллар США",
                OfficialRate = 2.89m,
                Scale = 1,
                ChangePercent = 0.12m
            };

            var json = JsonConvert.SerializeObject(record);

            Assert.IsFalse(json.Contains("ChangePercent"), "ChangePercent is UI-only state");
            Assert.IsTrue(json.Contains("\"OfficialRate\""));
        }

        [TestMethod]
        public void CurrencyRateRecord_RaisesPropertyChangedForEditedFields()
        {
            var record = new CurrencyRateRecord { Abbreviation = "USD", OfficialRate = 2.89m };
            var changed = new List<string>();
            record.PropertyChanged += (sender, args) => changed.Add(args.PropertyName);

            record.OfficialRate = 3.00m;
            record.OfficialRate = 3.00m;
            record.Abbreviation = "USD_NEW";

            CollectionAssert.AreEqual(new[] { "OfficialRate", "Abbreviation" }, changed);
        }
    }
}