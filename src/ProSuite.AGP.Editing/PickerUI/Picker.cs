using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.PickerUI
{
	class Picker : IPicker
	{
		private PickerViewModel _viewModel;
		private readonly List<IPickableItem> _candidateList;
		private Point _windowLocation;
		private const double _unknownLoc = -1.0;

		public Picker([NotNull] List<IPickableItem> candidateList)
		{
			_candidateList = candidateList;
			_windowLocation.X = _unknownLoc;
			_windowLocation.Y = _unknownLoc;
		}

		public Picker([NotNull] List<IPickableItem> candidateList,
		              Point pickerWindowLocation)
		{
			_candidateList = candidateList;
			_windowLocation = pickerWindowLocation;
		}

		[ItemCanBeNull]
		public async Task<IPickableItem> PickSingle()
		{
			if (_candidateList.Count == 0)
			{
				return null;
			}

			if (_candidateList.Count == 1)
			{
				return _candidateList.First();
			}

			await QueuedTask.Run(() =>
			{
				_viewModel =
					new PickerViewModel(_candidateList, true);
			});

			RunOnUIThread(() => { ShowPickerControl(_viewModel); });

			_viewModel.DisposeOverlays();

			return _viewModel.SelectedItem;
		}

		[CanBeNull]
		public async Task<List<IPickableItem>> PickMany()
		{
			if (_candidateList.Count == 0)
			{
				return null;
			}

			if (_candidateList.Count == 1)
			{
				return _candidateList;
			}

			await QueuedTask.Run(() =>
			{
				_viewModel = new PickerViewModel(_candidateList, false);
			});

			if (_candidateList.Count == 0)
			{
				return new List<IPickableItem>();
			}

			RunOnUIThread(() => { ShowPickerControl(_viewModel); });

			_viewModel.DisposeOverlays();

			return _viewModel.SelectedItems.ToList();
		}

		public static List<IPickableItem> CreatePickableFeatureItems(
			KeyValuePair<BasicFeatureLayer, List<long>> featuresOfLayer)
		{
			var pickCandidates = new List<IPickableItem>();
			foreach (Feature feature in MapUtils.GetFeatures(featuresOfLayer))
			{
				var text =
					$"{featuresOfLayer.Key.Name}: {feature.GetObjectID()}";
				var featureItem =
					new PickableFeatureItem(featuresOfLayer.Key, feature, text);
				pickCandidates.Add(featureItem);
			}

			return pickCandidates;
		}

		public static List<IPickableItem> CreatePickableFeatureItems(
			IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			var pickCandidates = new List<IPickableItem>();

			foreach (FeatureClassSelection classSelection in selectionByClasses)
			{
				foreach (Feature feature in classSelection.Features)
				{
					var text = GetPickerItemText(feature, classSelection.FeatureLayer);

					var featureItem =
						new PickableFeatureItem(classSelection.FeatureLayer, feature, text);

					pickCandidates.Add(featureItem);
				}
			}

			return pickCandidates;
		}

		private static string GetPickerItemText([NotNull] Feature feature,
		                                        [CanBeNull] BasicFeatureLayer layer = null)
		{
			if (layer != null)
			{
				string[] displayExpressions =
					layer.QueryDisplayExpressions(new[] {feature.GetObjectID()});
			}

			string className = layer == null ? feature.GetTable().GetName() : layer.Name;

			FeatureClassDefinition classDefinition = feature.GetTable().GetDefinition();

			string subtypeField = classDefinition.GetSubtypeField();

			string subtypeName = null;
			if (! string.IsNullOrEmpty(subtypeField))
			{
				int? subtypeCode = feature[subtypeField] as int?;
				Subtype subtype = classDefinition.GetSubtypes()
				                                 .FirstOrDefault(st => st.GetCode() == subtypeCode);

				if (subtype != null)
				{
					subtypeName = subtype.GetName();
				}
			}

			return string.IsNullOrEmpty(subtypeName)
				       ? $"{className} ID: {feature.GetObjectID()}"
				       : $"{className} ({subtypeName}) ID: {feature.GetObjectID()}";
		}

		private void ShowPickerControl(PickerViewModel vm)
		{
			PickerWindow window = new PickerWindow(vm);
			ManageWindowLocation(window);
			bool? accepted = window.ShowDialog();
			if (accepted == false)
			{
				vm.SelectedItem = null;
			}

			vm.DisposeOverlays();
		}

		private void ManageWindowLocation(Window window)
		{
			Window ownerWindow = FrameworkApplication.Current.MainWindow;
			window.Owner = ownerWindow;
			if (_windowLocation.X == _unknownLoc && _windowLocation.Y == _unknownLoc)
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			window.Left = _windowLocation.X;
			window.Top = _windowLocation.Y;
		}

		private static void RunOnUIThread(Action action)
		{
			if (FrameworkApplication.Current.Dispatcher.CheckAccess())
				action(); //No invoke needed
			else
				//We are not on the UI
				FrameworkApplication.Current.Dispatcher.BeginInvoke(action);
		}
	}
}
