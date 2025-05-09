using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.Proxy
{
	public class SegmentPartList
	{
		private readonly IPolycurve _line;
		private readonly List<SegmentPart> _list;

		private int _currentPart = -1;
		private WKSPointZ[] _currentWks;

		public SegmentPartList([NotNull] IPolycurve line)
		{
			_line = line;
			_list = new List<SegmentPart>();
		}

		public int Count => _list.Count;

		public void Add(int part, int segmentIndex)
		{
			Add(part, segmentIndex, 0, 1, true);
		}

		public void Add(int part, int segmentIndex, double min, double max)
		{
			bool complete = min == 0 && max == 1;
			Add(part, segmentIndex, min, max, complete);
		}

		private void Add(int part, int segment, double min, double max,
		                 bool complete)
		{
			var s = new SegmentPart(part, segment, min, max, complete);
			_list.Add(s);
		}

		[NotNull]
		public IList<IPolyline> GetParts(ISpatialReference spatialReference)
		{
			_list.Sort(new SegmentPartComparer());
			int part0 = -1;
			double max = -1;
			double min = 0;

			var result = new List<IPolyline>();

			foreach (SegmentPart seg in _list)
			{
				if (seg.PartIndex != part0 ||
				    seg.FullMin > max)
				{
					if (min < max)
					{
						IPolyline part = CreateLine(part0, min, max, spatialReference);
						result.Add(part);
					}

					min = seg.FullMin;
					part0 = seg.PartIndex;
				}

				max = seg.FullMax;
			}

			if (min < max)
			{
				IPolyline part = CreateLine(part0, min, max, spatialReference);
				result.Add(part);
			}

			return result;
		}

		[NotNull]
		private IPolyline CreateLine(int part, double min, double max,
		                             [NotNull] ISpatialReference spatialReference)
		{
			if (part != _currentPart)
			{
				_currentPart = -1;
				var coll = (IPointCollection4) ((IGeometryCollection) _line).Geometry[part];
				_currentWks = new WKSPointZ[coll.PointCount];
				GeometryUtils.QueryWKSPointZs(coll, _currentWks);

				_currentPart = part;
			}

			min = Math.Max(min, 0);
			max = Math.Min(max, _currentWks.Length - 1);
			var points = new List<WKSPointZ>();
			var iMin = (int) min;

			WKSPointZ p = Factor(_currentWks[iMin], _currentWks[iMin + 1], min - iMin);
			points.Add(p);

			var iMax = (int) max;
			for (int i = iMin + 1; i <= iMax; i++)
			{
				points.Add(_currentWks[i]);
			}

			if (iMax != max)
			{
				p = Factor(_currentWks[iMax], _currentWks[iMax + 1], max - iMax);
				points.Add(p);
			}

			WKSPointZ[] array = points.ToArray();
			return GeometryFactory.CreatePolyline(array, spatialReference);
		}

		private static WKSPointZ Factor(WKSPointZ p0, WKSPointZ p1, double factor)
		{
			return new WKSPointZ
			       {
				       X = p0.X + factor * (p1.X - p0.X),
				       Y = p0.Y + factor * (p1.Y - p0.Y),
				       Z = p0.Z + factor * (p1.Z - p0.Z)
			       };
		}

		public void Clear()
		{
			_list.Clear();
		}
	}
}
