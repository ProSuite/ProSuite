﻿using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.TestContainer
{
	[CLSCompliant(false)]
	public class SimpleBaseRow : BaseRow
	{
		public SimpleBaseRow([NotNull] IFeature feature)
			: base(feature, QaGeometryUtils.CreateBox(feature.Shape), GetOidList(feature)) { }

		protected override Box GetExtent()
		{
			throw new InvalidOperationException("Box was transferred with Constructor");
		}

		protected override IList<int> GetOidList()
		{
			throw new NotImplementedException("Box was transferred with Constructor");
		}
	}
}