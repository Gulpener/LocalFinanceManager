using NUnit.Framework;

// Disable test parallelization to prevent SQLite file access conflicts
// E2E tests use file-based SQLite databases which cannot be safely deleted
// while another test is potentially accessing them
[assembly: LevelOfParallelism(1)]
[assembly: Parallelizable(ParallelScope.None)]
