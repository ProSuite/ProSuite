using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// The short segments of a polycurve that are still candidates for removal, kept up to date
	/// while the segments are removed one after the other.
	/// </summary>
	internal class ShortSegmentCandidates
	{
		[NotNull] private readonly IPolycurve _polycurve;
		private readonly double _minimumSegmentLength;
		[CanBeNull] private readonly IGeometry _perimeter;
		private readonly bool _use3DLength;
		private readonly bool _verifyUpdates;

		[NotNull] private List<esriSegmentInfo> _current;

		public ShortSegmentCandidates([NotNull] IPolycurve polycurve,
		                              double minimumSegmentLength,
		                              [CanBeNull] IGeometry perimeter,
		                              bool use3DLength)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			_polycurve = polycurve;
			_minimumSegmentLength = minimumSegmentLength;
			_perimeter = perimeter;
			_use3DLength = use3DLength && GeometryUtils.IsZAware(polycurve);

			_verifyUpdates = string.Equals(
				Environment.GetEnvironmentVariable(
					SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable),
				"true", StringComparison.OrdinalIgnoreCase);

			_current = MeasureAllSegments();
		}

		/// <summary>
		/// The segments of the polycurve that are currently shorter than the minimum segment
		/// length, in part and segment order.
		/// </summary>
		[NotNull]
		public IList<esriSegmentInfo> Current => _current;

		/// <summary>
		/// Brings the candidate list in line with the change that was just made to the polycurve.
		/// </summary>
		public void Update(SegmentRemovalResult removal)
		{
			if (removal.RemovedInnerVertexIndex == null)
			{
				// The change re-arranged the part, so the remaining segment indices of that part
				// are not known here: measure every segment again.
				_current = MeasureAllSegments();
				return;
			}

			ApplyInnerVertexRemoval(removal.PartIndex, removal.RemovedInnerVertexIndex.Value);

			if (_verifyUpdates)
			{
				VerifyAgainstFullMeasurement(removal);
			}
		}

		private void VerifyAgainstFullMeasurement(SegmentRemovalResult removal)
		{
			List<esriSegmentInfo> expected = MeasureAllSegments();

			string difference = DescribeDifference(expected, _current);

			if (difference == null)
			{
				return;
			}

			throw new InvalidOperationException(
				$"The list of short segments kept while removing them differs from measuring " +
				$"all segments of the geometry, after removing vertex " +
				$"{removal.RemovedInnerVertexIndex} in part {removal.PartIndex}: {difference}");
		}

		[CanBeNull]
		private static string DescribeDifference([NotNull] List<esriSegmentInfo> expected,
		                                         [NotNull] List<esriSegmentInfo> actual)
		{
			if (expected.Count != actual.Count)
			{
				return $"expected {expected.Count} short segment(s), the list has {actual.Count}";
			}

			for (var i = 0; i < expected.Count; i++)
			{
				if (expected[i].iPart != actual[i].iPart ||
				    expected[i].iRelSegment != actual[i].iRelSegment ||
				    expected[i].iAbsSegment != actual[i].iAbsSegment)
				{
					return
						$"at position {i} the expected segment is " +
						$"{expected[i].iRelSegment} in part {expected[i].iPart} " +
						$"(absolute {expected[i].iAbsSegment}), the list has " +
						$"{actual[i].iRelSegment} in part {actual[i].iPart} " +
						$"(absolute {actual[i].iAbsSegment})";
				}
			}

			return null;
		}

		/// <summary>
		/// Updates the candidate list after the vertex at the specified part-local index was
		/// removed from the specified part.
		/// </summary>
		private void ApplyInnerVertexRemoval(int partIndex, int removedVertexIndex)
		{
			// Before the removal, segment i of the part ran from vertex i to vertex i + 1. The
			// two segments that met at the removed vertex, removedVertexIndex - 1 and
			// removedVertexIndex, are now a single segment at index removedVertexIndex - 1. Every
			// segment after the removed vertex keeps its shape but moves down by one index.
			int mergedSegmentIndex = removedVertexIndex - 1;

			esriSegmentInfo? mergedSegment = MeasureSegment(partIndex, mergedSegmentIndex);

			var updated = new List<esriSegmentInfo>(_current.Count + 1);

			var mergedSegmentHandled = false;

			foreach (esriSegmentInfo candidate in _current)
			{
				bool isBeforeMergedSegment =
					candidate.iPart < partIndex ||
					(candidate.iPart == partIndex && candidate.iRelSegment < mergedSegmentIndex);

				if (isBeforeMergedSegment)
				{
					updated.Add(candidate);
					continue;
				}

				if (! mergedSegmentHandled)
				{
					mergedSegmentHandled = true;

					if (mergedSegment != null)
					{
						updated.Add(mergedSegment.Value);
					}
				}

				if (candidate.iPart == partIndex &&
				    (candidate.iRelSegment == mergedSegmentIndex ||
				     candidate.iRelSegment == removedVertexIndex))
				{
					// One of the two segments that were merged: it no longer exists. The merged
					// segment that replaces them was measured above.
					continue;
				}

				esriSegmentInfo shifted = candidate;

				if (shifted.iPart == partIndex)
				{
					shifted.iRelSegment -= 1;
				}

				shifted.iAbsSegment -= 1;

				updated.Add(shifted);
			}

			if (! mergedSegmentHandled && mergedSegment != null)
			{
				updated.Add(mergedSegment.Value);
			}

			_current = updated;
		}

		[NotNull]
		private List<esriSegmentInfo> MeasureAllSegments()
		{
			return new List<esriSegmentInfo>(
				GeometryUtils.GetShortSegments(_polycurve, _minimumSegmentLength, _perimeter,
				                               _use3DLength));
		}

		/// <summary>
		/// Returns the segment info for the specified segment, or null if that segment is not
		/// shorter than the minimum segment length (or lies outside the perimeter).
		/// </summary>
		private esriSegmentInfo? MeasureSegment(int partIndex, int localSegmentIndex)
		{
			ISegment segment = GeometryUtils.GetSegment((ISegmentCollection) _polycurve,
			                                            partIndex, localSegmentIndex);

			if (! GeometryUtils.IsShortSegment(segment, _minimumSegmentLength, _use3DLength,
			                                   _perimeter))
			{
				return null;
			}

			return new esriSegmentInfo
			       {
				       pSegment = segment,
				       iPart = partIndex,
				       iRelSegment = localSegmentIndex,
				       iAbsSegment = GeometryUtils.GetGlobalSegmentIndex(
					       _polycurve, partIndex, localSegmentIndex)
			       };
		}
	}

	/// <summary>
	/// Describes the change that <see cref="SegmentReplacementUtils.TryRemoveSegment"/> made to
	/// a geometry, so that a caller can update its own list of short segments without inspecting
	/// the whole geometry again.
	/// </summary>
	internal struct SegmentRemovalResult
	{
		/// <summary>The index of the part that was changed.</summary>
		public int PartIndex { get; set; }

		/// <summary>
		/// The part-local index of the removed vertex, but only if a single vertex between the
		/// first and the last vertex of the part was removed and nothing else was changed. It is
		/// null for every other kind of change, that is when an entire segment was removed or
		/// when the first or the last vertex of a part was removed. Those changes re-arrange the
		/// whole part (a closed part is re-anchored at a different vertex), so the segment
		/// indices of that part cannot simply be shifted.
		/// </summary>
		public int? RemovedInnerVertexIndex { get; set; }
	}
}
