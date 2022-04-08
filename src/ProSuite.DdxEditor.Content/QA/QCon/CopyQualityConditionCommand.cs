using System.Drawing;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	internal class CopyQualityConditionCommand : ItemCommandBase<QualityConditionItem>
	{
		private static readonly Image _image;
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes the <see cref="CopyQualityConditionCommand"/> class.
		/// </summary>
		static CopyQualityConditionCommand()
		{
			_image = Resources.Copy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CopyQualityConditionCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public CopyQualityConditionCommand(QualityConditionItem item,
		                                   IApplicationController applicationController)
			: base(item)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;
		}

		public override Image Image => _image;

		public override string Text => "Create Copy...";

		protected override bool EnabledCore =>
			! _applicationController.HasPendingChanges && Item.CanCreateCopy;

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
