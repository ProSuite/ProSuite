using System;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.DomainModel.AO.Workflow
{
	public abstract class WorkUnitBase : WorkContextBase, IWorkUnit
	{
		public abstract IPolygon PerimeterCopy { get; }

		public abstract IPolygon Perimeter { get; }

		public virtual int WorkUnitId => Id;

		public abstract DateTime? StartDate { get; }
	}
}
