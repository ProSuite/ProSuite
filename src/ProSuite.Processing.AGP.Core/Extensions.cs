using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Exceptions;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.AGP.Core
{
	public static class Extensions
	{
		#region FieldSetter & Co.

		public static FieldSetter ValidateTargetFields(
			this FieldSetter instance, FeatureClass featureClass, string parameterName)
		{
			if (instance is null) return null;
			if (featureClass is null) return instance;

			try
			{
				using var definition = featureClass.GetDefinition();
				var fields = definition.GetFields();
				return instance.ValidateTargetFields(fields.Select(f => f.Name));
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Parameter {parameterName} is invalid: {ex.Message}");
			}
		}

		public static StandardEnvironment DefineFields(
			this StandardEnvironment env, Row row, string qualifier = null)
		{
			if (env is null)
				throw new ArgumentNullException(nameof(env));
			return env.DefineFields(row.RowValues(), qualifier);
		}

		public static StandardEnvironment DefineFields(
			this StandardEnvironment env, RowBuffer row, string qualifier = null)
		{
			if (env is null)
				throw new ArgumentNullException(nameof(env));
			return env.DefineFields(row.RowValues(), qualifier);
		}

		public static void Execute(
			this FieldSetter instance, Row row, IEvaluationEnvironment env, Stack<object> stack = null)
		{
			if (instance is null)
				throw new ArgumentNullException(nameof(instance));
			instance.Execute(row.RowValues(), env, stack);
		}

		public static void Execute(
			this FieldSetter instance, RowBuffer row, IEvaluationEnvironment env, Stack<object> stack = null)
		{
			if (instance is null)
				throw new ArgumentNullException(nameof(instance));
			instance.Execute(row.RowValues(), env, stack);
		}

		#endregion

		/// <summary>
		/// Get an array of the point ID values of the given curve.
		/// Useful for testing; avoid in production code.
		/// </summary>
		public static int[] GetPointIDs(this Multipart curve)
		{
			if (curve == null)
				return Array.Empty<int>();
			int count = curve.PointCount;
			var points = curve.Points;
			var ids = new int[count];
			for (int i = 0; i < count; i++)
			{
				ids[i] = points[i].ID;
			}

			return ids;
		}
	}
}
