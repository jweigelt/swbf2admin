using System.Collections.Generic;

namespace SWBF2Admin.Runtime.Permissions
{
    class PermissionGroup
    {
        public string name { get; }
        private ISet<Permission> permissions { get; }
        public PermissionGroup(string name, ISet<Permission> permissions)
        {
            this.name = name;
            this.permissions = permissions;
        }

        public bool HasPermission(Permission permission)
        {
            return this.permissions.Contains(permission);
        }
    }
}