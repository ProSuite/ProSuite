using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class ShapeEnvelopePropertyAliasColumnInfoBase : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();
		[ThreadStatic] private static IEnvelope _envelope; // always access via property

		protected ShapeEnvelopePropertyAliasColumnInfoBase(
			[NotNull] ITable table,
			[NotNull] string columnName)
			: base(table, columnName, typeof(double))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IFeatureClass;
			if (featureClass != null)
			{
				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames => _baseFieldNames;

		protected override object ReadValueCore(IRow row)
		{
			var feature = row as IFeature;

			IGeometry shape = feature?.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return DBNull.Value;
			}

			IEnvelope envelope = GetEnvelope();

			shape.QueryEnvelope(envelope);

			double result = GetValue(envelope);

			return double.IsNaN(result)
				       ? (object) DBNull.Value
				       : result;
		}

		protected abstract double GetValue([NotNull] IEnvelope envelope);

		[NotNull]
		private static IEnvelope GetEnvelope()
		{
			return _envelope ?? (_envelope = new EnvelopeClass());
		}
	}
}
