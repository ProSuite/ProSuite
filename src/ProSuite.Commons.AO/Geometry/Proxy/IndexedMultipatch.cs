using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public class IndexedMultiPatch : IIndexedMultiPatch
	{
		[NotNull] private readonly IEnvelope _envelope;
		[NotNull] private readonly List<PatchProxy> _patchProxies;

		public IndexedMultiPatch([NotNull] IMultiPatch baseGeometry)
		{
			BaseGeometry = baseGeometry;
			_envelope = baseGeometry.Envelope;
			_patchProxies = QaGeometryUtils.GetPatchProxies(baseGeometry).ToList();
		}

		public void Dispose() { }

		[NotNull]
		public IMultiPatch BaseGeometry { get; }

		public IEnvelope Envelope => GeometryFactory.Clone(_envelope);

		public bool AllowIndexing
		{
			get { return false; }
			set { }
		}

		public IEnumerable<SegmentProxy> GetSegments()
		{
			foreach (PatchProxy patchProxy in _patchProxies)
			{
				foreach (SegmentProxy segment in patchProxy.GetSegments())
				{
					yield return segment;
				}
			}
		}

		public IEnumerable<SegmentProxy> GetSegments(IBox box)
		{
			foreach (SegmentProxy segment in GetSegments())
			{
				Pnt min = Pnt.Create(segment.Min);
				Pnt max = Pnt.Create(segment.Max);
				var segBox = new Box(min, max);

				if (segBox.Intersects(box))
				{
					yield return segment;
				}
			}
		}

		public List<int> GetPartIndexes(int patchIndex)
		{
			int startPartIndex = 0;
			for (int index = 0; index < patchIndex; index++)
			{
				startPartIndex += _patchProxies[index].PlanesCount;
			}

			int partsCount = _patchProxies[patchIndex].PlanesCount;

			var result = new List<int>(partsCount);
			for (int partIndex = 0; partIndex < partsCount; partIndex++)
			{
				result.Add(startPartIndex + partIndex);
			}

			return result;
		}

		public int GetPatchIndex(int partIndex)
		{
			int planesCount = 0;
			for (int patchIndex = 0; patchIndex < _patchProxies.Count; patchIndex++)
			{
				int patchPlanesCount = _patchProxies[patchIndex].PlanesCount;
				if (planesCount + patchPlanesCount > partIndex)
				{
					return patchIndex;
				}

				planesCount += patchPlanesCount;
			}

			return -1;
		}

		public SegmentProxy GetSegment(int partIndex, int segmentIndex)
		{
			PlaneProxy plane = GetPlaneProxy(partIndex);
			return plane.GetSegment(segmentIndex);
		}

		public bool IsPartClosed(int part)
		{
			PlaneProxy plane = GetPlaneProxy(part);
			return plane.IsClosed;
		}

		public int GetPartsCount()
		{
			int partsCount = 0;
			foreach (PatchProxy patchProxy in _patchProxies)
			{
				partsCount += patchProxy.PlanesCount;
			}

			return partsCount;
		}

		public int GetPartSegmentCount(int part)
		{
			PlaneProxy plane = GetPlaneProxy(part);
			return plane.GetSegmentCount();
		}

		public IPolyline GetSubpart(int partIndex, int startSegmentIndex,
									double startFraction, int endSegmentIndex,
									double endFraction)
		{
			PlaneProxy plane = GetPlaneProxy(partIndex);
			IPolyline subpart = plane.GetSubpart(startSegmentIndex,
												 startFraction,
												 endSegmentIndex, endFraction);
			return subpart;
		}

		bool IIndexedSegments.TryGetSegmentNeighborhoods(
			IIndexedSegments neighborSegments, IBox commonBox, double searchDistance,
			out IEnumerable<SegmentProxyNeighborhood> neighborhoods)
		{
			neighborhoods = null;
			return false;
		}

		[NotNull]
		private PlaneProxy GetPlaneProxy(int partIndex)
		{
			int planesCount = 0;
			foreach (PatchProxy patchProxy in _patchProxies)
			{
				if (planesCount + patchProxy.PlanesCount > partIndex)
				{
					return patchProxy.GetPlane(partIndex - planesCount);
				}

				planesCount += patchProxy.PlanesCount;
			}

			throw new ArgumentOutOfRangeException(nameof(partIndex));
		}
	}
}
