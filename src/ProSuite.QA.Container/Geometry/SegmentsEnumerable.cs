using System.Collections;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentsEnumerable : IEnumerable<ISegment>
	{
		private readonly SegmentsEnumerator _enumerator;

		public SegmentsEnumerable(SegmentsEnumerator segments)
		{
			_enumerator = segments;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<ISegment> GetEnumerator()
		{
			return _enumerator;
		}

		public SegmentsEnumerator Enumerator
		{
			get { return _enumerator; }
		}
	}
}
