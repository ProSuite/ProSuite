using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;
using ProSuite.QA.Container.TestSupport;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Network
{
	/// <summary>
	/// Check if there is always exactly one outgoing vertex
	/// </summary>
	public abstract class QaNetworkBase : ContainerTest
	{
		private readonly bool _includeBorderNodes;

		private List<NetElement> _netCache;

		[NotNull] private readonly IPoint _queryPnt = new PointClass();
		[NotNull] private readonly IEnvelope _queryEnv = new EnvelopeClass();

		private IList<ISpatialFilter> _filters;
		private IList<QueryFilterHelper> _helpers;

		#region Constructors

		protected QaNetworkBase([NotNull] IFeatureClass polylineClass,
		                        bool includeBorderNodes)
			: this(CastToTables(polylineClass), includeBorderNodes) { }

		protected QaNetworkBase([NotNull] IEnumerable<ITable> featureClasses,
		                        bool includeBorderNodes,
		                        [CanBeNull] IList<int> nonNetworkClassIndexList = null)
			: this(featureClasses, 0, includeBorderNodes, nonNetworkClassIndexList) { }

		protected QaNetworkBase([NotNull] IEnumerable<ITable> featureClasses,
		                        double tolerance,
		                        bool includeBorderNodes,
		                        [CanBeNull] IList<int> nonNetworkClassIndexList)
			: base(featureClasses)
		{
			Tolerance = tolerance;
			_includeBorderNodes = includeBorderNodes;
			NonNetworkClassIndexList = nonNetworkClassIndexList ?? new List<int>();

			KeepRows = true;
			SearchDistance = Tolerance;

			UseMultiParts = true;
		}

		#endregion

		public bool UseMultiParts { get; set; }

		[CanBeNull]
		public List<List<DirectedRow>> ConnectedLinesList { get; private set; }

		[CanBeNull]
		public List<List<NetElement>> ConnectedElementsList { get; private set; }

		protected double Tolerance { get; }

		[NotNull]
		protected IList<int> NonNetworkClassIndexList { get; }

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			if (NonNetworkClassIndexList.Contains(tableIndex))
			{
				return 0;
			}

			if (_netCache == null)
			{
				_netCache = new List<NetElement>();
				ConnectedLinesList = new List<List<DirectedRow>>();
				ConnectedElementsList = new List<List<NetElement>>();
			}

			AddNetElements(new TableIndexRow(row, tableIndex), _netCache);

			return 0;
		}

		private void AddNetElements([NotNull] TableIndexRow row,
		                            [NotNull] List<NetElement> netElems)
		{
			IGeometry geom = ((IFeature) row.Row).Shape;

			switch (geom.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
					netElems.Add(new NetPoint(row));
					break;

				case esriGeometryType.esriGeometryPolyline:

					foreach (DirectedRow directedRow in GetDirectedRows(row))
					{
						netElems.Add(directedRow);
						netElems.Add(directedRow.Reverse());
					}

					break;

				default:
					throw new ArgumentException("Invalid geometry type " + geom.GeometryType);
			}
		}

		protected IEnumerable<DirectedRow> GetDirectedRows(TableIndexRow row)
		{
			IGeometry geom = ((IFeature) row.Row).Shape;
			var paths = (IGeometryCollection) geom;

			if (! UseMultiParts)
			{
				yield return new DirectedRow(row, -1, isBackward: false);
				yield break;
			}

			int pathCount = paths.GeometryCount;
			for (var iPath = 0; iPath < pathCount; iPath++)
			{
				yield return new DirectedRow(row, iPath, isBackward: false);
			}
		}

		protected override int CompleteTileCore(TileInfo args)
		{
			if (args.State == TileState.Initial)
			{
				_netCache?.Clear();
				ConnectedLinesList?.Clear();
				ConnectedElementsList?.Clear();
				return 0;
			}

			if (_netCache == null)
			{
				return 0;
			}

			ConnectedLinesList?.Clear();
			ConnectedElementsList?.Clear();

			IEnvelope verificationBox = Assert.NotNull(args.AllBox, "AllBox");
			WKSEnvelope verificationEnvelope;
			verificationBox.QueryWKSCoords(out verificationEnvelope);

			IEnvelope tileBox = Assert.NotNull(args.CurrentEnvelope, "CurrentEnvelope");
			WKSEnvelope tileEnvelope;
			tileBox.QueryWKSCoords(out tileEnvelope);

			BuildToleranceCache(tileEnvelope);

			BuildNet(verificationEnvelope, tileEnvelope, Tolerance);
			_netCache.Clear();

			return 0;
		}

		private void BuildNet(WKSEnvelope verificationEnvelope,
		                      WKSEnvelope tileEnvelope,
		                      double tolerance)
		{
			var xElements = new List<NetElementXY>(_netCache.Count);
			foreach (NetElement elem in _netCache)
			{
				var elemXY = new NetElementXY(elem, _queryPnt);
				xElements.Add(elemXY);
			}

			SortElements(xElements, tolerance,
			             neigbors => AddElements(neigbors, verificationEnvelope, tileEnvelope));
		}

		protected void SortElements<T>([NotNull] List<T> xElements, double tolerance,
		                               [NotNull] Action<List<T>> addAction)
			where T : INetElementXY

		{
			double tolerance2 = tolerance * tolerance;

			var cmpX = new ComparerX<T>();
			xElements.Sort(cmpX);

			int elemsCount = xElements.Count;

			for (var iX = 0; iX < elemsCount; iX++)
			{
				T elem = xElements[iX];
				if (elem.Handled)
				{
					continue;
				}

				var neighbors = new List<T>();
				elem.Handled = true;
				neighbors.Add(elem);

				double x = elem.X;
				double y = elem.Y;

				double xMax = x + tolerance;
				int iNeighbor = iX;
				double xNeighbor = x;
				while (iNeighbor + 1 < elemsCount && xNeighbor <= xMax)
				{
					iNeighbor++;
					T neighbor = xElements[iNeighbor];
					xNeighbor = neighbor.X;
					if (neighbor.Handled)
					{
						continue;
					}

					if (xNeighbor > xMax)
					{
						continue;
					}

					double dx = xNeighbor - x;
					double dy = neighbor.Y - y;
					double r2 = dx * dx + dy * dy;
					if (r2 <= tolerance2)
					{
						neighbor.Handled = true;
						if (r2 > 0)
						{
							//							neighbor.Element.SetNetPoint(x, y);
						}

						neighbors.Add(neighbor);
					}
				}

				addAction(neighbors);
			}
		}

		private void InitFilters()
		{
			if (_filters != null)
			{
				return;
			}

			CopyFilters(out _filters, out _helpers);
			foreach (QueryFilterHelper helper in _helpers)
			{
				helper.ForNetwork = true;
			}

			foreach (ISpatialFilter filter in _filters)
			{
				filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
			}
		}

		private void BuildToleranceCache(WKSEnvelope tileEnvelope)
		{
			if (Tolerance <= 0)
			{
				return;
			}

			InitFilters();

			_netCache.Clear();
			_queryEnv.PutCoords(tileEnvelope.XMin - Tolerance, tileEnvelope.YMin - Tolerance,
			                    tileEnvelope.XMax + Tolerance, tileEnvelope.YMax + Tolerance);

			for (var iTable = 0; iTable < InvolvedTables.Count; iTable++)
			{
				if (NonNetworkClassIndexList.Contains(iTable))
				{
					continue;
				}

				_filters[iTable].Geometry = _queryEnv;
				foreach (
					IRow feature in
					Search(InvolvedTables[iTable], _filters[iTable], _helpers[iTable]))
				{
					AddNetElements(new TableIndexRow(feature, iTable), _netCache);
				}
			}
		}

		private void AddElements([CanBeNull] IList<NetElementXY> netElems,
		                         WKSEnvelope verificationEnvelope,
		                         WKSEnvelope tileEnvelope)
		{
			if (netElems == null || netElems.Count == 0)
			{
				return;
			}

			if (! HandleInCurrentTile(netElems[0], verificationEnvelope, tileEnvelope))
			{
				// Border object, handled in other tile
				return;
			}

			var lines = new List<DirectedRow>(netElems.Count);
			var elems = new List<NetElement>(netElems.Count);

			foreach (NetElementXY elem in netElems)
			{
				if (elem.Element is DirectedRow)
				{
					lines.Add((DirectedRow) elem.Element);
				}

				elems.Add(elem.Element);
			}

			Assert.NotNull(ConnectedLinesList).Add(lines);
			Assert.NotNull(ConnectedElementsList).Add(elems);
		}

		private bool HandleInCurrentTile([NotNull] NetElementXY netElem,
		                                 WKSEnvelope verificationEnvelope,
		                                 WKSEnvelope tileEnvelope)
		{
			double x = netElem.X;
			double y = netElem.Y;

			if (Math.Abs(verificationEnvelope.XMin - tileEnvelope.XMin) < double.Epsilon)
			{
				if (_includeBorderNodes == false &&
				    x < verificationEnvelope.XMin)
				{
					return false;
				}
			}
			else if (x <= tileEnvelope.XMin)
			{
				// the point was handled in a previous tile
				return false;
			}

			if (Math.Abs(tileEnvelope.XMax - verificationEnvelope.XMax) < double.Epsilon)
			{
				if (_includeBorderNodes == false &&
				    x > verificationEnvelope.XMax)
				{
					return false;
				}
			}
			else if (x > tileEnvelope.XMax)
			{
				// the point will be handled in a following tile
				return false;
			}

			if (Math.Abs(verificationEnvelope.YMin - tileEnvelope.YMin) < double.Epsilon)
			{
				if (_includeBorderNodes == false &&
				    y < verificationEnvelope.YMin)
				{
					return false;
				}
			}
			else if (y <= tileEnvelope.YMin)
			{
				// the point was handled in a previous tile
				return false;
			}

			if (Math.Abs(tileEnvelope.YMax - verificationEnvelope.YMax) < double.Epsilon)
			{
				if (_includeBorderNodes == false &&
				    y > verificationEnvelope.YMax)
				{
					return false;
				}
			}
			else if (y > tileEnvelope.YMax)
			{
				// the point will be handled in a following tile
				return false;
			}

			return true;
		}

		[NotNull]
		protected static IEnumerable<InvolvedRow> GetInvolvedRows(
			[NotNull] ICollection<IRow> connectedRows)
		{
			var result = new List<InvolvedRow>(connectedRows.Count);

			foreach (IRow row in connectedRows)
			{
				result.Add(new InvolvedRow(row));
			}

			return result;
		}

		[NotNull]
		protected IList<InvolvedRow> GetInvolvedRows([NotNull] ITableIndexRow tableIndexRow)
		{
			IRow row = tableIndexRow.GetRow(InvolvedTables);
			RelatedTables related = GetRelatedTables(row);

			return related?.GetInvolvedRows(row) ?? new[] {new InvolvedRow(row)};
		}

		#region nested classes

		protected interface INetElementXY
		{
			double X { get; }
			double Y { get; }
			bool Handled { get; set; }
		}

		private class NetElementXY : INetElementXY
		{
			public NetElement Element { get; }
			public double X { get; }
			public double Y { get; }
			public bool Handled { get; set; }

			public NetElementXY([NotNull] NetElement element, [NotNull] IPoint pointTemplate)
			{
				Element = element;
				IPoint point = element.QueryNetPoint(pointTemplate);

				double x, y;
				point.QueryCoords(out x, out y);
				X = x;
				Y = y;
			}

			public override string ToString()
			{
				return string.Format("{0},{1}; {2}", X, Y, Element);
			}
		}

		private class ComparerX<T> : IComparer<T> where T : INetElementXY
		{
			public int Compare(T x, T y)
			{
				if (x == null)
				{
					if (y == null)
					{
						return 0;
					}

					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				return x.X.CompareTo(y.X);
			}
		}

		#endregion
	}
}
