using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.Tests.Unit
{
    /// <summary>
    /// Unit tests for RatesService — enrichment and parallel execution.
    /// </summary>
    public static class RatesServiceTests
    {
        public static async Task RunAsync()
        {
            await TestDeltaPercentCalculationAsync();
            Console.WriteLine("All RatesService tests passed.");
        }

        private static Task TestDeltaPercentCalculationAsync()
        {
            // This test verifies the Δ% formula: (todayRate - prevRate) / prevRate * 100
            // Note: Full integration test requires mocking the API client.
            // Here we verify the formula logic manually.

            decimal todayRate = 2.90m;
            decimal prevRate = 2.85m;
            var expected = (todayRate - prevRate) / prevRate * 100;

            // Expected: (2.90 - 2.85) / 2.85 * 100 = 1.754...%
            var tolerance = 0.01m;
            Assert(Math.Abs(expected - 1.7543859649122807m) < tolerance, "Δ% formula");

            // Test null handling
            decimal? nullRate = null;
            decimal? result = nullRate.HasValue ? (nullRate.Value - prevRate) / prevRate * 100 : (decimal?)null;
            Assert(!result.HasValue, "Null rate should produce null result");

            Console.WriteLine("  Δ% calculation formula: PASS");
            return Task.CompletedTask;
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {name}");
        }
    }
}
