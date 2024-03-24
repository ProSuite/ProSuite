using System;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Reference to a quality specification which can be used for display purposes.
	/// Implementers shall add relevant identifiers that allows the verification system
	/// to load the actual specification.
	/// </summary>
	public interface IQualitySpecificationReference : IEquatable<IQualitySpecificationReference>
	{
		int Id { get; }

		string Name { get; }

		string Connection { get; }
	}
}
