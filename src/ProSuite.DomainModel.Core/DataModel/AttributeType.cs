using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class AttributeType : EntityWithMetadata, INamed, IAnnotated,
	                                      IEquatable<AttributeType>
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AttributeType"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected AttributeType() { }

		protected AttributeType([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public override string ToString()
		{
			return _name;
		}

		public bool Equals(AttributeType attributeType)
		{
			if (attributeType == null)
			{
				return false;
			}

			return Equals(_name, attributeType._name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as AttributeType);
		}

		public override int GetHashCode()
		{
			return _name != null
				       ? _name.GetHashCode()
				       : 0;
		}
	}
}