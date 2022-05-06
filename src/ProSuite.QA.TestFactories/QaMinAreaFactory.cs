using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinAreaFactory : QaLinearUnitFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaMinArea.Codes; }
		}

		public QaMinAreaFactory() : base(typeof(QaMinArea), 0, typeof(AreaUnits))
		{
			// Constructor
			//new QaMinArea(table,limit)
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			int n = args.Length;

			// get constructor arguments
			var fc = (IGeoDataset) args[0];
			var limit = (double) args[1];

			// convert units and referenceScale
			esriUnits units = EsriUnits((AreaUnits) args[n - 2]);
			double scale = ((Scale) args[n - 1]).ScaleValue;

			// adapt "limit"
			limit = limit / UnitFactor(fc.SpatialReference, units, scale);

			// reassign arguments
			args[1] = limit;

			args[n - 2] = units;
			args[n - 1] = scale;
			return base.CreateTestInstance(args);
		}
	}
}
