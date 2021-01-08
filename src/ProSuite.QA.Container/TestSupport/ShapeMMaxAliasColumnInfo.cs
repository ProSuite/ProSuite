using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	[CLSCompliant(false)]
	public class ShapeMMaxAliasColumnInfo : ShapeEnvelopePropertyAliasColumnInfoBase
	{
		public ShapeMMaxAliasColumnInfo([NotNull] ITable table,
		                                [NotNull] string columnName)
			: base(table, columnName) { }

		protected override double GetValue(IEnvelope envelope)
		{
			return envelope.MMax;
		}
	}
}
