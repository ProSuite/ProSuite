using System;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.QA.Container
{
	public class TestRowException : Exception
	{
		public TestRowException(ITest test, IReadOnlyRow row, string msg)
			: base(msg)
		{
			Test = test;
			Row = row;
		}

		public ITest Test { get; }

		public IReadOnlyRow Row { get; }
	}
}
