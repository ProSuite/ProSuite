using System;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// An evaluation environment that contains no bindings.
	/// All Lookup requests produce <c>null</c> as the result.
	/// </summary>
	public class NullEnvironment : EnvironmentBase
	{
		public override object Lookup(string name, string qualifier)
		{
			return null;
		}

		public override object Invoke(Function target, params object[] args)
		{
			throw new NotSupportedException();
		}
	}
}
