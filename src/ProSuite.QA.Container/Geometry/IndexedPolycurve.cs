using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.QA.Container.Geometry
{
	public class IndexedPolycurve : IIndexedSegments
	{
		private readonly BoxTree<SegmentProxy> _boxTree;
		private readonly List<PartProxy> _partProxies;
		private readonly IEnvelope _envelope;

		#region Constructors

		public IndexedPolycurve([NotNull] IPointCollection4 baseGeometry)
		{
			Assert.ArgumentNotNull(baseGeometry, nameof(baseGeometry));

			const bool @dynamic = true;
			const int dimension = 2;
			const int maxElementCountPerTile = 4; //  was: 64;
			_boxTree = new BoxTree<SegmentProxy>(dimension, maxElementCountPerTile, @dynamic);

			var geometry = (IGeometry) baseGeometry;

			_envelope = geometry.Envelope;
			double tolerance = GeometryUtils.GetXyTolerance(geometry);

			Box extent = QaGeometryUtils.CreateBox(_envelope);
			Expand(extent, tolerance);

			_boxTree.InitSize(new IGmtry[] {extent});

			var geometryCollection = baseGeometry as IGeometryCollection;
			if (geometryCollection != null)
			{
				int partCount = geometryCollection.GeometryCount;

				if (partCount > 1)
				{
					// unpack and add individual parts
					_partProxies = new List<PartProxy>(partCount);

					for (int partIndex = 0; partIndex < partCount; partIndex++)
					{
						var part = (IPointCollection4) geometryCollection.Geometry[partIndex];

						var partProxy = new PartProxy(_boxTree, partIndex, part);

						_partProxies.Add(partProxy);

						Marshal.ReleaseComObject(part);
					}
				}
				else
				{
					// single part in collection
					_partProxies = AddSinglePartProxy(baseGeometry);
				}
			}
			else
			{
				// no geometry collection
				_partProxies = AddSinglePartProxy(baseGeometry);
			}
		}

		#endregion

		public void Dispose() { }

		public IEnvelope Envelope => GeometryFactory.Clone(_envelope);

		public bool AllowIndexing
		{
			get { return true; }
			set { }
		}

		public SegmentProxy GetSegment(int partIndex, int segmentIndex)
		{
			PartProxy partProxy = _partProxies[partIndex];
			SegmentProxy segmentProxy = partProxy.GetSegment(segmentIndex);
			return segmentProxy;
		}

		public IEnumerable<SegmentProxy> GetSegments()
		{
			foreach (PartProxy partProxy in _partProxies)
			{
				foreach (SegmentProxy segment in partProxy.GetSegments())
				{
					yield return segment;
				}
			}
		}

		public bool TryGetSegmentNeighborhoods(
			IIndexedSegments neighborSegments, IBox commonBox, double searchDistance,
			out IEnumerable<SegmentProxyNeighborhood> neighborhoods)
		{
			IndexedPolycurve neighbor = neighborSegments as IndexedPolycurve;
			if (neighbor == null)
			{
				neighborhoods = null;
				return false;
			}

			BoxTree<SegmentProxy> neighborBoxTree = neighbor._boxTree;

			neighborhoods = GetNeighborhoods(neighborBoxTree, searchDistance, commonBox);
			return true;
		}

		private IEnumerable<SegmentProxyNeighborhood> GetNeighborhoods(
			BoxTree<SegmentProxy> neighborBoxTree, double searchDistance, IBox commonBox)
		{
			return _boxTree.EnumerateNeighborhoods(neighborBoxTree, searchDistance, commonBox)
			               .Select(boxPairs => new SegmentProxyNeighborhood
			                                   {
				                                   SegmentProxy = boxPairs.Entry.Value,
				                                   Neighbours = GetSegments(boxPairs.Neighbours)
			                                   });
		}

		[NotNull]
		private IEnumerable<SegmentProxy> GetSegments(
			IEnumerable<BoxTree<SegmentProxy>.TileEntry> tileEntries)
		{
			return tileEntries.Select(entry => entry.Value);
		}

		public IEnumerable<SegmentProxy> GetSegments(IBox box)
		{
			foreach (BoxTree<SegmentProxy>.TileEntry tileEntry in _boxTree.Search(box))
			{
				yield return tileEntry.Value;
			}
		}

		public int GetPartsCount()
		{
			return _partProxies.Count;
		}

		public bool IsPartClosed(int part)
		{
			bool isClosed = _partProxies[part].IsClosed;
			return isClosed;
		}

		public int GetPartSegmentCount(int part)
		{
			int segCount = _partProxies[part].SegmentCount;
			return segCount;
		}

		public IPolyline GetSubpart(int partIndex, int startSegmentIndex,
		                            double startFraction,
		                            int endSegmentIndex, double endFraction)
		{
			IPolyline subpart = _partProxies[partIndex].GetSubpart(startSegmentIndex,
				startFraction,
				endSegmentIndex, endFraction);
			return subpart;
		}

		[NotNull]
		private List<PartProxy> AddSinglePartProxy([NotNull] IPointCollection4 baseGeometry)
		{
			var result = new List<PartProxy>(1);

			var partProxy = new PartProxy(_boxTree, 0, baseGeometry);
			result.Add(partProxy);

			return result;
		}

		private static void Expand([NotNull] Box box, double tolerance)
		{
			box.Min.X -= tolerance;
			box.Min.Y -= tolerance;
			box.Max.X += tolerance;
			box.Max.Y += tolerance;
		}
	}
}
