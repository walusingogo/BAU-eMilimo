using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    public class Committee : BaseEntity
    {
        public string CommitteeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid? ParentSectionId { get; set; }
        public Guid? ParentGroupId { get; set; }
        public Section? ParentSection { get; set; }
        public Group? ParentGroup { get; set; }
        public ICollection<UserCommitteeAssignment> UserAssignments { get; set; } = new List<UserCommitteeAssignment>();
    }
}
