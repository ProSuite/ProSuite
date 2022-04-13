using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Framework;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ExportDatasetDependenciesForQualitySpecificationCommand :
		ExportDatasetDependenciesCommandBase<QualitySpecificationItem>
	{
		[NotNull] private readonly IQualitySpecificationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="qualitySpecificationItem">The quality specification item.</param>
		/// <param name="containerItem">The quality specification container item</param>
		/// <param name="applicationController">The application controller.</param>
		public ExportDatasetDependenciesForQualitySpecificationCommand(
			[NotNull] QualitySpecificationItem qualitySpecificationItem,
			[NotNull] IQualitySpecificationContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(qualitySpecificationItem, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		protected override void ExecuteCore()
		{
			QualitySpecification qualitySpecification = Item.GetQualitySpecification();
			Assert.NotNull(qualitySpecification, "Quality specification no longer exists");

			using (var form = new ExportDatasetDependenciesForm(FileFilter,
			                                                    DefaultExtension))
			{
				new ExportDatasetDependenciesController(form,
				                                        _containerItem.GetQualitySpecifications(),
				                                        new[] {qualitySpecification});

				DialogResult result = UIEnvironment.ShowDialog(form);

				if (result != DialogResult.OK)
				{
					return;
				}

				var options =
					new ExportDatasetDependenciesOptions(
						form.ExportBidirectionalDependenciesAsUndirectedEdges,
						form.ExportModelsAsParentNodes,
						form.IncludeSelfDependencies);

				Item.ExportDatasetDependencies(form.QualitySpecificationsByFileName,
				                               form.DeletableFiles, options);
			}
		}
	}
}
