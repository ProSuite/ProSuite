using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class LinearNetworksItem : EntityTypeItem<LinearNetwork>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static LinearNetworksItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DatasetTypeLinearNetworkOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.DatasetTypeLinearNetworkOverlay);
		}

		public LinearNetworksItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(
				"Linear Networks",
				"Definition of linear networks for a data model")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddLinearNetworkCommand(this, applicationController));
			commands.Add(new ImportLinearNetworksCommand(this, applicationController));
			commands.Add(
				new ExportLinearNetworksCommand(this, applicationController, filterByModel: false));
			commands.Add(
				new ExportLinearNetworksCommand(this, applicationController, filterByModel: true));
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<LinearNetworkTableRow> GetTableRows()
		{
			return _modelBuilder.LinearNetworks.GetAll()
			                    .Select(entity => new LinearNetworkTableRow(entity));
		}

		public IList<ModelTableRow> GetModelTableRows()
		{
			return
				_modelBuilder.ReadOnlyTransaction(
					() =>
						_modelBuilder.Models.GetAll()
						             .Select(entity => new ModelTableRow(entity))
						             .ToList());
		}

		[NotNull]
		public LinearNetworkItem AddLinearNetworkItem()
		{
			var linearNetwork = new LinearNetwork();

			var item = new LinearNetworkItem(_modelBuilder, linearNetwork,
			                                 _modelBuilder.LinearNetworks);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		public void ImportNetworks([NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.NewTransaction(
					() => _modelBuilder.LinearNetworksImporter.Import(fileName));
			}

			_msg.InfoFormat("Linear Networks imported from {0}", fileName);

			RefreshChildren();
		}

		public void ExportNetworks([NotNull] string fileName,
		                           [CanBeNull] DdxModel model)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.LinearNetworksExporter.Export(fileName, model);
			}

			if (model != null)
			{
				_msg.InfoFormat("Linear Networks for {0} exported to {1}", model.Name, fileName);
			}
			else
			{
				_msg.InfoFormat("Linear Networks exported to {0}", fileName);
			}
		}
	}
}
