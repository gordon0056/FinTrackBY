using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public class NbrbApiClient : INbrbApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.nbrb.by/exrates/")
        };

        public async Task<List<Currency>> GetCurrenciesAsync()
        {
            var json = await _httpClient.GetStringAsync("currencies").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<Currency>>(json) ?? new List<Currency>();
        }

        public async Task<List<Rate>> GetRatesAsync(DateTime? onDate = null, int periodicity = 0)
        {
            var url = $"rates?periodicity={periodicity}";
            if (onDate.HasValue)
            {
                url += $"&ondate={onDate.Value:yyyy-MM-dd}";
            }
            var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<Rate>>(json) ?? new List<Rate>();
        }

        public async Task<List<RateShort>> GetDynamicsAsync(int curId, DateTime startDate, DateTime endDate)
        {
            var url = $"rates/dynamics/{curId}?startdate={startDate:yyyy-MM-dd}&enddate={endDate:yyyy-MM-dd}";
            var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<RateShort>>(json) ?? new List<RateShort>();
        }
    }
}
