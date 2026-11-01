using System;
using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    /// <summary>
    /// One row per role, holding the JSON array of permission keys granted to
    /// that role. Lives in its own table rather than the generic SystemConfig
    /// key-value store, since role permissions are structured, security-
    /// sensitive data that deserves its own audit trail and constraints
    /// (unique per role) rather than sharing a table with arbitrary app
    /// settings.
    /// </summary>
    public class RolePermission : BaseEntity
    {
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty;

        /// <summary>JSON-encoded string array of permission keys, e.g. ["requisitions.view", ...].</summary>
        public string PermissionsJson { get; set; } = "[]";
    }
}