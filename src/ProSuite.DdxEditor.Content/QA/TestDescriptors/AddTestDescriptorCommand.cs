using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class AddTestDescriptorCommand : AddItemCommandBase<TestDescriptorsItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddTestDescriptorCommand"/> class.
		/// </summary>
		/// <param name="testDescriptorsItem">The test descriptors item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddTestDescriptorCommand(
			[NotNull] TestDescriptorsItem testDescriptorsItem,
			[NotNull] IApplicationController applicationController)
			: base(testDescriptorsItem, applicationController) { }

		public override string Text => "Add Test Descriptor";

		protected override void ExecuteCore()
		{
			Item.AddTestDescriptorItem();
		}
	}
}
