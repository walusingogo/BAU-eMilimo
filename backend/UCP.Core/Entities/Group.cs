using System;
using System.Collections.Generic;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    public class Group : BaseEntity
    {
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GroupType { get; set; } = "ChurchGroup";
        public bool IsActive { get; set; } = true;

        // Foreign Keys
        public Guid? LeaderId { get; set; }
        public Guid? ParentGroupId { get; set; }

        // Navigation
        public User? Leader { get; set; }
        public Group? ParentGroup { get; set; }

        // Collections
        public ICollection<User>? Users { get; set; }
        public ICollection<UserGroupAssignment> UserAssignments { get; set; } = new List<UserGroupAssignment>();
        public ICollection<Group>? ChildGroups { get; set; }
        public ICollection<Requisition>? Requisitions { get; set; }
    }
}
