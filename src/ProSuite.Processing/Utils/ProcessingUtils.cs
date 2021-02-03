using System;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// A container for static utility methods for running carto processes.
	/// </summary>
	public static class ProcessingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Create a string from the given object. Format:
		/// &quot;OID=123 Class=AliasNameOrDatasetName&quot;
		/// </summary>
		public static string Format([NotNull] Feature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			var oid = feature.GetObjectID();
			string className;

			using (var table = feature.GetTable())
			{
				className = table?.GetName() ?? "UnknownTable";
			}

			return FormattableString.Invariant($"OID={oid} Class={className}");
		}

		/// <returns>True iff the two features are the same</returns>
		/// <remarks>
		/// This is a cheap test, but it assumes that both features are from
		/// the <b>same workspace</b>.  If the two features are from different workspaces,
		/// this method <em>may</em> return true even though the features are different!
		/// </remarks>
		public static bool IsSameFeature(Feature feature1, Feature feature2)
		{
			if (ReferenceEquals(feature1, feature2)) return true;
			if (Equals(feature1.Handle, feature2.Handle)) return true;

			var oid1 = feature1.GetObjectID();
			var oid2 = feature2.GetObjectID();
			if (oid1 != oid2) return false;

			using (var table1 = feature1.GetTable())
			using (var table2 = feature2.GetTable())
			{
				return IsSameTable(table1, table2);
			}
		}

		public static bool IsSameTable(Table fc1, Table fc2)
		{
			if (ReferenceEquals(fc1, fc2)) return true;
			if (Equals(fc1.Handle, fc2.Handle)) return true;

			var id1 = fc1.GetID();
			var id2 = fc2.GetID();
			if (id1 != id2) return false;
			if (id1 >= 0) return true;

			// table id is negative for tables not registered with the Geodatabase
			// compare table name and workspace -- for now, give up and assume not same

			return false;
		}

		public static double Clip(double value, double min, double max,
		                          [CanBeNull] string parameter = null)
		{
			if (value < min)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, max);
				}

				return max;
			}

			return value;
		}

		public static int Clip(int value, int min, int max,
		                       [CanBeNull] string parameter = null)
		{
			if (value < min)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, min);
				}

				return min;
			}

			if (value > max)
			{
				if (! string.IsNullOrEmpty(parameter))
				{
					_msg.WarnFormat("{0} was {1}, clipped to {2}", parameter, value, max);
				}

				return max;
			}

			return value;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in degrees)
		/// so that it is in the range 0 (inclusive) to 360 (exclusive).
		/// </summary>
		/// <param name="angle">in degrees</param>
		/// <returns>angle, in degrees, normalized to 0..360</returns>
		public static double ToPositiveDegrees(double angle)
		{
			angle %= 360;

			if (angle < 0)
			{
				angle += 360;
			}

			return angle;
		}

		/// <summary>
		/// Normalize the given <paramref name="angle"/> (in radians)
		/// so that it is in the range -pi to pi (both inclusive).
		/// </summary>
		/// <param name="angle">in radians</param>
		/// <returns>angle, in radians, normalized to -pi..pi</returns>
		public static double NormalizeRadians(double angle)
		{
			const double twoPi = Math.PI * 2;

			angle %= twoPi; // -2pi .. 2pi

			if (angle > Math.PI)
			{
				angle -= twoPi;
			}
			else if (angle < -Math.PI)
			{
				angle += twoPi;
			}

			return angle; // -pi .. pi
		}

		[NotNull]
		public static QueryFilter CreateFilter(string whereClause, Geometry extent)
		{
			QueryFilter filter;

			if (extent != null)
			{
				filter = new SpatialQueryFilter
				         {
					         FilterGeometry = extent,
					         SpatialRelationship = SpatialRelationship.Intersects
				         };
			}
			else
			{
				filter = new QueryFilter();
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			return filter;
		}

		[NotNull]
		public static FieldSetter CreateFieldSetter([CanBeNull] string text,
		                                            [NotNull] FeatureClass featureClass,
		                                            [NotNull] string parameterName)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(parameterName, nameof(parameterName));

			try
			{
				var fieldSetter = FieldSetter.Create(text);

				var fieldNames = featureClass.GetDefinition().GetFields().Select(f => f.Name);

				fieldSetter.ValidateTargetFields(fieldNames);

				return fieldSetter;
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Unable to create FieldSetter for parameter '{parameterName}': {ex.Message}", ex);
			}
		}

		/// <summary>
		/// Return true iff <paramref name="shape"/> is within
		/// <paramref name="perimeter"/>; if <paramref name="perimeter"/> is
		/// <c>null</c> the <paramref name="shape"/> is considered within.
		/// </summary>
		// TODO Shouldn't this be on processing context?
		public static bool WithinPerimeter(Geometry shape, [CanBeNull] Geometry perimeter)
		{
			if (shape == null)
			{
				return false;
			}

			if (perimeter == null)
			{
				return true;
			}

			return GeometryEngine.Instance.Contains(perimeter, shape);
		}
	}
}
