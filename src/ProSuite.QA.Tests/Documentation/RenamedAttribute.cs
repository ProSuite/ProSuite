using System;

namespace ProSuite.QA.Tests.Documentation
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter,
	                AllowMultiple = false, Inherited = true)]
	public class RenamedAttribute : Attribute
	{
		private readonly string _oldName;

		public RenamedAttribute(string oldName)
		{
			_oldName = oldName;
		}

		public string OldName => _oldName;
	}
}
