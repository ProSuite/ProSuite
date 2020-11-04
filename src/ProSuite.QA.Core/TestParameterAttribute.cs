using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	[MeansImplicitUse]
	public class TestParameterAttribute : Attribute
	{
		public TestParameterAttribute() { }

		public TestParameterAttribute([CanBeNull] object defaultValue)
		{
			DefaultValue = defaultValue;
		}

		[CanBeNull]
		public object DefaultValue { get; }
	}
}
