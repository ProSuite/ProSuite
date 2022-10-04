using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public abstract class TableTransformer<T> : InvolvesTablesBase, ITableTransformer<T>
		where T : IReadOnlyTable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private T _transformed;

		protected TableTransformer(IEnumerable<IReadOnlyTable> involvedTables)
			: base(involvedTables) { }

		public T GetTransformed()
		{
			if (_transformed != null)
			{
				return _transformed;
			}

			using (_msg.IncrementIndentation("Creating transformer {0}...", TransformerName))
			{
				try
				{
					T transformed = GetTransformedCore(TransformerName);

					UpdateConstraints(transformed);

					_transformed = transformed;

					using (_msg.IncrementIndentation(
						       $"Created transformed table {_transformed.Name}:"))
					{
						LogTableProperties(_transformed);
					}
				}
				catch (Exception exception)
				{
					_msg.Debug($"Error creating {TransformerName}", exception);
					throw;
				}
			}

			return _transformed;
		}

		private static void LogTableProperties(IReadOnlyTable table)
		{
			if (table is IReadOnlyFeatureClass featureClass)
			{
				_msg.InfoFormat("Shape type: {0}", GetGeometryTypeText(featureClass.ShapeType));

				IGeometryDef geometryDef = DatasetUtils.GetGeometryDef(featureClass);

				_msg.InfoFormat("Has Z: {0}", geometryDef.HasZ);
				_msg.InfoFormat("Has M: {0}", geometryDef.HasM);

				_msg.InfoFormat(SpatialReferenceUtils.ToString(geometryDef.SpatialReference));
			}

			var fieldList = DatasetUtils.GetFields(table.Fields)
			                            .Where(f => f.Name != InvolvedRowUtils.BaseRowField)
			                            .Select(f => f.Name).ToList();

			string fieldDisplayList = $"List of fields: " +
			                          $"{Environment.NewLine}{StringUtils.Concatenate(fieldList, Environment.NewLine)}";

			_msg.Info(fieldDisplayList);
		}

		[NotNull]
		private static string GetGeometryTypeText(esriGeometryType shapeType)
		{
			switch (shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multipoint";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryMultiPatch:
					return "Multipatch";

				default:
					return shapeType.ToString();
			}
		}

		private void UpdateConstraints(T transformed)
		{
			if (transformed is GdbTable gdbTable &&
			    gdbTable.BackingDataset is TransformedBackingData backingData)

				for (int i = 0; i < InvolvedTables.Count; i++)
				{
					string constraint = GetConstraint(i);
					bool useCaseSensitiveSql = GetSqlCaseSensitivity(i);

					string tableName = InvolvedTables[i]?.Name;

					_msg.Debug(
						$"Adding constraint to {tableName}: {constraint ?? "<null>"}. " +
						$"Case-sensitive: {useCaseSensitiveSql}");

					backingData.SetConstraint(i, constraint);
					backingData.SetSqlCaseSensitivity(i, useCaseSensitiveSql);
				}
		}

		protected abstract T GetTransformedCore(string tableName);

		object ITableTransformer.GetTransformed() => GetTransformed();

		public string TransformerName { get; set; }

		#region Overrides of ProcessBase

		protected override void SetConstraintCore(IReadOnlyTable table, int tableIndex,
		                                          string constraint)
		{
			if (_transformed == null)
			{
				return;
			}

			UpdateConstraints(_transformed);
		}

		#endregion
	}
}
