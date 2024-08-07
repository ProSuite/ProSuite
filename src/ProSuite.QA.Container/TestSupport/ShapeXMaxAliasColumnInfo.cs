using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ShapeXMaxAliasColumnInfo : ShapeEnvelopePropertyAliasColumnInfoBase
	{
		public ShapeXMaxAliasColumnInfo([NotNull] IReadOnlyTable table,
		                                [NotNull] string columnName)
			: base(table, columnName) { }

		protected override double GetValue(IEnvelope envelope)
		{
			return envelope.XMax;
		}
	}
}
