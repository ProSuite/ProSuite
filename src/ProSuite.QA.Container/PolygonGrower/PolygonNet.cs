using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Geometry.SpatialIndex;
using ProSuite.QA.Container.Geometry;
using Ao = ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class PolygonNet
	{
		protected static readonly ThreadLocal<Ao.IPoint> FromPointTemplate =
			new ThreadLocal<Ao.IPoint>(() => new Ao.PointClass());

		protected static readonly ThreadLocal<Ao.IPoint> ToPointTemplate =
			new ThreadLocal<Ao.IPoint>(() => new Ao.PointClass());

		protected static readonly ThreadLocal<Ao.IEnvelope> QueryBox =
			new ThreadLocal<Ao.IEnvelope>(() => new Ao.EnvelopeClass());

		protected static readonly ThreadLocal<Ao.IEnvelope> QueryX =
			new ThreadLocal<Ao.IEnvelope>(() => new Ao.EnvelopeClass());

		protected static readonly ThreadLocal<Ao.IEnvelope> QueryY =
			new ThreadLocal<Ao.IEnvelope>(() => new Ao.EnvelopeClass());
	}

	public class PolygonNet<TDirectedRow> : PolygonNet,
	                                        IEnumerable<LineListPolygon<TDirectedRow>>
		where TDirectedRow : class, IPolygonDirectedRow
	{
		private readonly Ao.IPoint _qPntAssignInnerRings = new Ao.PointClass();
		private List<LineListPolygon<TDirectedRow>> _polyList;
		private Tree _spatialIndex;

		#region IEnumerable<LineListPolygon> Members

		public IEnumerator<LineListPolygon<TDirectedRow>> GetEnumerator()
		{
			return _polyList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public static PolygonNet<T> Create<T>(List<LineList<T>> outerRingList,
		                                      Ao.IEnvelope outerRingsBox,
		                                      List<LineList<T>> innerRingList,
		                                      List<T> innerLineList)
			where T : class, IPolygonDirectedRow
		{
			var net = new PolygonNet<T>();

			innerLineList.Sort(new PolygonNet<T>.DirRowComparer());

			var polyList = new List<LineListPolygon<T>>();
			net._polyList = polyList;

			foreach (LineList<T> ring in outerRingList)
			{
				var poly = new LineListPolygon<T>(ring);
				polyList.Add(poly);
			}

			foreach (LineList<T> ring in innerRingList)
			{
				// TODO revise
				var poly = new LineListPolygon<T>(ring, true);
			}

			net._spatialIndex = BuildSpatialIndex(outerRingsBox, outerRingList,
			                                      innerLineList,
			                                      new PolygonNet<T>.BoxComparer());
			net.AssignInnerRings(innerLineList, net._spatialIndex, outerRingsBox);

			return net;
		}

		private void AssignInnerRings(IEnumerable<TDirectedRow> innerLines, Tree tree,
		                              Ao.IEnvelope outerRingsBox)
		{
			foreach (TDirectedRow row in innerLines)
			{
				TopologicalLine line = row.TopologicalLine;
				LineListPolygon poly = null;
				if (line.RightPoly != null && line.RightPoly.IsInnerRing &&
				    line.RightPoly.Processed == false)
				{
					poly = line.RightPoly;
				}
				else if (line.LeftPoly != null && line.LeftPoly.IsInnerRing &&
				         line.LeftPoly.Processed == false)
				{
					poly = line.LeftPoly;
				}

				if (poly == null)
				{
					continue;
				}

				poly.Processed = true;
				line.Path.QueryEnvelope(QueryBox.Value);
				if (QueryBox.Value.XMax < outerRingsBox.XMin || QueryBox.Value.YMax < outerRingsBox.YMin)
				{
					continue;
				}

				_qPntAssignInnerRings.X = QueryBox.Value.XMax;
				_qPntAssignInnerRings.Y = line.YMax();

				// no unassigned line right of pnt exists, because we sorted the innerLines in this fashion
				int side;
				TopologicalLine nearLine = NearestLine(tree, _qPntAssignInnerRings, true,
				                                       out side);
				if (nearLine == null)
				{
					continue;
				}

				if (side > 0)
				{
					if (nearLine.RightPoly != null &&
					    nearLine.RightPoly.IsInnerRing == false)
						// else outside of outer rings
					{
						nearLine.RightPoly.Add(poly);
					}
				}
				else
				{
					if ((side != 0) == false)
					{
						throw new InvalidProgramException(
							"Error in software design assumption: " + side + " != 0");
					}

					if (nearLine.LeftPoly != null &&
					    nearLine.LeftPoly.IsInnerRing == false)
						// else outside of outer rings
					{
						nearLine.LeftPoly.Add(poly);
					}
				}
			}
		}

		[NotNull]
		private static PolygonNet<T>.Tree BuildSpatialIndex<T>(
			Ao.IEnvelope box,
			IEnumerable<LineList<T>> outerRingList,
			IEnumerable<T> innerLineList,
			PolygonNet<T>.BoxComparer comparer)
			where T : class, IPolygonDirectedRow
		{
			var tree = new PolygonNet<T>.Tree(comparer);

			// Add each line once to box tree
			tree.InitSize(new IGmtry[] {QaGeometryUtils.CreateBox(box)});
			foreach (LineList<T> ring in outerRingList)
			{
				foreach (T row in ring.DirectedRows)
				{
					if (row.RightPoly != null && row.RightPoly.IsInnerRing == false)
					{
						tree.Add(QaGeometryUtils.CreateBox(row.TopologicalLine.Path),
						         row.TopologicalLine);
					}
					else
					{
						if ((row.LeftPoly != null && row.LeftPoly.IsInnerRing == false) ==
						    false)
						{
							throw new InvalidProgramException(
								"Error in software design assumption");
						}

						tree.Add(QaGeometryUtils.CreateBox(row.TopologicalLine.Path),
						         row.TopologicalLine);
					}
				}
			}

			foreach (T row in innerLineList)
			{
				if (row.RightPoly == null || row.LeftPoly == null)
				{
					tree.Add(QaGeometryUtils.CreateBox(row.TopologicalLine.Path),
					         row.TopologicalLine);
				}
			}

			return tree;
		}

		public LineListPolygon AssignCentroid(IRow pointRow, out TopologicalLine line,
		                                      out int side)
		{
			var point = (Ao.IPoint) ((IFeature) pointRow).Shape;
			line = NearestLine(_spatialIndex, point, false, out side);
			if (line == null)
			{
				return null;
			}

			LineListPolygon poly = null;
			if (side > 0)
			{
				poly = line.RightPoly;
			}
			else if (side < 0)
			{
				poly = line.LeftPoly;
			}

			if (poly != null)
			{
				poly.Centroids.Add(pointRow);
			}

			return poly;
		}

		/// <summary>
		/// finds the line cutting the x-Axes through point closest to point on the lower side of point
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="searchPoint"></param>
		/// <param name="excludeEqual"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		[CanBeNull]
		private static TopologicalLine NearestLine([NotNull] Tree tree,
		                                           [NotNull] Ao.IPoint searchPoint,
		                                           bool excludeEqual,
		                                           out int side)
		{
			TopologicalLine nearestLine = null;
			side = 0;
			double xMax = tree.Extent.Max.X;

			double searchX;
			double searchY;
			searchPoint.QueryCoords(out searchX, out searchY);

			var searchBox = new Box(new Pnt2D(searchX, searchY), new Pnt2D(xMax, searchY));

			tree.ExcludeEqual = excludeEqual;
			BoxTree<TopologicalLine>.TileEntryEnumerator enumerator =
				tree.Search(searchBox).GetEnumerator();

			double x1Nearest = 0;
			double y1Nearest = 0;

			while (enumerator.MoveNext())
			{
				if (xMax <= searchX)
				{
					side = 0;
					return nearestLine;
				}

				TopologicalLine currentLine = enumerator.Current.Value;
				if (excludeEqual)
				{
					currentLine.Path.QueryEnvelope(QueryBox.Value);
					if (QueryBox.Value.XMax == searchX)
					{
						continue;
					}
				}

				QueryBox.Value.PutCoords(searchX, searchY, xMax, searchY);

				var segList = (Ao.ISegmentCollection) currentLine.Path;
				Ao.IEnumSegment enumSegments = segList.IndexedEnumSegments[QueryBox.Value];

				Ao.ISegment segment;
				int partIndex = 0;
				int segmentIndex = 0;
				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				while (segment != null)
				{
					if (xMax <= searchX)
					{
						side = 0;
						return nearestLine;
					}

					segment.QueryFromPoint(FromPointTemplate.Value);
					segment.QueryToPoint(ToPointTemplate.Value);

					double fromX;
					double fromY;
					FromPointTemplate.Value.QueryCoords(out fromX, out fromY);

					double toX;
					double toY;
					ToPointTemplate.Value.QueryCoords(out toX, out toY);

					if (fromY == searchY)
					{
						if (toY == searchY)
						{
							if (fromX < searchX != toX < searchX)
							{
								side = 0;
								return currentLine;
							}

							if (fromX < xMax)
							{
								side = 0;
								nearestLine = currentLine;
								xMax = fromX;
							}
							else if (toX < xMax)
							{
								side = 0;
								nearestLine = currentLine;
								xMax = fromX;
							}
						}
						else if (NewLineSide(searchPoint, ref xMax,
						                     FromPointTemplate.Value, ToPointTemplate.Value, true,
						                     ref x1Nearest, ref y1Nearest, ref side))
						{
							nearestLine = currentLine;
						}
					}
					else if (toY == searchY)
					{
						const bool startOnLine = false;
						if (NewLineSide(searchPoint, ref xMax,
						                ToPointTemplate.Value, FromPointTemplate.Value, startOnLine,
						                ref x1Nearest, ref y1Nearest, ref side))
						{
							nearestLine = currentLine;
						}
					}
					else if (fromY < searchY != toY < searchY)
					{
						double x = fromX + (toX - fromX) * (searchY - fromY) / (toY - fromY);
						if (CheckNewMax(x, searchX, ref xMax))
						{
							nearestLine = currentLine;
							side = Comparer<double>.Default.Compare(fromY, toY);
						}
					}

					Marshal.ReleaseComObject(segment);
					enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
				}

				searchBox.Max.X = xMax;
			}

			return nearestLine;
		}

		private static bool NewLineSide([NotNull] Ao.IPoint p,
		                                ref double xMax,
		                                [NotNull] Ao.IPoint onLine,
		                                [NotNull] Ao.IPoint offLine,
		                                bool startOnLine,
		                                ref double nearX, ref double nearY,
		                                ref int onRightSide)
		{
			if (onLine.X < p.X || onLine.X > xMax)
			{
				return false;
			}

			if (nearY == 0 || onLine.X < xMax)
			{
				nearX = offLine.X - onLine.X;
				nearY = offLine.Y - onLine.Y;
				xMax = onLine.X;
				onRightSide = ((nearY < 0) == startOnLine)
					              ? 1
					              : -1;
				return true;
			}

			if ((nearY > 0) == (offLine.Y > p.Y))
			{
				double nX = offLine.X - onLine.X;
				double nY = offLine.Y - onLine.Y;
				if ((nX * nearY - nY * nearX < 0) == (nY > 0))
				{
					nearX = nX;
					nearY = nY;
					onRightSide = ((nearY < 0) == startOnLine)
						              ? 1
						              : -1;
					return true;
				}
			}

			return false;
		}

		private static bool CheckNewMax(double x, double xMin, ref double xMax)
		{
			if (x < xMin)
			{
				return false;
			}

			if (x > xMax)
			{
				return false;
			}

			xMax = x;
			return true;
		}

		#region Nested type: BoxComparer

		private class BoxComparer : IComparer<BoxTree<TopologicalLine>.TileEntry>,
		                            IComparer<BoxTree.TileEntry>
		{
			public int Compare(BoxTree<TopologicalLine>.TileEntry x,
			                   BoxTree<TopologicalLine>.TileEntry y)
			{
				return CompareCore(x, y);
			}

			public int Compare(BoxTree.TileEntry x, BoxTree.TileEntry y)
			{
				return CompareCore(x, y);
			}

			private int CompareCore(BoxTree.TileEntry x, BoxTree.TileEntry y)
			{
				return Comparer<double>.Default.Compare(x.Box.Max.X, y.Box.Max.X);
			}
		}

		#endregion

		#region Nested type: DirRowComparer

		private class DirRowComparer : IComparer<TDirectedRow>
		{
			#region IComparer<DirectedRow> Members

			public int Compare(TDirectedRow x, TDirectedRow y)
			{
				x.TopologicalLine.Path.QueryEnvelope(QueryX.Value);
				y.TopologicalLine.Path.QueryEnvelope(QueryY.Value);
				// sort descending -> y before x
				return Comparer<double>.Default.Compare(QueryY.Value.XMax, QueryX.Value.XMax);
			}

			#endregion
		}

		#endregion

		#region Nested type: Tree

		private class Tree : BoxTree<TopologicalLine>
		{
			private const int _dimension = 2;
			private const int _maximumElementCountPerTile = 64; // TODO revise
			private const bool _dynamic = true;
			private readonly BoxComparer _comparer;
			private bool _excludeEqual;

			public Tree(BoxComparer comparer) : base(_dimension,
			                                         _maximumElementCountPerTile,
			                                         _dynamic)
			{
				_comparer = comparer;
			}

			public bool ExcludeEqual
			{
				set { _excludeEqual = value; }
			}

			protected override IEnumerator<BoxTree.TileEntry> GetTileEnumerator(
				BoxTree.TileEntryEnumerator enumerator,
				IEnumerable<BoxTree.TileEntry> list)
			{
				return new _Enumerator(enumerator, list, _comparer, _excludeEqual);
			}

			#region Nested type: _Enumerator

			private class _Enumerator : IEnumerator<BoxTree.TileEntry>
			{
				private readonly BoxComparer _comparer;
				private readonly List<BoxTree.TileEntry> _elemList;
				private readonly int _nElems;
				private readonly BoxTree.TileEntryEnumerator _enum;
				private readonly bool _excludeEqual;
				private BoxTree.TileEntry _current;
				private int _iPos;

				public _Enumerator(BoxTree.TileEntryEnumerator enumerator,
				                   IEnumerable<BoxTree.TileEntry> elemList, BoxComparer comparer,
				                   bool excludeEqual)
				{
					_elemList = new List<BoxTree.TileEntry>(elemList);
					_elemList.Sort(comparer);

					_nElems = _elemList.Count;
					_enum = enumerator;
					_comparer = comparer;
					_excludeEqual = excludeEqual;

					Reset();
				}

				#region IEnumerator<BoxTree<TopologicalLine>.TileEntry> Members

				public BoxTree.TileEntry Current
				{
					get { return _current; }
				}

				object IEnumerator.Current
				{
					get { return Current; }
				}

				public void Dispose() { }

				public bool MoveNext()
				{
					if (_iPos < _nElems)
					{
						_current = _elemList[_iPos];
					}
					else
					{
						_current = null;
						return false;
					}

					_iPos++;
					return true;
				}

				public void Reset()
				{
					IBox box = _enum.SearchBox.Clone();
					box.Max.X = box.Min.X;
					var entry = new TileEntry(box, null);
					_iPos = _elemList.BinarySearch(entry, _comparer);

					if (_iPos < 0)
					{
						_iPos = ~_iPos;
					}
					else
					{
						if (_excludeEqual)
						{
							_iPos++;
							while (_iPos < _nElems &&
							       _comparer.Compare(_elemList[_iPos], entry) == 0)
							{
								_iPos++;
							}
						}
						else
						{
							_iPos--;
							while (_iPos >= 0 &&
							       _comparer.Compare(_elemList[_iPos], entry) == 0)
							{
								_iPos--;
							}

							_iPos++;
						}
					}
				}

				#endregion
			}

			#endregion
		}

		#endregion
	}
}
