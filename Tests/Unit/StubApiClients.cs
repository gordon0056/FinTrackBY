using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Test double for INbrbApiClient: serves prepared responses and records how it was called.
    /// </summary>
    internal sealed class StubNbrbApiClient : INbrbApiClient
    {
        private int _runningDynamicsCalls;

        public List<Currency> Currencies { get; set; } = new List<Currency>();

        public Dictionary<DateTime, List<Rate>> RatesByDate { get; } = new Dictionary<DateTime, List<Rate>>();

        public Dictionary<int, List<RateShort>> DynamicsByCurId { get; } = new Dictionary<int, List<RateShort>>();

        /// <summary>Returns a non-null value to make GetDynamicsAsync fail for that currency.</summary>
        public Func<int, Exception> DynamicsFailure { get; set; }

        public List<DateTime> RequestedRateDates { get; } = new List<DateTime>();

        public List<int> RequestedCurrencyIds { get; } = new List<int>();

        public int DynamicsCallCount { get; private set; }

        public int CurrenciesCallCount { get; private set; }

        public int MaxParallelDynamicsCalls { get; private set; }

        public Task<List<Currency>> GetCurrenciesAsync()
        {
            CurrenciesCallCount++;
            return Task.FromResult(Currencies);
        }

        public Task<List<Rate>> GetRatesAsync(DateTime? onDate = null, int periodicity = 0)
        {
            var key = (onDate ?? DateTime.Today).Date;
            lock (RequestedRateDates)
            {
                RequestedRateDates.Add(key);
            }

            return Task.FromResult(RatesByDate.TryGetValue(key, out var rates)
                ? rates
                : new List<Rate>());
        }

        public async Task<List<RateShort>> GetDynamicsAsync(int curId, DateTime startDate, DateTime endDate)
        {
            lock (RequestedCurrencyIds)
            {
                RequestedCurrencyIds.Add(curId);
            }

            DynamicsCallCount++;

            var running = Interlocked.Increment(ref _runningDynamicsCalls);
            lock (RequestedCurrencyIds)
            {
                if (running > MaxParallelDynamicsCalls)
                    MaxParallelDynamicsCalls = running;
            }

            try
            {
                await Task.Yield();

                if (DynamicsFailure != null)
                {
                    var failure = DynamicsFailure(curId);
                    if (failure != null)
                        throw failure;
                }

                return DynamicsByCurId.TryGetValue(curId, out var dynamics)
                    ? dynamics
                    : new List<RateShort>();
            }
            finally
            {
                Interlocked.Decrement(ref _runningDynamicsCalls);
            }
        }
    }

    /// <summary>
    /// Test double for ICurrencyRepository backed by an in-memory dictionary.
    /// </summary>
    internal sealed class StubCurrencyRepository : ICurrencyRepository
    {
        private readonly Dictionary<int, Currency> _currencies;

        public StubCurrencyRepository(IEnumerable<Currency> currencies)
        {
            _currencies = currencies.ToDictionary(c => c.Cur_ID);
        }

        public Task<List<Currency>> GetActiveCurrenciesAsync()
        {
            return Task.FromResult(_currencies.Values.Where(c => c.IsActive).ToList());
        }

        public Currency GetById(int curId)
        {
            return _currencies.TryGetValue(curId, out var currency) ? currency : null;
        }
    }
}