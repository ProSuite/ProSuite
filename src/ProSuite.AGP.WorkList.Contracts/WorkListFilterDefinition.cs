using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

/// <summary>
/// A filter for a work list that can be selected by the user from a list of predefined filters.
/// </summary>
public class WorkListFilterDefinition
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkListFilterDefinition"/> class.
	/// </summary>
	/// <remarks>For deserialization</remarks>
	public WorkListFilterDefinition() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkListFilterDefinition"/> class.
	/// </summary>
	/// <param name="name">The name.</param>
	public WorkListFilterDefinition([NotNull] string name)
	{
		Assert.ArgumentNotNullOrEmpty(name, nameof(name));

		Name = name;
	}

	#endregion

	public string Name { get; }

	#region Object overrides

	public override string ToString()
	{
		return Name;
	}

	public bool Equals(WorkListFilterDefinition other)
	{
		if (other == null)
		{
			return false;
		}

		return Equals(Name, other.Name);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		return Equals(obj as WorkListFilterDefinition);
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	#endregion
}
