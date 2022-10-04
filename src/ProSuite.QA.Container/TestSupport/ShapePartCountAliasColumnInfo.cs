using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapePartCountAliasColumnInfo : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();

		public ShapePartCountAliasColumnInfo([NotNull] IReadOnlyTable table,
		                                     [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IReadOnlyFeatureClass;
			if (featureClass != null)
			{
				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			var feature = row as IReadOnlyFeature;

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
