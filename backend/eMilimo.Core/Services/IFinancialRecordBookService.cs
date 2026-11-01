using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;

namespace eMilimo.Core.Services;

public interface IFinancialRecordBookService
{
    Task<object> ListAsync(string? status, string? search, DateTime? from, DateTime? to, CancellationToken ct);
    Task<object?> GetAsync(Guid id, CancellationToken ct);
    Task<object> EligibleContributionsAsync(DateTime from, DateTime to, Guid? sectionId, Guid? contributionTypeId, string? search, CancellationToken ct);
    Task<object> EligibleServiceCollectionsAsync(DateTime from, DateTime to, Guid? sectionId, Guid? contributionTypeId, string? search, CancellationToken ct);
    Task<object> PreviewAsync(SaveFinancialRecordBookRequest request, CancellationToken ct);
    Task<object> CreateAsync(SaveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct);
    Task<object> UpdateAsync(Guid id, SaveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct);
    Task<object> ArchiveAsync(Guid id, ArchiveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct);
    Task<object> SubmitAsync(Guid id, Guid actorId, CancellationToken ct);
    Task<object> VerifyAsync(Guid id, FrbVerificationRequest request, Guid actorId, CancellationToken ct);
    Task<object> FinalizeAsync(Guid id, Guid actorId, CancellationToken ct);
}
