using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities.Companions
{
    public class UserLck : LockCompanionBase
    {
        public User? User { get; set; }
    }
}