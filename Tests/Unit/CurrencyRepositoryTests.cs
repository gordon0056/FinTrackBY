using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for Currency.IsActive and for CurrencyRepository caching.
    /// </summary>
    [TestClass]
    public class CurrencyRepositoryTests
    {
        private const int BynSentinelId = -1;

        private readonly StubNbrbApiClient _api = new StubNbrbApiClient();

        private static string CachePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "currencies_cache.json"); }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            DeleteCache();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DeleteCache();
        }

        [TestMethod]
        public void IsActive_IsTrueWhenDateEndIsNull()
        {
            Assert.IsTrue(new Currency { Cur_DateEnd = null }.IsActive);
        }

        [TestMethod]
        public void IsActive_IsTrueWhenDateEndIsInFuture()
        {
            Assert.IsTrue(new Currency { Cur_DateEnd = DateTime.Today.AddDays(1) }.IsActive);
            Assert.IsTrue(new Currency { Cur_DateEnd = new DateTime(2050, 1, 1) }.IsActive, "Future sentinel from NBRB");
        }

        [TestMethod]
        public void IsActive_IsFalseWhenDateEndIsInPast()
        {
            Assert.IsFalse(new Currency { Cur_DateEnd = DateTime.Today.AddDays(-1) }.IsActive);
            Assert.IsFalse(new Currency { Cur_DateEnd = new DateTime(2010, 1, 1) }.IsActive);
        }

        [TestMethod]
        public async Task GetActiveCurrenciesAsync_AddsBynSentinel()
        {
            _api.Currencies = Currencies(431);

            var currencies = await new CurrencyRepository(_api).GetActiveCurrenciesAsync();

            var byn = currencies.Single(c => c.Cur_ID == BynSentinelId);
            Assert.AreEqual("BYN", byn.Cur_Abbreviation);
            Assert.AreEqual(1, byn.Cur_Scale);
            Assert.IsTrue(byn.IsActive);
        }

        [TestMethod]
        public async Task GetActiveCurrenciesAsync_ExcludesExpiredCurrencies()
        {
            _api.Currencies = Currencies(431, 440);
            _api.Currencies.Add(new Currency { Cur_ID = 999, Cur_Abbreviation = "OLD", Cur_DateEnd = new DateTime(2010, 1, 1) });

            var currencies = await new CurrencyRepository(_api).GetActiveCurrenciesAsync();

            CollectionAssert.AreEquivalent(
                new[] { 431, 440, BynSentinelId },
                currencies.Select(c => c.Cur_ID).ToArray());
        }

        [TestMethod]
        public async Task GetById_ReturnsNullUntilCacheIsLoaded()
        {
            _api.Currencies = Currencies(431);
            var repository = new CurrencyRepository(_api);

            Assert.IsNull(repository.GetById(431));

            await repository.GetActiveCurrenciesAsync();

            Assert.IsNotNull(repository.GetById(431));
        }

        [TestMethod]
        public async Task GetById_ReturnsNullForUnknownCurrency()
        {
            _api.Currencies = Currencies(431);
            var repository = new CurrencyRepository(_api);

            await repository.GetActiveCurrenciesAsync();

            Assert.IsNull(repository.GetById(999));
        }

        [TestMethod]
        public async Task GetActiveCurrenciesAsync_LoadsCacheFileOnNextRun()
        {
            _api.Currencies = Currencies(431, 440, 298, 756, 757);
            var firstRun = new CurrencyRepository(_api);

            await firstRun.GetActiveCurrenciesAsync();

            Assert.AreEqual(1, _api.CurrenciesCallCount);
            Assert.IsTrue(File.Exists(CachePath), "Currencies must be cached next to the executable");

            var secondRunApi = new StubNbrbApiClient { Currencies = new List<Currency>() };
            var secondRun = new CurrencyRepository(secondRunApi);

            var currencies = await secondRun.GetActiveCurrenciesAsync();

            Assert.AreEqual(0, secondRunApi.CurrenciesCallCount, "Valid cache file must be reused");
            CollectionAssert.AreEquivalent(
                new[] { 431, 440, 298, 756, 757, BynSentinelId },
                currencies.Select(c => c.Cur_ID).ToArray());
        }

        [TestMethod]
        public async Task GetActiveCurrenciesAsync_IgnoresTooShortCacheFile()
        {
            File.WriteAllText(CachePath, "[{\"Cur_ID\":1,\"Cur_Abbreviation\":\"USD\"}]");
            _api.Currencies = Currencies(431);

            var currencies = await new CurrencyRepository(_api).GetActiveCurrenciesAsync();

            Assert.AreEqual(1, _api.CurrenciesCallCount, "A cache with less than 5 valid rows is not trusted");
            CollectionAssert.AreEquivalent(
                new[] { 431, BynSentinelId },
                currencies.Select(c => c.Cur_ID).ToArray());
        }

        private static List<Currency> Currencies(params int[] ids)
        {
            return ids.Select(id => new Currency
            {
                Cur_ID = id,
                Cur_Abbreviation = "C" + id,
                Cur_Name = "Currency " + id,
                Cur_Scale = 1,
                Cur_DateStart = new DateTime(1995, 1, 1),
                Cur_DateEnd = new DateTime(2050, 1, 1)
            }).ToList();
        }

        private static void DeleteCache()
        {
            if (File.Exists(CachePath))
                File.Delete(CachePath);
        }
    }
}