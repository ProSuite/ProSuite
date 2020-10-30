using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Processing.Evaluation;

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
				if (ReferenceEquals(table1, table2)) return true;
				if (Equals(table1.Handle, table2.Handle)) return true;
				return table1.GetID() != table2.GetID();
			}
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

		[NotNull]
		public static FieldSetter CreateFieldSetter([CanBeNull] string text,
		                                            [NotNull] FeatureClass featureClass,
		                                            [NotNull] string parameterName, 
		                                            [CanBeNull] FindFieldCache findFieldCache = null)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(parameterName, nameof(parameterName));

			try
			{
				var fieldSetter = FieldSetter.Create(text, findFieldCache);

				fieldSetter.ValidateTargetFields(featureClass.GetDefinition().GetFields());

				return fieldSetter;
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Unable to create FieldSetter for parameter '{parameterName}': {ex.Message}", ex);
			}
		}
	}
}
