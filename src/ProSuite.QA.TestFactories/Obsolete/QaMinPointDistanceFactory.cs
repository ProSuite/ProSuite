using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[CLSCompliant(false)]
	[Obsolete("use QaMinNodeDistanceFactory")]
	public class QaMinPointDistanceFactory : QaLinearUnitFactory
	{
		public QaMinPointDistanceFactory()
			: base(typeof(QaMinPointDistance), 1, typeof(LengthUnits))
		{
			// Constructor
			// new QaMinPointDistance(tables[],near,is3D)
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			int n = args.Length;

			// get constructor arguments
			var tables = (IList<ITable>) args[0];
			var fc = (IGeoDataset) tables[0];
			var near = (double) args[1];

			// convert units and referenceScale
			esriUnits units = EsriUnits((LengthUnits) args[n - 2]);
			double scale = ((Scale) args[n - 1]).ScaleValue;

			// adapt "limit"
			near = near / UnitFactor(fc.SpatialReference, units, scale);

			// reassign arguments
			args[1] = near;

			args[n - 2] = units;
			args[n - 1] = scale;
			return base.CreateTestInstance(args);
		}
	}
}
