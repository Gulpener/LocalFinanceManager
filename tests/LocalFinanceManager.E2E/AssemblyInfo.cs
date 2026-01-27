using NUnit.Framework;

// Enable test parallelization with worker-based isolation
// Each worker gets its own SQLite database file and web server instance
// Workers can run in parallel safely without file conflicts
[assembly: LevelOfParallelism(4)]
[assembly: Parallelizable(ParallelScope.Fixtures)]
