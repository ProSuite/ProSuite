using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class CopyQualitySpecificationCommand : ItemCommandBase<QualitySpecificationItem>
	{
		private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="CopyQualitySpecificationCommand"/> class.
		/// </summary>
		static CopyQualitySpecificationCommand()
		{
			_image = Resources.Copy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CopyQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="item">The quality specification item.</param>
		/// <param name="applicationController"></param>
		public CopyQualitySpecificationCommand(
			[NotNull] QualitySpecificationItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override Image Image => _image;

		public override string Text => "Create Copy...";

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
