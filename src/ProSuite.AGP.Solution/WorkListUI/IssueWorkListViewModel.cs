using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkListViewModel : WorkListViewModelBase<IssueWorkList>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private string _errorDescription;
		private IEnumerable<InvolvedObjectRow> _involvedObjectRows;

		private string _qualityCondition;

		public IssueWorkListViewModel([NotNull] IWorkList workList) : base(workList) { }

		public string QualityCondition
		{
			get
			{
				if (CurrentItemViewModel is IssueItemViewModel issueWorkItemVm)
				{
					return issueWorkItemVm.QualityCondition;
				}

				return string.Empty;
			}
			set { SetProperty(ref _qualityCondition, value, () => QualityCondition); }
		}

		public string ErrorDescription
		{
			get
			{
				if (CurrentItemViewModel is IssueItemViewModel issueWorkItemVm)
				{
					return issueWorkItemVm.ErrorDescription;
				}

				return string.Empty;
			}
			set { SetProperty(ref _errorDescription, value, () => ErrorDescription); }
		}

		public override string ToolTip => "Select Involved Objects";

		public IEnumerable<InvolvedObjectRow> InvolvedObjectRows
		{
			get => CompileInvolvedRows();
			private set { SetProperty(ref _involvedObjectRows, value, () => InvolvedObjectRows); }
		}

		protected override void SetCurrentItemCore(IWorkItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			var vm = new IssueItemViewModel((IssueItem) item, CurrentWorkList);

			CurrentItemViewModel = vm;

			InvolvedObjectRows =
				vm.IssueItem.InIssueInvolvedTables.SelectMany(
					InvolvedObjectRow.CreateObjectRows);

			QualityCondition = vm.QualityCondition;
			ErrorDescription = vm.ErrorDescription;
		}

		protected override async void SelectCurrentFeatureAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					SelectionUtils.ClearSelection();

					foreach (KeyValuePair<BasicFeatureLayer, List<long>> pair in
						GetInvolvedFeaturesByLayer())
					{
						pair.Key.Select(new QueryFilter {ObjectIDs = pair.Value},
						                SelectionCombinationMethod.Add);
					}
				});
			}, _msg);
		}

		private void FlashSelectedInvolvedFeature()
		{
			ViewUtils.Try(() =>
			{
				if (MapUtils.FindLayers(SelectedInvolvedObject.Name).FirstOrDefault() is
					    FeatureLayer involvedLayer)
				{
					MapView mapView = MapView.Active;

					if (mapView == null)
					{
						return;
					}

					mapView.FlashFeature(involvedLayer, SelectedInvolvedObject.ObjectId);
				}
				else
				{
					_msg.DebugFormat("No layer with name {0}", SelectedInvolvedObject.Name);
				}
			}, _msg);
		}

		private async Task FlashAllInvolvedFeaturesAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					MapView mapView = MapView.Active;

					if (mapView == null)
					{
						return;
					}

					mapView.FlashFeature(GetInvolvedFeaturesByLayer());
				});
			}, _msg);
		}

		private async Task ZoomToSelectedInvolvedFeatureAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				if (MapUtils.FindLayers(SelectedInvolvedObject.Name).FirstOrDefault() is
					    FeatureLayer involvedLayer)
				{
					MapView mapView = MapView.Active;

					if (mapView == null)
					{
						return Task.FromResult(0);
					}

					return mapView.ZoomToAsync(involvedLayer,
					                           SelectedInvolvedObject.ObjectId,
					                           TimeSpan.FromSeconds(Seconds));
				}

				_msg.DebugFormat("No layer with name {0}", SelectedInvolvedObject.Name);

				return Task.FromResult(0);
			}, _msg);
		}

		private async Task ZoomToAllInvolvedFeaturesAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					MapView mapView = MapView.Active;

					if (mapView == null)
					{
						return;
					}

					mapView.ZoomTo(GetInvolvedFeaturesByLayer(), TimeSpan.FromSeconds(Seconds));
				});
			}, _msg);
		}

		// todo daro move to IssueUtils?
		[NotNull]
		private Dictionary<BasicFeatureLayer, List<long>> GetInvolvedFeaturesByLayer()
		{
			var featuresByLayer = new Dictionary<BasicFeatureLayer, List<long>>();

			foreach (InvolvedObjectRow row in InvolvedObjectRows)
			{
				FeatureLayer layer =
					MapUtils.GetLayers<FeatureLayer>(
						lyr => string.Equals(
							lyr.GetFeatureClass().GetName(),
							row.Name,
							StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

				if (layer == null)
				{
					continue;
				}

				if (featuresByLayer.ContainsKey(layer))
				{
					featuresByLayer[layer].Add(row.ObjectId);
				}
				else
				{
					featuresByLayer.Add(layer, new List<long> {row.ObjectId});
				}
			}

			return featuresByLayer;
		}

		private IEnumerable<InvolvedObjectRow> CompileInvolvedRows()
		{
			if (! (CurrentItemViewModel is IssueItemViewModel issueWorkItemVm))
			{
				return Enumerable.Empty<InvolvedObjectRow>();
			}

			return issueWorkItemVm.IssueItem.InIssueInvolvedTables.SelectMany(
				table => InvolvedObjectRow.CreateObjectRows(table));
		}

		#region Commands

		public RelayCommand ZoomInvolvedAllCommand =>
			new RelayCommand(ZoomToAllInvolvedFeaturesAsync, () => InvolvedObjectRows.Any());

		public RelayCommand ZoomInvolvedSelectedCommand =>
			new RelayCommand(ZoomToSelectedInvolvedFeatureAsync, () => SelectedInvolvedObject != null);

		public RelayCommand FlashInvolvedAllCommand =>
			new RelayCommand(FlashAllInvolvedFeaturesAsync, () => InvolvedObjectRows.Any());

		public RelayCommand FlashInvolvedSelectedCommand =>
			new RelayCommand(FlashSelectedInvolvedFeature, () => SelectedInvolvedObject != null);

		#endregion
	}
}
