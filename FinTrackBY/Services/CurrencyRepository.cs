using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly INbrbApiClient _apiClient;
        private readonly string _cachePath;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromDays(1);
        private Dictionary<int, Currency> _cache;

        public CurrencyRepository(INbrbApiClient apiClient)
        {
            _apiClient = apiClient;
            _cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "currencies_cache.json");
        }

        public async Task<List<Currency>> GetActiveCurrenciesAsync()
        {
            await EnsureCacheAsync().ConfigureAwait(false);
            var currencies = _cache.Values.Where(c => c.IsActive).ToList();
            var byn = new Currency
            {
                Cur_ID = -1, // Sentinel ID for BYN
                Cur_Abbreviation = "BYN",
                Cur_Name = "Белорусский рубль",
                Cur_Name_Bel = "Беларускі рубель",
                Cur_Name_Eng = "Belarusian Ruble",
                Cur_Scale = 1,
                Cur_DateStart = DateTime.MinValue,
                Cur_DateEnd = DateTime.MaxValue
            };
            currencies.Add(byn);
            
            return currencies;
        }

        public Currency GetById(int curId)
        {
            if (_cache == null)
                return null;
            return _cache.TryGetValue(curId, out var currency) ? currency : null;
        }

        private async Task EnsureCacheAsync()
        {
            bool cacheValid = File.Exists(_cachePath) &&
                              (File.GetLastWriteTime(_cachePath) + _cacheTtl) > DateTime.Now;

            if (cacheValid && _cache != null)
                return;

            if (cacheValid)
            {
                var json = await Task.Run(() => File.ReadAllText(_cachePath)).ConfigureAwait(false);
                var loaded = JsonConvert.DeserializeObject<List<Currency>>(json) ?? new List<Currency>();
                int validRows = loaded.Count(c => c.Cur_ID > 0 && !string.IsNullOrWhiteSpace(c.Cur_Abbreviation));
                if (validRows >= 5)
                {
                    _cache = loaded
                        .GroupBy(c => c.Cur_ID)
                        .ToDictionary(g => g.Key, g => g.First());
                    return;
                }

            }
            var currencies = await _apiClient.GetCurrenciesAsync().ConfigureAwait(false);
            _cache = currencies
                .GroupBy(c => c.Cur_ID)
                .ToDictionary(g => g.Key, g => g.First());

            var jsonOut = JsonConvert.SerializeObject(currencies, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(_cachePath, jsonOut)).ConfigureAwait(false);
        }
    }
}
