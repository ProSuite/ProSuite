using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class LineList
	{
		[CLSCompliant(false)] protected static readonly IEnvelope QueryEnvelope =
			new EnvelopeClass();
	}

	[CLSCompliant(false)]
	public class LineList<TDirectedRow> : LineList where TDirectedRow : ILineDirectedRow
	{
		private readonly PathRowComparer _pathRowComparer;

		private readonly List<LineList<TDirectedRow>> _innerRings;
		[NotNull] private readonly LinkedList<TDirectedRow> _directedRows;
		private bool _hasEquals; // has equal 

		#region Constructors

		public LineList([NotNull] TDirectedRow row0,
		                [NotNull] PathRowComparer pathRowLineComparer)
		{
			_innerRings = new List<LineList<TDirectedRow>>();
			_directedRows = new LinkedList<TDirectedRow>();

			_directedRows.AddFirst(row0);

			_pathRowComparer = pathRowLineComparer;
		}

		public LineList([NotNull] TDirectedRow row0,
		                [NotNull] TDirectedRow row1,
		                [NotNull] PathRowComparer pathRowLineComparer)
		{
			_innerRings = new List<LineList<TDirectedRow>>();
			_directedRows = new LinkedList<TDirectedRow>();

			_directedRows.AddFirst(row0);
			_directedRows.AddLast(row1);

			if (pathRowLineComparer.Equals(row0, row1))
			{
				_hasEquals = true;
			}

			_pathRowComparer = pathRowLineComparer;
		}

		#endregion

		[CLSCompliant(false)]
		public IPoint FromPoint
		{
			get { return _directedRows.First.Value.FromPoint; }
		}

		[CLSCompliant(false)]
		public IPoint ToPoint
		{
			get { return _directedRows.Last.Value.ToPoint; }
		}

		[NotNull]
		public LinkedList<TDirectedRow> DirectedRows
		{
			get { return _directedRows; }
		}

		public bool IsClosed
		{
			get { return ((IRelationalOperator) FromPoint).Equals(ToPoint); }
		}

		[CLSCompliant(false)]
		[NotNull]
		public IList<IRow> GetUniqueRows(IList<ITable> tableIndexTables)
		{
			var sortList = new List<ITableIndexRow>(_directedRows.Count);

			foreach (TDirectedRow row in _directedRows)
			{
				sortList.Add(row.Row);
			}

			sortList.Sort(_pathRowComparer.RowComparer);
			var uniqueList = new List<IRow>(_directedRows.Count);

			ITableIndexRow previousRow = null;
			foreach (ITableIndexRow row in sortList)
			{
				//if (row0 == null || RowComparer.Compare(row0, row) != 0)
				if (previousRow != null && _pathRowComparer.RowComparer.Equals(row, previousRow))
				{
					continue;
				}

				uniqueList.Add(row.GetRow(tableIndexTables));
				previousRow = row;
			}

			return uniqueList;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>clockwise = 1, counterclockwise = -1, not closed or empty = 0</returns>
		public int Orientation()
		{
			if (_directedRows.Count == 0)
			{
				return 0;
			}

			if (((IRelationalOperator) FromPoint).Equals(ToPoint) == false)
			{
				return 0;
			}

			if (_hasEquals)
			{
				LineList<TDirectedRow> list = GetAnyRing();

				return list == null
					       ? 0
					       : list.Orientation();
			}

			double xMax = 0;
			LinkedListNode<TDirectedRow> nodeMax = null;
			LinkedListNode<TDirectedRow> node = _directedRows.First;

			while (node != null)
			{
				node.Value.QueryEnvelope(QueryEnvelope);
				if (nodeMax == null || QueryEnvelope.XMax > xMax)
				{
					xMax = QueryEnvelope.XMax;
					nodeMax = node;
				}

				node = node.Next;
			}

			if ((nodeMax != null) == false)
			{
				throw new InvalidProgramException("Error in software design assumption");
			}

			int orientation = nodeMax.Value.Orientation;
			if (orientation == 0)
			{
				return 0;
			}

			if (nodeMax.Value.IsBackward)
			{
				orientation = -orientation;
			}

			if (Math.Abs(orientation) > 1)
			{
				double angle0;
				double angle1;
				if (orientation < 0)
				{
					angle1 = nodeMax.Value.FromAngle;
					node = PreviousDirNode(nodeMax);
					angle0 = node.Value.ToAngle;
				}
				else
				{
					angle0 = nodeMax.Value.ToAngle;
					node = NextDirNode(nodeMax);
					angle1 = node.Value.FromAngle;
				}

				AssertAngle(Math.Abs(angle0), Relation.LargerEqual, Math.PI / 2);
				AssertAngle(Math.Abs(angle1), Relation.LargerEqual, Math.PI / 2);

				double dAngle = angle1 - angle0;
				if (dAngle == 0)
				{
					return 0;
				}

				if (dAngle < 0)
				{
					dAngle += Math.PI * 2;
				}

				// avoid numerical problems
				if (angle0 > 0)
				{
					if (dAngle < 4) // exactly 3 * Math.PI / 2
					{
						AssertAngle(dAngle, Relation.Smaller, 2 * Math.PI + 0.1);
						orientation = 1;
					}
					else
					{
						AssertAngle(dAngle, Relation.Larger, 3 * Math.PI / 2 - 0.1);
						orientation = -1;
					}
				}
				else
				{
					if (dAngle < 2) // exactly Math.PI / 2
					{
						AssertAngle(dAngle, Relation.Smaller, Math.PI / 2 + 0.1);
						orientation = 1;
					}
					else
					{
						AssertAngle(dAngle, Relation.Larger, Math.PI - 0.1);
						orientation = -1;
					}
				}
			}

			return orientation;
		}

		[NotNull]
		public List<TDirectedRow> GetEnds()
		{
			var result = new List<TDirectedRow>();
			var comparer = new PathRowComparer(new TableIndexRowComparer());
			TDirectedRow row0 = DirectedRows.Last.Value;
			foreach (TDirectedRow row1 in DirectedRows)
			{
				if (comparer.Compare(row0, row1) == 0)
				{
					result.Add(row0);
				}

				row0 = row1;
			}

			return result;
		}

		private static void AssertAngle(double value, Relation relation, double limit)
		{
			switch (relation)
			{
				case Relation.Smaller:
					if (value < limit)
					{
						return;
					}

					break;

				case Relation.Larger:
					if (value > limit)
					{
						return;
					}

					break;

				case Relation.LargerEqual:
					if (value >= limit)
					{
						return;
					}

					break;

				default:
					throw new NotImplementedException("Unhandled Relation " + relation);
			}

			throw new InvalidProgramException(
				string.Format("Error in software design assumption: {0} {1} {2} is false",
				              value, relation, limit));
		}

		[NotNull]
		private LinkedListNode<TDirectedRow> NextDirNode(
			[NotNull] LinkedListNode<TDirectedRow> node)
		{
			return node.Next ?? _directedRows.First;
		}

		[NotNull]
		private LinkedListNode<TDirectedRow> PreviousDirNode(
			[NotNull] LinkedListNode<TDirectedRow> node)
		{
			return node.Previous ?? _directedRows.Last;
		}

		[CanBeNull]
		public LineList<TDirectedRow> RemoveEnds()
		{
			if (! (((IRelationalOperator) FromPoint).Equals(ToPoint)))
			{
				throw new InvalidOperationException(
					"invalid context for calling of method");
			}

			if (_directedRows.Count == 1)
			{
				return new LineList<TDirectedRow>(_directedRows.First.Value, _pathRowComparer);
			}

			LinkedListNode<TDirectedRow> currNodeAll = _directedRows.First;
			TDirectedRow dir0 = currNodeAll.Value;
			LineList<TDirectedRow> removedList = null;

			while (currNodeAll.Next != null)
			{
				currNodeAll = currNodeAll.Next;
				TDirectedRow dir1 = currNodeAll.Value;

				if (dir0 == null)
				{
					dir0 = dir1;
				}
				else if (! _pathRowComparer.Equals(dir0, dir1)) // dir0.TopoLine != dir1.TopoLine)
				{
					if (removedList == null)
					{
						removedList = new LineList<TDirectedRow>(dir0, dir1, _pathRowComparer);
					}
					else
					{
						const bool inFront = false;
						removedList.AddRow(dir1, inFront);
					}

					dir0 = dir1;
				}
				else if (removedList != null)
				{
					if (removedList._directedRows.Count > 0)
					{
						removedList._directedRows.RemoveLast();
					}

					if (removedList._directedRows.Count == 0)
					{
						removedList = null;
						dir0 = default(TDirectedRow);
					}
					else
					{
						dir0 = removedList._directedRows.Last.Value;
					}
				}
				else
				{
					dir0 = default(TDirectedRow);
				}
			}

			while (removedList != null &&
			       _pathRowComparer.Equals(removedList._directedRows.First.Value,
			                               removedList._directedRows.Last.Value))
				//removedList._directedRows.First.Value.TopoLine ==
				//removedList._directedRows.Last.Value.TopoLine)
			{
				removedList._directedRows.RemoveFirst();
				if (removedList._directedRows.Count > 0)
				{
					removedList._directedRows.RemoveLast();
				}

				if (removedList._directedRows.Count == 0)
				{
					removedList = null;
				}
			}

			return removedList;
		}

		[CanBeNull]
		private LineList<TDirectedRow> GetAnyRing()
		{
			if (! (((IRelationalOperator) FromPoint).Equals(ToPoint)))
			{
				throw new InvalidOperationException(
					"invalid context for calling of method");
			}

			if (_directedRows.Count == 1)
			{
				return new LineList<TDirectedRow>(_directedRows.First.Value, _pathRowComparer);
			}

			var rowsDict =
				new Dictionary<IDirectedRow, List<IDirectedRow>>(_pathRowComparer);

			foreach (TDirectedRow directedRow in _directedRows)
			{
				IDirectedRow row = directedRow;
				List<IDirectedRow> equalBaseRows;

				if (! rowsDict.TryGetValue(row, out equalBaseRows))
				{
					equalBaseRows = new List<IDirectedRow>(2);
					rowsDict.Add(row, equalBaseRows);
				}

				equalBaseRows.Add(directedRow);
			}

			LineList<TDirectedRow> anyRing = null;
			IDirectedRow startBranch = null;
			for (LinkedListNode<TDirectedRow> currentNode = _directedRows.First;
			     currentNode != null;
			     currentNode = currentNode.Next)
			{
				List<IDirectedRow> equalBaseRows = rowsDict[currentNode.Value];
				if (equalBaseRows.Count > 1)
				{
					if (anyRing == null)
					{
						continue;
					}

					if (startBranch == null)
					{
						startBranch = equalBaseRows[0];
						continue;
					}

					if (startBranch == equalBaseRows[0])
					{
						startBranch = null;
						continue;
					}
				}

				if (anyRing == null)
				{
					anyRing = new LineList<TDirectedRow>(currentNode.Value, _pathRowComparer);
				}
				else if (startBranch == null)
				{
					anyRing.AddRow(currentNode.Value, false);
				}
			}

			return anyRing;
		}

		public void AddInnerRing([NotNull] LineList<TDirectedRow> innerRing)
		{
			_innerRings.Add(innerRing);
		}

		public override bool Equals(object obj)
		{
			var lc = obj as LineList<TDirectedRow>;
			if (lc == null)
			{
				return false;
			}

			return _directedRows.Equals(lc._directedRows);
		}

		// to make the compiler happy
		public override int GetHashCode()
		{
			return _innerRings.GetHashCode() + 29 * _directedRows.GetHashCode();
		}

		/// <summary>
		/// Get Border of polygon (only outer ring!)
		/// </summary>
		/// <returns></returns>
		[NotNull]
		[CLSCompliant(false)]
		public IPolyline GetBorder()
		{
			IPolyline border;
			if (_directedRows.First != null)
			{
				ICurve template = _directedRows.First.Value.GetBaseLine();
				border = QaGeometryUtils.CreatePolyline(template);
			}
			else
			{
				border = new PolylineClass();
			}

			var segments = (ISegmentCollection) border;

			foreach (TDirectedRow directedRow in _directedRows)
			{
				ISegmentCollection nextPart = directedRow.GetDirectedSegmentCollection();

				segments.AddSegmentCollection(nextPart);
			}

			((ITopologicalOperator) border).Simplify();

			return border;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IPolygon GetPolygon()
		{
			IPolygon poly = CombineRings();
			((ITopologicalOperator) poly).Simplify();
			return poly;
		}

		[NotNull]
		private IPolygon CombineRings()
		{
			IPolygon polygon = QaGeometryUtils.CreatePolygon(
				_directedRows.First.Value.GetBaseLine());

			var rings = (IGeometryCollection) polygon;

			IRing ring = new RingClass();
			var ringSegments = (ISegmentCollection) ring;

			JoinRows(ringSegments);
			object missing = Type.Missing;

			ring.Close();
			rings.AddGeometry(ring, ref missing, ref missing);

			foreach (LineList<TDirectedRow> lc in _innerRings)
			{
				rings.AddGeometry(
					((IGeometryCollection) lc.CombineRings()).Geometry[0], ref missing,
					ref missing);
			}

			((ITopologicalOperator) polygon).Simplify();
			return polygon;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IPolyline GetPolyline()
		{
			IPolyline polyline =
				QaGeometryUtils.CreatePolyline(_directedRows.First.Value.GetBaseLine());

			JoinRows((ISegmentCollection) polyline);
			((ITopologicalOperator) polyline).Simplify();
			return polyline;
		}

		private void JoinRows([NotNull] ISegmentCollection segments)
		{
			object missing = Type.Missing;

			foreach (TDirectedRow directedRow in _directedRows)
			{
				ISegmentCollection nextPart = directedRow.GetDirectedSegmentCollection();

				((IPointCollection) segments).AddPoint(((ICurve) nextPart).FromPoint,
				                                       ref missing,
				                                       ref missing);

				segments.AddSegmentCollection(nextPart);
			}
		}

		public LinkedListNode<TDirectedRow> AddRow(
			[NotNull] TDirectedRow directedRow,
			bool inFront)
		{
			LinkedListNode<TDirectedRow> added;
			if (inFront)
			{
				if (_hasEquals == false &&
				    _pathRowComparer.Equals(_directedRows.First.Value, directedRow))
					//				    _directedRows.First.Value.TopoLine == directedRow.TopoLine)
				{
					_hasEquals = true;
				}

				added = _directedRows.AddFirst(directedRow);
			}
			else
			{
				if (_hasEquals == false &&
				    _pathRowComparer.Equals(_directedRows.Last.Value, directedRow))
					//				    _directedRows.Last.Value.TopoLine == directedRow.TopoLine)
				{
					_hasEquals = true;
				}

				added = _directedRows.AddLast(directedRow);
			}

			return added;
		}

		public void AddCollection([NotNull] LineList<TDirectedRow> list)
		{
			if (! _hasEquals && (list._hasEquals ||
			                     _pathRowComparer.Equals(_directedRows.Last.Value,
			                                             list._directedRows.First.Value)))
				//_directedRows.Last.Value.TopoLine ==
				//list._directedRows.First.Value.TopoLine))
			{
				_hasEquals = true;
			}

			foreach (TDirectedRow row in list._directedRows)
			{
				_directedRows.AddLast(row);
			}
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public IEnvelope Envelope()
		{
			IEnvelope envelope = null;
			foreach (TDirectedRow directedRow in _directedRows)
			{
				directedRow.QueryEnvelope(QueryEnvelope);

				if (envelope == null)
				{
					envelope = GeometryFactory.Clone(QueryEnvelope);
				}
				else
				{
					envelope.Union(QueryEnvelope);
				}
			}

			return envelope;
		}

		internal void Completed()
		{
			if (_hasEquals == false && _directedRows.Count > 1 &&
			    _pathRowComparer.Equals(_directedRows.First.Value, _directedRows.Last.Value))
				//_directedRows.First.Value.TopoLine == _directedRows.Last.Value.TopoLine)
			{
				_hasEquals = true;
			}
		}

		#region Nested type: Relation

		private enum Relation
		{
			Smaller,
			Larger,
			LargerEqual
		}

		#endregion
	}
}
