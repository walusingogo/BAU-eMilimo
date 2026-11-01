namespace eMilimo.Core.Services;

public interface IReferenceNumberService
{
    Task<string> NextAsync(string referenceType, DateOnly? date = null, CancellationToken cancellationToken = default);
}
