namespace ProSuite.Commons.Geom
{
	internal class IntersectionRun
	{
		private readonly Pnt3D _includedRingStartPoint;

		public IntersectionRun(IntersectionPoint3D previousIntersection,
		                       IntersectionPoint3D nextIntersection,
		                       Linestring subcurve,
		                       Pnt3D includedRingStartPoint)
		{
			_includedRingStartPoint = includedRingStartPoint;
			NextIntersection = nextIntersection;
			PreviousIntersection = previousIntersection;
			Subcurve = subcurve;
		}

		public IntersectionPoint3D PreviousIntersection { get; }
		public IntersectionPoint3D NextIntersection { get; }
		public Linestring Subcurve { get; }

		public bool RunsAlongSource { get; set; }
		public bool RunsAlongTarget { get; set; }

		public bool RunsAlongForward { get; set; }

		public bool IsBoundaryLoop { get; set; }

		public bool ContainsSourceStart(out Pnt3D startPoint)
		{
			if (_includedRingStartPoint != null)
			{
				startPoint = _includedRingStartPoint;

				return true;
			}

			startPoint = null;
			return false;
		}
	}
}
