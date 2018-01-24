/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
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

        public Permission(int id, string name, IList<PermissionGroup> groups = null)
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
            return ((Permission)obj).Id == this.Id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        public static bool operator ==(Permission a, Permission b)
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
            //TODO: messy quickfix to get basic permissions working -> generating new Permission object if perm not found

            return _permissionNameToId.TryGetValue(name, out var id) ? FromId(id) : new Permission(-1, name) /*null*/;
        }
    }
}