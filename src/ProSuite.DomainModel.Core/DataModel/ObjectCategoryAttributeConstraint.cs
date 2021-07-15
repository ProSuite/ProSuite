using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class ObjectCategoryAttributeConstraint : EntityWithMetadata
	{
		[UsedImplicitly] private readonly ObjectAttribute _objectAttribute;
		[UsedImplicitly] private readonly ObjectCategory _objectCategory;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryAttributeConstraint"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate.</remarks>
		protected ObjectCategoryAttributeConstraint() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryAttributeConstraint"/> class.
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <param name="objectAttribute">The object attribute.</param>
		protected ObjectCategoryAttributeConstraint([NotNull] ObjectCategory objectCategory,
		                                            [NotNull] ObjectAttribute objectAttribute)
		{
			Assert.ArgumentNotNull(objectCategory, nameof(objectCategory));
			Assert.ArgumentNotNull(objectAttribute, nameof(objectAttribute));

			_objectCategory = objectCategory;
			_objectAttribute = objectAttribute;
		}

		#endregion

		[NotNull]
		public ObjectAttribute ObjectAttribute => _objectAttribute;

		[NotNull]
		public ObjectCategory ObjectCategory => _objectCategory;

		[CanBeNull]
		public ObjectCategoryAttributeConstraint TryCreateCopy(
			[NotNull] ObjectDataset targetDataset)
		{
			Assert.ArgumentNotNull(targetDataset, nameof(targetDataset));

			ObjectAttribute targetAttribute =
				targetDataset.GetAttribute(ObjectAttribute.Name);

			if (targetAttribute == null)
			{
				return null;
			}

			ObjectCategory targetObjectCategory =
				GetEquivalentObjectCategory(targetDataset, ObjectCategory);

			return targetObjectCategory == null
				       ? null
				       : CreateCopy(targetObjectCategory, targetAttribute);
		}

		[NotNull]
		protected virtual ObjectCategoryAttributeConstraint CreateCopy(
			[NotNull] ObjectCategory targetObjectCategory,
			[NotNull] ObjectAttribute targetAttribute)
		{
			// NOTE: can't make abstract because method is not cls compliant
			throw new NotImplementedException("Not overridden in subclass");
		}

		/// <summary>
		/// Gets an equivalent object category on a target dataset for a given source
		/// object category.
		/// </summary>
		/// <param name="targetDataset">The target dataset.</param>
		/// <param name="sourceObjectCategory">The source object category.</param>
		/// <returns></returns>
		[CanBeNull]
		private static ObjectCategory GetEquivalentObjectCategory(
			[NotNull] IObjectDataset targetDataset,
			[NotNull] ObjectCategory sourceObjectCategory)
		{
			Assert.ArgumentNotNull(targetDataset, nameof(targetDataset));
			Assert.ArgumentNotNull(sourceObjectCategory, nameof(sourceObjectCategory));

			ObjectType targetObjectType =
				targetDataset.GetObjectType(sourceObjectCategory.SubtypeCode);

			if (targetObjectType == null)
			{
				return null;
			}

			if (! (sourceObjectCategory is ObjectSubtype))
			{
				return targetObjectType;
			}

			// object subtypes are not (yet) supported to have constraints, but 
			// this restriction is not assumed here

			var sourceObjectSubtype = (ObjectSubtype) sourceObjectCategory;

			foreach (ObjectSubtype targetObjectSubtype in targetObjectType.ObjectSubtypes)
			{
				if (Equals(targetObjectSubtype.Name, sourceObjectSubtype.Name))
				{
					return targetObjectSubtype;
				}
			}

			// no object subtype with matching name found
			return null;
		}
	}
}
