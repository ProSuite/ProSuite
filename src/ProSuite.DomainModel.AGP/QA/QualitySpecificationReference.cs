namespace ProSuite.DomainModel.AGP.QA
{
	public class QualitySpecificationReference
	{
		public string Name { get; }
		public int Id { get; }

		public QualitySpecificationReference(int id, string name)
		{
			Id = id;
			Name = name;
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

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Id)}: {Id}";
		}
	}
}
