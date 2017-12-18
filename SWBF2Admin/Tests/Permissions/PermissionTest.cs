using System.Collections.Generic;
using NUnit.Framework;
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