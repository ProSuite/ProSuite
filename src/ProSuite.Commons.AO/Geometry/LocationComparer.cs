using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.AO.Geometry
{
	public sealed class LocationComparer : IEqualityComparer<Location>
	{
		private readonly double _toleranceSquared;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationComparer"/> class.
		/// </summary>
		/// <param name="tolerance">The xy tolerance for comparing points.</param>
		public LocationComparer(double tolerance)
		{
			Assert.ArgumentCondition(tolerance >= 0, "invalid tolerance: {0}", tolerance);

			_toleranceSquared = tolerance * tolerance;
		}

		public bool Equals(Location loc1, Location loc2)
		{
			double distanceSquared = loc1.GetSquaredDistanceTo(loc2);

			return Math.Abs(distanceSquared) <= _toleranceSquared;
		}

		public int GetHashCode(Location obj)
		{
			return obj.GetHashCode();
		}
	}
}
