using System;

namespace ProSuite.Commons.Essentials.Callbacks
{
	public class NopDisposableCallback : IDisposable
	{
		public void Dispose() { }
	}
}
