using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class AddTestDescriptorsFromAssemblyCommand :
		AddItemCommandBase<TestDescriptorsItem>
	{
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddTestDescriptorsFromAssemblyCommand"/> class.
		/// </summary>
		/// <param name="testDescriptorsItem">The test descriptors item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddTestDescriptorsFromAssemblyCommand(
			[NotNull] TestDescriptorsItem testDescriptorsItem,
			[NotNull] IApplicationController applicationController)
			: base(testDescriptorsItem, applicationController)
		{
			_applicationController = applicationController;
		}

		public override string Text => "Add Test Descriptors from Assembly";

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			Item.AddTestDescriptors(dllFilePath, _applicationController);
		}
	}
}
