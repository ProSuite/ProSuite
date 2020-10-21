using System.Collections.Generic;

namespace ProSuite.Commons.Geometry.Wkb
{
	public abstract class GeometryBuilderBase<L, P>
	{
		public abstract L CreateLinestring(IEnumerable<P> points, int? knownPointCount = null);

		public abstract IPointFactory<P> GetPointFactory(Ordinates forOrdinates);
	}
}
