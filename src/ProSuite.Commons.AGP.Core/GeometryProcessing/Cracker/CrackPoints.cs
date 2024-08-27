using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker {
	/// <summary>
	/// Holds the result of a crack points calculation.
	/// </summary>
	public class CrackPoints {

		[NotNull]
		public IDictionary<GdbObjectReference, IList<CrackPoint>> CrackPointLocations { get; }

		[NotNull]
		public NotificationCollection Notifications { get; }

		public CrackPoints() {
			CrackPointLocations = new Dictionary<GdbObjectReference, IList<CrackPoint>>();
			Notifications = new NotificationCollection();
		}

		public void AddCrackPoints(GdbObjectReference sourceFeatureRef,
								   [NotNull] IList<CrackPoint> crackPoints) {
			CrackPointLocations.Add(sourceFeatureRef, crackPoints);
		}

		public void AddCrackPoints([NotNull] CrackPoints fromIntersections,
								   [CanBeNull] Predicate<CrackPoint> predicate) {
			Add(fromIntersections.CrackPointLocations, predicate, this);
		}

		public int CrackPointsCount => CrackPointLocations.Count;

		public bool HasCrackPoints() {
			return CrackPointLocations.Count > 0;
		}

		public CrackPoints SelectNewCrackPoints([CanBeNull] Predicate<CrackPoint> predicate) {
			var result = new CrackPoints();

			IDictionary<GdbObjectReference, IList<CrackPoint>> crackPointsToAdd = CrackPointLocations;

			Add(crackPointsToAdd, predicate, result);

			return result;
		}

		private static void Add(IDictionary<GdbObjectReference, IList<CrackPoint>> crackPointsToAdd,
								Predicate<CrackPoint> predicate, CrackPoints toResult) {
			foreach (var crackPointsBySourceRef in crackPointsToAdd) {
				List<CrackPoint> selectedCrackPoints =
					crackPointsBySourceRef.Value.Where(cp => predicate == null || predicate(cp))
												.ToList();

				if (selectedCrackPoints.Count > 0) {
					toResult.AddCrackPoints(crackPointsBySourceRef.Key, selectedCrackPoints);
				}
			}
		}

	}
}
