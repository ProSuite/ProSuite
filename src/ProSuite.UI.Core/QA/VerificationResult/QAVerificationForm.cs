using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public partial class QAVerificationForm : Form,
	                                          IFormStateAware<QAVerificationFormState>
	{
		[NotNull] private readonly IDomainTransactionManager _domainTransactionManager;

		private readonly FormStateManager<QAVerificationFormState> _formStateManager;
		private QualityVerification _verification;
		private string _contextType;
		private string _contextName;
		private bool _loaded;
		private bool _matchCase;
		private bool _filterRows;

		private readonly int _viewIndexGridConditions;
		private readonly int _viewIndexGridDatasets;
		private readonly int _viewIndexTreeLayers;
		private readonly int _viewIndexTreeHierarchic;
		private readonly int _viewIndexTreeConditionsByCategory;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QAVerificationForm"/> class.
		/// </summary>
		/// <param name="domainTransactionManager">The domain transaction manager.</param>
		public QAVerificationForm(
			[NotNull] IDomainTransactionManager domainTransactionManager)
		{
			Assert.ArgumentNotNull(domainTransactionManager, nameof(domainTransactionManager));

			_domainTransactionManager = domainTransactionManager;

			InitializeComponent();

			ComboBox.ObjectCollection items = _toolStripComboBoxView.Items;
			_viewIndexGridConditions = items.Add("Quality Conditions");
			_viewIndexGridDatasets = items.Add("Verified Datasets");
			_viewIndexTreeLayers = items.Add("Quality Conditions by Dataset");
			_viewIndexTreeHierarchic = items.Add("Quality Conditions by Involved Datasets");
			_viewIndexTreeConditionsByCategory = items.Add("Quality Conditions by Category");

			_toolStripComboBoxView.SelectedIndex = _viewIndexGridConditions;

			_formStateManager = new FormStateManager<QAVerificationFormState>(this);
		}

		#endregion

		#region IFormStateAware<QAVerificationFormState> Members

		public void RestoreState(QAVerificationFormState formState)
		{
			if (formState.ListHeight > 0)
			{
				_splitContainerDetail.SplitterDistance = formState.ListHeight;
			}

			if (formState.ListWidth > 0)
			{
				_verifiedConditionsHierarchyControl.SplitterDistance = formState.ListWidth;
			}

			_toolStripComboBoxView.SelectedIndex = GetViewIndex(formState.ActiveMode);

			_toolStripButtonNoIssues.Checked = formState.ShowFulfilled;
			_toolStripButtonWarnings.Checked = formState.ShowWarning;
			_toolStripButtonErrors.Checked = formState.ShowFailed;

			_verifiedDatasetsControl.RestoreSortState(formState.VerifiedDatasetsSortState);
			_verifiedConditionsControl.RestoreSortState(formState.VerifiedConditionsSortState);

			_matchCase = formState.MatchCase;
			_filterRows = formState.FilterRows;
		}

		public void GetState(QAVerificationFormState formState)
		{
			formState.ListHeight = _splitContainerDetail.SplitterDistance;
			formState.ListWidth = _verifiedConditionsHierarchyControl.SplitterDistance;

			formState.ActiveMode = SelectedDisplayMode;

			formState.ShowFulfilled = _toolStripButtonNoIssues.Checked;
			formState.ShowWarning = _toolStripButtonWarnings.Checked;
			formState.ShowFailed = _toolStripButtonErrors.Checked;

			formState.VerifiedDatasetsSortState = _verifiedDatasetsControl.GetSortState();
			formState.VerifiedConditionsSortState = _verifiedConditionsControl.GetSortState();

			GetFilterSettingsFromVisibleControl();

			formState.MatchCase = _matchCase;
			formState.FilterRows = _filterRows;
		}

		#endregion

		public void SetVerification([NotNull] QualityVerification verification,
		                            string contextType, string contextName)
		{
			Assert.ArgumentNotNull(verification, nameof(verification));

			_verification = verification;
			_contextType = contextType;
			_contextName = contextName;
		}

		private QAVerificationDisplayMode SelectedDisplayMode
		{
			get
			{
				int index = _toolStripComboBoxView.SelectedIndex;

				if (index == _viewIndexGridConditions)
				{
					return QAVerificationDisplayMode.Plain;
				}

				if (index == _viewIndexGridDatasets)
				{
					return QAVerificationDisplayMode.List;
				}

				if (index == _viewIndexTreeLayers)
				{
					return QAVerificationDisplayMode.Layer;
				}

				if (index == _viewIndexTreeHierarchic)
				{
					return QAVerificationDisplayMode.Hierachric;
				}

				if (index == _viewIndexTreeConditionsByCategory)
				{
					return QAVerificationDisplayMode.ConditionsByCategory;
				}

				throw new InvalidOperationException("no selected displayMode");
			}
		}

		private int GetViewIndex(QAVerificationDisplayMode displayMode)
		{
			switch (displayMode)
			{
				case QAVerificationDisplayMode.Plain:
					return _viewIndexGridConditions;

				case QAVerificationDisplayMode.Layer:
					return _viewIndexTreeLayers;

				case QAVerificationDisplayMode.Hierachric:
					return _viewIndexTreeHierarchic;

				case QAVerificationDisplayMode.List:
					return _viewIndexGridDatasets;

				case QAVerificationDisplayMode.ConditionsByCategory:
					return _viewIndexTreeConditionsByCategory;

				default:
					return 0; // first
			}
		}

		private static string FormatDate(DateTime dateTime)
		{
			return dateTime.ToString("dd.MM.yyyy HH:mm");
		}

		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return string.Format("{0:00}:{1:00}:{2:00}",
			                     Math.Truncate(timeSpan.TotalHours),
			                     timeSpan.Minutes,
			                     timeSpan.Seconds);
		}

		private void SetQualityConditionVerification(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			Assert.ArgumentNotNull(conditionVerification, nameof(conditionVerification));

			_domainTransactionManager.UseTransaction(
				delegate
				{
					QualityCondition condition = conditionVerification.DisplayableCondition;

					if (condition.IsPersistent)
					{
						_domainTransactionManager.Reattach(condition);
					}

					_qualityConditionVerificationControl.SetCondition(conditionVerification);
				});
		}

		private void SetQualityConditionVerification([CanBeNull] TreeNode node)
		{
			QualityConditionVerification qualityConditionVerification =
				GetQualityConditionVerification(node);

			if (qualityConditionVerification == null)
			{
				_qualityConditionVerificationControl.Clear();
				_groupBoxCondition.Enabled = false;
			}
			else
			{
				SetQualityConditionVerification(qualityConditionVerification);
				_groupBoxCondition.Enabled = true;
			}
		}

		private void Render(QAVerificationDisplayMode displayMode)
		{
			_msg.DebugFormat("Render({0})", displayMode);

			switch (displayMode)
			{
				case QAVerificationDisplayMode.Plain:
					RenderPlainView();
					break;

				case QAVerificationDisplayMode.Layer:
					RenderLayerView();
					break;

				case QAVerificationDisplayMode.Hierachric:
					RenderHierarchicView();
					break;

				case QAVerificationDisplayMode.List:
					RenderListView();
					break;

				case QAVerificationDisplayMode.ConditionsByCategory:
					RenderConditionsByCategory();
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(displayMode));
			}
		}

		private void RenderListView()
		{
			// hide other controls
			_verifiedConditionsControl.Visible = false;
			_verifiedConditionsControl.Clear();
			_verifiedConditionsHierarchyControl.Visible = false;

			// show control
			_verifiedDatasetsControl.Clear();
			_verifiedDatasetsControl.FilterRows = _filterRows;
			_verifiedDatasetsControl.MatchCase = _matchCase;
			_verifiedDatasetsControl.Visible = true;
			_verifiedDatasetsControl.Bind(_verification, IncludeConditionVerification);
		}

		private void RenderHierarchicView()
		{
			// hide other controls
			_verifiedDatasetsControl.Visible = false;
			_verifiedDatasetsControl.Clear();
			_verifiedConditionsControl.Visible = false;
			_verifiedConditionsControl.Clear();

			// show control
			_verifiedConditionsHierarchyControl.Visible = true;
			_verifiedConditionsHierarchyControl.RenderHierarchicView(_verification,
				IncludeConditionVerification);
		}

		private void RenderConditionsByCategory()
		{
			// hide other controls
			_verifiedDatasetsControl.Visible = false;
			_verifiedDatasetsControl.Clear();
			_verifiedConditionsControl.Visible = false;
			_verifiedConditionsControl.Clear();

			// show control
			_verifiedConditionsHierarchyControl.Visible = true;
			_verifiedConditionsHierarchyControl.RenderConditionsByCategoryView(_verification,
				IncludeConditionVerification);
		}

		private void RenderLayerView()
		{
			// hide other controls
			_verifiedDatasetsControl.Visible = false;
			_verifiedDatasetsControl.Clear();
			_verifiedConditionsControl.Visible = false;
			_verifiedConditionsControl.Clear();

			// show control
			_verifiedConditionsHierarchyControl.Visible = true;
			_verifiedConditionsHierarchyControl.RenderLayerView(_verification,
			                                                    IncludeConditionVerification);
		}

		private void RenderPlainView()
		{
			// hide other controls
			_verifiedConditionsHierarchyControl.Visible = false;
			_verifiedDatasetsControl.Clear();
			_verifiedDatasetsControl.Visible = false;

			// show control
			_verifiedConditionsControl.Clear();
			_verifiedConditionsControl.FilterRows = _filterRows;
			_verifiedConditionsControl.MatchCase = _matchCase;
			_verifiedConditionsControl.Visible = true;

			_verifiedConditionsControl.Bind(
				_verification.ConditionVerifications.Where(IncludeConditionVerification));
		}

		private void GetFilterSettingsFromVisibleControl()
		{
			if (_verifiedDatasetsControl.Visible)
			{
				_matchCase = _verifiedDatasetsControl.MatchCase;
				_filterRows = _verifiedDatasetsControl.FilterRows;
			}
			else if (_verifiedConditionsControl.Visible)
			{
				_matchCase = _verifiedConditionsControl.MatchCase;
				_filterRows = _verifiedConditionsControl.FilterRows;
			}
		}

		private bool IncludeConditionVerification(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			if (conditionVerification.ErrorCount == 0 &&
			    _toolStripButtonNoIssues.Checked)
			{
				return true;
			}

			if (conditionVerification.ErrorCount > 0 &&
			    conditionVerification.AllowErrors &&
			    _toolStripButtonWarnings.Checked)
			{
				return true;
			}

			if (conditionVerification.ErrorCount > 0 &&
			    ! conditionVerification.AllowErrors &&
			    _toolStripButtonErrors.Checked)
			{
				return true;
			}

			return false;
		}

		[CanBeNull]
		private static QualityConditionVerification GetQualityConditionVerification(
			[CanBeNull] TreeNode node)
		{
			if (node == null)
			{
				return null;
			}

			var result = node.Tag as QualityConditionVerification;

			return result;
		}

		private void RenderSelectedDisplayMode()
		{
			if (! _loaded || _verification == null)
			{
				return;
			}

			GetFilterSettingsFromVisibleControl();

			Render(SelectedDisplayMode);
		}

		#region events

		private void QAVerificationForm_Load(object sender, EventArgs e)
		{
			// leads to errors in control designer when specified in control designer
			_splitContainerDetail.Panel2MinSize = 150;

			// restore in Load event due to splitter distance problem when maximized
			// http://social.msdn.microsoft.com/forums/en-US/winforms/thread/57f38145-b3b1-488d-8988-da8c397e4d80/
			_formStateManager.RestoreState();

			Color colorNoIssues = Color.LightGreen;
			Color colorErrors = Color.FromArgb(255, 100, 100);
			Color colorWarnings = Color.Yellow;
			Color colorCancelled = Color.Orange;

			_textBoxSpecification.Text = _verification.SpecificationName;
			_textBoxDescription.Text = _verification.SpecificationDescription;

			int right = _labelContext.Right;

			_labelContext.Text = string.Format("{0}:", _contextType);
			_labelContext.Location = new Point(right - _labelContext.Width, _labelContext.Top);
			_textBoxContext.Text = _contextName;

			_textBoxStartDate.Text = FormatDate(_verification.StartDate);
			_textBoxEndDate.Text = FormatDate(_verification.EndDate);

			TimeSpan timeSpan = _verification.EndDate - _verification.StartDate;

			_textBoxTotalTime.Text = FormatTimeSpan(timeSpan);
			TimeSpan cpuTime = TimeSpan.FromSeconds(_verification.ProcessorTimeSeconds);

			_textBoxCPUTime.Text = FormatTimeSpan(cpuTime);

			_textBoxUser.Text = _verification.Operator;

			if (_verification.Fulfilled)
			{
				_textBoxVerificationStatus.Text =
					LocalizableStrings.QualityVerificationStatusFulfilled;
				_textBoxVerificationStatus.BackColor = colorNoIssues;
			}
			else if (_verification.Cancelled)
			{
				_textBoxVerificationStatus.Text =
					LocalizableStrings.QualityVerificationStatusCancelled;
				_textBoxVerificationStatus.BackColor = colorCancelled;
			}
			else
			{
				_textBoxVerificationStatus.Text =
					LocalizableStrings.QualityVerificationStatusNotFulfilled;
				_textBoxVerificationStatus.BackColor = colorErrors;
			}

			int issueCount = _verification.IssueCount;
			int warningCount = _verification.WarningCount;
			int errorCount = _verification.ErrorCount;

			_textBoxIssueCount.Text = issueCount.ToString("N0");
			if (errorCount > 0)
			{
				_textBoxIssueCount.BackColor = colorErrors;
			}
			else if (warningCount > 0)
			{
				_textBoxIssueCount.BackColor = colorWarnings;
			}
			else
			{
				_textBoxIssueCount.BackColor = colorNoIssues;
			}

			_textBoxErrorCount.Text = errorCount.ToString("N0");
			_textBoxErrorCount.BackColor = errorCount == 0
				                               ? colorNoIssues
				                               : colorErrors;

			_textBoxWarningCount.Text = warningCount.ToString("N0");
			_textBoxWarningCount.BackColor = warningCount == 0
				                                 ? colorNoIssues
				                                 : colorWarnings;

			Render(SelectedDisplayMode);

			_loaded = true;
		}

		private void QAVerificationForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager.SaveState();

			// ... and this is for http://social.msdn.microsoft.com/Forums/en/winformsdesigner/thread/ee6abc76-f35a-41a4-a1ff-5be942ae3425
			_splitContainerDetail.Panel1MinSize = 200;
			_splitContainerDetail.Panel2MinSize = 170;
		}

		private void QAVerificationForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				e.Handled = true;
				Hide();
			}
		}

		private void _toolStripComboBoxView_SelectedIndexChanged(object sender, EventArgs e)
		{
			RenderSelectedDisplayMode();
		}

		private void _toolStripButtonNoIssues_CheckedChanged(object sender, EventArgs e)
		{
			RenderSelectedDisplayMode();
		}

		private void _toolStripButtonWarnings_Click(object sender, EventArgs e)
		{
			RenderSelectedDisplayMode();
		}

		private void _toolStripButtonErrors_Click(object sender, EventArgs e)
		{
			RenderSelectedDisplayMode();
		}

		private void _verifiedConditionsControl_SelectionChanged(object sender, EventArgs e)
		{
			if (_verifiedConditionsControl.SelectionCount != 1)
			{
				_groupBoxCondition.Enabled = false;
				return;
			}

			SetQualityConditionVerification(
				_verifiedConditionsControl.SelectedConditionVerifications[0]);

			_groupBoxCondition.Enabled = true;
		}

		private void _verifiedConditionsHierarchyControl_SelectionChanged(object sender,
			EventArgs e)
		{
			if (_verifiedConditionsHierarchyControl.SelectedNode == null)
			{
				_groupBoxCondition.Enabled = false;
				return;
			}

			SetQualityConditionVerification(_verifiedConditionsHierarchyControl.SelectedNode);

			_groupBoxCondition.Enabled = true;
		}

		private void _verifiedDatasetsControl_SelectionChanged(object sender, EventArgs e)
		{
			if (_verifiedDatasetsControl.SelectionCount != 1)
			{
				_groupBoxCondition.Enabled = false;
				return;
			}

			SetQualityConditionVerification(
				_verifiedDatasetsControl.SelectedConditionVerifications[0]);

			_groupBoxCondition.Enabled = true;
		}

		private void _qualityConditionVerificationControl_EnabledChanged(object sender,
			EventArgs e)
		{
			if (_qualityConditionVerificationControl.Enabled)
			{
				return;
			}

			_qualityConditionVerificationControl.Clear();
		}

		private void _toolStripSplitButtonClose_ButtonClick(object sender, EventArgs e)
		{
			Close();
		}

		#endregion events
	}
}
