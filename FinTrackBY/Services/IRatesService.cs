using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public interface IRatesService
    {
        Task<List<CurrencyRateRecord>> GetTodaySnapshotWithChangeAsync();
        Task<List<CurrencyRateRecord>> GetRatesForPeriodAsync(IEnumerable<int> curIds, DateTime startDate, DateTime endDate);
    }
}
