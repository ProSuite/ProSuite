using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

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

		private void FlashInvolvedSelected()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					FeatureLayer involvedLayer = GetFeatureLayerByName(SelectedInvolvedObject.Name);
					if (involvedLayer == null)
					{
						return;
					}
					MapView.Active.FlashFeature(involvedLayer, SelectedInvolvedObject.ObjectId);
				});
			}, _msg);
		}

		private void FlashInvolvedAll()
		{
			var featureSet = GetInvolvedFeatureSet();
			MapView.Active.FlashFeature(featureSet);
		}

		private void ZoomInvolvedAll()
		{
			ViewUtils.Try(() =>
			{
				Dictionary<BasicFeatureLayer, List<long>> featureSet = GetInvolvedFeatureSet();
				MapView.Active.ZoomToAsync(featureSet);
			}, _msg);
		}

		[CanBeNull]
		private Dictionary<BasicFeatureLayer, List<long>> GetInvolvedFeatureSet()
		{
			Dictionary<BasicFeatureLayer, List<long>> featureSet =
				new Dictionary<BasicFeatureLayer, List<long>>();
			foreach (InvolvedObjectRow row in InvolvedObjectRows)
			{
				FeatureLayer involvedLayer = GetFeatureLayerByName(row.Name);
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
			ViewUtils.Try(() =>
			{
				Layer involvedLayer = GetFeatureLayerByName(SelectedInvolvedObject.Name);
				if (involvedLayer == null)
				{
					return;
				}

				MapView.Active.ZoomToAsync(involvedLayer as FeatureLayer,
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
