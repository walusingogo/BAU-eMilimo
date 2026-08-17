// backend/UCP.Core/Services/IContributionService.cs
using UCP.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace UCP.Core.Services
{
    public interface IContributionService
    {
        // ============================================
        // MAIN CONTRIBUTION METHODS
        // ============================================

        Task<ContributionResponse> CreateContributionAsync(CreateContributionRequest request, Guid userId);
        
        Task<ContributionResponse> ConfirmContributionAsync(Guid contributionId, Guid userId);
        
        Task<(List<ContributionResponse> Data, int Total)> GetContributionsAsync(UCP.Core.DTOs.ContributionFilter filter);
        
        Task<ContributionResponse?> GetContributionByIdAsync(Guid id);
        
        Task<ContributionDashboardStats> GetDashboardStatsAsync(string period);

        // ============================================
        // UPDATE, REJECT, SUBMIT, ARCHIVE METHODS
        // ============================================

        Task<ContributionResponse> UpdateContributionAsync(Guid contributionId, UpdateContributionDto dto, Guid userId);
        Task<ContributionResponse> RejectContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId);
        Task<ContributionResponse> ReturnContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId);
        Task<ContributionResponse> SubmitContributionAsync(Guid contributionId, Guid userId);
        Task ArchiveContributionAsync(Guid contributionId, ArchiveContributionDto dto, Guid userId);

        // ============================================
        // DROPDOWN DATA METHODS
        // ============================================

        Task<List<ContributionTypeResponse>> GetContributionTypesAsync();
        Task<List<ContributionSourceResponse>> GetContributionSourcesAsync();
        Task<List<BankAccountResponse>> GetBankAccountsAsync();

        // ============================================
        // ATTACHMENT METHODS
        // ============================================

        Task<List<ContributionAttachmentResponse>> UploadAttachmentsAsync(
            Guid contributionId, 
            List<IFormFile> files, 
            Guid userId);

        Task<List<ContributionAttachmentResponse>> GetAttachmentsAsync(Guid contributionId);

        Task DeleteAttachmentAsync(Guid contributionId, Guid attachmentId, Guid userId);

        // ============================================
        // NEW: GET NEXT CONTRIBUTION NUMBER
        // ============================================

        /// <summary>
        /// Returns the next contribution number in the sequence (e.g. CONT-2026-000123)
        /// </summary>
        Task<string> GetNextContributionNumberAsync();
    }
}
