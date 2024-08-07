using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class GeometryType : EntityWithMetadata
	{
		[UsedImplicitly] private string _name;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryType"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected GeometryType() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeometryType"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		protected GeometryType([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
		}

		#endregion

		[NotNull]
		public string Name => _name;

		[NotNull]
		public GeometryType Clone()
		{
			return CreateClone();
		}

		[NotNull]
		protected virtual GeometryType CreateClone()
		{
			var clone = (GeometryType) Activator.CreateInstance(GetType(), _name);

			clone._name = _name;

			return clone;
		}

		#region Overrides of Object

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

			return Equals((GeometryType) obj);
		}

		private bool Equals(GeometryType other)
		{
			return _name.Equals(other._name, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			// ReSharper disable once NonReadonlyMemberInGetHashCode
			return StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
		}

		#endregion
	}
}
