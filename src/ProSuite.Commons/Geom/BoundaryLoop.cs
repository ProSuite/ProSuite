using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	public class BoundaryLoop
	{
		private readonly bool _isSourceRing;
		private Linestring _loop1;
		private Linestring _loop2;

		public static IList<BoundaryLoop> CreateSourceBoundaryLoops(
			[NotNull]
			IEnumerable<Tuple<IntersectionPoint3D, IntersectionPoint3D>> intersectionPairs,
			[NotNull] ISegmentList source,
			double tolerance)
		{
			Linestring fullSourceRing = null;

			List<BoundaryLoop> allBoundaryLoops = new List<BoundaryLoop>(1);

			// Decide which is the 'main' intersection, i.e. the one between outer and inner ring?
			foreach (var pair in intersectionPairs)
			{
				IntersectionPoint3D intersection1 = pair.Item1;
				IntersectionPoint3D intersection2 = pair.Item2;

				if (fullSourceRing == null)
				{
					fullSourceRing = source.GetPart(intersection1.SourcePartIndex);
				}

				allBoundaryLoops.Add(
					new BoundaryLoop(intersection1, intersection2, fullSourceRing, true));
			}

			if (allBoundaryLoops.Count == 1)
			{
				return allBoundaryLoops;
			}

			List<BoundaryLoop> result = new List<BoundaryLoop>();

			// TODO: Consider boundary loop de-duplication!

			// Currently all result boundary loops need to get the ExtraLoopIntersections!
			// The only difference is the various target intersection indexes, which should
			// probably be modelled differently/explicitly.

			foreach (BoundaryLoop boundaryLoop in allBoundaryLoops
			                                      .OrderBy(bl => bl.Start.Point.X)
			                                      .ThenBy(bl => bl.Start.Point.Y))
			{
				if (result.Count == 0)
				{
					result.Add(boundaryLoop);
				}
				else if (result.Any(
					         bl =>
						         bl.Start.ReferencesSameSourceVertex(
							         boundaryLoop.Start, source, tolerance) ||
						         bl.End.ReferencesSameSourceVertex(
							         boundaryLoop.Start, source, tolerance)))
				{
					// To be backward compatible: all boundary loops for all target vertex intersections
					// -> consider special parameter to exclude these?
					result.Add(boundaryLoop);
				}
				else
				{
					// Extra loop:
					foreach (BoundaryLoop resultLoop in result)
					{
						resultLoop.AddExtraLoopIntersections(boundaryLoop.Start, boundaryLoop.End);
					}
				}
			}

			return result;
		}

		public BoundaryLoop([NotNull] IntersectionPoint3D start,
		                    [NotNull] IntersectionPoint3D end,
		                    [NotNull] Linestring fullRing,
		                    bool isSourceRing)
		{
			Start = start;
			End = end;
			FullRing = fullRing;

			_isSourceRing = isSourceRing;
		}

		public IList<Tuple<IntersectionPoint3D, IntersectionPoint3D>> ExtraLoopIntersections
		{
			get;
			set;
		}

		[NotNull]
		public IntersectionPoint3D Start { get; }

		[NotNull]
		public IntersectionPoint3D End { get; }

		/// <summary>
		/// The entire linestring containing both loops.
		/// </summary>
		[NotNull]
		private Linestring FullRing { get; }

		/// <summary>
		/// The first loop, which is the segments between the start and the end intersection.
		/// </summary>
		public Linestring Loop1
		{
			get
			{
				if (_loop1 == null)
				{
					_loop1 = GetSubcurve(Start, End, FullRing, true);
				}

				return _loop1;
			}
		}

		/// <summary>
		/// The second loop, which is the segments between the end and the start intersection.
		/// </summary>
		public Linestring Loop2
		{
			get
			{
				if (_loop2 == null)
				{
					_loop2 = GetSubcurve(End, Start, FullRing, true);
				}

				return _loop2;
			}
		}

		public bool IsLoopingToOutside
		{
			get
			{
				if (Loop1.ClockwiseOriented == true && Loop2.ClockwiseOriented == true)
				{
					return true;
				}

				if (Loop1.ClockwiseOriented == false && Loop2.ClockwiseOriented == false)
				{
					return false;
				}

				// Check the actual rings
				bool? loop1Contains2 =
					GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon);
				if (loop1Contains2 == true)
				{
					return false;
				}

				bool? loop2Contains1 =
					GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon);

				if (loop2Contains1 == true)
				{
					return false;
				}

				return true;
			}
		}

		public bool Loop1ContainsLoop2 =>
			GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon) == true;

		public bool Loop2ContainsLoop1 =>
			GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon) == true;

		public IEnumerable<Linestring> GetBothRings()
		{
			yield return Loop1;

			yield return Loop2;
		}

		/// <summary>
		/// Returns each individual loop in form of a list of linestrings that can be used to
		/// create the full ring.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IList<IntersectionRun>> GetLoopSubcurves()
		{
			int loopCount = 0;

			foreach (IList<IntersectionRun> loopSubcurves in GetLoopSubcurves(Start, End))
			{
				yield return loopSubcurves;
				loopCount = ValidateLoopCount(loopCount, loopSubcurves.Count);
			}

			foreach (IList<IntersectionRun> loopSubcurves in GetLoopSubcurves(End, Start))
			{
				yield return loopSubcurves;
				loopCount = ValidateLoopCount(loopCount, loopSubcurves.Count);
			}
		}

		private static int ValidateLoopCount(int previousCount,
		                                     int addedLoopCount)
		{
			const int maxLoops = 100;

			int result = previousCount + addedLoopCount;

			if (result > maxLoops)
			{
				throw new InvalidOperationException(
					"Maximum number of boundary loops exceeded");
			}

			return previousCount;
		}

		private IEnumerable<IList<IntersectionRun>> GetLoopSubcurves(
			[NotNull] IntersectionPoint3D start,
			[NotNull] IntersectionPoint3D end)
		{
			if (ExtraLoopIntersections?.Count > 0)
			{
				foreach (var intersectionPair in ExtraLoopIntersections
					         .OrderBy(i => i.Item1.VirtualSourceVertex))
				{
					foreach (IList<IntersectionRun> loopCurves in BuildSourceLoops(
						         start, end, intersectionPair))
					{
						yield return loopCurves;
					}
				}
			}
			else
			{
				yield return new List<IntersectionRun> { GetIntersectionRun(start, end) };
			}
		}

		private IEnumerable<IList<IntersectionRun>> BuildSourceLoops(
			[NotNull] IntersectionPoint3D start,
			[NotNull] IntersectionPoint3D end,
			[NotNull] Tuple<IntersectionPoint3D, IntersectionPoint3D> intersectionPair)
		{
			double startVertex = start.VirtualSourceVertex;
			double endVertex = end.VirtualSourceVertex;

			double intersection1Vertex = intersectionPair.Item1.VirtualSourceVertex;
			double intersection2Vertex = intersectionPair.Item2.VirtualSourceVertex;

			if (IsRingVertexIndexBetween(intersection1Vertex, startVertex, endVertex))
			{
				// The loop is broken up at this extra intersection

				GetSourceIntersectionOrder(start, intersectionPair.Item1, intersectionPair.Item2,
				                           out IntersectionPoint3D first,
				                           out IntersectionPoint3D second);

				yield return new List<IntersectionRun>
				             {
					             GetIntersectionRun(start, first),
					             GetIntersectionRun(second, end)
				             };

				// The remaining loop, which potentially contains more sub-loops
				foreach (var subLoopCurves in GetLoopSubcurves(first, second))
				{
					yield return subLoopCurves;
				}
			}
			else if (IsRingVertexIndexBetween(intersection2Vertex, startVertex, endVertex))
			{
				// Does this ever happen (probably for seriously non-simple inputs, such as
				// 8-shaped rings)? -> Construct extra unit tests

				// The loop is broken up at this extra intersection

				GetSourceIntersectionOrder(start, intersectionPair.Item1, intersectionPair.Item2,
				                           out IntersectionPoint3D first,
				                           out IntersectionPoint3D second);

				yield return new List<IntersectionRun>
				             {
					             GetIntersectionRun(start, first),
					             GetIntersectionRun(second, end)
				             };

				// The remaining loop, which potentially contains more sub-loops
				foreach (var subLoopCurves in GetLoopSubcurves(first, second))
				{
					yield return subLoopCurves;
				}
			}
			else
			{
				yield return new List<IntersectionRun> { GetIntersectionRun(start, end) };
			}
		}

		private void GetSourceIntersectionOrder([NotNull] IntersectionPoint3D start,
		                                        [NotNull] IntersectionPoint3D intersectionA,
		                                        [NotNull] IntersectionPoint3D intersectionB,
		                                        out IntersectionPoint3D first,
		                                        out IntersectionPoint3D second)
		{
			double startA = SegmentCountBetween(FullRing, start.VirtualSourceVertex,
			                                    intersectionA.VirtualSourceVertex);
			double startB = SegmentCountBetween(FullRing, start.VirtualSourceVertex,
			                                    intersectionB.VirtualSourceVertex);

			if (startA < startB)
			{
				first = intersectionA;
				second = intersectionB;
			}
			else
			{
				first = intersectionB;
				second = intersectionA;
			}
		}

		private static double SegmentCountBetween(
			[NotNull] Linestring ring,
			double firstVirtualVertex,
			double secondVirtualVertex)
		{
			double result = secondVirtualVertex - firstVirtualVertex;

			if (result < 0)
			{
				result += ring.SegmentCount;
			}

			return result;
		}

		private static bool IsRingVertexIndexBetween(double ringVertex, double start, double end)
		{
			return start <= end
				       ? ringVertex > start && ringVertex < end
				       : (ringVertex > start) || (ringVertex < start && ringVertex < end);
		}

		private IntersectionRun GetIntersectionRun(IntersectionPoint3D start,
		                                           IntersectionPoint3D end)
		{
			return new IntersectionRun(start, end, GetSubcurve(start, end, FullRing, false), null);
		}

		private Linestring GetSubcurve([NotNull] IntersectionPoint3D fromIntersection,
		                               [NotNull] IntersectionPoint3D toIntersection,
		                               [NotNull] Linestring fullRing,
		                               bool closed)
		{
			double fromDistanceAlongAsRatio;
			double toDistanceAlongAsRatio;

			int fromIndex;
			int toIndex;
			if (_isSourceRing)
			{
				fromIndex = fromIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}
			else
			{
				fromIndex = fromIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}

			const bool forward = true;
			const bool preferFullRing = true;
			Linestring subcurve = fullRing.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, ! forward, preferFullRing);

			if (closed)
			{
				// Typically the Z-values could differ:
				subcurve.Close();
			}

			return subcurve;
		}

		private void AddExtraLoopIntersections([NotNull] IntersectionPoint3D start,
		                                       [NotNull] IntersectionPoint3D end)
		{
			if (ExtraLoopIntersections == null)
			{
				ExtraLoopIntersections =
					new List<Tuple<IntersectionPoint3D, IntersectionPoint3D>>();
			}

			ExtraLoopIntersections.Add(
				new Tuple<IntersectionPoint3D, IntersectionPoint3D>(start, end));
		}

		#region Equality members

		protected bool Equals(BoundaryLoop other)
		{
			return Start.Equals(other.Start) && End.Equals(other.End);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((BoundaryLoop) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Start.GetHashCode() * 397) ^ End.GetHashCode();
			}
		}

		#endregion
	}
}
