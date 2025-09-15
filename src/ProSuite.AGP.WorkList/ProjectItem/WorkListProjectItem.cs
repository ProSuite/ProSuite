
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProSuite.AGP.WorkList.ProjectItem
{
	[UsedImplicitly]
	public abstract class WorkListProjectItem : CustomProjectItemBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _canDelete = true;
		private bool _canRename = true;

		protected WorkListProjectItem() { }

		protected WorkListProjectItem(ItemInfoValue itemInfoValue) : base(itemInfoValue) { }

		protected WorkListProjectItem(string name, string catalogPath, string typeID,
		                              string containerType) : base(
			name, catalogPath, typeID, containerType)
		{ }

		public override bool IsContainer => false;

		protected override bool CanRename => _canRename;

		[CanBeNull]
		public string WorkListName { get; set; }

		public void DisableRename(bool disable)
		{
			_canRename = ! disable;
		}

		public void DisableDelete(bool disable)
		{
			_canDelete = ! disable;
		}

		public override async void Delete()
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				if (Project.Current.RemoveItem(this))
				{
					if (File.Exists(Path))
					{
						File.Delete(Path);
					}

					// necessary?
					NotifyPropertyChanged();
				}
				else
				{
					Gateway.ShowError($"Cannot delete {Name}", _msg);
				}
			}), _msg);
		}

		protected override bool OnRename(string newName)
		{
			Assert.ArgumentNotNullOrEmpty(newName, nameof(newName));

			try
			{
				Assert.NotNull(Path);

				string fileName = System.IO.Path.GetFileNameWithoutExtension(newName);

				if (string.IsNullOrWhiteSpace(fileName))
				{
					return false;
				}

				if (string.IsNullOrEmpty(System.IO.Path.GetExtension(newName)))
				{
					string extension = System.IO.Path.GetExtension(Path);
					newName = System.IO.Path.ChangeExtension(newName, extension);
					Assert.NotNullOrEmpty(newName);
				}

				string directoryName = System.IO.Path.GetDirectoryName(Path);
				Assert.NotNullOrEmpty(directoryName);

				string newPath = System.IO.Path.Combine(directoryName, newName);
				File.Move(Path, newPath);
				//Project.Current.RepairProjectItems(Path, newPath);

				// It's necessary to update Path.
				// https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Custom-Items#renaming
				Path = newPath;
				Name = System.IO.Path.GetFileName(newPath);

				WorkListName ??= WorkListUtils.GetWorklistName(Path);

				if (WorkListName == null)
				{
					return true;
				}

				IWorkList workList = WorkListRegistry.Instance.Get(WorkListName);

				if (workList == null)
				{
					return true;
				}

				workList.Rename(fileName);

				IReadOnlyList<Layer> layers = MapUtils.GetActiveMap().GetLayersAsFlattenedList();

				foreach (Layer worklistLayer in
				         WorkListUtils.GetWorklistLayers(layers, WorkListName))
				{
					LayerUtils.Rename(worklistLayer, fileName);
				}

				return true;
			}
			catch (IOException ex)
			{
				// NOTE: a failed rename creates an operation on the undo stack
				_msg.Debug(ex.Message, ex);

				Gateway.ShowMessage(
					$"There is already a file with the name {newName} in this location.", "Rename",
					MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				Gateway.ShowError(ex, _msg);
			}

			return false;
		}

		public override bool CanDelete()
		{
			return _canDelete;
		}

		public override ProjectItemInfo OnGetInfo()
		{
			return new ProjectItemInfo
			       {
				       Name = Name,
				       Path = Path,
				       Type = ContainerType
			       };
		}

		public override string ToString()
		{
			return $"{WorkListName} {Path}";
		}

		protected static ImageSource GetImageSource(string relativePath, string imageName)
		{
			try
			{
				string resourcePath = $"{relativePath}/{imageName}";

				string uri = string.Format(
					"pack://application:,,,/{0};component/{1}",
					Assembly.GetCallingAssembly().GetName().Name,
					resourcePath
				);

				Uri uriSource = new Uri(uri);

				return new BitmapImage(uriSource);
			}
			catch (Exception ex)
			{
				_msg.Debug(ex.Message, ex);
			}

			return null;
		}
	}
}
