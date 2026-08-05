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
        
        // Use the DTO version - fully qualified to avoid ambiguity
        Task<(List<ContributionResponse> Data, int Total)> GetContributionsAsync(UCP.Core.DTOs.ContributionFilter filter);
        
        Task<ContributionResponse?> GetContributionByIdAsync(Guid id);
        
        Task<ContributionDashboardStats> GetDashboardStatsAsync(string period);

        // ============================================
        // UPDATE, REJECT, SUBMIT, ARCHIVE METHODS
        // ============================================

        /// <summary>
        /// Update a contribution (only allowed in DRAFT status)
        /// </summary>
        Task<ContributionResponse> UpdateContributionAsync(Guid contributionId, UpdateContributionDto dto, Guid userId);

        /// <summary>
        /// Reject a contribution (only allowed in PENDING status)
        /// </summary>
        Task<ContributionResponse> RejectContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId);

        /// <summary>
        /// Submit a draft contribution for confirmation (changes status from DRAFT to PENDING)
        /// </summary>
        Task<ContributionResponse> SubmitContributionAsync(Guid contributionId, Guid userId);

        /// <summary>
        /// Archive a contribution (only allowed in DRAFT status)
        /// </summary>
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
    }
}