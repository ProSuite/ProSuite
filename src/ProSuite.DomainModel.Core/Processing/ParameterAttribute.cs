using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.Processing
{
	/// <summary>
	/// Parameter Attribute for Carto Process implementations:
	/// Use [Parameter] so the system knows a public property is a configurable process parameter
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	[MeansImplicitUse]
	public class ParameterAttribute : Attribute
	{
		public Type EditorType { get; set; }

		public bool AdminOnly { get; set; }

		public string DisplayType { get; set; }

		/// <summary>
		/// User interface may order process parameters by this ordinal number.
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// User interface may group process parameters by this heading text.
		/// </summary>
		public string Group { get; set; }
	}
}
