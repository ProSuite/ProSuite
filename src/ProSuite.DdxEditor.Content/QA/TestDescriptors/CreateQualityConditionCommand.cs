using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class CreateQualityConditionCommand : AddItemCommandBase<TestDescriptorItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateQualityConditionCommand"/> class.
		/// </summary>
		/// <param name="item">The test descriptor item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CreateQualityConditionCommand([NotNull] TestDescriptorItem item,
		                                     [NotNull] IApplicationController
			                                     applicationController)
			: base(item, applicationController) { }

		public override string Text => "Create Quality Condition...";

		protected override void ExecuteCore()
		{
			// TODO allow selection of target category

			QualityCondition qualityCondition = Item.CreateQualityCondition();

			var qualityConditionsItem =
				ApplicationController.FindFirstItem<QualityConditionsItem>();

			Assert.NotNull(qualityConditionsItem, "Quality conditions item not found");

			qualityConditionsItem.AddQualityConditionItem(qualityCondition);
		}
	}
}
