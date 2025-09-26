using System;
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
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AttributeDependenciesItem : EntityTypeItem<AttributeDependency>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static AttributeDependenciesItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.AttributeDependencyOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.AttributeDependencyOverlay);
		}

		public AttributeDependenciesItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Attribute Dependencies", "Attribute Dependencies")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected virtual IEnumerable<AttributeDependencyTableRow> GetTableRows()
		{
			if (TryGetAttributeDependencies(out IList<AttributeDependency> entities))
			{
				foreach (AttributeDependency entity in entities)
				{
					yield return new AttributeDependencyTableRow(entity);
				}
			}
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddAttributeDependencyCommand(this, applicationController));
			commands.Add(new CheckAttributeDependenciesCommand(this, applicationController));

			commands.Add(new ImportAttributeDependenciesCommand(this, applicationController));

			const bool filterByModel = true;
			commands.Add(new ExportAttributeDependenciesCommand(this, applicationController,
			                                                    ! filterByModel));
			commands.Add(new ExportAttributeDependenciesCommand(this, applicationController,
			                                                    filterByModel));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override bool SortChildren => true;

		[NotNull]
		public AttributeDependencyItem AddAttributeDependencyItem()
		{
			var attributeDependency = new AttributeDependency();

			var item = new AttributeDependencyItem(
				_modelBuilder, attributeDependency,
				_modelBuilder.AttributeDependencies);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		public void CheckAttributeDependencies()
		{
			using (new WaitCursor())
			{
				_modelBuilder.ReadOnlyTransaction(
					delegate
					{
						if (TryGetAttributeDependencies(out IList<AttributeDependency> entities))
						{
							foreach (AttributeDependency attributeDependency in entities)
							{
								using (_msg.IncrementIndentation(
									       "Checking {0}", attributeDependency))
								{
									CheckAttributeDependencyUtils.CheckAttributeDependency(
										attributeDependency);
								}
							}
						}
					});
			}

			_msg.Info("Check Attribute Dependencies finished (see log for details).");
		}

		public void ImportAttributeDependencies([NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.NewTransaction(
					() => _modelBuilder.AttributeDependenciesImporter.Import(fileName));
			}

			_msg.InfoFormat("Attribute Dependencies imported from {0}", fileName);

			RefreshChildren();
		}

		public void ExportAttributeDependencies([NotNull] string fileName,
		                                        [CanBeNull] DdxModel model)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				_modelBuilder.AttributeDependenciesExporter.Export(fileName, model);
			}

			if (model != null)
			{
				_msg.InfoFormat("Attribute Dependencies for {0} exported to {1}", model.Name,
				                fileName);
			}
			else
			{
				_msg.InfoFormat("Attribute Dependencies exported to {0}", fileName);
			}
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

		[ContractAnnotation("=>true, entities:notnull; =>false, entities:canbenull")]
		private bool TryGetAttributeDependencies(out IList<AttributeDependency> entities)
		{
			try
			{
				entities = _modelBuilder.AttributeDependencies.GetAll();
				return true;
			}
			catch (Exception ex)
			{
				_msg.Warn("Attribute Dependencies not available.", ex);

				entities = null;
				return false;
			}
		}
	}
}
