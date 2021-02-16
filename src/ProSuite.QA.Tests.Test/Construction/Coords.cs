using System.Collections.Generic;

namespace ProSuite.QA.Tests.Test.Construction
{
	public class Coords : List<double[]>
	{
		public void Add(double x, double y)
		{
			Add(new[] {x, y});
		}
	}
}
