using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ExportDatasetDependenciesCommand :
		ExportDatasetDependenciesCommandBase<Item>
	{
		[NotNull] private readonly IQualitySpecificationContainer _container;
		private readonly bool _includeSubCategories;

		public ExportDatasetDependenciesCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] IQualitySpecificationContainer container,
			bool includeSubCategories)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(container, nameof(container));

			_container = container;
			_includeSubCategories = includeSubCategories;
		}

		protected override void ExecuteCore()
		{
			List<QualitySpecification> specifications =
				_container.GetQualitySpecifications(_includeSubCategories)
				          .ToList();

			if (specifications.Count == 0)
			{
				Dialog.Info(ApplicationController.Window, Text,
				            "There are no quality specifications");
				return;
			}

			using (var form = new ExportDatasetDependenciesForm(FileFilter, DefaultExtension))
			{
				new ExportDatasetDependenciesController(form, specifications);

				DialogResult result = UIEnvironment.ShowDialog(form, ApplicationController.Window);

				if (result != DialogResult.OK)
				{
					return;
				}

				var options =
					new ExportDatasetDependenciesOptions(
						form.ExportBidirectionalDependenciesAsUndirectedEdges,
						form.ExportModelsAsParentNodes,
						form.IncludeSelfDependencies);

				_container.ExportDatasetDependencies(form.QualitySpecificationsByFileName,
				                                     form.DeletableFiles, options);
			}
		}
	}
}
