using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	internal class CopyInstanceConfigurationCommand : ItemCommandBase<InstanceConfigurationItem>
	{
		private static readonly Image _image;

		static CopyInstanceConfigurationCommand()
		{
			_image = Resources.Copy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CopyInstanceConfigurationCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CopyInstanceConfigurationCommand(
			[NotNull] InstanceConfigurationItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Create Copy...";

		protected override bool EnabledCore =>
			! ApplicationController.HasPendingChanges && Item.CanCreateCopy;

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
