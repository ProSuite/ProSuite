using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AddInstanceDescriptorsFromAssemblyCommand<T> :
		AddItemCommandBase<InstanceDescriptorsItem<T>> where T : InstanceDescriptor
	{
		[NotNull] private readonly string _instanceTypeDisplayName;

		public AddInstanceDescriptorsFromAssemblyCommand(
			[NotNull] InstanceDescriptorsItem<T> descriptorsItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] string instanceTypeDisplayName)
			: base(descriptorsItem, applicationController)
		{
			_instanceTypeDisplayName = instanceTypeDisplayName;
		}

		public override string Text => $"Add {_instanceTypeDisplayName}s from .Net Assembly";

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			Item.AddInstanceDescriptors(dllFilePath, ApplicationController);
		}
	}
}
