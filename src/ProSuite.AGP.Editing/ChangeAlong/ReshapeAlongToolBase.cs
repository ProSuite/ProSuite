using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ReshapeAlongToolBase : ChangeGeometryAlongToolBase
	{
		protected ReshapeAlongToolBase()
		{

			SelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorShift);

			TargetSelectionCursor = ToolUtils.GetCursor(Resources.ReshapeAlongToolCursorProcess);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon;
		}
	}
}
