using System.Collections.Generic;
using System.Linq;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Concrete class for polygons or polylines. Should probably be split into two
	/// separate classes once there is a need for them.
	/// </summary>
	public class MultiPolycurve : MultiLinestring
	{
		public static MultiPolycurve CreateEmpty()
		{
			var result = new MultiPolycurve(new List<Linestring>(0));

			result.SetEmpty();

			return result;
		}

		public MultiPolycurve(IEnumerable<Linestring> linestrings) : base(linestrings) { }

		public MultiPolycurve(IEnumerable<MultiLinestring> multiLinestrings) : base(
			multiLinestrings.SelectMany(g => g.GetLinestrings())) { }

		public override MultiLinestring Clone()
		{
			var result = new MultiPolycurve(Linestrings)
			             {
				             //Id = Id
			             };

			return result;
		}
	}
}