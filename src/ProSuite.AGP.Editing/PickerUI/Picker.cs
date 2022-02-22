using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Editing.PickerUI
{
	public class Picker : IPicker
	{
		private readonly List<IPickableItem> _candidateList;
		private readonly Point _pickerScreenLocation = new Point(double.NaN, double.NaN);
		private PickerViewModel _viewModel;

		public Picker([NotNull] List<IPickableItem> candidateList)
		{
			_candidateList = candidateList;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Picker" /> class.
		/// </summary>
		/// <param name="candidateList">The list of pickable items</param>
		/// <param name="pickerScreenCoordinates">
		/// The location  of the picker dialog's top left corner in screen coordinates.
		/// </param>
		public Picker([NotNull] List<IPickableItem> candidateList,
		              Point pickerScreenCoordinates)
		{
			_candidateList = candidateList;
			_pickerScreenLocation = pickerScreenCoordinates;
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

			bool? dialogResult = await ShowPickerControl(_viewModel);

			return dialogResult == true ? _viewModel.SelectedPickableItem : null;
		}

		[NotNull]
		public async Task<List<IPickableItem>> PickMany()
		{
			if (_candidateList.Count == 0)
			{
				return new List<IPickableItem>(0);
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

		/// <summary>
		/// Picks a single feature from the list of features in the provided selection sets.
		/// Must be called on the UI thread.
		/// </summary>
		/// <param name="selectionByClass"></param>
		/// <param name="pickerWindowLocation"></param>
		/// <returns></returns>
		public static async Task<PickableFeatureItem> PickSingleFeatureAsync(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClass,
			Point pickerWindowLocation)
		{
			List<IPickableItem> pickableItems =
				await QueuedTaskUtils.Run(
					delegate
					{
						selectionByClass =
							GeometryReducer.ReduceByGeometryDimension(selectionByClass)
							               .ToList();

						return CreatePickableFeatureItems(selectionByClass);
					});

			var picker = new Picker(pickableItems, pickerWindowLocation);

			// Must not be called from a background Task!
			return await picker.PickSingle() as PickableFeatureItem;
		}

		public static List<IPickableItem> CreatePickableFeatureItems(
			KeyValuePair<BasicFeatureLayer, List<long>> featuresOfLayer)
		{
			var pickCandidates = new List<IPickableItem>();
			foreach (Feature feature in MapUtils.GetFeatures(featuresOfLayer))
			{
				string text = GetPickerItemText(feature, featuresOfLayer.Key);
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
				pickCandidates.AddRange(CreatePickableFeatureItems(classSelection));
			}

			return pickCandidates;
		}

		public static IEnumerable<IPickableItem> CreatePickableFeatureItems(
			[NotNull] FeatureClassSelection classSelection)
		{
			foreach (Feature feature in classSelection.GetFeatures())
			{
				string text = GetPickerItemText(feature, classSelection.FeatureLayer);

				yield return new PickableFeatureItem(classSelection.FeatureLayer, feature, text);
			}
		}

		private static string GetPickerItemText([NotNull] Feature feature,
		                                        [CanBeNull] BasicFeatureLayer layer = null)
		{
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration
			// string[] displayExpressions = layer.QueryDisplayExpressions(new[] { feature.GetObjectID() });

			string className = layer == null ? feature.GetTable().GetName() : layer.Name;

			return GdbObjectUtils.GetDisplayValue(feature, className);
		}

		private async Task<bool?> ShowPickerControl(PickerViewModel vm)
		{
			var window = new PickerWindow(vm);

			SetWindowLocation(window);

			window.Show();

			return await vm.ResultTask;
		}

		private void SetWindowLocation(Window window)
		{
			Window ownerWindow = Assert.NotNull(Application.Current.MainWindow);

			window.Owner = ownerWindow;

			if (IsUnknownLocation(_pickerScreenLocation))
			{
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				return;
			}

			// NOTE: The window's Top/Left coordinates must be set in logical units or DIP (1/96 inch)
			Point dipLocation =
				DisplayUtils.ToDeviceIndependentPixels(_pickerScreenLocation, ownerWindow);

			window.Left = dipLocation.X;
			window.Top = dipLocation.Y;
		}

		private static void RunOnUIThread(Action action)
		{
			if (Application.Current.Dispatcher.CheckAccess())
				action(); //No invoke needed
			else
				//We are not on the UI
				Application.Current.Dispatcher.BeginInvoke(action);
			}

		private static bool IsUnknownLocation(Point location)
		{
			return double.IsNaN(location.X) || double.IsNaN(location.Y);
		}
	}
}
