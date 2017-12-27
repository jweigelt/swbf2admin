using System.Collections.Generic;
using System.Linq;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Permissions
{
    public class PermissionUser
    {
        private Player _player;
        private readonly ISet<PermissionGroup> _groups = new HashSet<PermissionGroup>();
        private readonly ISet<Permission> _permissions = new HashSet<Permission>();
        
        public PermissionUser(Player player)
        {
            this._player = player;
        }

        public bool HasPermission(Permission permission)
        {
            return this._permissions.Contains(permission) || this._groups.Any(group => group.HasPermission(permission));
        }

        public void AddPermission(Permission permission)
        {
            this._permissions.Add(permission);
        }

        public void RemovePermission(Permission permission)
        {
            this._permissions.Remove(permission);
        }

        public void AddPermissionGroup(PermissionGroup group)
        {
            this._groups.Add(group);
        }

        public void RemovePermissionGroup(PermissionGroup group)
        {
            this._groups.Remove(group);
        }
    }
}