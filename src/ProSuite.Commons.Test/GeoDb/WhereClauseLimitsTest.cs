using System;
using NUnit.Framework;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.Test.GeoDb
{
	[TestFixture]
	public class WhereClauseLimitsTest
	{
		[Test]
		public void OracleAllowsOneThousandValuesPerInList()
		{
			WhereClauseLimits.Get(WorkspaceDbType.ArcSDEOracle, out int length, out int count);

			Assert.AreEqual(60000, length);
			Assert.AreEqual(1000, count);
		}

		[Test]
		public void EveryDbTypeGetsLimits_UnknownOnesTheSafeOnes()
		{
			foreach (WorkspaceDbType dbType in Enum.GetValues(typeof(WorkspaceDbType)))
			{
				WhereClauseLimits.Get(dbType, out int length, out int count);

				Assert.GreaterOrEqual(length, WhereClauseLimits.SafeMaximumLength,
				                      dbType.ToString());
				Assert.GreaterOrEqual(count, WhereClauseLimits.SafeMaximumInValueCount,
				                      dbType.ToString());
			}

			WhereClauseLimits.Get(WorkspaceDbType.ArcSDE, out int genericLength, out _);
			Assert.AreEqual(WhereClauseLimits.SafeMaximumLength, genericLength);
		}
	}
}
