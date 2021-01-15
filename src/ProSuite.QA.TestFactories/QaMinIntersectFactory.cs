using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.TestFactories
{
	[CLSCompliant(false)]
	[UsedImplicitly]
	[TopologyTest]
	[IntersectionParameterTest]
	public class QaMinIntersectFactory : QaLinearUnitFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes
		{
			get { return QaMinIntersect.Codes; }
		}

		public QaMinIntersectFactory()
			: base(typeof(QaMinIntersect), 0, typeof(AreaUnits))
		{
			// Constructor
			//new QaMinIntersect(table,limit)
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			int n = args.Length;

			// get constructor arguments
			var tables = (IList<IFeatureClass>) args[0];
			var fc = (IGeoDataset) tables[0];
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
