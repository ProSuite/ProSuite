using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Represents a field in an objectclass
	/// </summary>
	public class ObjectAttribute : Attribute
	{
		[UsedImplicitly] private bool? _readOnly;
		[UsedImplicitly] private bool? _isObjectDefining;
		[UsedImplicitly] private ObjectAttributeType _objectAttributeType;
		[UsedImplicitly] private VariantValue _nonApplicableValue;

		[UsedImplicitly] private ObjectDataset _dataset;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectAttribute"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectAttribute() { }

		public ObjectAttribute([NotNull] string name,
		                       FieldType fieldType,
		                       [CanBeNull] ObjectAttributeType attributeType = null)
			: base(name, fieldType)
		{
			_objectAttributeType = attributeType;
		}

		#endregion

		public virtual ObjectAttributeType ObjectAttributeType
		{
			get { return _objectAttributeType; }
			set
			{
				_objectAttributeType = value;
				OnPropertyChanged();
			}
		}

		public bool ReadOnly
		{
			get
			{
				if (_readOnly.HasValue)
				{
					return _readOnly.Value;
				}

				return ObjectAttributeType != null && ObjectAttributeType.ReadOnly;
			}
			set
			{
				_readOnly = value;
				OnPropertyChanged();
			}
		}

		public bool? ReadOnlyOverride
		{
			get { return _readOnly; }
			set
			{
				_readOnly = value;
				OnPropertyChanged();
			}
		}

		public object NonApplicableValue
		{
			get
			{
				return _nonApplicableValue != null
					       ? _nonApplicableValue.Value
					       : DBNull.Value;
			}
			set
			{
				// TODO: TEST delete / re-add field, with different data type (string -> int, so cast would fail)

				if (value is DBNull || value == null)
				{
					_nonApplicableValue = null;
					return;
				}

				if (! VariantValueFactory.IsSupported(FieldType))
				{
					// a value other than null/DBNull was supplied, but the field is not 
					// supported --> throw exception
					throw new ArgumentException(
						@"A non-null value was supplied as non-applicable value, " +
						$@"but the field type is not supported. Field: {Name} Value: {value}",
						nameof(value));
				}

				_nonApplicableValue = VariantValueFactory.Create(FieldType, value);
			}
		}

		protected override bool IsTableDeleted => _dataset != null && _dataset.Deleted;

		[UsedImplicitly]
		public bool IsObjectDefining
		{
			get
			{
				if (_isObjectDefining.HasValue)
				{
					return _isObjectDefining.Value;
				}

				return ObjectAttributeType != null && ObjectAttributeType.IsObjectDefining;
			}
			set
			{
				if (Equals(_isObjectDefining, value))
				{
					return;
				}

				_isObjectDefining = value;

				OnPropertyChanged();
			}
		}

		public bool? IsObjectDefiningOverride
		{
			get { return _isObjectDefining; }
			set
			{
				if (value == _isObjectDefining)
				{
					return;
				}

				_isObjectDefining = value;
				OnPropertyChanged();
			}
		}

		public void InheritReadonly()
		{
			_readOnly = null;
		}

		public override DdxModel Model => _dataset?.Model;

		public ObjectDataset Dataset
		{
			get { return _dataset; }
			internal set { _dataset = value; }
		}

		[CanBeNull]
		public AttributeRole Role => _objectAttributeType != null
			                             ? ObjectAttributeType.AttributeRole
			                             : null;

		#region Object overrides

		public override string ToString()
		{
			return string.Format("{0} (id={1} ds={2})",
			                     Name, Id, _dataset);
		}

		public override int GetHashCode()
		{
			return (Name != null
				        ? Name.GetHashCode()
				        : 0) +
			       29 * (_dataset != null
				             ? _dataset.GetHashCode()
				             : 0);
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var attribute = obj as ObjectAttribute;
			if (attribute == null)
			{
				return false;
			}

			if (! Equals(Name, attribute.Name))
			{
				return false;
			}

			if (! Equals(_dataset, attribute._dataset))
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Non-public members

		protected void OnPropertyChanged()
		{
			_dataset?.AttributeChanged(this);
		}

		#endregion
	}
}
