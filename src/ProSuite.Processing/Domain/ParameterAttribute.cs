using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProSuite.Processing.Domain
{
	//[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Property)]
	public class ParameterAttribute : Attribute
	{
		public bool Required { get; }
		public bool Multivalued { get; set; }
		public int Order { get; set; }
		public string Group { get; set; }

		public int LineNumber { get; }
		public string FileName { get; }

		protected ParameterAttribute(
			int lineNumber = 0, string filePath = null, bool required = false)
		{
			LineNumber = lineNumber;
			FileName = string.IsNullOrWhiteSpace(filePath)
				           ? null
				           : FileName = Path.GetFileName(filePath);
			Required = required;
		}
	}

	public class RequiredParameterAttribute : ParameterAttribute
	{
		public RequiredParameterAttribute(
			[CallerLineNumber] int lineNumber = 0,
			[CallerFilePath] string filePath = null)
			: base(lineNumber, filePath, true) { }
	}

	public class OptionalParameterAttribute : ParameterAttribute
	{
		public object DefaultValue { get; set; }

		public OptionalParameterAttribute(
				[CallerLineNumber] int lineNumber = 0,
				[CallerFilePath] string filePath = null)
			: base(lineNumber, filePath) { }
	}
}
