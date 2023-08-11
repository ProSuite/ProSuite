using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Exceptions;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.AGP.Core
{
	public static class Extensions
	{
		public static IRowValues RowValues(this Row row)
		{
			return row is null ? null : new RowAdapter(row);
		}

		public static IRowValues RowValues(this RowBuffer row)
		{
			return row is null ? null : new RowBufferAdapter(row);
		}

		#region FieldSetter & Co.

		public static FieldSetter ValidateTargetFields(
			this FieldSetter instance, FeatureClass featureClass, string parameterName)
		{
			if (instance is null) return null;
			if (featureClass is null) return instance;

			try
			{
				var fieldNames = featureClass.GetDefinition().GetFields().Select(f => f.Name);
				return instance.ValidateTargetFields(fieldNames);
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

		#region Nested type: RowAdapter

		private class RowAdapter : IRowValues
		{
			// TODO the old Evaluation.RowValues has support for a pseudo fields "_DATASET" and "_DATASET_" that return the dataset's name

			public RowAdapter(Row row)
			{
				Row = row ?? throw new ArgumentNullException();
				FieldNames = row.GetFields().Select(f => f.Name).ToArray();
			}

			private Row Row { get; }

			public IReadOnlyList<string> FieldNames { get; }

			public object this[int index]
			{
				get => Row[index];
				set => Row[index] = value;
			}

			public int FindField(string fieldName)
			{
				if (fieldName is null) return -1;
				return Row.FindField(fieldName);
			}

			public bool Exists(string name)
			{
				if (name is null) return false;
				return Row.FindField(name) >= 0;
			}

			public object GetValue(string name)
			{
				int index = Row.FindField(name);
				return index < 0 ? null : Row[index];
			}
		}

		#endregion

		#region Nested type: RowBufferAdapter

		private class RowBufferAdapter : IRowValues
		{
			public RowBufferAdapter(RowBuffer row)
			{
				Row = row ?? throw new ArgumentNullException();
				FieldNames = row.GetFields().Select(f => f.Name).ToArray();
			}

			private RowBuffer Row { get; }

			public IReadOnlyList<string> FieldNames { get; }

			public object this[int index]
			{
				get => Row[index];
				set => Row[index] = value;
			}

			public int FindField(string fieldName)
			{
				if (fieldName is null) return -1;
				return Row.FindField(fieldName);
			}

			public bool Exists(string name)
			{
				if (name is null) return false;
				return Row.FindField(name) >= 0;
			}

			public object GetValue(string name)
			{
				int index = Row.FindField(name);
				return index < 0 ? null : Row[index];
			}
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
