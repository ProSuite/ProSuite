using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps
{
	/// <summary>
	/// Holds the result of an overlap calculation.
	/// </summary>
	public class Overlaps
	{
		[NotNull]
		public IDictionary<GdbObjectReference, IList<Geometry>> OverlapGeometries { get; }

		[NotNull]
		public NotificationCollection Notifications { get; }

		public Overlaps()
		{
			OverlapGeometries = new Dictionary<GdbObjectReference, IList<Geometry>>();
			Notifications = new NotificationCollection();
		}

		public void AddGeometries(GdbObjectReference sourceFeatureRef,
		                          [NotNull] IList<Geometry> overlapGeometries)
		{
			OverlapGeometries.Add(sourceFeatureRef, overlapGeometries);
		}

		public void AddGeometries([NotNull] Overlaps fromOverlaps,
		                          [CanBeNull] Predicate<Geometry> predicate)
		{
			Add(fromOverlaps.OverlapGeometries, predicate, this);
		}

		public int OverlapCount => OverlapGeometries.Count;

		public bool HasOverlaps()
		{
			return OverlapGeometries.Count > 0;
		}

		public Overlaps SelectNewOverlaps([CanBeNull] Predicate<Geometry> predicate)
		{
			var result = new Overlaps();

			IDictionary<GdbObjectReference, IList<Geometry>> overlapsToAdd = OverlapGeometries;

			Add(overlapsToAdd, predicate, result);

			return result;
		}

		private static void Add(IDictionary<GdbObjectReference, IList<Geometry>> overlapsToAdd,
		                        Predicate<Geometry> predicate, Overlaps toResult)
		{
			foreach (var overlapsBySourceRef in overlapsToAdd)
			{
				List<Geometry> selectedGeometries =
					overlapsBySourceRef.Value.Where(g => predicate == null || predicate(g))
					                   .ToList();

				if (selectedGeometries.Count > 0)
				{
					toResult.AddGeometries(overlapsBySourceRef.Key, selectedGeometries);
				}
			}
		}
	}
}
