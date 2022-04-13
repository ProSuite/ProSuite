using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectSubtypeCriterionTableRow
	{
		private readonly ObjectSubtypeCriterion _criterion;

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSubtypeCriterion"/> class.
		/// </summary>
		/// <param name="criterion">The criterion.</param>
		public ObjectSubtypeCriterionTableRow(ObjectSubtypeCriterion criterion)
		{
			Assert.ArgumentNotNull(criterion, nameof(criterion));

			_criterion = criterion;
		}

		public override string ToString()
		{
			return AttributeName;
		}

		public string AttributeName => _criterion.Attribute.Name;

		public ObjectAttribute Attribute => _criterion.Attribute;

		public string AttributeValue
		{
			get
			{
				if (_criterion.AttributeValue != null)
				{
					return _criterion.AttributeValue.ToString();
				}

				return null;
			}
			set { _criterion.AttributeValue = value; }
		}

		public string AttributeType => _criterion.AttributeValueType.ToString();
	}
}
