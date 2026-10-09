using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Core.Signatures
{
	/// <summary>
	/// The parameter values available to
	/// <see cref="AlgorithmDefinition.ValidateParameters"/> /
	/// <see cref="TestFactoryDefinition.ValidateParameters"/> when validating a
	/// configured algorithm instance. Implemented by the (headless) edit model.
	/// </summary>
	public interface IParameterValidationContext
	{
		/// <summary>
		/// The constructor id resolved from the current fill state
		/// (<c>AlgorithmSignatureFactory.FactoryConstructorId</c> for factories).
		/// </summary>
		int ResolvedConstructorId { get; }

		/// <summary>
		/// The current value of the parameter: a scalar, an
		/// <see cref="ITableSchemaDef"/>, or a list thereof for list parameters.
		/// Null if not filled.
		/// </summary>
		[CanBeNull]
		object GetValue([NotNull] string parameterName);

		/// <summary>
		/// The table schema of a dataset parameter, or null when the parameter is not
		/// filled or is fed by a transformer whose output schema is not available.
		/// </summary>
		[CanBeNull]
		ITableSchemaDef GetTableSchema([NotNull] string parameterName);
	}
}
