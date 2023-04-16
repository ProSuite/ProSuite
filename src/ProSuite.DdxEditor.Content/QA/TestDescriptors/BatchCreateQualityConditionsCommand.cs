using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class BatchCreateQualityConditionsCommand : AddItemCommandBase<TestDescriptorItem>
	{
		private string _toolTip;

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchCreateQualityConditionsCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public BatchCreateQualityConditionsCommand(
			[NotNull] TestDescriptorItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController) { }

		public override string Text => "Batch Create Quality Conditions...";

		public override string ToolTip => _toolTip;

		protected override bool EnabledCore
		{
			get
			{
				bool enabled =
					Item.CanBatchCreateQualityConditions(out string _,
					                                     out string reason);

				string message =
					$"Operation not supported for test descriptor {Item.Text}: {reason}";

				_toolTip = enabled
					           ? Text
					           : message;
				return enabled;
			}
		}

		protected override void ExecuteCore()
		{
			try
			{
				if (! Item.BatchCreateQualityConditions(ApplicationController.Window,
				                                        out DataQualityCategory category))
				{
					return;
				}

				if (category == null)
				{
					ApplicationController.RefreshFirstItem<QualityConditionsItem>();
				}
				else
				{
					ApplicationController.RefreshItem(category);
				}

				// reload test descriptor item to update list of quality conditions
				ApplicationController.ReloadCurrentItem();
			}
			catch
			{
				ApplicationController.ReloadCurrentItem();

				throw;
			}
		}
	}
}
