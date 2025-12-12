using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

/// <summary>
/// A work list filter expression associated with a <see cref="WorkListFilterDefinition"/>,
/// applicable to a specific source class of the work list.
/// </summary>
public class WorkListFilterDefinitionExpression
{
	private readonly WorkListFilterDefinition _filterDefinition;

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkListFilterDefinitionExpression"/> class.
	/// </summary>
	/// <remarks>Required for deserialization</remarks>
	public WorkListFilterDefinitionExpression() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkListFilterDefinitionExpression"/> class.
	/// </summary>
	/// <param name="filterDefinition">The definition.</param>
	/// <param name="expression">The expression.</param>
	public WorkListFilterDefinitionExpression([NotNull] WorkListFilterDefinition filterDefinition,
	                                          [CanBeNull] string expression)
	{
		Assert.ArgumentNotNull(filterDefinition, nameof(filterDefinition));

		_filterDefinition = filterDefinition;
		Expression = expression;
	}

	#endregion

	[NotNull]
	public WorkListFilterDefinition FilterDefinition => _filterDefinition;

	[CanBeNull]
	public string Expression { get; set; }

	#region Object overrides

	public override string ToString()
	{
		return string.Format("{0}: {1}", _filterDefinition.Name, Expression ?? "<null>");
	}

	public bool Equals(WorkListFilterDefinitionExpression other)
	{
		if (other == null)
		{
			return false;
		}

		return Equals(_filterDefinition, other._filterDefinition) &&
		       Equals(Expression, other.Expression);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		return Equals(obj as WorkListFilterDefinitionExpression);
	}

	public override int GetHashCode()
	{
		return
			_filterDefinition.GetHashCode() +
			29 * (Expression != null
				      ? Expression.GetHashCode()
				      : 0);
	}

	#endregion
}
