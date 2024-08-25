using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker
{
	/// <summary>
	/// Holds the result of a crack points calculation.
	/// </summary>
	public class CrackPoints
	{
		[NotNull]
		public IDictionary<GdbObjectReference, IList<Geometry>> CrackPointLocations{ get; }

		[NotNull]
		public NotificationCollection Notifications { get; }

		public CrackPoints()
		{
			CrackPointLocations= new Dictionary<GdbObjectReference, IList<Geometry>>();
			Notifications = new NotificationCollection();
		}

		public void AddGeometries(GdbObjectReference sourceFeatureRef,
		                          [NotNull] IList<Geometry> crackPoints)
		{
			CrackPointLocations.Add(sourceFeatureRef, crackPoints);
		}

		public void AddGeometries([NotNull] CrackPoints fromIntersections,
		                          [CanBeNull] Predicate<Geometry> predicate)
		{
			Add(fromIntersections.CrackPointLocations, predicate, this);
		}

		public int CrackPointsCount => CrackPointLocations.Count;

		public bool HasCrackPoints()
		{
			return CrackPointLocations.Count > 0;
		}

		public CrackPoints SelectNewCrackPoints([CanBeNull] Predicate<Geometry> predicate)
		{
			var result = new CrackPoints();

			IDictionary<GdbObjectReference, IList<Geometry>> crackPointsToAdd = CrackPointLocations;

			Add(crackPointsToAdd, predicate, result);

			return result;
		}

		private static void Add(IDictionary<GdbObjectReference, IList<Geometry>> crackPointsToAdd,
		                        Predicate<Geometry> predicate, CrackPoints toResult)
		{
			foreach (var crackPointsBySourceRef in crackPointsToAdd)
			{
				List<Geometry> selectedGeometries =
					crackPointsBySourceRef.Value.Where(g => predicate == null || predicate(g))
					                      .ToList();

				if (selectedGeometries.Count > 0)
				{
					toResult.AddGeometries(crackPointsBySourceRef.Key, selectedGeometries);
				}
			}
		}
	}
}
