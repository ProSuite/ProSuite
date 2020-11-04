using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	/// <summary>
	/// Holds the result of an overlap calculation.
	/// </summary>
	public class Overlaps
	{
		[NotNull]
		public IList<Geometry> OverlapGeometries { get; }

		[NotNull]
		public NotificationCollection Notifications { get; set; }

		public Overlaps(IList<Geometry> overlapGeometries)
		{
			OverlapGeometries = overlapGeometries;
			Notifications = new NotificationCollection();
		}

		public void AddGeometries(IEnumerable<Geometry> overlapGeometries)
		{
			foreach (Geometry overlapGeometry in overlapGeometries)
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
