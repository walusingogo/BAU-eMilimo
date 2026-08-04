namespace UCP.Core.Entities
{
    public class UserCommitteeAssignment
    {
        public Guid UserId { get; set; }
        public Guid CommitteeId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Committee Committee { get; set; } = null!;
    }
}
