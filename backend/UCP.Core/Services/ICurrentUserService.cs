using System;

namespace UCP.Core.Services
{
    public interface ICurrentUserService
    {
        Guid? GetUserId();
        string? GetUserEmail();
        string? GetUserRole();
        bool IsAuthenticated();
    }
}

