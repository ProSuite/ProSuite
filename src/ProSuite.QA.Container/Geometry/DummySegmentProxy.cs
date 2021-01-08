using System;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container.Geometry
{
	internal class DummySegmentProxy : ISegmentProxy
	{
		private readonly int _partIndex;

		private readonly int _segmentIndex;
		// TODO revise, problematic use of NotImplementedException - what is really needed from this class? 
		//
		// there should probably be an interface (implemented on SegmentProxy) with a mini class just 
		// covering the interface needs --> in many places where current SegmentProxy is used, this interface would be used.
		// 
		// ISegmentReference?
		// 
		// It seems that not much more than PartIndex and SegmentIndex can be called safely on an instance
		// of this class.

		public DummySegmentProxy(int partIndex, int segmentIndex)
		{
			_partIndex = partIndex;
			_segmentIndex = segmentIndex;
		}

		public int SegmentIndex
		{
			get { return _segmentIndex; }
		}

		public int PartIndex
		{
			get { return _partIndex; }
		}

		public IPnt Max
		{
			get { throw new NotImplementedException(); }
		}
	}
}
