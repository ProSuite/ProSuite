using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.TestFactories
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	public class QaMaxLengthFactory : QaLinearUnitFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaMaxLength.Codes; }
		}

		public QaMaxLengthFactory() : base(typeof(QaMaxLength), 0, typeof(LengthUnits))
		{
			// Constructor
			//new QaMaxLength(table,limit,is3D)
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			int n = args.Length;

			// get constructor arguments
			var fc = (IGeoDataset) args[0];
			var limit = (double) args[1];

			// convert units and referenceScale
			esriUnits units = EsriUnits((LengthUnits) args[n - 2]);
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
