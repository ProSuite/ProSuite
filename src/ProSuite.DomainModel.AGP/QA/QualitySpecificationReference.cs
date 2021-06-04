namespace ProSuite.DomainModel.AGP.QA
{
	public class QualitySpecificationRef
	{
		public string Name { get; }
		public int Id { get; }

		public QualitySpecificationRef(int id, string name)
		{
			Id = id;
			Name = name;
		}

		protected bool Equals(QualitySpecificationRef other)
		{
			return Id == other.Id;
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

			return Equals((QualitySpecificationRef) obj);
		}

		public override int GetHashCode()
		{
			return Id;
		}

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Id)}: {Id}";
		}
	}
}
