using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Dependencies
{
	public abstract class DependingItem : IEquatable<DependingItem>
	{
		// there could be hierarchy of further depending items here, if needed. UI could show a tree

		/// <summary>
		/// Initializes a new instance of the <see cref="DependingItem"/> class.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="name">The name.</param>
		protected DependingItem([NotNull] Entity entity, [NotNull] string name)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Entity = entity;
			Name = name;
		}

		[NotNull]
		public Entity Entity { get; }

		[NotNull]
		public string Name { get; }

		public virtual bool CanRemove => true;

		public virtual bool RequiresConfirmation => true;

		public abstract bool RemovedByCascadingDeletion { get; }

		public void RemoveDependency()
		{
			Assert.True(CanRemove, "Cannot remove dependency");

			RemoveDependencyCore();
		}

		protected abstract void RemoveDependencyCore();

		public bool Equals(DependingItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.Entity.Id == Entity.Id &&
			       Equals(other.Name, Name) &&
			       other.GetType() == GetType() &&
			       other.Entity.GetType() == Entity.GetType();
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

			if (! (obj is DependingItem))
			{
				return false;
			}

			return Equals((DependingItem) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = Entity.GetType().GetHashCode();
				result = (result * 397) ^ Entity.Id;
				result = (result * 397) ^ Name.GetHashCode();
				return result;
			}
		}
	}
}
