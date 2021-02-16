using System;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.QA.Container
{
	public class TestRowException : Exception
	{
		public TestRowException(ITest test, IRow row, string msg)
			: base(msg)
		{
			Test = test;
			Row = row;
		}

		public ITest Test { get; }

		public IRow Row { get; }
	}
}
