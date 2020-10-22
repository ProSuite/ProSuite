using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	[CLSCompliant(false)]
	public class WksPointListBuilder : GeometryBuilderBase<WKSPointZ[], WKSPointZ>
	{
		private readonly bool _reverseOrder;

		public WksPointListBuilder(bool reverseOrder = false)
		{
			_reverseOrder = reverseOrder;
		}

		public override WKSPointZ[] CreateLinestring(IEnumerable<WKSPointZ> points,
		                                             int? knownPointCount = null)
		{
			// TODO: Recycle arrays

			Assert.ArgumentNotNull(knownPointCount, nameof(knownPointCount));

			int length = (int) knownPointCount;
			var result = new WKSPointZ[length];

			int index = _reverseOrder ? length - 1 : 0;
			foreach (WKSPointZ wksPoint in points)
			{
				result[index] = wksPoint;

				if (_reverseOrder)
					index--;
				else
					index++;
			}

			return result;
		}

		public override IPointFactory<WKSPointZ> GetPointFactory(Ordinates forOrdinates)
		{
			if (forOrdinates == Ordinates.Xym || forOrdinates == Ordinates.Xyzm)
			{
				throw new NotImplementedException($"Unsupported ordinates type: {forOrdinates}");
			}

			return new WksPointZFactory();
		}
	}
}
