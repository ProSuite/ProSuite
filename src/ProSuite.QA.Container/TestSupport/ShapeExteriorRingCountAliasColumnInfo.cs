using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeExteriorRingCountAliasColumnInfo : ColumnInfo
	{
		private readonly List<string> _baseFieldNames = new List<string>();
		private readonly bool _canHaveExteriorRings;

		public ShapeExteriorRingCountAliasColumnInfo([NotNull] IReadOnlyTable table,
		                                             [NotNull] string columnName)
			: base(table, columnName, typeof(int))
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));

			var featureClass = table as IReadOnlyFeatureClass;
			if (featureClass == null)
			{
				_canHaveExteriorRings = false;
			}
			else
			{
				_canHaveExteriorRings = featureClass.ShapeType ==
				                        esriGeometryType.esriGeometryPolygon;

				_baseFieldNames.Add(featureClass.ShapeFieldName);
			}
		}

		public override IEnumerable<string> BaseFieldNames
		{
			get { return _baseFieldNames; }
		}

		protected override object ReadValueCore(IReadOnlyRow row)
		{
			if (! _canHaveExteriorRings)
			{
				return 0;
			}

			var feature = row as IReadOnlyFeature;
			if (feature == null)
			{
				return 0;
			}

			IGeometry shape = feature.Shape;

			if (shape == null || shape.IsEmpty)
			{
				return 0;
			}

			var polygon = (IPolygon) shape;

			try
			{
				return polygon.ExteriorRingCount;
			}
			catch (COMException)
			{
				return null;
			}
		}
	}
}
