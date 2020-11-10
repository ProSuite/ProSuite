using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
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
		public IDictionary<GdbObjectReference, IList<IGeometry>> OverlapsBySourceRef { get; }

		[NotNull]
		public NotificationCollection Notifications { get; }

		public Overlaps()
		{
			OverlapsBySourceRef = new Dictionary<GdbObjectReference, IList<IGeometry>>();

			Notifications = new NotificationCollection();
		}

		public void AddGeometries(GdbObjectReference sourceFeatureRef,
		                          IList<IGeometry> overlapGeometries)
		{
			if (overlapGeometries.Count == 0)

			{
				return;
			}

			OverlapsBySourceRef.Add(sourceFeatureRef, overlapGeometries);
		}

		public int OverlapCount => OverlapsBySourceRef.Count;

		public bool HasOverlaps()
		{
			return OverlapsBySourceRef.Count > 0;
		}

		public IEnumerable<IGeometry> GetAllOverlapGeometries()
		{
			foreach (var geometries in OverlapsBySourceRef.Values)
			{
				foreach (IGeometry geometry in geometries)
				{
					yield return geometry;
				}
			}
		}
	}
}
