using System.Collections.Generic;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.Commons.AGP.Carto;

public class BasicFeatureLayerComparer : IEqualityComparer<BasicFeatureLayer>
{
	public bool Equals(BasicFeatureLayer x, BasicFeatureLayer y)
	{
		if (ReferenceEquals(x, y))
		{
			// both null or reference equal
			return true;
		}

		if (x == null || y == null)
		{
			return false;
		}

		var left = new GdbTableIdentity(x.GetTable());
		var right = new GdbTableIdentity(y.GetTable());

		return Equals(left, right);
	}

	public int GetHashCode(BasicFeatureLayer obj)
	{
		return new GdbTableIdentity(obj.GetTable()).GetHashCode();
	}
}
