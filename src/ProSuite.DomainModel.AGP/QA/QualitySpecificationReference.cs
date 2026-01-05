using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AGP.QA;

public class QualitySpecificationReference : IQualitySpecificationReference
{
	public string Name { get; }

	public string Connection { get; }

	public int Id { get; }

	public QualitySpecificationReference(int id, string name, string connection = "")
	{
		Id = id;
		Name = name;
		Connection = connection;
	}

	protected bool Equals(QualitySpecificationReference other)
	{
		return Id == other.Id && Name == other.Name;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != this.GetType())
		{
			return false;
		}

		return Equals((QualitySpecificationReference) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
		}
	}

	public bool Equals(IQualitySpecificationReference other)
	{
		if (other is QualitySpecificationReference otherSpecificationReference)
		{
			return Equals(otherSpecificationReference);
		}

		return false;
	}

	public override string ToString()
	{
		return $"{nameof(Name)}: {Name}, {nameof(Id)}: {Id}";
	}
}
