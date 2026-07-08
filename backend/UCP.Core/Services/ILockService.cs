using System;
using System.Threading.Tasks;

namespace UCP.Core.Services
{
    public interface ILockService
    {
        /// <summary>
        /// Acquires a lock for an entity. Returns true if successful.
        /// </summary>
        Task<bool> AcquireLockAsync<T>(Guid entityId, Guid userId, int? durationSeconds = 300, string? reason = null) where T : class;

        /// <summary>
        /// Releases a lock (only if the current user holds it).
        /// </summary>
        Task ReleaseLockAsync<T>(Guid entityId, Guid userId) where T : class;

        /// <summary>
        /// Checks if a record is locked by anyone.
        /// </summary>
        Task<bool> IsLockedAsync<T>(Guid entityId) where T : class;

        /// <summary>
        /// Gets the current lock owner (returns null if not locked).
        /// </summary>
        Task<Guid?> GetLockOwnerAsync<T>(Guid entityId) where T : class;

        /// <summary>
        /// Forcefully removes expired locks.
        /// </summary>
        Task CleanExpiredLocksAsync();
    }
}