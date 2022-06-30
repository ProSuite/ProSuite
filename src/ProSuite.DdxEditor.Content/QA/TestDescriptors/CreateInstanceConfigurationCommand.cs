using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class CreateInstanceConfigurationCommand<T> : AddItemCommandBase<InstanceDescriptorItem> where T : Item
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateQualityConditionCommand"/> class.
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

			T configurationsItem = ApplicationController.FindFirstItem<T>();

			Assert.NotNull(configurationsItem, "Configuration item not found");

			InstanceConfigurationsItem instanceConfigurationsItem = (InstanceConfigurationsItem) (object)configurationsItem;

			instanceConfigurationsItem.AddConfigurationItem(configuration);
		}
	}
}
