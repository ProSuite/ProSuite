using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry.Wkb;

namespace ProSuite.Commons.AO.Geometry.Serialization
{
	public class WksPointListBuilder : GeometryBuilderBase<WKSPointZ[], WKSPointZ[], WKSPointZ>
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

			return CreatePointArray(points, knownPointCount, _reverseOrder);
		}

		public override WKSPointZ[] CreateMultipoint(IEnumerable<WKSPointZ> points,
		                                             int? knownPointCount = null)
		{
			return CreatePointArray(points, knownPointCount, false);
		}

		public override IPointFactory<WKSPointZ> GetPointFactory(Ordinates forOrdinates)
		{
			if (forOrdinates == Ordinates.Xym || forOrdinates == Ordinates.Xyzm)
			{
				throw new NotImplementedException($"Unsupported ordinates type: {forOrdinates}");
			}

			return new WksPointZFactory();
		}

		private static WKSPointZ[] CreatePointArray([NotNull] IEnumerable<WKSPointZ> points,
		                                            int? knownPointCount,
		                                            bool reverseOrder)
		{
			Assert.ArgumentNotNull(knownPointCount, nameof(knownPointCount));

			int length = (int) knownPointCount;
			var result = new WKSPointZ[length];

			int index = reverseOrder ? length - 1 : 0;

			foreach (WKSPointZ wksPoint in points)
			{
				result[index] = wksPoint;

				if (reverseOrder)
					index--;
				else
					index++;
			}

			return result;
		}
	}
}
