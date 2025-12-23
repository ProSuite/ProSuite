using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Spatial
{
	/// <summary>
	/// Utility methods for spatial references.
	/// </summary>
	public static class SpatialReferenceUtils
	{
		/// <summary>
		/// Returns a value indicating if two spatial references are equal with regard to
		/// their factory codes and optionally also the spatial domain properties and 
		/// vertical coordinate systems.
		/// </summary>
		/// <param name="sref1">The first spatial reference.</param>
		/// <param name="sref2">The second spatial reference.</param>
		/// <param name="comparePrecisionAndTolerance">Indicates if precision and tolerance 
		/// values should be compared also.</param>
		/// <param name="compareVerticalCoordinateSystems">Indicates if the vertical coordinate 
		/// systems of the spatial references should be compared also.</param>
		/// <returns><c>true</c> if the spatial references are equal, <c>false</c> otherwise.</returns>
		/// <remarks>M precision/tolerance not yet properly dealt with</remarks>
		public static bool AreEqual([CanBeNull] SpatialReference sref1,
		                            [CanBeNull] SpatialReference sref2,
		                            bool comparePrecisionAndTolerance,
		                            bool compareVerticalCoordinateSystems)
		{
			// TODO add support for comparing M settings

			if (sref1 == null && sref2 == null)
			{
				// both null -> equal
				return true;
			}

			if (sref1 == null || sref2 == null)
			{
				// null / not null combination -> not equal
				return false;
			}

			if (ReferenceEquals(sref1, sref2))
			{
				// same instance -> equal
				return true;
			}

			if (sref1.Wkid != sref2.Wkid)
			{
				// factory code different -> not equal
				return false;
			}

			if (compareVerticalCoordinateSystems)
			{
				if (sref1.HasVcs != sref2.HasVcs)
				{
					return false;
				}

				if (sref1.VcsWkid <= 0 || sref2.VcsWkid <= 0)
				{
					if (sref1.VcsWkt != sref2.VcsWkt)
					{
						return false;
					}
				}
				else if (sref1.VcsWkid != sref2.VcsWkid)
				{
					return false;
				}
			}

			if (comparePrecisionAndTolerance)
			{
				// compare precision first
				bool compareOnlyXYPrecision = ! compareVerticalCoordinateSystems;

				double sref1Resolution = sref1.XYResolution;
				double sref2Resolution = sref2.XYResolution;

				const double epsilon = 1E-17;
				if (! MathUtils.AreEqual(sref1Resolution, sref2Resolution, epsilon))
				{
					return false;
				}

				// if precision equal, compare relevant tolerances also
				if (! MathUtils.AreEqual(sref1.XYTolerance, sref2.XYTolerance, epsilon))
				{
					return false;
				}

				if (! compareVerticalCoordinateSystems ||
				    ! sref1.HasVcs && ! sref2.HasVcs)
				{
					return true;
				}

				double sr1ResolutionZ = get_ZResolution(sref1);
				double sr2ResolutionZ = get_ZResolution(sref2);

				if (! MathUtils.AreEqual(sr1ResolutionZ, sr2ResolutionZ, epsilon))
				{
					return false;
				}

				if (! MathUtils.AreEqual(sref1.ZTolerance, sref2.ZTolerance, epsilon))
				{
					return false;
				}
			}

			return true;
		}

		private static double get_ZResolution([NotNull] SpatialReference sref)
		{
			if (sref.ZScale > 0)
			{
				return 1 / sref.ZScale;
			}

			return double.NaN;
		}
	}
}
