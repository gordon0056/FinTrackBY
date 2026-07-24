using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for JsonRatesStorage — insert-only merge and field update.
    /// </summary>
    public static class JsonRatesStorageTests
    {
        public static async Task RunAsync()
        {
            await TestMergeAndSaveInsertOnlyAsync();
            await TestUpdateFieldAsync();
            Console.WriteLine("All JsonRatesStorage tests passed.");
        }

        private static async Task TestMergeAndSaveInsertOnlyAsync()
        {
            var storage = new JsonRatesStorage();
            var testPath = Path.Combine(Path.GetTempPath(), "test_merge.json");

            // Create initial file with one record
            var existing = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord
                {
                    CurId = 1, Date = new DateTime(2026, 7, 24),
                    Abbreviation = "USD", Name = "Dollar", OfficialRate = 2.89m, Scale = 1
                }
            };
            await storage.SaveAsync(testPath, existing);

            // Merge new records — one existing, one new
            var newRecords = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord
                {
                    CurId = 1, Date = new DateTime(2026, 7, 24),
                    Abbreviation = "USD_MODIFIED", Name = "Dollar", OfficialRate = 9.99m, Scale = 1
                },
                new CurrencyRateRecord
                {
                    CurId = 2, Date = new DateTime(2026, 7, 24),
                    Abbreviation = "EUR", Name = "Euro", OfficialRate = 3.35m, Scale = 1
                }
            };
            await storage.MergeAndSaveAsync(testPath, newRecords);

            // Verify
            var result = await storage.LoadAsync(testPath);
            Assert(result.Count == 2, "Should have 2 records (existing + new)");

            var usd = result.First(r => r.CurId == 1);
            Assert(usd.Abbreviation == "USD", "Existing USD should NOT be overwritten (insert-only)");
            Assert(usd.OfficialRate == 2.89m, "Existing rate should be preserved");

            var eur = result.First(r => r.CurId == 2);
            Assert(eur.Abbreviation == "EUR", "New EUR should be added");

            // Cleanup
            File.Delete(testPath);
            Console.WriteLine("  MergeAndSaveAsync insert-only: PASS");
        }

        private static async Task TestUpdateFieldAsync()
        {
            var storage = new JsonRatesStorage();
            var testPath = Path.Combine(Path.GetTempPath(), "test_update.json");

            var records = new List<CurrencyRateRecord>
            {
                new CurrencyRateRecord
                {
                    CurId = 1, Date = new DateTime(2026, 7, 24),
                    Abbreviation = "USD", Name = "Dollar", OfficialRate = 2.89m, Scale = 1
                },
                new CurrencyRateRecord
                {
                    CurId = 2, Date = new DateTime(2026, 7, 24),
                    Abbreviation = "EUR", Name = "Euro", OfficialRate = 3.35m, Scale = 1
                }
            };
            await storage.SaveAsync(testPath, records);

            // Update only the rate for USD
            await storage.UpdateFieldAsync(testPath, 1, new DateTime(2026, 7, 24), newOfficialRate: 3.00m);

            var result = await storage.LoadAsync(testPath);
            var usd = result.First(r => r.CurId == 1);
            var eur = result.First(r => r.CurId == 2);

            Assert(usd.OfficialRate == 3.00m, "USD rate should be updated");
            Assert(usd.Abbreviation == "USD", "USD abbreviation should be unchanged");
            Assert(eur.OfficialRate == 3.35m, "EUR should be completely unchanged");

            // Cleanup
            File.Delete(testPath);
            Console.WriteLine("  UpdateFieldAsync: PASS");
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {name}");
        }
    }
}
