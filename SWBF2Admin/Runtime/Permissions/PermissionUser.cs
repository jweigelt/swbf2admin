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