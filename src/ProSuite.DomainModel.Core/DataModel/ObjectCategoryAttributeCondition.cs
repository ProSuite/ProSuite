using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class ObjectCategoryAttributeCondition : ObjectCategoryAttributeConstraint
	{
		[UsedImplicitly] private string _expression;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryAttributeCondition"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectCategoryAttributeCondition() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategoryAttributeCondition"/> class.
		/// </summary>
		/// <param name="objectCategory">The object category.</param>
		/// <param name="objectAttribute">The object attribute.</param>
		/// <param name="expression">The expression.</param>
		public ObjectCategoryAttributeCondition([NotNull] ObjectCategory objectCategory,
		                                        [NotNull] ObjectAttribute objectAttribute,
		                                        [NotNull] string expression)
			: base(objectCategory, objectAttribute)
		{
			Assert.ArgumentNotNullOrEmpty(expression, nameof(expression));

			_expression = expression;
		}

		#endregion

		[NotNull]
		public string Expression
		{
			get { return _expression; }
			set
			{
				Assert.ArgumentNotNullOrEmpty(value, nameof(value));
				_expression = value;
			}
		}

		protected override ObjectCategoryAttributeConstraint CreateCopy(
			ObjectCategory targetObjectCategory, ObjectAttribute targetAttribute)
		{
			Assert.ArgumentNotNull(targetObjectCategory, nameof(targetObjectCategory));
			Assert.ArgumentNotNull(targetAttribute, nameof(targetAttribute));

			return new ObjectCategoryAttributeCondition(targetObjectCategory,
			                                            targetAttribute,
			                                            _expression);
		}
	}
}
