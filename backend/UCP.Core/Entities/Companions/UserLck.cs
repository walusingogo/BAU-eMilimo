using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    public class UserLck : LockCompanionBase
    {
        public User? User { get; set; }
    }
}