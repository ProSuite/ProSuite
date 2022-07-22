using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class CreateInstanceConfigurationCommand : AddItemCommandBase<InstanceDescriptorItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateInstanceConfigurationCommand"/> class.
		/// </summary>
		/// <param name="item">The test descriptor item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CreateInstanceConfigurationCommand([NotNull] InstanceDescriptorItem item,
		                                          [NotNull] IApplicationController
			                                          applicationController)
			: base(item, applicationController) { }

		public override string Text => $"Create {Item.TypeName} Configuration...";

		protected override void ExecuteCore()
		{
			// TODO allow selection of target category

			InstanceConfiguration configuration = Item.CreateConfiguration();

			InstanceConfigurationsItem instanceConfigurationsItem =
				FindConfigurationsItem(configuration);

			instanceConfigurationsItem.AddConfigurationItem(configuration);
		}

		private InstanceConfigurationsItem FindConfigurationsItem(
			InstanceConfiguration configuration)
		{
			object foundItem;
			switch (configuration)
			{
				case TransformerConfiguration _:
					foundItem = ApplicationController
						.FindFirstItem<TransformerConfigurationsItem>();
					break;
				case IssueFilterConfiguration _:
					foundItem = ApplicationController
						.FindFirstItem<IssueFilterConfigurationsItem>();
					break;
				case RowFilterConfiguration _:
					foundItem = ApplicationController
						.FindFirstItem<RowFilterConfigurationsItem>();
					break;
				default:
					throw new NotImplementedException(
						$"Unsupported configuration type: {configuration}");
			}

			Assert.NotNull(foundItem, $"Configurations item not found for {configuration.Name}");

			InstanceConfigurationsItem result = (InstanceConfigurationsItem) foundItem;

			return result;
		}
	}
}
