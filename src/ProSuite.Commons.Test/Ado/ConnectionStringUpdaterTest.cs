using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Ado;

namespace ProSuite.Commons.Test.Ado
{
	[TestFixture]
	public class ConnectionStringUpdaterTest
	{
		[Test]
		public void CanUpdate()
		{
			var updater = new ConnectionStringBuilder("User Id=blah; Password=blah2");

			updater.Update("password", "blah3");

			Assert.AreEqual("user id=blah;password=blah3", updater.ConnectionString);
		}

		[Test]
		public void CanGetAndUpdateItems()
		{
			var updater = new ConnectionStringBuilder("User Id=blah; Password=blah2");

			foreach (KeyValuePair<string, string> entry in updater.GetEntries())
			{
				updater.Update(entry.Key, entry.Value.ToUpper());
			}

			Assert.AreEqual("user id=BLAH;password=BLAH2", updater.ConnectionString);
		}

		[Test]
		public void CanAdd()
		{
			var updater = new ConnectionStringBuilder("User Id=blah; Password=blah2");

			updater.Add("something", "blah3");

			Assert.AreEqual("user id=blah;password=blah2;something=blah3",
			                updater.ConnectionString);
		}

		[Test]
		public void CanAddToEmpty()
		{
			var updater = new ConnectionStringBuilder();

			updater.Add("something", "blah3");

			Assert.AreEqual("something=blah3", updater.ConnectionString);
		}

		[Test]
		public void CanDelete()
		{
			var updater = new ConnectionStringBuilder("User Id=blah; Password=blah2");

			updater.Remove("Password");

			Assert.AreEqual("user id=blah", updater.ConnectionString);
		}
	}
}