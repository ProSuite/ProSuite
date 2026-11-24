using System;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Marks a text parameter whose value can contain line breaks, so that the
	/// condition editor shows a box with several lines. Declared on the property that
	/// matches the constructor parameter name, like <see cref="SqlExpressionAttribute"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class MultilineTextAttribute : Attribute { }
}
