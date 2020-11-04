using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ObjectClassComparer : IEqualityComparer<IObjectClass>
	{
		private readonly ObjectClassEquality _classEquality;

		public ObjectClassComparer() : this(ObjectClassEquality.SameTableSameVersion) { }

		public ObjectClassComparer(ObjectClassEquality classEquality)
		{
			_classEquality = classEquality;
		}

		#region Implementation of IEqualityComparer<IFeatureClass>

		[CLSCompliant(false)]
		public bool Equals(IObjectClass x, IObjectClass y)
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

			return DatasetUtils.IsSameObjectClass(x, y, _classEquality);
		}

		[CLSCompliant(false)]
		public int GetHashCode(IObjectClass featureClass)
		{
			return featureClass.GetHashCode();
		}

		#endregion
	}
}
