using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class MDomainProperties
	{
		public MDomainProperties(double mMin, double mMax)
		{
			MMin = mMin;
			MMax = mMax;
		}

		[DisplayName("M Minimum")]
		[UsedImplicitly]
		public double MMin { get; private set; }

		[DisplayName("M Maximum")]
		[UsedImplicitly]
		public double MMax { get; private set; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", MMin, MMax);
		}
	}
}
