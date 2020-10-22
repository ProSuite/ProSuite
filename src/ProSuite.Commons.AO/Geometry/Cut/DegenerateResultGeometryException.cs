using System;

namespace ProSuite.Commons.AO.Geometry.Cut
{
	public class DegenerateResultGeometryException : Exception
	{
		public DegenerateResultGeometryException(string message) : base(message) { }
	}
}
