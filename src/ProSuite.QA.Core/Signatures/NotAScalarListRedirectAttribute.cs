using System;

namespace ProSuite.QA.Core.Signatures
{
	/// <summary>
	/// Marks a constructor overload that must NOT be folded into its list-valued
	/// counterpart by the AlgorithmSignatureBuilder.UnifyScalarListParameters() method,
	/// even though it is positionally identical: the overload converts its scalar
	/// parameter instead of wrapping it into a one-element list — typically a token
	/// string that is parsed into several values (e.g.
	/// <c>QaRequiredFieldsDefinition(…, requiredFieldNamesString, …)</c> splitting
	/// "A,B" into two field names). Migrating a persisted value of such a parameter
	/// onto the list parameter would silently change its meaning ("A,B" would become
	/// the single element "A,B" instead of the two elements "A" and "B").
	/// <para>
	/// The unification harness
	/// (<c>ScalarListUnificationHarnessTest.ScalarConstructionIsEquivalentToListConstruction</c>)
	/// constructs every folded overload with a separator-containing probe value and
	/// fails when the scalar overload is not a pure wrap, pointing to this attribute.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor)]
	public class NotAScalarListRedirectAttribute : Attribute { }
}
