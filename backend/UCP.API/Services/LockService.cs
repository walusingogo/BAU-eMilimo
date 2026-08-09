using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities.Companions;
using UCP.Core.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Services
{
    public class LockService : ILockService
    {
        private readonly AppDbContext _context;

        public LockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AcquireLockAsync<T>(Guid entityId, Guid userId, int? durationSeconds = 300, string? reason = null) where T : class
        {
            var lockEntry = await FindLockAsync<T>(entityId);

            if (lockEntry != null && lockEntry.ExpiresAt.HasValue && lockEntry.ExpiresAt.Value > DateTime.UtcNow)
            {
                return false;
            }

            if (lockEntry == null)
            {
                var newLock = CreateLockInstance<T>();
                newLock.EntityId = entityId;
                newLock.LockedById = userId;
                newLock.LockedAt = DateTime.UtcNow;
                newLock.ExpiresAt = DateTime.UtcNow.AddSeconds(durationSeconds ?? 300);
                newLock.LockReason = reason;

                _context.Set<T>().Add((T)(object)newLock);
            }
            else
            {
                lockEntry.LockedById = userId;
                lockEntry.LockedAt = DateTime.UtcNow;
                lockEntry.ExpiresAt = DateTime.UtcNow.AddSeconds(durationSeconds ?? 300);
                lockEntry.LockReason = reason;
                _context.Set<T>().Update((T)(object)lockEntry);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ReleaseLockAsync<T>(Guid entityId, Guid userId) where T : class
        {
            var lockEntry = await FindLockAsync<T>(entityId);
            if (lockEntry == null)
                return;

            if (lockEntry.LockedById != userId)
                throw new UnauthorizedAccessException("Only the lock owner may release the lock.");

            _context.Set<T>().Remove((T)(object)lockEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsLockedAsync<T>(Guid entityId) where T : class
        {
            var lockEntry = await FindLockAsync<T>(entityId);
            return lockEntry != null && lockEntry.ExpiresAt.HasValue && lockEntry.ExpiresAt.Value > DateTime.UtcNow;
        }

        public async Task<Guid?> GetLockOwnerAsync<T>(Guid entityId) where T : class
        {
            var lockEntry = await FindLockAsync<T>(entityId);
            return lockEntry?.LockedById;
        }

        public async Task CleanExpiredLocksAsync()
        {
            var expiredRequisitionLocks = await _context.Set<RequisitionLck>()
                .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            var expiredUserLocks = await _context.Set<UserLck>()
                .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync();
            var expiredAssetLocks = await _context.Set<AssetLck>().Where(l=>l.ExpiresAt.HasValue&&l.ExpiresAt<=DateTime.UtcNow).ToListAsync();

            _context.RemoveRange(expiredRequisitionLocks);
            _context.RemoveRange(expiredUserLocks);
            _context.RemoveRange(expiredAssetLocks);
            await _context.SaveChangesAsync();
        }

        private async Task<LockCompanionBase?> FindLockAsync<T>(Guid entityId) where T : class
        {
            var items = await _context.Set<T>().ToListAsync();
            return items
                .Select(item => item as LockCompanionBase)
                .FirstOrDefault(lockEntry => lockEntry != null && lockEntry.EntityId == entityId);
        }

        private static LockCompanionBase CreateLockInstance<T>() where T : class
        {
            if (typeof(T) == typeof(RequisitionLck))
                return new RequisitionLck();

            if (typeof(T) == typeof(UserLck))
                return new UserLck();
            if (typeof(T) == typeof(AssetLck))
                return new AssetLck();

            throw new InvalidOperationException($"Unsupported lock type: {typeof(T).FullName}");
        }
    }
}
