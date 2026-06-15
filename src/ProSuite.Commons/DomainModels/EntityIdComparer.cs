using System.Collections.Generic;

namespace ProSuite.Commons.DomainModels
{
	/// <summary>
	/// Comparer for entities to check for equality with the id of the entity
	/// </summary>
	public class EntityIdComparer : IEqualityComparer<Entity>
	{
		public bool Equals(Entity x, Entity y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (x == null || y == null) return false;
			return x.Id == y.Id;
		}

		public int GetHashCode(Entity obj)
		{
			return obj.Id.GetHashCode();
		}
	}
}
