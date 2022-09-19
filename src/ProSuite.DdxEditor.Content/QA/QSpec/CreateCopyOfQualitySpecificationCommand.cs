using System.Drawing;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class CreateCopyOfQualitySpecificationCommand :
		ItemCommandBase<QualitySpecificationItem>
	{
		private static readonly Image _image;
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes the <see cref="CreateCopyOfQualitySpecificationCommand"/> class.
		/// </summary>
		static CreateCopyOfQualitySpecificationCommand()
		{
			_image = Resources.Copy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CreateCopyOfQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="qualitySpecificationItem">The quality specification item.</param>
		/// <param name="applicationController"></param>
		public CreateCopyOfQualitySpecificationCommand(
			QualitySpecificationItem qualitySpecificationItem,
			IApplicationController applicationController)
			: base(qualitySpecificationItem)
		{
			_applicationController = applicationController;
		}

		public override Image Image => _image;

		public override string Text => "Create Copy...";

		protected override bool EnabledCore => ! _applicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
