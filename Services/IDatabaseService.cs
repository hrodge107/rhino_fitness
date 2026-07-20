using SQLite;

namespace FitnessApp.Services
{
    /// <summary>DB seam for SQLite connection and seed lifecycle.</summary>
    public interface IDatabaseService
    {
        /// <summary>The shared async SQLite connection used by repositories.</summary>
        SQLiteAsyncConnection Connection { get; }

        /// <summary>Triggered when the offline catalog seed completes.</summary>
        event EventHandler OnSeedCompleted;

        /// <summary>True if the seed has already completed.</summary>
        bool IsSeedComplete { get; }

        /// <summary>Creates schema and seeds catalog idempotently off-UI-thread.</summary>
        Task SeedAsync();
    }
}
