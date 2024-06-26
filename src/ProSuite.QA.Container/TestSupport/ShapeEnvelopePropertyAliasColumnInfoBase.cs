using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public abstract class ShapeEnvelopePropertyAliasColumnInfoBase : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();
		[ThreadStatic] private static IEnvelope _envelope; // always access via property

		protected ShapeEnvelopePropertyAliasColumnInfoBase(
			[NotNull] IReadOnlyTable table,
			[NotNull] string columnName)
			: base(table, columnName, typeof(double))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IReadOnlyFeatureClass;
			if (featureClass != null)
			{
				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames => _baseFieldNames;

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			var feature = row as IReadOnlyFeature;

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
