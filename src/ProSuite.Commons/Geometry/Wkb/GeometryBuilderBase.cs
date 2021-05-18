using System.Collections.Generic;

namespace ProSuite.Commons.Geometry.Wkb
{
	/// <summary>
	/// Abstraction for the geometry creation.
	/// </summary>
	/// <typeparam name="TMultipoint">The actual multipoint type</typeparam>
	/// <typeparam name="TLinestring">The actual linestring type</typeparam>
	/// <typeparam name="TPoint">The actual point type</typeparam>
	public abstract class GeometryBuilderBase<TMultipoint, TLinestring, TPoint>
	{
		public abstract TMultipoint CreateMultipoint(IEnumerable<TPoint> points,
		                                             int? knownPointCount = null);

		public abstract TLinestring CreateLinestring(IEnumerable<TPoint> points,
		                                             int? knownPointCount = null);

		public abstract IPointFactory<TPoint> GetPointFactory(Ordinates forOrdinates);
	}
}
