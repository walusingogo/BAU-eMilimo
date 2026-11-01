namespace eMilimo.Core.Entities
{
    public class UserGroupAssignment
    {
        public Guid UserId { get; set; }
        public Guid GroupId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Group Group { get; set; } = null!;
    }
}
