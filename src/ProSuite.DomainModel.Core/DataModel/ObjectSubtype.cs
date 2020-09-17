using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// An object category that extends a Gdb subtype by additional 
	/// attribute criteria (e.g. "10m_road over a bridge")
	/// </summary>
	public class ObjectSubtype : ObjectCategory
	{
		[UsedImplicitly] private readonly ObjectType _objectType;

		[UsedImplicitly] private readonly IList<ObjectSubtypeCriterion> _criteria =
			new List<ObjectSubtypeCriterion>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtype"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		public ObjectSubtype() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtype"/> class.
		/// </summary>
		/// <param name="objectType">The object type that this subtype belongs to.</param>
		/// <param name="name">The name for the object subtype.</param>
		public ObjectSubtype([NotNull] ObjectType objectType, string name)
			: base(objectType.ObjectDataset, name)
		{
			_objectType = objectType;
		}

		#endregion

		public override bool CanChangeName => true;

		/// <summary>
		/// Gets the objecttype that this subtype belongs to.
		/// </summary>
		/// <value>The object type.</value>
		public ObjectType ObjectType => _objectType;

		public override int SubtypeCode => _objectType?.SubtypeCode ?? -1;

		/// <summary>
		/// Gets the additional attribute criteria that make up this object subtype.
		/// </summary>
		/// <value>The criteria.</value>
		[NotNull]
		public IList<ObjectSubtypeCriterion> Criteria =>
			new ReadOnlyList<ObjectSubtypeCriterion>(_criteria);

		[NotNull]
		public ObjectSubtypeCriterion AddCriterion([NotNull] string attributeName,
		                                           object attributeValue,
		                                           VariantValueType valueType =
			                                           VariantValueType.Null)
		{
			Assert.ArgumentNotNullOrEmpty(attributeName, nameof(attributeName));

			ObjectAttribute attribute =
				_objectType.ObjectDataset.GetAttribute(attributeName);

			Assert.NotNull(attribute, "Attribute {0} not found for {1}",
			               attributeName,
			               _objectType.ObjectDataset.Name);

			return AddCriterion(attribute, attributeValue, valueType);
		}

		public bool RemoveCriterion([NotNull] ObjectAttribute attribute)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			foreach (ObjectSubtypeCriterion criteria in _criteria)
			{
				if (Equals(criteria.Attribute, attribute))
				{
					_criteria.Remove(criteria);
					return true;
				}
			}

			return false;
		}

		[NotNull]
		public ObjectSubtypeCriterion AddCriterion([NotNull] ObjectAttribute attribute,
		                                           object attributeValue,
		                                           VariantValueType variantValueType =
			                                           VariantValueType.Null)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			var criterion = new ObjectSubtypeCriterion(attribute, attributeValue,
			                                           variantValueType);

			if (_criteria.Contains(criterion))
			{
				throw new ArgumentException(
					string.Format(
						"Criterion already exists in collection. Attribue={0}, Value={1}",
						attribute.Name, attributeValue ?? "<null>"));
			}

			_criteria.Add(criterion);

			return criterion;
		}

		public bool ContainsCriterion([NotNull] ObjectAttribute attribute)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));

			return _criteria.Any(criterion => Equals(criterion.Attribute, attribute));
		}

		protected override void ValidateForPersistenceCore(Notification notification)
		{
			base.ValidateForPersistenceCore(notification);

			if (_criteria.Count == 0)
			{
				notification.RegisterMessage("Criteria",
				                             "At least one attribute criterion must be defined",
				                             Severity.Error);
			}
		}
	}
}