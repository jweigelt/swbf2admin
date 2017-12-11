using System;
using System.Collections.Generic;
using System.Linq;
using SWBF2Admin.Utility;

namespace SWBF2Admin.Runtime.Permissions
{
    /*
     * According to SO, this was a good pattern for enum-like values that require
     * more complex data than just a name and number value. I dont know if we will actually need this,
     * but its very easy to turn this into a normal enum if we dont.
     */
    public sealed class Permission
    {
        private static readonly ISet<Permission> Permissions = new HashSet<Permission>();
        public int Value { get; }
        public string Name { get; }

        public static readonly Permission Kick = new Permission(1, "kick");
        public static readonly Permission Ban = new Permission(2, "ban");
        public static readonly Permission SetMap = new Permission(3, "map");

        private Permission(int value, string name)
        {
            this.Value = value;
            this.Name = name;

            Permissions.Add(this);
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
            return ((Permission) obj).Value == this.Value;
        }

        public override int GetHashCode()
        {
            return this.Value;
        }

        public static bool operator==(Permission a, Permission b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(Permission a, Permission b)
        {
            return !object.Equals(a, b);
        }
    }
}