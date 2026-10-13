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
    /// Unit tests for JsonRatesStorage - insert-only merge, field update and file round-trip.
    /// </summary>
    [TestClass]
    public class JsonRatesStorageTests
    {
        private readonly JsonRatesStorage _storage = new JsonRatesStorage();
        private readonly List<string> _tempFiles = new List<string>();

        [TestCleanup]
        public void TestCleanup()
        {
            foreach (var file in _tempFiles.Where(File.Exists))
            {
                File.Delete(file);
            }

            _tempFiles.Clear();
        }

        [TestMethod]
        public async Task LoadAsync_ReturnsEmptyListWhenFileDoesNotExist()
        {
            var path = TempFile();

            var records = await _storage.LoadAsync(path);

            Assert.AreEqual(0, records.Count);
        }

        [TestMethod]
        public async Task SaveAsync_ThenLoadAsync_RoundTripsRecords()
        {
            var path = TempFile();
            var saved = new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, 0.12m),
                Record(440, new DateTime(2026, 7, 24), "EUR", "Евро", 3.35m, 1, null)
            };

            await _storage.SaveAsync(path, saved);
            var loaded = await _storage.LoadAsync(path);

            Assert.AreEqual(2, loaded.Count);

            var usd = loaded.Single(r => r.CurId == 431);
            Assert.AreEqual("USD", usd.Abbreviation);
            Assert.AreEqual("Доллар США", usd.Name);
            Assert.AreEqual(2.89m, usd.OfficialRate.Value, 0.0001m);
            Assert.AreEqual(1, usd.Scale);
            Assert.IsNull(usd.ChangePercent, "ChangePercent is not persisted to JSON");
        }

        [TestMethod]
        public async Task MergeAndSaveAsync_KeepsExistingRecordsAndAddsNewOnes()
        {
            var path = TempFile();
            await _storage.SaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            await _storage.MergeAndSaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD_MODIFIED", "Доллар США", 9.99m, 1, null),
                Record(440, new DateTime(2026, 7, 24), "EUR", "Евро", 3.35m, 1, null)
            });

            var loaded = await _storage.LoadAsync(path);

            Assert.AreEqual(2, loaded.Count);

            var usd = loaded.Single(r => r.CurId == 431);
            Assert.AreEqual("USD", usd.Abbreviation, "Existing record must not be overwritten");
            Assert.AreEqual(2.89m, usd.OfficialRate.Value, 0.0001m);

            Assert.AreEqual("EUR", loaded.Single(r => r.CurId == 440).Abbreviation);
        }

        [TestMethod]
        public async Task MergeAndSaveAsync_TreatsCurrencyAndDateAsKey()
        {
            var path = TempFile();
            await _storage.SaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            await _storage.MergeAndSaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 3.00m, 1, null),
                Record(431, new DateTime(2026, 7, 25), "USD", "Доллар США", 2.95m, 1, null)
            });

            var loaded = await _storage.LoadAsync(path);

            Assert.AreEqual(2, loaded.Count, "Same currency on another date is a new record");
            Assert.AreEqual(2.89m, loaded.Single(r => r.Date == new DateTime(2026, 7, 24)).OfficialRate.Value, 0.0001m);
            Assert.AreEqual(2.95m, loaded.Single(r => r.Date == new DateTime(2026, 7, 25)).OfficialRate.Value, 0.0001m);
        }

        [TestMethod]
        public async Task MergeAndSaveAsync_SortsByAbbreviationThenDate()
        {
            var path = TempFile();

            await _storage.MergeAndSaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(440, new DateTime(2026, 7, 25), "EUR", "Евро", 3.36m, 1, null),
                Record(431, new DateTime(2026, 7, 25), "USD", "Доллар США", 2.90m, 1, null),
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            var loaded = await _storage.LoadAsync(path);

            var keys = loaded.Select(r => r.Abbreviation + " " + r.Date.ToString("yyyy-MM-dd")).ToList();
            CollectionAssert.AreEqual(new[] { "EUR 2026-07-25", "USD 2026-07-24", "USD 2026-07-25" }, keys);
        }

        [TestMethod]
        public async Task UpdateFieldAsync_ChangesOnlyRequestedField()
        {
            var path = TempFile();
            await _storage.SaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null),
                Record(440, new DateTime(2026, 7, 24), "EUR", "Евро", 3.35m, 1, null)
            });

            await _storage.UpdateFieldAsync(path, 431, new DateTime(2026, 7, 24), newOfficialRate: 3.00m);

            var loaded = await _storage.LoadAsync(path);

            var usd = loaded.Single(r => r.CurId == 431);
            Assert.AreEqual(3.00m, usd.OfficialRate.Value, 0.0001m);
            Assert.AreEqual("USD", usd.Abbreviation);
            Assert.AreEqual("Доллар США", usd.Name);
            Assert.AreEqual(1, usd.Scale);

            var eur = loaded.Single(r => r.CurId == 440);
            Assert.AreEqual(3.35m, eur.OfficialRate.Value, 0.0001m);
            Assert.AreEqual("EUR", eur.Abbreviation);
        }

        [TestMethod]
        public async Task UpdateFieldAsync_ChangesAbbreviationWhenProvided()
        {
            var path = TempFile();
            await _storage.SaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            await _storage.UpdateFieldAsync(path, 431, new DateTime(2026, 7, 24), newAbbreviation: "USD_NEW");

            var loaded = await _storage.LoadAsync(path);

            Assert.AreEqual("USD_NEW", loaded.Single().Abbreviation);
            Assert.AreEqual(2.89m, loaded.Single().OfficialRate.Value, 0.0001m);
        }

        [TestMethod]
        public async Task UpdateFieldAsync_IgnoresUnknownCurrencyOrDate()
        {
            var path = TempFile();
            await _storage.SaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            await _storage.UpdateFieldAsync(path, 999, new DateTime(2026, 7, 24), newOfficialRate: 3.00m);
            await _storage.UpdateFieldAsync(path, 431, new DateTime(2026, 1, 1), newOfficialRate: 3.00m);

            var loaded = await _storage.LoadAsync(path);

            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual(2.89m, loaded.Single().OfficialRate.Value, 0.0001m);
        }

        [TestMethod]
        public async Task MergeAndSaveAsync_CreatesFileWhenMissing()
        {
            var path = TempFile();

            await _storage.MergeAndSaveAsync(path, new List<CurrencyRateRecord>
            {
                Record(431, new DateTime(2026, 7, 24), "USD", "Доллар США", 2.89m, 1, null)
            });

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(1, (await _storage.LoadAsync(path)).Count);
        }

        private string TempFile()
        {
            var directory = Path.Combine(Path.GetTempPath(), "FinTrackBY.Tests");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
            _tempFiles.Add(path);
            return path;
        }

        private static CurrencyRateRecord Record(
            int curId, DateTime date, string abbreviation, string name, decimal? rate, int scale, decimal? changePercent)
        {
            return new CurrencyRateRecord
            {
                CurId = curId,
                Date = date,
                Abbreviation = abbreviation,
                Name = name,
                OfficialRate = rate,
                Scale = scale,
                ChangePercent = changePercent
            };
        }
    }
}