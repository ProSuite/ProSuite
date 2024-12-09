using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.AdvancedReshapeReshape;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.YReshape
{
	public class YReshapeFeedback : AdvancedReshapeFeedback
	{
		public YReshapeFeedback(ReshapeToolOptions advancedReshapeToolOptions) : base(advancedReshapeToolOptions) { }
	}
}
