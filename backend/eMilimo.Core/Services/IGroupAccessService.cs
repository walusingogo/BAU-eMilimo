namespace eMilimo.Core.Services
{
    public interface IGroupAccessService
    {
        bool HasGlobalAccess();
        Task<List<Guid>> GetAccessibleGroupIdsAsync();
        Task<bool> CanAccessGroupAsync(Guid groupId);
        Task<List<Guid>> GetAccessibleSectionIdsAsync();
        Task<List<Guid>> GetAccessibleCommitteeIdsAsync();
        Task<bool> CanAccessEntityAsync(Guid? groupId, Guid? sectionId, Guid? committeeId);
    }
}
