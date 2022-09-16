using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ExportQualitySpecificationsCommand :
		ExchangeQualitySpecificationCommand<Item>
	{
		private readonly IQualitySpecificationContainer _container;
		private readonly bool _includeSubCategories;
		private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="ExportQualitySpecificationsCommand"/> class.
		/// </summary>
		static ExportQualitySpecificationsCommand()
		{
			_image = Resources.Export;
		}

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportQualitySpecificationsCommand"/> class.
		/// </summary>
		/// <param name="item">The quality specifications item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="container">The container item for the quality specifications</param>
		/// <param name="includeSubCategories"></param>
		public ExportQualitySpecificationsCommand(
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

		#endregion

		public override Image Image => _image;

		public override string Text => "Export Quality Specifications...";

		protected override bool EnabledCore => ! Item.IsDirty && Item.Children.Count > 0;

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

			using (var form = new ExportQualitySpecificationsForm(FileFilter,
				       DefaultExtension))
			{
				new ExportQualitySpecificationsController(form, specifications);

				DialogResult result = UIEnvironment.ShowDialog(form, ApplicationController.Window);

				if (result != DialogResult.OK)
				{
					return;
				}

				_container.ExportQualitySpecifications(
					form.QualitySpecificationsByFileName,
					form.DeletableFiles,
					form.ExportMetadata,
					form.ExportWorkspaceConnections,
					form.ExportConnectionFilePaths,
					form.ExportAllDescriptors,
					form.ExportAllCategories,
					form.ExportNotes);
			}
		}
	}
}
