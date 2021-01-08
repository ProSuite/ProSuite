using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Geometry
{
	public class SegmentsEnumerator : IEnumerator<ISegment>
	{
		private readonly IEnumSegment _enumSegment;
		private readonly bool _recycling;

		private bool _reverse;

		private ISegment _current;
		private int _currentPartIndex;
		private int _currentSegmentIndex;

		[CLSCompliant(false)]
		public SegmentsEnumerator([NotNull] IEnumSegment enumSegment)
		{
			Assert.ArgumentNotNull(enumSegment, "enumSegment");

			_enumSegment = enumSegment;

			Reset();

			_recycling = _enumSegment.IsRecycling;
		}

		public bool Reverse
		{
			get { return _reverse; }
			set
			{
				_reverse = value;
				Reset();
			}
		}

		public bool Recycle { get; set; }

		object IEnumerator.Current
		{
			get { return Current; }
		}

		[CLSCompliant(false)]
		public ISegment Current
		{
			get { return _current; }
		}

		public int CurrentPartIndex
		{
			get { return _currentPartIndex; }
		}

		public int CurrentSegmentIndex
		{
			get { return _currentSegmentIndex; }
		}

		public void Dispose()
		{
			DisposeCurrent();
		}

		public bool MoveNext()
		{
			DisposeCurrent();

			ISegment segment;
			if (! _reverse)
			{
				_enumSegment.Next(out segment, ref _currentPartIndex, ref _currentSegmentIndex);
			}
			else
			{
				_enumSegment.Previous(out segment, ref _currentPartIndex, ref _currentSegmentIndex);
			}

			if (segment == null)
			{
				_current = null;
			}
			else
			{
				if (_recycling && ! Recycle)
				{
					_current = GeometryFactory.Clone(segment);
					Marshal.ReleaseComObject(segment);
				}
				else
				{
					_current = segment;
				}
			}

			return _current != null;
		}

		public void Reset()
		{
			if (! _reverse)
			{
				_enumSegment.Reset();
			}
			else
			{
				_enumSegment.ResetToEnd();
			}
		}

		private void DisposeCurrent()
		{
			if (_current != null && _recycling && Recycle)
			{
				Marshal.ReleaseComObject(_current);
				_current = null;
			}
		}
	}
}
