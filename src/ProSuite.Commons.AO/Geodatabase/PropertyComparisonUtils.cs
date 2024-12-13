using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class PropertyComparisonUtils
	{
		public static bool Equals([CanBeNull] UID uid1, [CanBeNull] UID uid2)
		{
			if (uid1 == null && uid2 == null)
			{
				return true;
			}

			if (uid1 == null || uid2 == null)
			{
				return false;
			}

			return uid1.Compare(uid2);
		}

		public static bool Equals(double? value1, double? value2)
		{
			if ((value1 == null) != (value2 == null))
			{
				return false;
			}

			if (value1 == null)
			{
				return true;
			}

			return MathUtils.AreSignificantDigitsEqual(value1.Value, value2.Value);
		}
	}
}
