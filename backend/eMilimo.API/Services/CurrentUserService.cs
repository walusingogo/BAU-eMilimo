using BAU.Platform.Identity;
using eMilimo.Core.Services;

namespace eMilimo.API.Services
{
    public sealed class CurrentUserService(ICurrentActor actor) : ICurrentUserService
    {
        public Guid? GetUserId() => actor.UserId;
        public string? GetUserEmail() => actor.Email;
        public string? GetUserRole() => actor.Roles.FirstOrDefault();
        public bool IsAuthenticated() => actor.IsAuthenticated;
    }
}
