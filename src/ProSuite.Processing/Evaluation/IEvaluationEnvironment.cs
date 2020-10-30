using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Evaluation
{
	public interface IEvaluationEnvironment
	{
		/// <summary>
		/// Look up and return the value associated with the given
		/// <paramref name="name"/> and optional <paramref name="qualifier"/>.
		/// <para/>
		/// An implementation is free to do whatever it pleases if no value is
		/// associated with <paramref name="qualifier"/>.<paramref name="name"/>;
		/// in particular, it may return <c>null</c> or throw an exception.
		/// </summary>
		/// <param name="name">The name; required</param>
		/// <param name="qualifier">The qualifier; optional</param>
		/// <returns>
		/// The value associated with <paramref name="qualifier"/>.<paramref name="name"/>
		/// </returns>
		/// <remarks>
		/// This method is called by the evaluator to resolve names in an expression.
		/// </remarks>
		object Lookup(string name, [CanBeNull] string qualifier);

		/// <summary>
		/// Assume <paramref name="target"/> is a function, invoke it with
		/// the given <paramref name="args"/>, and return the resulting value.
		/// <para/>
		/// If <paramref name="target"/> is not a function or if
		/// <paramref name="target"/> is an unknown function, an
		/// exception will be thrown.
		/// <para/>
		/// If functions are overloaded, then <paramref name="target"/>
		/// signifies the whole class of functions with the same name,
		/// and the actual function to be invoked will be chosen based
		/// on the number of arguments passed.
		/// </summary>
		/// <remarks>
		/// This method is called by the evaluator for each invocation
		/// in an expression.
		/// </remarks>
		object Invoke(Function target, params object[] args);

		#region Operational Semantics

		// These methods are called by the EvaluatorEngine
		// to perform operations on values, or to compare
		// values. The specific implementation defines the
		// behaviour, for example with respect to null.

		object Add(object x, object y);

		object Sub(object x, object y);

		object Mul(object x, object y);

		object Div(object x, object y);

		object Rem(object x, object y);

		object Pos(object value);

		object Neg(object value);

		object Not(object value);

		object And(object x, object y);

		object Or(object x, object y);

		bool IsType(object value, string type);

		bool IsFalse(object value);

		bool IsTrue(object value);

		/// <summary>
		/// Establish a partial ordering of the values in our universe.
		/// Return negative, zero, or positive, if <paramref name="x"/>
		/// is less than, equal to, or greater than <paramref name="y"/>.
		/// Return <c>null</c> if the two values are not comparable;
		/// the evaluator will cope with this special result.
		/// Throw an exception if comparing the two values is considered
		/// an error; the evaluator will not handle the exception.
		/// </summary>
		int? Compare(object x, object y);

		#endregion
	}
}
