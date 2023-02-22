using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	internal class CopyQualityConditionCommand : ItemCommandBase<QualityConditionItem>
	{
		private static readonly Image _image;

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
		public CopyQualityConditionCommand([NotNull] QualityConditionItem item,
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
