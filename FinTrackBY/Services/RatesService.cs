using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public class RatesService : IRatesService
    {
        private readonly INbrbApiClient _apiClient;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IJsonRatesStorage _jsonStorage;
        private const int MaxConcurrency = 8;
        private const int MaxRetryDays = 7;

        public RatesService(INbrbApiClient apiClient, ICurrencyRepository currencyRepository, IJsonRatesStorage jsonStorage)
        {
            _apiClient = apiClient;
            _currencyRepository = currencyRepository;
            _jsonStorage = jsonStorage;
        }
        public async Task<List<CurrencyRateRecord>> GetTodaySnapshotWithChangeAsync()
        {
            var today = DateTime.Today;
            var businessDay = today;
            while (businessDay.DayOfWeek == DayOfWeek.Saturday || businessDay.DayOfWeek == DayOfWeek.Sunday)
                businessDay = businessDay.AddDays(-1);

            var todayRates = await _apiClient.GetRatesAsync(businessDay).ConfigureAwait(false);

            if (todayRates == null || todayRates.Count == 0)
                return new List<CurrencyRateRecord>();

            var records = todayRates.Select(r => new CurrencyRateRecord
            {
                CurId = r.Cur_ID,
                Date = today,
                Abbreviation = r.Cur_Abbreviation,
                Name = r.Cur_Name,
                OfficialRate = r.Cur_OfficialRate,
                Scale = r.Cur_Scale
            }).ToList();
            DateTime? prevDate = null;
            List<Rate> prevRates = null;
            for (int i = 1; i <= MaxRetryDays; i++)
            {
                var candidate = businessDay.AddDays(-i);
                if (candidate.DayOfWeek == DayOfWeek.Saturday || candidate.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                prevRates = await _apiClient.GetRatesAsync(candidate).ConfigureAwait(false);
                if (prevRates != null && prevRates.Count > 0)
                {
                    prevDate = candidate;
                    break;
                }
            }
            if (prevDate.HasValue && prevRates != null)
            {
                Dictionary<int, Rate> prevDict;
                try
                {
                    prevDict = prevRates.ToDictionary(r => r.Cur_ID);
                }
                catch (Exception)
                {
                    return records;
                }

                foreach (var record in records)
                {
                    if (prevDict.TryGetValue(record.CurId, out var prevRate) &&
                        record.OfficialRate.HasValue && prevRate.Cur_OfficialRate.HasValue &&
                        prevRate.Cur_OfficialRate.Value != 0)
                    {
                        record.ChangePercent = (record.OfficialRate.Value - prevRate.Cur_OfficialRate.Value)
                            / prevRate.Cur_OfficialRate.Value * 100;
                    }
                }
            }

            return records;
        }
        public async Task<List<CurrencyRateRecord>> GetRatesForPeriodAsync(IEnumerable<int> curIds, DateTime startDate, DateTime endDate)
        {
            var semaphore = new SemaphoreSlim(MaxConcurrency);
            var allRecords = new List<CurrencyRateRecord>();
            var tasks = curIds.Select(async curId =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    try
                    {
                        var dynamics = await _apiClient.GetDynamicsAsync(curId, startDate, endDate).ConfigureAwait(false);
                        if (dynamics == null || dynamics.Count == 0)
                            return new List<CurrencyRateRecord>();

                        var currency = _currencyRepository.GetById(curId);
                        return dynamics.Select(d => new CurrencyRateRecord
                        {
                            CurId = d.Cur_ID,
                            Date = d.Date,
                            Abbreviation = currency?.Cur_Abbreviation ?? string.Empty,
                            Name = currency?.Cur_Name ?? string.Empty,
                            OfficialRate = d.Cur_OfficialRate,
                            Scale = currency?.Cur_Scale ?? 1
                        }).ToList();
                    }
                    catch (Exception)
                    {
                        return new List<CurrencyRateRecord>();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var result in results)
            {
                if (result != null)
                    allRecords.AddRange(result);
            }

            return allRecords;
        }
    }
}
