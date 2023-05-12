using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.PolygonGrower;

namespace ProSuite.QA.Tests.Network
{
	/// <summary>
	/// Check if there is always exactly one outgoing vertex
	/// </summary>
	public abstract class QaNetworkBase : ContainerTest
	{
		private readonly bool _includeBorderNodes;

		private NetworkBuilder _networkBuilder;

		[NotNull]
		private NetworkBuilder NetworkBuilder =>
			_networkBuilder ?? (_networkBuilder = new NetworkBuilder(_includeBorderNodes));

		[NotNull] private readonly IEnvelope _queryEnv = new EnvelopeClass();

		private IList<ISpatialFilter> _filters;
		private IList<QueryFilterHelper> _helpers;

		#region Constructors

		protected QaNetworkBase([NotNull] IReadOnlyFeatureClass polylineClass,
		                        bool includeBorderNodes)
			: this(CastToTables(polylineClass), includeBorderNodes) { }

		protected QaNetworkBase([NotNull] IEnumerable<IReadOnlyTable> featureClasses,
		                        bool includeBorderNodes,
		                        [CanBeNull] IList<int> nonNetworkClassIndexList = null)
			: this(featureClasses, 0, includeBorderNodes, nonNetworkClassIndexList) { }

		protected QaNetworkBase([NotNull] IEnumerable<IReadOnlyTable> featureClasses,
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

		public bool UseMultiParts
		{
			get => NetworkBuilder.UseMultiParts;
			set => NetworkBuilder.UseMultiParts = value;
		}

		[CanBeNull]
		public IReadOnlyList<List<DirectedRow>> ConnectedLinesList =>
			_networkBuilder?.ConnectedLinesList;

		[CanBeNull]
		public IReadOnlyList<List<NetElement>> ConnectedElementsList =>
			_networkBuilder?.ConnectedElementsList;

		protected double Tolerance { get; }

		[NotNull]
		protected IList<int> NonNetworkClassIndexList { get; }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			if (NonNetworkClassIndexList.Contains(tableIndex))
			{
				return 0;
			}

			if (_networkBuilder == null)
			{
				_networkBuilder = new NetworkBuilder(_includeBorderNodes);
			}

			NetworkBuilder.AddNetElements(row, tableIndex);

			return 0;
		}

		protected IEnumerable<DirectedRow> GetDirectedRows(TableIndexRow row)
		{
			IGeometry geom = ((IReadOnlyFeature) row.Row).Shape;
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
				_networkBuilder?.ClearAll();
				return 0;
			}

			if (_networkBuilder == null)
			{
				return 0;
			}

			_networkBuilder.ClearConnected();

			IEnvelope verificationBox = Assert.NotNull(args.AllBox, "AllBox");
			WKSEnvelope verificationEnvelope;
			verificationBox.QueryWKSCoords(out verificationEnvelope);

			IEnvelope tileBox = Assert.NotNull(args.CurrentEnvelope, "CurrentEnvelope");
			WKSEnvelope tileEnvelope;
			tileBox.QueryWKSCoords(out tileEnvelope);

			BuildToleranceCache(tileEnvelope);

			_networkBuilder.BuildNet(verificationEnvelope, tileEnvelope, Tolerance);
			_networkBuilder.ClearCache();

			return 0;
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

			_networkBuilder.ClearCache();
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
					IReadOnlyRow feature in
					Search(InvolvedTables[iTable], _filters[iTable], _helpers[iTable]))
				{
					_networkBuilder.AddNetElements(feature, iTable);
				}
			}
		}

		[NotNull]
		protected IList<InvolvedRow> GetInvolvedRows([NotNull] ITableIndexRow tableIndexRow)
		{
			IReadOnlyRow row = tableIndexRow.GetRow(InvolvedTables);

			return InvolvedRowUtils.GetInvolvedRows(row);
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
