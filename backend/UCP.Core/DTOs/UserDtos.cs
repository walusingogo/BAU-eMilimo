using System;

namespace UCP.Core.DTOs
{
    public class CreateUserDto
    {
        public string Title { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid? SectionId { get; set; }
        public Guid? GroupId { get; set; }
        public List<Guid> GroupIds { get; set; } = new();
        public List<Guid> CommitteeIds { get; set; } = new();
    }

    public class UpdateUserDto
    {
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? GroupId { get; set; }
        public List<Guid>? GroupIds { get; set; }
        public List<Guid>? CommitteeIds { get; set; }
    }

    public class UserFilterDto
    {
        public string? Role { get; set; }
        public string? Status { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? GroupId { get; set; }
        public string? Search { get; set; }
    }
}
