using System;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Commons.Geometry.Wkb
{
	public class GeomBuilder : GeometryBuilderBase<Multipoint<IPnt>, Linestring, IPnt>
	{
		private readonly bool _reverseOrder;

		public GeomBuilder(bool reverseOrder)
		{
			_reverseOrder = reverseOrder;
		}

		public override Linestring CreateLinestring(IEnumerable<IPnt> points,
		                                            int? knownPointCount = null)
		{
			IEnumerable<Pnt3D> pointEnum = points.Cast<Pnt3D>();

			if (_reverseOrder)
			{
				pointEnum = pointEnum.Reverse();
			}

			return new Linestring(pointEnum);
		}

		public override Multipoint<IPnt> CreateMultipoint(IEnumerable<IPnt> points, int? knownPointCount = null)
		{
			return new Multipoint<IPnt>(points, knownPointCount);
		}

		public override IPointFactory<IPnt> GetPointFactory(Ordinates forOrdinates)
		{
			if (forOrdinates == Ordinates.Xyz || forOrdinates == Ordinates.Xy)
			{
				return new PntFactory();
			}

			throw new NotImplementedException($"Unsupported ordinates type: {forOrdinates}");
		}
	}
}
