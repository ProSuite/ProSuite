using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Tests
{
	internal static class SegmentPairUtils
	{
		public static IPolyline CreateGeometry<T>([NotNull] IEnumerable<T> segmentPairs)
			where T : ISegmentPair
		{
			var uniqueSegments =
				new Dictionary<SegmentProxy, SegmentProxy>(
					new SegmentEqualityComparer());

			foreach (T segmentPair in segmentPairs)
			{
				if (! uniqueSegments.ContainsKey(segmentPair.BaseSegment))
				{
					uniqueSegments.Add(segmentPair.BaseSegment, segmentPair.BaseSegment);
				}

				if (! uniqueSegments.ContainsKey(segmentPair.RelatedSegment))
				{
					uniqueSegments.Add(segmentPair.RelatedSegment, segmentPair.RelatedSegment);
				}
			}

			IGeometryCollection result = new PolylineClass {ZAware = true};
			bool first = true;
			object missing = Type.Missing;
			foreach (SegmentProxy segment in uniqueSegments.Keys)
			{
				IPolyline line = segment.GetPolyline(true);
				IPath path = new PathClass();
				((ISegmentCollection) path).AddSegmentCollection((ISegmentCollection) line);
				result.AddGeometry(path, ref missing, ref missing);
				if (first)
				{
					GeometryUtils.EnsureSpatialReference((IGeometry) result,
					                                     line.SpatialReference);
				}

				first = false;
			}

			return (IPolyline) result;
		}

		public static void AddRelatedPairsRecursive<T>(IList<T> relatedPairs,
		                                               List<T> unhandledPairs)
			where T : ISegmentPair
		{
			var segmentEqualityComparer = new SegmentEqualityComparer();
			var relatedDictionary =
				new Dictionary<SegmentProxy, SegmentProxy>(segmentEqualityComparer);

			foreach (T relatedPair in relatedPairs)
			{
				AddSegmentKeys(relatedDictionary, relatedPair);
			}

			bool newAddedToRelated = true;
			List<T> candidates = unhandledPairs;
			while (newAddedToRelated)
			{
				newAddedToRelated = false;

				var newlyUnhandledPairs = new List<T>(candidates.Count);

				foreach (T unhandledPair in candidates)
				{
					if (relatedDictionary.ContainsKey(unhandledPair.BaseSegment))
					{
						newAddedToRelated |= AddSegmentKeys(relatedDictionary, unhandledPair);
						relatedPairs.Add(unhandledPair);
					}
					else if (relatedDictionary.ContainsKey(unhandledPair.RelatedSegment))
					{
						newAddedToRelated |= AddSegmentKeys(relatedDictionary, unhandledPair);
						relatedPairs.Add(unhandledPair);
					}
					else
					{
						newlyUnhandledPairs.Add(unhandledPair);
					}
				}

				candidates = newlyUnhandledPairs;
			}

			unhandledPairs.Clear();
			unhandledPairs.AddRange(candidates);
		}

		private static bool AddSegmentKeys<T>(
			Dictionary<SegmentProxy, SegmentProxy> relatedDictionary,
			T relatedPair) where T : ISegmentPair
		{
			bool added = false;
			if (! relatedDictionary.ContainsKey(relatedPair.BaseSegment))
			{
				added = true;
				relatedDictionary.Add(relatedPair.BaseSegment, relatedPair.BaseSegment);
			}

			if (! relatedDictionary.ContainsKey(relatedPair.RelatedSegment))
			{
				added = true;
				relatedDictionary.Add(relatedPair.RelatedSegment, relatedPair.RelatedSegment);
			}

			return added;
		}

		private static bool HasCommonSegment(ISegmentPair x, ISegmentPair y)
		{
			return AreEqualSegments(x.BaseSegment, y.BaseSegment) ||
			       AreEqualSegments(x.BaseSegment, y.RelatedSegment) ||
			       AreEqualSegments(x.RelatedSegment, y.BaseSegment) ||
			       AreEqualSegments(x.RelatedSegment, y.RelatedSegment);
		}

		private static bool AreEqualSegments(SegmentProxy x, SegmentProxy y)
		{
			return x.PartIndex == y.PartIndex &&
			       x.SegmentIndex == y.SegmentIndex;
		}

		private class SegmentEqualityComparer : IEqualityComparer<SegmentProxy>
		{
			public bool Equals(SegmentProxy x, SegmentProxy y)
			{
				return AreEqualSegments(x, y);
			}

			public int GetHashCode(SegmentProxy obj)
			{
				return obj.PartIndex.GetHashCode() ^
				       obj.SegmentIndex.GetHashCode();
			}
		}
	}
}
