using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeXMinAliasColumnInfo : ShapeEnvelopePropertyAliasColumnInfoBase
	{
		public ShapeXMinAliasColumnInfo([NotNull] ITable table,
		                                [NotNull] string columnName)
			: base(table, columnName) { }

		protected override double GetValue(IEnvelope envelope)
		{
			return envelope.XMin;
		}
	}
}
