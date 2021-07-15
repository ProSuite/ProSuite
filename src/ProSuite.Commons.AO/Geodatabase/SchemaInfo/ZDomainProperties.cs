using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class ZDomainProperties
	{
		public ZDomainProperties(double zMin, double zMax)
		{
			ZMin = zMin;
			ZMax = zMax;
		}

		[DisplayName("Z Minimum")]
		[UsedImplicitly]
		public double ZMin { get; private set; }

		[DisplayName("Z Maximum")]
		[UsedImplicitly]
		public double ZMax { get; private set; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", ZMin, ZMax);
		}
	}
}
