using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    public class Committee : BaseEntity
    {
        public string CommitteeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<UserCommitteeAssignment> UserAssignments { get; set; } = new List<UserCommitteeAssignment>();
    }
}
