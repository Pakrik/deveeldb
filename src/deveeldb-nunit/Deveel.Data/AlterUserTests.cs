﻿using System;
using System.Linq;

using Deveel.Data.Security;

using NUnit.Framework;

namespace Deveel.Data {
	[TestFixture]
	public sealed class AlterUserTests : ContextBasedTest {
		protected override void OnSetUp(string testName) {
			Query.Access.CreateUser("test_user", "0123456789");
			Query.Access.CreateRole("test_role1");
			Query.Access.CreateRole("role2");

			if (testName == "Unlock")
				Query.Access.SetUserStatus("test_user", UserStatus.Locked);
		}

		[Test]
		public void SetPassword() {
			Query.SetPassword("test_user", "1234");

			var authenticated = Query.Access.Authenticate("test_user", "1234");
			Assert.IsTrue(authenticated);
		}

		[Test]
		public void SetRoles() {
			Query.SetRoles("test_user", "test_role1", "role2");

			var userRoles = Query.Access.GetUserRoles("test_user");

			Assert.IsNotNull(userRoles);
			Assert.IsNotEmpty(userRoles);

			var roleNames = userRoles.Select(x => x.Name).ToArray();
			Assert.Contains("test_role1", roleNames);
			Assert.Contains("role2", roleNames);
		}

		[Test]
		public void Unlock() {
			Query.SetAccountUnlocked("test_user");

			var newStatus = Query.Access.GetUserStatus("test_user");

			Assert.AreEqual(UserStatus.Unlocked, newStatus);
		}

		[Test]
		public void Lock() {
			Query.SetAccountLocked("test_user");

			var newStatus = Query.Access.GetUserStatus("test_user");

			Assert.AreEqual(UserStatus.Locked, newStatus);
		}
	}
}