using System;
using System.Collections.Generic;

namespace SWBF2Admin.Runtime.Permissions
{
    /*
     * According to SO, this was a good pattern for enum-like values that require
     * more complex data than just a name and number value. I dont know if we will actually need this,
     * but its very easy to turn this into a normal enum if we dont.
     */
    public class Permission
    {
        private static IDictionary<int, Permission> _permissions = new Dictionary<int, Permission>();
        private static IDictionary<string, int> _permissionNameToId = new Dictionary<string, int>();
        public int Id { get; }
        public string Name { get; }
        public IList<PermissionGroup> Groups { get; }
        

        public Permission(int id, string name, IList<PermissionGroup> groups)
        {
            this.Id = id;
            this.Name = name;

            if (groups != null)
            {
                this.Groups = groups;
            }

            _permissions[id] = this;
            _permissionNameToId[name] = id;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override bool Equals(object obj)
        {
            if ((obj as Permission) == null)
            {
                return false;
            }
            return ((Permission) obj).Id == this.Id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        public static bool operator==(Permission a, Permission b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(Permission a, Permission b)
        {
            return !object.Equals(a, b);
        }

        public static void InitPermissions(IDictionary<int, Permission> permissions)
        {
            _permissions = permissions;
        }

        public static IDictionary<int, Permission> GetPermissions()
        {
            return new Dictionary<int, Permission>(_permissions);
        }

        public static Permission FromId(int id)
        {
            return _permissions.TryGetValue(id, out Permission permission) ? permission : null;
        }

        public static Permission FromName(string name)
        {
            return _permissionNameToId.TryGetValue(name, out var id) ? FromId(id) : null;
        }
    }
}