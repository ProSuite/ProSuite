using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AddInstanceDescriptorsFromAssemblyCommand<T> : AddItemCommandBase<Item>
		where T : InstanceDescriptor
	{
		[NotNull] private readonly InstanceDescriptorsItem<T> _descriptorsItem;
		[NotNull] private readonly string _instanceTypeDisplayName;

		public AddInstanceDescriptorsFromAssemblyCommand(
			[NotNull] InstanceDescriptorsItem<T> descriptorsItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] string instanceTypeDisplayName)
			: base(descriptorsItem, applicationController)
		{
			_descriptorsItem = descriptorsItem;
			_instanceTypeDisplayName = instanceTypeDisplayName;
		}

		public override string Text => $"Add {_instanceTypeDisplayName} from Assembly";

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			_descriptorsItem.AddInstanceDescriptors(dllFilePath, ApplicationController);
		}
	}
}
