using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeMMinAliasColumnInfo : ShapeEnvelopePropertyAliasColumnInfoBase
	{
		public ShapeMMinAliasColumnInfo([NotNull] IReadOnlyTable table,
		                                [NotNull] string columnName)
			: base(table, columnName) { }

		protected override double GetValue(IEnvelope envelope)
		{
			return envelope.MMin;
		}
	}
}
