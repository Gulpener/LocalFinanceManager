using NUnit.Framework;

// Sequential fixture execution for deterministic E2E test results.
// Previously LevelOfParallelism(4) caused flaky failures due to ML training + Blazor SignalR
// thread starvation under parallel load. All fixtures still run, just one at a time.
// Each fixture has its own PostgreSQL DB and Kestrel server — no isolation concerns.
[assembly: LevelOfParallelism(1)]
[assembly: Parallelizable(ParallelScope.Fixtures)]
