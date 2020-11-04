using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry.RemoveOverlaps
{
	/// <summary>
	/// Holds the result of an overlaps calculation.
	/// </summary>
	[CLSCompliant(false)]
	public class Overlaps
	{
		[NotNull]
		public IList<IGeometry> OverlapGeometries { get; }

		[NotNull]
		public NotificationCollection Notifications { get; set; }

		public Overlaps(IList<IGeometry> overlapGeometries)
		{
			OverlapGeometries = overlapGeometries;
			Notifications = new NotificationCollection();
		}

		public void AddGeometries(IEnumerable<IGeometry> overlapGeometries)
		{
			foreach (IGeometry overlapGeometry in overlapGeometries)
			{
				OverlapGeometries.Add(overlapGeometry);
			}
		}

		public int OverlapCount => OverlapGeometries.Count;

		public bool HasOverlaps()
		{
			return OverlapGeometries.Count > 0;
		}
	}
}
