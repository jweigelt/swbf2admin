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
using SWBF2Admin.Runtime.Permissions;

// Im new to C#, just trying to make sure I understand how this works
namespace SWBF2Admin.Tests.Permissions
{
    // TODO Redo these tests eventually
    /*
    [TestFixture]
    public class PermissionTest
    {
        [Test]
        public void Names()
        {
            Assert.AreEqual(Permission.Kick.Name, "kick");
            Assert.AreEqual(Permission.Kick.Value, 1);
            Assert.AreEqual(Permission.Kick.ToString(), Permission.Kick.Name);
        }

        [Test]
        public void Equality()
        {
            object a = Permission.Kick;
            object b = Permission.Kick;
            object c = Permission.Ban;
            Assert.True(a == b);
            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, 1));
            Assert.False(a == c);
            Assert.False(object.Equals(a, c));
        }

        [Test]
        public void UserPermissions()
        {
            var user = new PermissionUser(null);
            user.AddPermission(Permission.Kick);
            
            var banGroup = new PermissionGroup("ban", new HashSet<Permission> {Permission.Ban});
            user.AddPermissionGroup(banGroup);
            
            Assert.True(user.HasPermission(Permission.Ban));
            Assert.True(user.HasPermission(Permission.Kick));
            Assert.False(user.HasPermission(Permission.SetMap));

            user.AddPermission(Permission.SetMap);
            Assert.True(user.HasPermission(Permission.SetMap));

            user.RemovePermission(Permission.SetMap);
            Assert.False(user.HasPermission(Permission.SetMap));
            
            var mapGroup = new PermissionGroup("map", new HashSet<Permission>{Permission.SetMap});
            user.AddPermissionGroup(mapGroup);
            Assert.True(user.HasPermission(Permission.SetMap));
            user.RemovePermissionGroup(mapGroup);
            Assert.False(user.HasPermission(Permission.SetMap));
        }
    }
    */
}