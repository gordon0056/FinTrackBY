using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for RatesService: change percentage, business day handling and period loading.
    /// </summary>
    [TestClass]
    public class RatesServiceTests
    {
        private const int UsdId = 431;
        private const int EurId = 440;

        [TestMethod]
        public async Task GetTodaySnapshot_ComputesChangePercentAgainstPreviousBusinessDay()
        {
            var api = new StubNbrbApiClient();
            var today = DateTime.Today;
            var businessDay = PreviousOrSameBusinessDay(today);

            api.RatesByDate[businessDay] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Name = "Доллар США", Cur_Scale = 1, Cur_OfficialRate = 2.90m },
                new Rate { Cur_ID = EurId, Cur_Abbreviation = "EUR", Cur_Name = "Евро", Cur_Scale = 1, Cur_OfficialRate = 3.35m }
            };
            api.RatesByDate[PreviousBusinessDay(businessDay)] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Scale = 1, Cur_OfficialRate = 2.85m },
                new Rate { Cur_ID = EurId, Cur_Scale = 1, Cur_OfficialRate = 3.50m }
            };

            var records = await CreateService(api).GetTodaySnapshotWithChangeAsync();

            Assert.AreEqual(2, records.Count);

            var usd = records.Single(r => r.CurId == UsdId);
            Assert.AreEqual("USD", usd.Abbreviation);
            Assert.AreEqual("Доллар США", usd.Name);
            Assert.AreEqual(2.90m, usd.OfficialRate.Value, 0.0001m);
            Assert.AreEqual((2.90m - 2.85m) / 2.85m * 100, usd.ChangePercent.Value, 0.0001m);

            var eur = records.Single(r => r.CurId == EurId);
            Assert.AreEqual((3.35m - 3.50m) / 3.50m * 100, eur.ChangePercent.Value, 0.0001m);
        }

        [TestMethod]
        public async Task GetTodaySnapshot_MarksEveryRecordWithTodayDate()
        {
            var api = new StubNbrbApiClient();
            var businessDay = PreviousOrSameBusinessDay(DateTime.Today);

            api.RatesByDate[businessDay] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Scale = 1, Cur_OfficialRate = 2.90m }
            };

            var records = await CreateService(api).GetTodaySnapshotWithChangeAsync();

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(DateTime.Today, records[0].Date);
        }

        [TestMethod]
        public async Task GetTodaySnapshot_SkipsWeekendWhenLookingForPreviousRates()
        {
            var api = new StubNbrbApiClient();
            var businessDay = PreviousOrSameBusinessDay(DateTime.Today);

            api.RatesByDate[businessDay] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Scale = 1, Cur_OfficialRate = 2.90m }
            };
            api.RatesByDate[PreviousBusinessDay(businessDay)] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Scale = 1, Cur_OfficialRate = 2.85m }
            };

            await CreateService(api).GetTodaySnapshotWithChangeAsync();

            Assert.IsFalse(api.RequestedRateDates.Any(d => d.DayOfWeek == DayOfWeek.Saturday), "Saturday must not be requested");
            Assert.IsFalse(api.RequestedRateDates.Any(d => d.DayOfWeek == DayOfWeek.Sunday), "Sunday must not be requested");
            CollectionAssert.Contains(api.RequestedRateDates, businessDay);
            CollectionAssert.Contains(api.RequestedRateDates, PreviousBusinessDay(businessDay));
        }

        [TestMethod]
        public async Task GetTodaySnapshot_ReturnsEmptyListWhenBusinessDayHasNoRates()
        {
            var api = new StubNbrbApiClient();

            var records = await CreateService(api).GetTodaySnapshotWithChangeAsync();

            Assert.AreEqual(0, records.Count);
        }

        [TestMethod]
        public async Task GetTodaySnapshot_LeavesChangePercentNullWhenPreviousRateIsMissing()
        {
            var api = new StubNbrbApiClient();
            var businessDay = PreviousOrSameBusinessDay(DateTime.Today);

            api.RatesByDate[businessDay] = new List<Rate>
            {
                new Rate { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Scale = 1, Cur_OfficialRate = 2.90m },
                new Rate { Cur_ID = EurId, Cur_Abbreviation = "EUR", Cur_Scale = 1, Cur_OfficialRate = 3.35m }
            };
            api.RatesByDate[PreviousBusinessDay(businessDay)] = new List<Rate>
            {
                new Rate { Cur_ID = EurId, Cur_Scale = 1, Cur_OfficialRate = 3.50m }
            };

            var records = await CreateService(api).GetTodaySnapshotWithChangeAsync();

            Assert.IsNull(records.Single(r => r.CurId == UsdId).ChangePercent, "ChangePercent needs a previous rate");
            Assert.IsNotNull(records.Single(r => r.CurId == EurId).ChangePercent);
        }

        [TestMethod]
        public async Task GetTodaySnapshot_LeavesChangePercentNullWhenPreviousRateIsEmptyOrZero()
        {
            foreach (var previousRate in new decimal?[] { null, 0m })
            {
                var api = new StubNbrbApiClient();
                var businessDay = PreviousOrSameBusinessDay(DateTime.Today);

                api.RatesByDate[businessDay] = new List<Rate>
                {
                    new Rate { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Scale = 1, Cur_OfficialRate = 2.90m }
                };
                api.RatesByDate[PreviousBusinessDay(businessDay)] = new List<Rate>
                {
                    new Rate { Cur_ID = UsdId, Cur_Scale = 1, Cur_OfficialRate = previousRate }
                };

                var records = await CreateService(api).GetTodaySnapshotWithChangeAsync();

                Assert.IsNull(records.Single().ChangePercent, $"ChangePercent for previous rate {previousRate}");
            }
        }

        [TestMethod]
        public async Task GetRatesForPeriod_FillsCurrencyMetadataFromRepository()
        {
            var start = new DateTime(2026, 7, 1);
            var end = new DateTime(2026, 7, 24);

            var api = new StubNbrbApiClient();
            api.DynamicsByCurId[UsdId] = new List<RateShort>
            {
                new RateShort { Cur_ID = UsdId, Date = new DateTime(2026, 7, 1), Cur_OfficialRate = 2.80m },
                new RateShort { Cur_ID = UsdId, Date = new DateTime(2026, 7, 2), Cur_OfficialRate = 2.81m }
            };

            var repository = new StubCurrencyRepository(new List<Currency>
            {
                new Currency { Cur_ID = UsdId, Cur_Abbreviation = "USD", Cur_Name = "Доллар США", Cur_Scale = 1 }
            });

            var records = await CreateService(api, repository).GetRatesForPeriodAsync(new[] { UsdId }, start, end);

            Assert.AreEqual(2, records.Count);
            Assert.IsTrue(records.All(r => r.Abbreviation == "USD"));
            Assert.IsTrue(records.All(r => r.Name == "Доллар США"));
            Assert.IsTrue(records.All(r => r.Scale == 1));
            Assert.IsTrue(records.All(r => !r.ChangePercent.HasValue), "ChangePercent is computed for the snapshot only");
        }

        [TestMethod]
        public async Task GetRatesForPeriod_LeavesMetadataEmptyForUnknownCurrency()
        {
            var api = new StubNbrbApiClient();
            api.DynamicsByCurId[UsdId] = new List<RateShort>
            {
                new RateShort { Cur_ID = UsdId, Date = new DateTime(2026, 7, 1), Cur_OfficialRate = 2.80m }
            };

            var records = await CreateService(api).GetRatesForPeriodAsync(
                new[] { UsdId }, new DateTime(2026, 7, 1), new DateTime(2026, 7, 24));

            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(string.Empty, records[0].Abbreviation);
            Assert.AreEqual(string.Empty, records[0].Name);
            Assert.AreEqual(1, records[0].Scale);
        }

        [TestMethod]
        public async Task GetRatesForPeriod_SkipsCurrencyWhenApiThrows()
        {
            var api = new StubNbrbApiClient();
            api.DynamicsByCurId[EurId] = new List<RateShort>
            {
                new RateShort { Cur_ID = EurId, Date = new DateTime(2026, 7, 1), Cur_OfficialRate = 3.30m }
            };
            api.DynamicsFailure = curId => curId == UsdId ? new InvalidOperationException("HTTP 500") : null;

            var records = await CreateService(api).GetRatesForPeriodAsync(
                new[] { UsdId, EurId }, new DateTime(2026, 7, 1), new DateTime(2026, 7, 24));

            Assert.AreEqual(2, api.DynamicsCallCount, "Both currencies must be requested");
            Assert.AreEqual(1, records.Count, "A failing currency must not break the whole period");
            Assert.AreEqual(EurId, records[0].CurId);
        }

        [TestMethod]
        public async Task GetRatesForPeriod_RequestsEveryCurrencyAndKeepsConcurrencyBounded()
        {
            const int MaxConcurrency = 8;
            var curIds = Enumerable.Range(1, 20).ToArray();
            var api = new StubNbrbApiClient();
            foreach (var curId in curIds)
            {
                api.DynamicsByCurId[curId] = new List<RateShort>
                {
                    new RateShort { Cur_ID = curId, Date = new DateTime(2026, 7, 1), Cur_OfficialRate = curId }
                };
            }

            var records = await CreateService(api).GetRatesForPeriodAsync(
                curIds, new DateTime(2026, 7, 1), new DateTime(2026, 7, 24));

            Assert.AreEqual(curIds.Length, api.DynamicsCallCount);
            CollectionAssert.AreEquivalent(curIds, api.RequestedCurrencyIds);
            Assert.AreEqual(curIds.Length, records.Count);
            Assert.IsTrue(api.MaxParallelDynamicsCalls <= MaxConcurrency,
                $"Concurrency limit exceeded: {api.MaxParallelDynamicsCalls} > {MaxConcurrency}");
        }

        [TestMethod]
        public async Task GetRatesForPeriod_ReturnsEmptyListWhenNoCurrenciesRequested()
        {
            var api = new StubNbrbApiClient();

            var records = await CreateService(api).GetRatesForPeriodAsync(
                new int[0], new DateTime(2026, 7, 1), new DateTime(2026, 7, 24));

            Assert.AreEqual(0, records.Count);
            Assert.AreEqual(0, api.DynamicsCallCount);
        }

        private static RatesService CreateService(StubNbrbApiClient api, ICurrencyRepository repository = null)
        {
            return new RatesService(
                api,
                repository ?? new StubCurrencyRepository(new List<Currency>()),
                new JsonRatesStorage());
        }

        private static DateTime PreviousOrSameBusinessDay(DateTime day)
        {
            var result = day;
            while (IsWeekend(result))
                result = result.AddDays(-1);
            return result;
        }

        private static DateTime PreviousBusinessDay(DateTime day)
        {
            return PreviousOrSameBusinessDay(day.AddDays(-1));
        }

        private static bool IsWeekend(DateTime day)
        {
            return day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}