using System.Collections.Generic;

namespace SWBF2Admin.Runtime.Permissions
{
    public class PermissionGroup
    {
        public string name { get; }
        private ISet<Permission> Permissions { get; }
        public PermissionGroup(string name, ISet<Permission> permissions)
        {
            this.name = name;
            this.Permissions = permissions;
        }

        public bool HasPermission(Permission permission)
        {
            return this.Permissions.Contains(permission);
        }
    }
}