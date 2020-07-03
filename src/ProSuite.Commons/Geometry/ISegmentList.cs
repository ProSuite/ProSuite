using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geometry
{
	/// <summary>
	/// Provides access to properties and methods of 1-dimensional curves that are linestrings or
	/// contain linestrings. Specifically provides methods that allow navigating connected segments.
	/// </summary>
	public interface ISegmentList : IEnumerable<Line3D>, IBoundedXY
	{
		int SegmentCount { get; }

		int PartCount { get; }

		bool IsEmpty { get; }

		bool IsClosed { get; }

		Line3D GetSegment(int index);

		Linestring GetPart(int index);

		void SetEmpty();

		bool IsFirstPointInPart(int vertexIndex);

		bool IsLastPointInPart(int vertexIndex);

		bool IsFirstSegmentInPart(int segmentIndex);

		bool IsLastSegmentInPart(int segmentIndex);

		/// <summary>
		/// Returns the (global) point index of the specified segment's start point.
		/// </summary>
		/// <param name="segmentIndex"></param>
		/// <returns></returns>
		int GetSegmentStartPointIndex(int segmentIndex);

		Line3D this[int index] { get; }

		/// <summary>
		/// Returns the next connected segment index or null, if the specified (global) segment
		/// index is the last in a non-closed linestring.
		/// </summary>
		/// <param name="currentSegmentIndex"></param>
		/// <returns></returns>
		int? NextSegmentIndex(int currentSegmentIndex);

		/// <summary>
		/// Returns the previous connected segment index or null, if the specified (global) segment
		/// index is the first in a non-closed linestring.
		/// </summary>
		/// <param name="currentSegmentIndex"></param>
		/// <returns></returns>
		int? PreviousSegmentIndex(int currentSegmentIndex);

		/// <summary>
		/// Returns the next connected segment or null if the specified (global) segment
		/// index ist the last in a non-closed linestring.
		/// </summary>
		/// <param name="currentSegmentIdx">The current (global) segment index</param>
		/// <param name="skipVerticalSegments">Whether vertical segments, i.e. segments that have a
		/// 2-D length shorter than the tolerance, should be skipped.</param>
		/// <param name="tolerance">The 2D tolerance that determines whether a segment is vertical.</param>
		/// <returns></returns>
		[CanBeNull]
		Line3D NextSegment(int currentSegmentIdx,
		                   bool skipVerticalSegments = false,
		                   double tolerance = 0);

		/// <summary>
		/// Returns the previous connected segment or null if the specified (global) segment
		/// index is the first in a non-closed linestring.
		/// </summary>
		/// <param name="currentSegmentIdx">The current (global) segment index</param>
		/// <param name="skipVerticalSegments">Whether vertical segments, i.e. segments that have a
		/// 2-D length shorter than the tolerance, should be skipped.</param>
		/// <param name="tolerance">The 2D tolerance that determines whether a segment is vertical.</param>
		/// <returns></returns>
		[CanBeNull]
		Line3D PreviousSegment(int currentSegmentIdx,
		                       bool skipVerticalSegments = false,
		                       double tolerance = 0);

		/// <summary>
		/// Finds the segments whose extent intersects the extent of the specified search geometry.
		/// </summary>
		/// <param name="searchGeometry">The geometry whose bounds are used to search</param>
		/// <param name="tolerance"></param>
		/// <param name="allowIndexing">Whether a spatial should be created if appropriate.</param>
		/// <param name="predicate">A predicate that allows quick filtering by segment index.</param>
		/// <returns>The found segments with their segment index.</returns>
		IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			IBoundedXY searchGeometry,
			double tolerance,
			bool allowIndexing = true,
			Predicate<int> predicate = null);

		/// <summary>
		/// Finds the segments whose extent intersects the specified search extent.
		/// </summary>
		/// <param name="xMin"></param>
		/// <param name="yMin"></param>
		/// <param name="xMax"></param>
		/// <param name="yMax"></param>
		/// <param name="tolerance"></param>
		/// <param name="allowIndexing"></param>
		/// <param name="predicate">A predicate that allows quick filtering by segment index.</param>
		/// <returns>The found segments with their segment index.</returns>
		IEnumerable<KeyValuePair<int, Line3D>> FindSegments(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, bool allowIndexing = true,
			Predicate<int> predicate = null);

		int GetLocalSegmentIndex(int globalSegmentIndex, out int linestringIndex);

		int GetGlobalSegmentIndex(int linestringIndex, int localSegmentIndex);
	}
}