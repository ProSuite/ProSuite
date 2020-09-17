using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ObjectSubtypeCriterion : IEquatable<ObjectSubtypeCriterion>
	{
		[UsedImplicitly] private readonly ObjectAttribute _attribute;
		[UsedImplicitly] private readonly VariantValue _attributeValue;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtypeCriterion"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectSubtypeCriterion() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtypeCriterion"/> class.
		/// </summary>
		/// <param name="attribute">The attribute that the criterion is based on.</param>
		/// <param name="attributeValue">The attribute value.</param>
		/// <param name="valueType">Type of the value.</param>
		public ObjectSubtypeCriterion([NotNull] ObjectAttribute attribute,
		                              [CanBeNull] object attributeValue,
		                              VariantValueType valueType = VariantValueType.Null)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			_attribute = attribute;

			if (valueType == VariantValueType.Null)
			{
				// attributeValue = null; // "" can not be cast to numeric
				_attributeValue = VariantValueFactory.Create(attribute, attributeValue);
			}
			else
			{
				_attributeValue = new VariantValue(attributeValue, valueType);
			}
		}

		#endregion

		public ObjectAttribute Attribute => _attribute;

		public object AttributeValue
		{
			get { return _attributeValue.Value; }
			set { _attributeValue.Value = value; }
		}

		public VariantValueType AttributeValueType => _attributeValue.Type;

		public override string ToString()
		{
			return string.Format("Attribute={0} value={1}",
			                     _attribute.Name,
			                     _attributeValue.Value ?? "<null>");
		}

		public bool Equals(ObjectSubtypeCriterion objectSubtypeCriterion)
		{
			if (objectSubtypeCriterion == null)
			{
				return false;
			}

			return
				Equals(_attribute, objectSubtypeCriterion._attribute) &&
				Equals(_attributeValue,
				       objectSubtypeCriterion._attributeValue);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as ObjectSubtypeCriterion);
		}

		public override int GetHashCode()
		{
			return _attribute.GetHashCode() + 29 * _attributeValue.GetHashCode();
		}
	}
}