using System;

namespace ProSuite.Commons.Callbacks
{
	public class NopDisposableCallback : IDisposable
	{
		public void Dispose() { }
	}
}