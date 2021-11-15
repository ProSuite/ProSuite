using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping.Events;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.ProjectItem
{
	[UsedImplicitly]
	public abstract class WorklistItem : CustomProjectItemBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _canDelete = true;
		private bool _canRename = true;

		protected WorklistItem() { }

		protected WorklistItem(ItemInfoValue itemInfoValue) : base(itemInfoValue) { }

		protected WorklistItem(string name, string catalogPath, string typeID,
		                       string containerType) : base(
			name, catalogPath, typeID, containerType) { }

		public override bool IsContainer => false;

		protected override bool CanRename => _canRename;

		[CanBeNull]
		public string WorklistName { get; set; }

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
					File.Delete(Path);
					// necessary?
					NotifyPropertyChanged();
				}
				else
				{
					ErrorHandler.HandleFailure($"Cannot delete {Name}", _msg);
				}
			}), _msg);
		}

		public override bool Repair(string newPath)
		{
			return base.Repair(newPath);
		}

		public override void OnAddToProject()
		{
			base.OnAddToProject();
		}

		public override void OnRemoveFromProject()
		{
			base.OnRemoveFromProject();
		}

		protected override bool OnRename(string newName)
		{
			Assert.ArgumentNotNullOrEmpty(newName, nameof(newName));

			var result = false;

			ViewUtils.Try(() =>
			{
				if (string.IsNullOrWhiteSpace(System.IO.Path.GetFileNameWithoutExtension(newName)))
				{
					result = false;
				}
				else
				{
					string extension = System.IO.Path.GetExtension(newName);

					if (string.IsNullOrEmpty(extension))
					{
						extension = System.IO.Path.GetExtension(Path);
						newName = System.IO.Path.ChangeExtension(newName, extension);
						Assert.NotNullOrEmpty(newName);
					}

					string directoryName = System.IO.Path.GetDirectoryName(Path);
					Assert.NotNullOrEmpty(directoryName);

					string fullName = System.IO.Path.Combine(directoryName, newName);
					File.Move(Path, fullName);

					// todo daro is it necessary to update Path?
					Path = fullName;
					// don't forget to update WorklistName

					result = base.OnRename(newName);

					// todo daro inline
					var eventArgs =
						new ProjectItemsChangedEventArgs(NotifyCollectionChangedAction.Replace,
						                                 new List<Item> {this}, false);

					ProjectItemsChangedEvent.Publish(eventArgs);

					// necessary?
					NotifyPropertyChanged();
				}
			}, _msg);

			return result;
		}

		protected override void Update()
		{
			base.Update();
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
				       Type = WorklistsContainer.ContainerTypeName
			       };
		}

		public override string ToString()
		{
			return $"{WorklistName} {Path}";
		}
	}
}
