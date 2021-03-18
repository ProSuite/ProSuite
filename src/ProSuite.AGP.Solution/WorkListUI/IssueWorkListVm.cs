using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkListVm : WorkListViewModelBase
	{
		private WorkItemVmBase _currentWorkItem;
		private string _qualityCondition;
		private List<InvolvedObjectRow> _involvedObjectRows;
		private string _errorDescription;
		private RelayCommand _zoomInvolvedAllCmd;
		private RelayCommand _zoomInvolvedSelectedCmd;
		private RelayCommand _flashInvolvedAllCmd;
		private RelayCommand _flashInvolvedSelectedCmd;
		private RelayCommand _selectInvolvedObjectsCmd;
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IssueWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkItem = new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
		}

		public override WorkItemVmBase CurrentWorkItem
		{
			get => new IssueWorkItemVm(CurrentWorkList.Current as IssueItem);
			set
			{
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
				Status = CurrentWorkItem.Status;
				Visited = CurrentWorkItem.Visited;
				InvolvedObjectRows = CompileInvolvedRows();
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					QualityCondition = issueWorkItemVm.QualityCondition;
					ErrorDescription = issueWorkItemVm.ErrorDescription;
				}

				CurrentIndex = CurrentWorkList.CurrentIndex;
				Count = GetCount();
			}
		}

		public string QualityCondition
		{
			get
			{
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					return issueWorkItemVm.QualityCondition;
				}
				else return string.Empty;
			}
			set { SetProperty(ref _qualityCondition, value, () => QualityCondition); }
		}

		public string ErrorDescription
		{
			get
			{
				if (CurrentWorkItem is IssueWorkItemVm issueWorkItemVm)
				{
					return issueWorkItemVm.ErrorDescription;
				}
				else return string.Empty;
			}
			set { SetProperty(ref _errorDescription, value, () => QualityCondition); }
		}

		public List<InvolvedObjectRow> InvolvedObjectRows
		{
			get => CompileInvolvedRows();
			set { SetProperty(ref _involvedObjectRows, value, () => InvolvedObjectRows); }
		}

		public RelayCommand SelectInvolvedObjectsCmd
		{
			get
			{
				_selectInvolvedObjectsCmd =
					new RelayCommand(SelectInvolvedObjects, () => true); // loaded?
				return _selectInvolvedObjectsCmd;
			}
		}

		public RelayCommand ZoomInvolvedAllCmd
		{
			get
			{
				_zoomInvolvedAllCmd = new RelayCommand(ZoomInvolvedAll, () => true);
				return _zoomInvolvedAllCmd;
			}
		}

		public RelayCommand ZoomInvolvedSelectedCmd
		{
			get
			{
				_zoomInvolvedSelectedCmd =
					new RelayCommand(ZoomInvolvedSelected, () => SelectedInvolvedObject != null);
				return _zoomInvolvedSelectedCmd;
			}
		}

		public RelayCommand FlashInvolvedAllCmd
		{
			get
			{
				_flashInvolvedAllCmd = new RelayCommand(FlashInvolvedAll, () => true);
				return _flashInvolvedAllCmd;
			}
		}

		public RelayCommand FlashInvolvedSelectedCmd
		{
			get
			{
				_flashInvolvedSelectedCmd =
					new RelayCommand(FlashInvolvedSelected, () => SelectedInvolvedObject != null);
				return _flashInvolvedSelectedCmd;
			}
		}

		private async void FlashInvolvedSelected()
		{
			ViewUtils.Try(async() =>
			{
				FeatureLayer involvedLayer = await GetFeatureLayerByFeatureClassName(SelectedInvolvedObject.Name);
				if (involvedLayer == null)
				{
					return;
				}
				MapView.Active.FlashFeature(involvedLayer, SelectedInvolvedObject.ObjectId);
			}, _msg);
		}

		private async void FlashInvolvedAll()
		{
			var featureSet = await GetInvolvedFeatureSet();
			MapView.Active.FlashFeature(featureSet);
		}

		private void ZoomInvolvedAll()
		{
			ViewUtils.Try(async () =>
			{
				Dictionary<BasicFeatureLayer, List<long>> featureSet = await GetInvolvedFeatureSet();
				await MapView.Active.ZoomToAsync(featureSet);
			}, _msg);
		}

		private void SelectInvolvedObjects()
		{
			ViewUtils.Try(async () =>
			{
				Dictionary<BasicFeatureLayer, List<long>> involvedFeatureClasses = await GetInvolvedFeatureSet();

				await QueuedTask.Run(() =>
				{
					SelectionUtils.ClearSelection(MapView.Active.Map);
					foreach (var involvedFeatureClass in involvedFeatureClasses)
					{
						var qf = new QueryFilter() { ObjectIDs = involvedFeatureClass.Value };
						involvedFeatureClass.Key.Select(qf, SelectionCombinationMethod.Add);
					}
				});
			}, _msg);
		}

		[CanBeNull]
		private async Task<Dictionary<BasicFeatureLayer, List<long>>> GetInvolvedFeatureSet()
		{
			Dictionary<BasicFeatureLayer, List<long>> featureSet =
				new Dictionary<BasicFeatureLayer, List<long>>();
			foreach (InvolvedObjectRow row in InvolvedObjectRows)
			{
				FeatureLayer involvedLayer = await GetFeatureLayerByFeatureClassName(row.Name);
				if (involvedLayer == null)
				{
					continue;
				}

				if (featureSet.Keys.Contains(involvedLayer))
				{
					featureSet[involvedLayer].Add(row.ObjectId);
				}
				else
				{
					featureSet.Add(involvedLayer, new List<long> {row.ObjectId});
				}
			}

			return featureSet;
		}

		private void ZoomInvolvedSelected()
		{
			ViewUtils.Try(async() =>
			{
				Layer involvedLayer = await GetFeatureLayerByFeatureClassName(SelectedInvolvedObject.Name);
				if (involvedLayer == null)
				{
					return;
				}

				await MapView.Active.ZoomToAsync(involvedLayer as FeatureLayer,
										   SelectedInvolvedObject.ObjectId);
			}, _msg);
		}

		private List<InvolvedObjectRow> CompileInvolvedRows()
		{
			var issueWorkItemVm = CurrentWorkItem as IssueWorkItemVm;
			List<InvolvedObjectRow> involvedRows = new List<InvolvedObjectRow>();

			if (issueWorkItemVm == null)
			{
				return involvedRows;
			}

			foreach (var table in issueWorkItemVm.IssueItem.InIssueInvolvedTables)
			{
				involvedRows.AddRange(InvolvedObjectRow.CreateObjectRows(table));
			}

			return involvedRows;
		}
	}
}
