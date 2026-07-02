using SQLite;

namespace FitnessApp.Services
{
    /// <summary>
    /// Infrastructure seam over the embedded SQLite store. Owns the single
    /// <see cref="SQLiteAsyncConnection"/> and the idempotent seed pipeline
    /// (packaged JSON → SQLite). ViewModels and repositories depend on this
    /// abstraction, never on a concrete connection (DIP / offline-data-standards).
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>The shared async SQLite connection used by repositories.</summary>
        SQLiteAsyncConnection Connection { get; }

        /// <summary>Triggered when the offline catalog seed completes.</summary>
        event EventHandler OnSeedCompleted;

        /// <summary>True if the seed has already completed.</summary>
        bool IsSeedComplete { get; }

        /// <summary>
        /// Creates the schema and bulk-seeds the exercise catalog from the
        /// packaged <c>exercises.json</c> asset. Idempotent — a row-count guard
        /// prevents duplicate inserts on re-runs. Runs off the UI thread.
        /// </summary>
        Task SeedAsync();
    }
}
