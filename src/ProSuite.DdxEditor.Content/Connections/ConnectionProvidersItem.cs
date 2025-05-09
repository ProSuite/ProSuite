using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class ConnectionProvidersItem : EntityTypeItem<ConnectionProvider>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static ConnectionProvidersItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.ConnectionProviderOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.ConnectionProviderOverlay);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionProvidersItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		public ConnectionProvidersItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Connection Providers", "Connections to geodatabase workspaces")
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

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<ConnectionProviderTableRow> GetTableRows()
		{
			IList<ConnectionProvider> connectionProviders =
				_modelBuilder.ConnectionProviders.GetAll();

			foreach (ConnectionProvider connectionProvider in connectionProviders)
			{
				yield return new ConnectionProviderTableRow(connectionProvider);
			}
		}

		protected override void CollectCommands(
			List<ICommand> commands, IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddConnectionProviderCommand
				             <FileGdbConnectionProvider>(
					             this, applicationController,
					             "Add File Geodatabase Connection Provider",
					             "Add File GDB Connection Provider"));
			commands.Add(new AddConnectionProviderCommand
				             <MobileGdbConnectionProvider>(
					             this, applicationController,
					             "Add Mobile Geodatabase Connection Provider",
					             "Add Mobile GDB Connection Provider"));
			commands.Add(new AddConnectionProviderCommand
				             <SdeDirectOsaConnectionProvider>(
					             this, applicationController,
					             "Add ArcSDE OSA Direct Connection Provider",
					             "Add ArcSDE OSA Direct Connect Provider"));
			commands.Add(new AddConnectionProviderCommand
				             <SdeDirectDbUserConnectionProvider>(
					             this, applicationController,
					             "Add ArcSDE Username/Password Direct Connection Provider",
					             "Add ArcSDE UID/PWD Direct Connect Provider"));
			commands.Add(new AddConnectionProviderCommand
				             <ConnectionFileConnectionProvider>(
					             this, applicationController,
					             "Add ArcSDE Connection File Connection Provider",
					             "Add ArcSDE (.sde) Provider"));
		}

		public void AddConnectionProvider<E>() where E : ConnectionProvider, new()
		{
			var provider = new E();

			Item item;

			switch (provider)
			{
				case FilePathConnectionProviderBase filePathConnectionProviderBase:
					item = new FilePathConnectionProviderItem(
						_modelBuilder, filePathConnectionProviderBase,
						_modelBuilder.ConnectionProviders);
					break;
				case SdeDirectDbUserConnectionProvider sdeDirectDbUserConnectionProvider:
					item = new SdeDirectDbUserConnectionProviderItem(
						_modelBuilder, sdeDirectDbUserConnectionProvider,
						_modelBuilder.ConnectionProviders);
					break;
				case SdeDirectOsaConnectionProvider sdeDirectOsaConnectionProvider:
					item = new SdeDirectConnectionProviderItem<SdeDirectOsaConnectionProvider>(
						_modelBuilder, sdeDirectOsaConnectionProvider,
						_modelBuilder.ConnectionProviders);
					break;
				default:
					throw new NotSupportedException(
						$"Unsupported connection provider type: {provider.GetType()}");
			}

			AddChild(item);

			item.NotifyChanged();
		}

		protected override bool SortChildren => true;
	}
}
