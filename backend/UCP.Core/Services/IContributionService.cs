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
        // UPDATE, REJECT, ARCHIVE METHODS
        // ============================================

        /// <summary>
        /// Update a contribution (only allowed in DRAFT status)
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <param name="dto">Update data</param>
        /// <param name="userId">ID of the user updating</param>
        /// <returns>Updated contribution response</returns>
        Task<ContributionResponse> UpdateContributionAsync(Guid contributionId, UpdateContributionDto dto, Guid userId);

        /// <summary>
        /// Reject a contribution (only allowed in PENDING status)
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <param name="dto">Rejection reason</param>
        /// <param name="userId">ID of the user rejecting</param>
        /// <returns>Rejected contribution response</returns>
        Task<ContributionResponse> RejectContributionAsync(Guid contributionId, RejectContributionDto dto, Guid userId);

        /// <summary>
        /// Archive a contribution (only allowed in DRAFT status)
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <param name="dto">Archive reason</param>
        /// <param name="userId">ID of the user archiving</param>
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

        /// <summary>
        /// Upload attachments for a contribution
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <param name="files">List of files to upload</param>
        /// <param name="userId">ID of the user uploading the files</param>
        /// <returns>List of uploaded attachment responses</returns>
        Task<List<ContributionAttachmentResponse>> UploadAttachmentsAsync(
            Guid contributionId, 
            List<IFormFile> files, 
            Guid userId);

        /// <summary>
        /// Get all attachments for a contribution
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <returns>List of attachment responses</returns>
        Task<List<ContributionAttachmentResponse>> GetAttachmentsAsync(Guid contributionId);

        /// <summary>
        /// Delete a specific attachment
        /// </summary>
        /// <param name="contributionId">The ID of the contribution</param>
        /// <param name="attachmentId">The ID of the attachment to delete</param>
        /// <param name="userId">ID of the user deleting the attachment</param>
        Task DeleteAttachmentAsync(Guid contributionId, Guid attachmentId, Guid userId);
    }
}