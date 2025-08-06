using System;

namespace ProSuite.Processing.Domain
{
	public abstract class MapContextBase : IMapContext
	{
		protected abstract double MetersPerMapUnit { get; }

		public abstract double ReferenceScale { get; }

		public double MapUnitsPerPoint
		{
			get
			{
				double referenceScale = ReferenceScale;
				if (!(referenceScale > 0))
					throw new InvalidOperationException(
						"Reference Scale is missing: cannot convert between points and map units");

				double distancePoints = 1.0 * referenceScale;
				double meters = distancePoints * Constants.MetersPerPoint;
				return meters / MetersPerMapUnit;
			}
		}

		public double PointsPerMapUnit
		{
			get
			{
				double referenceScale = ReferenceScale;
				if (!(referenceScale > 0))
					throw new InvalidOperationException(
						"Reference Scale is missing: cannot convert between points and map units");

				double distanceMapUnits = 1.0 / referenceScale;
				double meters = distanceMapUnits * MetersPerMapUnit;
				return meters * Constants.PointsPerMeter;
			}
		}
	}
}
