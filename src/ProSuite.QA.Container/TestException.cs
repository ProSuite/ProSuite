using System;

namespace ProSuite.QA.Container
{
	internal class TestException : Exception
	{
		public TestException(ITest test, string msg)
			: base(msg)
		{
			Test = test;
		}

		public ITest Test { get; }
	}
}
