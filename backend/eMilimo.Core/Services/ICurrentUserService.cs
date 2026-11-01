using System;

namespace eMilimo.Core.Services
{
    public interface ICurrentUserService
    {
        Guid? GetUserId();
        string? GetUserEmail();
        string? GetUserRole();
        bool IsAuthenticated();
    }
}

