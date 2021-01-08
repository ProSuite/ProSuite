using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public class ShapePartCountAliasColumnInfo : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();

		public ShapePartCountAliasColumnInfo([NotNull] ITable table,
		                                     [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IFeatureClass;
			if (featureClass != null)
			{
				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IRow row)
		{
			var feature = row as IFeature;

			IGeometry shape = feature?.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return 0;
			}

			var parts = shape as IGeometryCollection;

			return parts?.GeometryCount ?? 1;
		}
	}
}
