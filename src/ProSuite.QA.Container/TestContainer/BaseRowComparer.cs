using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.QA.Container.TestContainer
{
	public class BaseRowComparer : IEqualityComparer<BaseRow>
	{
		public bool Equals(BaseRow x, BaseRow y)
		{
			// Assumption :
			// x.Feature.Table = y.Feature.Table

			if (x == y)
			{
				return true;
			}

			if (x.OID != y.OID)
			{
				return false;
			}

			if (x.UniqueId != null && y.UniqueId != null)
			{
				return x.UniqueId.Id == y.UniqueId.Id;
			}

			if (x.Table != null && x.Table.HasOID)
			{
				return true;
			}

			if (! x.Extent.Equals(y.Extent))
			{
				return false;
			}

			return CompareObjectIds(x, y);
		}

		// compare integer fields, which include also ObjectID fields
		private static bool CompareObjectIds(BaseRow x, BaseRow y)
		{
			int oidCount = x.OidList.Count;

			if (oidCount != y.OidList.Count)
			{
				return false;
			}

			for (var oidIndex = 0; oidIndex < oidCount; oidIndex++)
			{
				if (x.OidList[oidIndex] != y.OidList[oidIndex])
				{
					return false;
				}
			}

			// --> all ObjectID fields are equal
			return true;
		}

		public int GetHashCode(BaseRow obj)
		{
			if (obj.UniqueId?.Id != null)
			{
				return obj.UniqueId.Id;
			}

			long oid = obj.OID;
			Assert.True(oid >= 0, "negative OID, but no assigned UniqueId");

			return oid.GetHashCode();
		}
	}
}
