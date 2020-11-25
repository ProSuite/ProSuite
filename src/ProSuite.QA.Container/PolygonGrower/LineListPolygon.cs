using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using Ao = ESRI.ArcGIS.Geometry;

namespace ProSuite.QA.Container.PolygonGrower
{
	public abstract class LineListPolygon
	{
		private readonly List<IRow> _centroids = new List<IRow>();

		private readonly bool _isInnerRing;

		protected LineListPolygon(bool isInnerRing)
		{
			_isInnerRing = isInnerRing;
		}

		public bool IsInnerRing
		{
			get { return _isInnerRing; }
		}

		[CLSCompliant(false)]
		public List<IRow> Centroids
		{
			get { return _centroids; }
		}

		public bool Processed { get; set; }

		public void Add(LineListPolygon poly) { }

		protected abstract void AddCore(LineListPolygon poly);
	}

	[CLSCompliant(false)]
	public class LineListPolygon<TDirectedRow> : LineListPolygon
		where TDirectedRow : class, IPolygonDirectedRow
	{
		private readonly LineList<TDirectedRow> _mainRing;
		private bool _canProcess;
		private List<LineList<TDirectedRow>> _innerRingList = new List<LineList<TDirectedRow>>();

		#region Constructors

		public LineListPolygon([NotNull] LineList<TDirectedRow> outerRing)
			: base(false)
		{
			Assert.ArgumentNotNull(outerRing, "outerRing");

			_mainRing = outerRing;
			_canProcess = true;
			Processed = true;

			foreach (TDirectedRow row in outerRing.DirectedRows)
			{
				row.RightPoly = this;
			}
		}

		public LineListPolygon([NotNull] LineList<TDirectedRow> ring, bool isInnerRing)
			: base(isInnerRing)
		{
			Assert.ArgumentNotNull(ring, "ring");

			if (isInnerRing == false)
			{
				_canProcess = true;
				Processed = false;
			}

			_mainRing = ring;
			foreach (TDirectedRow row in ring.DirectedRows)
			{
				row.RightPoly = this;
			}
		}

		#endregion

		public LineList<TDirectedRow> OuterRing
		{
			get
			{
				if (IsInnerRing)
				{
					throw new ArgumentException("cannot query outer Ring of an innerRing polygon");
				}

				return _mainRing;
			}
		}

		public List<LineList<TDirectedRow>> InnerRingList
		{
			get
			{
				if (IsInnerRing)
				{
					throw new ArgumentException("cannot query inner rings of an innerRing polygon");
				}

				return _innerRingList;
			}
		}

		public bool CanProcess
		{
			get { return _canProcess; }
			set { _canProcess = value; }
		}

		[CLSCompliant(false)]
		public Ao.IPolygon GetPolygon()
		{
			Ao.IPolygon polygon = new Ao.PolygonClass();
			Ao.IRing ring = new Ao.RingClass();
			var polyCollection = (Ao.IGeometryCollection) polygon;
			var ringCollection = (Ao.ISegmentCollection) ring;

			foreach (TDirectedRow pRow in _mainRing.DirectedRows)
			{
				Ao.ISegmentCollection nextPart = pRow.GetDirectedSegmentCollection();
				ringCollection.AddSegmentCollection(nextPart);
			}

			object missing = Type.Missing;
			ring.Close();
			polyCollection.AddGeometry(ring, ref missing, ref missing);

			foreach (LineList<TDirectedRow> lc in _innerRingList)
			{
				polyCollection.AddGeometry(
					((Ao.IGeometryCollection) lc.GetPolygon()).Geometry[0],
					ref missing, ref missing);
			}

			((Ao.ITopologicalOperator) polygon).Simplify();

			return polygon;
		}

		protected override void AddCore(LineListPolygon poly)
		{
			Add((LineListPolygon<TDirectedRow>) poly);
		}

		public void Add(LineListPolygon<TDirectedRow> innerRing)
		{
			if (IsInnerRing)
			{
				throw new ArgumentException("Cannot add rings to an inner ring");
			}

			if (innerRing.IsInnerRing == false)
			{
				throw new ArgumentException("Cannot add outer ring to a polygon");
			}

			if (_innerRingList == null)
			{
				_innerRingList = new List<LineList<TDirectedRow>>();
			}

			_innerRingList.Add(innerRing._mainRing);

			foreach (TDirectedRow row in innerRing._mainRing.DirectedRows)
			{
				row.RightPoly = this;
			}
		}
	}
}
