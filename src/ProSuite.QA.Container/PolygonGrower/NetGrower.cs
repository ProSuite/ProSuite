using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public class NetGrower<TDirectedRow> where TDirectedRow : class, INodesDirectedRow
	{
		public class GeometryCompleteEventArgs : EventArgs
		{
			private readonly NetGrower<TDirectedRow> _net;
			private readonly List<TDirectedRow> _netRows;

			public GeometryCompleteEventArgs(NetGrower<TDirectedRow> net,
			                                 List<TDirectedRow> netRows)
			{
				_net = net;
				_netRows = netRows;
			}

			public List<TDirectedRow> NetRows
			{
				get { return _netRows; }
			}

			public NetGrower<TDirectedRow> Net
			{
				get { return _net; }
			}
		}

		public event EventHandler<GeometryCompleteEventArgs> GeometryCompleted;

		public void Remove(List<TDirectedRow> group)
		{
			Dictionary<IDirectedRow, TDirectedRow> netEnds = _netEnds[group];
			foreach (IDirectedRow row in netEnds.Keys)
			{
				_endRowNetDict.Remove(row);
			}

			netEnds.Clear();
			_netEnds.Remove(group);
		}

		public void RemoveEnd(TDirectedRow endRow, bool removeInList)
		{
			List<TDirectedRow> net = _endRowNetDict[endRow];
			Dictionary<IDirectedRow, TDirectedRow> netEnds = _netEnds[net];
			netEnds.Remove(endRow);

			_endRowNetDict.Remove(endRow);

			if (removeInList)
			{
				foreach (TDirectedRow row in net)
				{
					if (_pathRowComparer.Equals(row, endRow))
					{
						net.Remove(row);
						break;
					}
				}
			}
		}

		[NotNull]
		public List<TDirectedRow> GetEndNodes(
			[NotNull] List<TDirectedRow> lineList)
		{
			var endNodes = new List<TDirectedRow>();
			foreach (TDirectedRow row in lineList)
			{
				if (row.FromNode.RowsCount == 1 ||
				    (row.ToNode != null && row.ToNode.RowsCount == 1))
				{
					endNodes.Add(row);
				}
			}

			return endNodes;
		}

		private readonly PathRowComparer _pathRowComparer;
		private readonly Dictionary<IDirectedRow, List<TDirectedRow>> _endRowNetDict;

		private readonly
			Dictionary<List<TDirectedRow>, Dictionary<IDirectedRow, TDirectedRow>> _netEnds;

		public NetGrower()
		{
			_pathRowComparer = new PathRowComparer(new TableIndexRowComparer());
			_endRowNetDict = new Dictionary<IDirectedRow, List<TDirectedRow>>(_pathRowComparer);
			_netEnds =
				new Dictionary<List<TDirectedRow>, Dictionary<IDirectedRow, TDirectedRow>>();
		}

		public int NetsCount
		{
			get { return _netEnds.Count; }
		}

		public IEnumerable<List<TDirectedRow>> GetNets()
		{
			return _netEnds.Keys;
		}

		public List<TDirectedRow> AddNode(NetNode<TDirectedRow> node)
		{
			var existingNets = new List<List<TDirectedRow>>();
			var existingEnds = new List<TDirectedRow>();
			var incomplete = new Dictionary<IDirectedRow, TDirectedRow>(_pathRowComparer);

			foreach (TDirectedRow directedRow in node.Rows)
			{
				List<TDirectedRow> net;
				if (_endRowNetDict.TryGetValue(directedRow, out net))
				{
					if (! existingNets.Contains(net))
					{
						existingNets.Add(net);
					}

					existingEnds.Add(directedRow);
				}
				else
				{
					TDirectedRow invers;
					if (! incomplete.TryGetValue(directedRow, out invers))
					{
						incomplete.Add(directedRow, directedRow);
					}
					else
					{
						invers.ToNode = directedRow.FromNode;
					}
				}
			}

			List<TDirectedRow> newNet;
			Dictionary<IDirectedRow, TDirectedRow> endRows;

			if (existingNets.Count == 0)
			{
				newNet = new List<TDirectedRow>();
				endRows = new Dictionary<IDirectedRow, TDirectedRow>(_pathRowComparer);
				_netEnds.Add(newNet, endRows);
			}
			else
			{
				newNet = existingNets[0];
				endRows = _netEnds[newNet];
				for (var i = 1; i < existingNets.Count; i++)
				{
					List<TDirectedRow> existingNet = existingNets[i];
					newNet.AddRange(existingNet);
					foreach (KeyValuePair<IDirectedRow, TDirectedRow> pair in _netEnds[existingNet]
					)
					{
						endRows.Add(pair.Key, pair.Value);
						_endRowNetDict[pair.Key] = newNet;
					}

					_netEnds.Remove(existingNet);
				}

				foreach (TDirectedRow existingEnd in existingEnds)
				{
					TDirectedRow endRow = endRows[existingEnd];
					endRow.ToNode = node;
					existingEnd.ToNode = endRow.FromNode;

					endRows.Remove(existingEnd);
					_endRowNetDict.Remove(existingEnd);
				}
			}

			var isIncomplete = false;
			foreach (TDirectedRow newEnd in incomplete.Values)
			{
				newNet.Add(newEnd);
				if (newEnd.ToNode == null)
				{
					isIncomplete = true;
					endRows.Add(newEnd, newEnd);
					_endRowNetDict.Add(newEnd, newNet);
				}
			}

			if (! isIncomplete && endRows.Count == 0)
			{
				OnClosing(newNet);
				_netEnds.Remove(newNet);
			}

			return newNet;
		}

		private void OnClosing(List<TDirectedRow> closedNet)
		{
			if (GeometryCompleted != null)
			{
				GeometryCompleted(this, new GeometryCompleteEventArgs(this, closedNet));
			}
		}
	}
}
