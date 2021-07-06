using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom.SpatialIndex
{
	public interface ISpatialSearcher<T>
	{
		IEnumerable<T> Search([NotNull] IBox searchBox,
		                      double tolerance);

		IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			double tolerance, Predicate<T> predicate = null);

		IEnumerable<T> Search(
			double xMin, double yMin, double xMax, double yMax,
			IBoundedXY knownBounds, double tolerance,
			Predicate<T> predicate = null);
	}
}