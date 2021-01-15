using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Coincidence
{
	[CLSCompliant(false)]
	public abstract class QaFullCoincidenceBase : QaPolycurveCoincidenceBase
	{
		protected QaFullCoincidenceBase(
			[NotNull] IEnumerable<IFeatureClass> featureClasses,
			double searchDistance,
			[NotNull] IFeatureDistanceProvider nearDistanceProvider,
			bool is3D)
			: base(featureClasses, searchDistance, nearDistanceProvider, is3D) { }

		[CanBeNull]
		protected static IList<Subcurve> GetMissingSegments(
			[NotNull] IFeature feature,
			[NotNull] IIndexedSegments segList,
			[NotNull] SortedDictionary<SegmentPart, SegmentParts> nearList)
		{
			var result = new List<Subcurve>();

			foreach (SegmentProxy segProxy in segList.GetSegments())
			{
				int partIndex = segProxy.PartIndex;
				int segmentIndex = segProxy.SegmentIndex;

				var key = new SegmentPart(partIndex, segmentIndex, 0, 1, true);

				SegmentParts parts;
				if (! nearList.TryGetValue(key, out parts))
				{
					// ReSharper disable once RedundantAssignment
					parts = null;
				}

				if (parts == null)
				{
					AddSegment(feature, segList, result, segProxy, 0, 1);
					continue;
				}

				parts.Sort(new SegmentPartComparer());
				double tMax = 0;
				foreach (SegmentPart part in parts)
				{
					if (part.MinFraction > tMax)
					{
						AddSegment(feature, segList, result, segProxy, tMax, part.MinFraction);
					}

					tMax = Math.Max(tMax, part.MaxFraction);
				}

				if (tMax < 1)
				{
					AddSegment(feature, segList, result, segProxy, tMax, 1);
				}
			}

			return result;
		}

		private static void AddSegment([NotNull] IFeature feature,
		                               [NotNull] IIndexedSegments geom,
		                               [NotNull] IList<Subcurve> connectedList,
		                               [NotNull] SegmentProxy segProxy, double min,
		                               double max)
		{
			if (min >= max)
			{
				return;
			}

			Subcurve current = null;
			if (connectedList.Count > 0)
			{
				current = connectedList[connectedList.Count - 1];
			}

			if (current != null)
			{
				if (current.PartIndex != segProxy.PartIndex)
				{
					current = null;
				}
				else if (current.EndSegmentIndex + current.EndFraction <
				         segProxy.SegmentIndex + min)
				{
					current = null;
				}
			}

			if (current == null)
			{
				current = new Subcurve(geom, segProxy.PartIndex, segProxy.SegmentIndex,
				                       min, segProxy.SegmentIndex, max);
				connectedList.Add(current);
			}
			else
			{
				if (current.EndSegmentIndex + current.EndFraction < segProxy.SegmentIndex + max)
				{
					current.EndSegmentIndex = segProxy.SegmentIndex;
					current.EndFraction = max;
				}
			}
		}

		protected sealed class FullNeighborhoodFinder : NeighborhoodFinder
		{
			public FullNeighborhoodFinder(IFeatureRowsDistance rowsDistance,
			                              [NotNull] IFeature feature, int tableIndex,
			                              [CanBeNull] IFeature neighbor, int neighborTableIndex)
				: base(rowsDistance, feature, tableIndex, neighbor, neighborTableIndex) { }

			protected override bool VerifyContinue(SegmentProxy seg0, SegmentProxy seg1,
			                                       SegmentNeighbors processed1,
			                                       SegmentParts partsOfSeg0, bool coincident)
			{
				bool isComplete = SegmentPart.VerifyComplete(partsOfSeg0);
				TryAssignComplete(seg1, processed1, partsOfSeg0);
				return ! isComplete;
			}
		}
	}
}
