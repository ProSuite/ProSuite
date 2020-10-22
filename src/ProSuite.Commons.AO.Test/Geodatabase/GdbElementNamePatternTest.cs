using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class GdbElementNamePatternTest
	{
		[Test]
		public void CanMatchFixedTablePattern()
		{
			var pattern = new GdbElementNamePattern("TABLE");

			Assert.IsTrue(pattern.Matches("table", "owner"));
			Assert.IsTrue(pattern.Matches("TABLE", "owner"));
			Assert.IsTrue(pattern.Matches("TABLE", "owner", "database"));
			Assert.IsFalse(pattern.Matches("TABLE2", "owner"));
		}

		[Test]
		public void CanMatchFixedOwnerTablePattern()
		{
			var pattern = new GdbElementNamePattern("owner.TABLE");

			Assert.IsTrue(pattern.Matches("table", "owner"));
			Assert.IsTrue(pattern.Matches("TABLE", "owner"));
			Assert.IsTrue(pattern.Matches("TABLE", "owner", "database"));
			Assert.IsFalse(pattern.Matches("TABLE2", "owner"));
		}

		[Test]
		public void CanMatchWildcardTableNamePattern()
		{
			var pattern = new GdbElementNamePattern("owner.prefix_*");

			Assert.IsFalse(pattern.Matches("table", "owner"));
			Assert.IsTrue(pattern.Matches("prefix_table", "owner"));
			Assert.IsTrue(pattern.Matches("prefix_table", "owner", "database"));
		}

		[Test]
		public void CanMatchWildcardOwnerTableNamePattern()
		{
			var pattern = new GdbElementNamePattern("*.prefix_*");

			Assert.IsFalse(pattern.Matches("table", "owner1"));
			Assert.IsTrue(pattern.Matches("prefix_table", "owner2"));
			Assert.IsTrue(pattern.Matches("prefix_table", "owner2", "database"));
		}

		[Test]
		public void CanMatchAny()
		{
			var pattern = new GdbElementNamePattern("*");

			Assert.IsTrue(pattern.Matches(string.Empty));
			Assert.IsTrue(pattern.Matches("table"));
			Assert.IsTrue(pattern.Matches("table", "owner"));
		}

		[Test]
		public void CantMatchMoreQualifiedPattern()
		{
			var pattern = new GdbElementNamePattern("database.owner.table");

			Assert.IsFalse(pattern.Matches("table"));
			Assert.IsFalse(pattern.Matches("table", "owner"));
			Assert.IsTrue(pattern.Matches("table", "owner", "database"));
		}

		[Test]
		public void CanMatchMoreQualifiedAnyPattern()
		{
			var pattern = new GdbElementNamePattern("*.owner.table");

			Assert.IsFalse(pattern.Matches("table"));
			Assert.IsTrue(pattern.Matches("table", "owner"));
			Assert.IsTrue(pattern.Matches("table", "owner", "database"));
		}

		[Test]
		public void CanMatchWildcardOwnerTableNamePattern2()
		{
			var pattern = new GdbElementNamePattern("owner*.topo_*");

			Assert.IsFalse(pattern.Matches("other.topo_1")); // other owner
			Assert.IsFalse(pattern.Matches("topo_1")); // no owner, does not match "owner*"
			Assert.IsTrue(pattern.Matches("topo_1", "owner1"));
			Assert.IsTrue(pattern.Matches("topo_2", "owner2", "database"));
		}
	}
}
