using System.Collections.Generic;

namespace ProSuite.QA.Tests.EdgeMatch
{
	internal class FeatureKeyComparer : IEqualityComparer<FeatureKey>
	{
		public bool Equals(FeatureKey x, FeatureKey y)
		{
			return x.ObjectId == y.ObjectId && x.TableIndex == y.TableIndex;
		}

		public int GetHashCode(FeatureKey obj)
		{
			return obj.ObjectId.GetHashCode() ^ 37 * obj.TableIndex.GetHashCode();
		}
	}
}
