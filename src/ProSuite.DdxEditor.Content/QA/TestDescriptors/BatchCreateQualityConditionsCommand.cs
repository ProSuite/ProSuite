using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class BatchCreateQualityConditionsCommand :
		AddItemCommandBase<TestDescriptorItem>
	{
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="BatchCreateQualityConditionsCommand"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		public BatchCreateQualityConditionsCommand(
			[NotNull] TestDescriptorItem item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController)
		{
			_applicationController = applicationController;
		}

		public override string Text => "Batch Create Quality Conditions...";

		protected override bool EnabledCore => Item.CanBatchCreateQualityConditions();

		protected override void ExecuteCore()
		{
			try
			{
				DataQualityCategory category;
				if (! Item.BatchCreateQualityConditions(ApplicationController.Window,
				                                        out category))
				{
					return;
				}

				if (category == null)
				{
					_applicationController.RefreshFirstItem<QualityConditionsItem>();
				}
				else
				{
					_applicationController.RefreshItem(category);
				}

				// reload test descriptor item to update list of quality conditions
				_applicationController.ReloadCurrentItem();
			}
			catch
			{
				_applicationController.ReloadCurrentItem();

				throw;
			}
		}
	}
}
