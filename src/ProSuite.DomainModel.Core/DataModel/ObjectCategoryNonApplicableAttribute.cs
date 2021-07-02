using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ObjectCategoryNonApplicableAttribute : ObjectCategoryAttributeConstraint
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryNonApplicableAttribute"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectCategoryNonApplicableAttribute() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryNonApplicableAttribute"/> class.
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <param name="objectAttribute">The object attribute.</param>
		public ObjectCategoryNonApplicableAttribute([NotNull] ObjectCategory objectCategory,
		                                            [NotNull] ObjectAttribute objectAttribute)
			: base(objectCategory, objectAttribute) { }

		#endregion

		public object GetNonApplicableValue()
		{
			return ObjectAttribute.NonApplicableValue;
		}

		protected override ObjectCategoryAttributeConstraint CreateCopy(
			ObjectCategory targetObjectCategory, ObjectAttribute targetAttribute)
		{
			Assert.ArgumentNotNull(targetObjectCategory, nameof(targetObjectCategory));
			Assert.ArgumentNotNull(targetAttribute, nameof(targetAttribute));

			return new ObjectCategoryNonApplicableAttribute(targetObjectCategory,
			                                                targetAttribute);
		}
	}
}
