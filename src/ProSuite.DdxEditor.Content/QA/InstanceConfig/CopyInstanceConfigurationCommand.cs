using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class CopyInstanceConfigurationCommand : CopyItemCommandBase<InstanceConfigurationItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CopyInstanceConfigurationCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CopyInstanceConfigurationCommand(
			[NotNull] InstanceConfigurationItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		protected override bool EnabledCore => base.EnabledCore && Item.CanCreateCopy;

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
