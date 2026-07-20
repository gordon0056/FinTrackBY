using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public interface INbrbApiClient
    {
        Task<List<Currency>> GetCurrenciesAsync();
        Task<List<Rate>> GetRatesAsync(DateTime? onDate = null, int periodicity = 0);
        Task<List<RateShort>> GetDynamicsAsync(int curId, DateTime startDate, DateTime endDate);
    }
}
