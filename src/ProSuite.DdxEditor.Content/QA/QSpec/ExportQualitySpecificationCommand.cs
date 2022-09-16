using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class ExportQualitySpecificationCommand :
		ExchangeQualitySpecificationCommand<QualitySpecificationItem>
	{
		[NotNull] private readonly IQualitySpecificationContainerItem _containerItem;
		private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="ExportQualitySpecificationCommand"/> class.
		/// </summary>
		static ExportQualitySpecificationCommand()
		{
			_image = Resources.Export;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="qualitySpecificationItem">The quality specification item.</param>
		/// <param name="containerItem">The quality specification container item</param>
		/// <param name="applicationController">The application controller.</param>
		public ExportQualitySpecificationCommand(
			[NotNull] QualitySpecificationItem qualitySpecificationItem,
			[NotNull] IQualitySpecificationContainerItem containerItem,
			[NotNull] IApplicationController applicationController)
			: base(qualitySpecificationItem, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override Image Image => _image;

		public override string Text => "Export...";

		protected override bool EnabledCore => ! Item.IsDirty;

		protected override void ExecuteCore()
		{
			QualitySpecification qualitySpecification = Item.GetQualitySpecification();
			Assert.NotNull(qualitySpecification, "Quality specification no longer exists");

			using (var form = new ExportQualitySpecificationsForm(FileFilter,
				       DefaultExtension))
			{
				new ExportQualitySpecificationsController(form,
				                                          _containerItem.GetQualitySpecifications(),
				                                          new[] {qualitySpecification});

				DialogResult result = UIEnvironment.ShowDialog(form);

				if (result != DialogResult.OK)
				{
					return;
				}

				Item.ExportQualitySpecifications(form.QualitySpecificationsByFileName,
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
