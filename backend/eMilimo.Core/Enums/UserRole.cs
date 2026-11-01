namespace eMilimo.Core.Enums
{
    public enum UserRole
    {
        SuperAdmin, //EXCLUSIVE RIGHTS
        Admin, //IT ADMIN
        Auditor, //AUDIT
        ChurchAuth, //CHURCH AUTHORITY
        SeniorRep, //ELDERS, CONVENER
        GroupRep //STEWARDS, GROUP REPRESENTATIVE, 
    }

    public static class AuthorityRoles
    {
        public static readonly string[] All =
        [
            nameof(UserRole.SuperAdmin), nameof(UserRole.Admin), nameof(UserRole.Auditor),
            nameof(UserRole.ChurchAuth), nameof(UserRole.SeniorRep), nameof(UserRole.GroupRep)
        ];

        public static readonly string[] DesignationAssignable =
        [
            nameof(UserRole.Admin), nameof(UserRole.Auditor), nameof(UserRole.ChurchAuth),
            nameof(UserRole.SeniorRep), nameof(UserRole.GroupRep)
        ];

        public static bool IsDefined(string? role) => All.Contains(role, StringComparer.OrdinalIgnoreCase);
        public static bool IsDesignationAssignable(string? role) => DesignationAssignable.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
