using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public class JsonRatesStorage : IJsonRatesStorage
    {
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public async Task<List<CurrencyRateRecord>> LoadAsync(string path)
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                return await LoadCoreAsync(path).ConfigureAwait(false);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveAsync(string path, List<CurrencyRateRecord> records)
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SaveCoreAsync(path, records).ConfigureAwait(false);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task MergeAndSaveAsync(string path, List<CurrencyRateRecord> newRecords)
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var existing = await LoadCoreAsync(path).ConfigureAwait(false);
                var keySet = new HashSet<(int CurId, DateTime Date)>(
                    existing.Select(r => (r.CurId, r.Date)));

                foreach (var record in newRecords)
                {
                    if (!keySet.Contains((record.CurId, record.Date)))
                    {
                        existing.Add(record);
                        keySet.Add((record.CurId, record.Date));
                    }
                }
                existing.Sort((a, b) =>
                {
                    int cmp = string.Compare(a.Abbreviation, b.Abbreviation, StringComparison.Ordinal);
                    return cmp != 0 ? cmp : a.Date.CompareTo(b.Date);
                });

                await SaveCoreAsync(path, existing).ConfigureAwait(false);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task UpdateFieldAsync(string path, int curId, DateTime date, string newAbbreviation = null, decimal? newOfficialRate = null)
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var records = await LoadCoreAsync(path).ConfigureAwait(false);
                var record = records.FirstOrDefault(r => r.CurId == curId && r.Date == date);
                if (record == null)
                    return;

                if (newAbbreviation != null)
                    record.Abbreviation = newAbbreviation;
                if (newOfficialRate.HasValue)
                    record.OfficialRate = newOfficialRate;

                await SaveCoreAsync(path, records).ConfigureAwait(false);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<List<CurrencyRateRecord>> LoadCoreAsync(string path)
        {
            if (!File.Exists(path))
                return new List<CurrencyRateRecord>();

            var json = await Task.Run(() => File.ReadAllText(path)).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<CurrencyRateRecord>>(json)
                ?? new List<CurrencyRateRecord>();
        }

        private async Task SaveCoreAsync(string path, List<CurrencyRateRecord> records)
        {
            var json = JsonConvert.SerializeObject(records, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(path, json)).ConfigureAwait(false);
        }
    }
}
