using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	[CLSCompliant(false)]
	public class RingGrower<TDirectedRow> where TDirectedRow : class, ILineDirectedRow
	{
		private readonly Func<TDirectedRow, TDirectedRow> _revertFunc;
		public event GeometryCompletedEventHandler<TDirectedRow> GeometryCompleted;

		private const double _tolerance = 1e-5;

		private readonly PathRowComparer _pathRowComparer;
		private readonly DirectedRowComparer _directedPartComparer;
		private readonly SortedDictionary<IDirectedRow, LineList<TDirectedRow>> _endRows;
		private readonly SortedDictionary<IDirectedRow, LineList<TDirectedRow>> _startRows;

		[CLSCompliant(false)]
		public RingGrower([NotNull] Func<TDirectedRow, TDirectedRow> revertFunc)
		{
			_revertFunc = revertFunc;
			_pathRowComparer = new PathRowComparer(new TableIndexRowComparer());

			_directedPartComparer = new DirectedRowComparer(_pathRowComparer.RowComparer);
			_startRows =
				new SortedDictionary<IDirectedRow, LineList<TDirectedRow>>(_directedPartComparer);
			_endRows =
				new SortedDictionary<IDirectedRow, LineList<TDirectedRow>>(_directedPartComparer);
		}

		public IEnumerable<LineList<TDirectedRow>> GetLineLists()
		{
			foreach (LineList<TDirectedRow> lines in _endRows.Values)
			{
				yield return lines;
			}
		}

		[NotNull]
		public LineList<TDirectedRow> Add([NotNull] TDirectedRow row0,
		                                  [NotNull] TDirectedRow row1)
		{
			try
			{
				LineList<TDirectedRow> pre;
				LineList<TDirectedRow> post;

				if (_endRows.TryGetValue(row0, out pre))
				{
					_endRows.Remove(row0);
				}

				if (_startRows.TryGetValue(row1, out post))
				{
					_startRows.Remove(row1);
				}

				if (pre == null && post == null)
				{
					if (_directedPartComparer.Equals(row0, row1))
						//if (row0.TopoLine == row1.TopoLine && row0.IsBackward == row1.IsBackward)
					{
						pre = new LineList<TDirectedRow>(row0, _pathRowComparer);
						pre.Completed();
						OnClosing(pre);
						return pre;
					}

					pre = new LineList<TDirectedRow>(row0, row1, _pathRowComparer);
					_startRows.Add(row0, pre);
					_endRows.Add(row1, pre);

					return pre;
				}

				if (pre == null)
				{
					post.AddRow(row0, true);
					_startRows.Add(row0, post);

					return post;
				}

				if (post == null)
				{
					pre.AddRow(row1, false);
					_endRows.Add(row1, pre);

					return pre;
				}

				if (pre == post) // Polygon completed
				{
					pre.Completed();
					OnClosing(pre);

					return pre;
				}

				pre.AddCollection(post);
				IDirectedRow z = pre.DirectedRows.Last.Value;
				// updating _endRows, 

				Assert.True(z == post.DirectedRows.Last.Value, "unexpected directed row");
				Assert.True(_endRows[z] == post, "unexpected end rows");

				_endRows[z] = pre;

				return pre;
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException(
					$"Error adding pair 0(uniqueOID:{row0.Row.RowOID}, {row0.Row}); 1(uniqueOID:'{row1.Row.RowOID}', {row1.Row})",
					exception);
			}
		}

		private void OnClosing(LineList<TDirectedRow> closedPolygon)
		{
			if (GeometryCompleted != null)
			{
				GeometryCompleted(this, closedPolygon);
			}
		}

		[CLSCompliant(false)]
		public void ResolvePoint(IPoint point,
		                         [NotNull] List<TDirectedRow> intersectList,
		                         TDirectedRow row)
		{
			if (intersectList.Count > 1)
			{
				var comparer = (IComparer<TDirectedRow>) new DirectedRow.RowByLineAngleComparer();
				intersectList.Sort(comparer);
				int index = intersectList.BinarySearch(row);
				ResolvePoint(point, intersectList, index);
			}
			else if (intersectList.Count != 0)
			{
				ResolvePoint(point, intersectList, 0);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"> the point to be resolved</param>
		/// <param name="intersectList"> an array listing the lines to the point in counterclockwise order. first line appears twice</param>
		/// <param name="index"> the index of the current row in the array</param>
		private void ResolvePoint(IPoint point,
		                          [NotNull] IList<TDirectedRow> intersectList,
		                          int index)
		{
			if (intersectList.Count < 2)
			{
				Add(_revertFunc(intersectList[0]), intersectList[0]);
				return;
			}

			TDirectedRow middleRow = intersectList[index];

			TDirectedRow rightRow = index < intersectList.Count - 1
				                        ? intersectList[index + 1]
				                        : intersectList[0];

			TDirectedRow leftRow = index == 0
				                       ? intersectList[intersectList.Count - 1]
				                       : intersectList[index - 1];

			if (_pathRowComparer.Compare(middleRow, rightRow) < 0)
			{
				Add(_revertFunc(middleRow), rightRow);
			}

			if (_pathRowComparer.Compare(middleRow, leftRow) < 0)
			{
				Add(_revertFunc(leftRow), middleRow);
			}
		}

		[CLSCompliant(false)]
		[NotNull]
		public List<LineList<TDirectedRow>> GetAndRemoveCollectionsInside(
			[CanBeNull] IEnvelope envelope)
		{
			var insideList = new List<LineList<TDirectedRow>>();

			if (envelope == null || envelope.IsEmpty)
			{
				insideList.AddRange(_startRows.Select(pair => pair.Value));
			}
			else
			{
				double envXMin;
				double envYMin;
				double envXMax;
				double envYMax;
				envelope.QueryCoords(out envXMin, out envYMin,
				                     out envXMax, out envYMax);

				foreach (KeyValuePair<IDirectedRow, LineList<TDirectedRow>> pair in _startRows)
				{
					IEnvelope polyEnvelope = pair.Value.Envelope();
					Assert.NotNull(polyEnvelope, "polygon envelope is null");

					double polyXMin;
					double polyYMin;
					double polyXMax;
					double polyYMax;
					polyEnvelope.QueryCoords(out polyXMin, out polyYMin,
					                         out polyXMax, out polyYMax);

					if ((polyXMin + _tolerance >= envXMin &&
					     polyYMin + _tolerance >= envYMin &&
					     polyXMax - _tolerance <= envXMax &&
					     polyYMax - _tolerance <= envYMax))
					{
						insideList.Add(pair.Value);
					}
				}
			}

			foreach (LineList<TDirectedRow> poly in insideList)
			{
				_startRows.Remove(poly.DirectedRows.First.Value);
				_endRows.Remove(poly.DirectedRows.Last.Value);
			}

			return insideList;
		}
	}
}
