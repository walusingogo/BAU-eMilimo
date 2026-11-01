using System;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    public class User : BaseEntity
    {
        // Personal Information
        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty; // Male/Female
        public string? ProfilePhoto { get; set; } // base64 data URL
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string MembershipId { get; set; } = string.Empty;

        // Authentication
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? PasswordExpiryDate { get; set; }
        public bool PasswordExpired { get; set; } = false;
        public int PasswordAttempts { get; set; } = 0;
        public DateTime? PasswordLockDate { get; set; }
        public bool Locked { get; set; } = false;

        // Role & Status
        public string Role { get; set; } = "GroupRep";
        public string Status { get; set; } = "PENDING"; // PENDING, ACTIVE, INACTIVE, LOCKED

        // Foreign Keys
        public Guid? SectionId { get; set; }
        public Guid? GroupId { get; set; }

        // Signature
        public string SignatureData { get; set; } = string.Empty;

        // Navigation properties (for EF Core)
        public Section? Section { get; set; }
        public Group? Group { get; set; }
        public ICollection<UserGroupAssignment> GroupAssignments { get; set; } = new List<UserGroupAssignment>();
        public ICollection<UserCommitteeAssignment> CommitteeAssignments { get; set; } = new List<UserCommitteeAssignment>();
        public ICollection<OfficeAppointment> OfficeAppointments { get; set; } = new List<OfficeAppointment>();
        public string? CurrentSessionId { get; set; }
        // Self-referencing navigation properties for CreatedBy/UpdatedBy
        public User? CreatedBy { get; set; }
        public User? UpdatedBy { get; set; }
    }
}
