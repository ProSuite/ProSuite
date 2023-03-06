using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class CopyQualitySpecificationCommand : CopyItemCommandBase<QualitySpecificationItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CopyQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="item">The quality specification item.</param>
		/// <param name="applicationController"></param>
		public CopyQualitySpecificationCommand(
			[NotNull] QualitySpecificationItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		protected override void ExecuteCore()
		{
			Item.CreateCopy();
		}
	}
}
