using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Properties;

namespace ProSuite.UI.QA.VerificationResult
{
	public partial class VerifiedConditionsControl : UserControl
	{
		private readonly BoundDataGridHandler<VerifiedConditionItem> _gridHandler;

		public event EventHandler SelectionChanged;
		private readonly Latch _latch = new Latch();

		public VerifiedConditionsControl()
		{
			InitializeComponent();

			_gridHandler = new BoundDataGridHandler<VerifiedConditionItem>(_dataGridView);
			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip);
		}

		public void Clear()
		{
			_gridHandler.ClearRows();
		}

		public void Bind(
			[NotNull] IEnumerable<QualityConditionVerification> conditionVerifications)
		{
			int maximumIssueCount;
			SortableBindingList<VerifiedConditionItem> bindingList =
				GetVerifiedConditionItems(conditionVerifications, out maximumIssueCount);

			_dataGridView.SuspendLayout();

			try
			{
				_columnIssueCountBar.MaximumIssueCount = maximumIssueCount;

				_latch.RunInsideLatch(() =>
				{
					DataGridViewSortState state = GetSortState();
					_gridHandler.BindTo(bindingList);
					RestoreSortState(state);
				});
			}
			finally
			{
				_dataGridView.ResumeLayout();
			}

			OnSelectionChanged();
		}

		[NotNull]
		private static SortableBindingList<VerifiedConditionItem> GetVerifiedConditionItems(
			[NotNull] IEnumerable<QualityConditionVerification> conditionVerifications,
			out int maximumIssueCount)
		{
			var result = new SortableBindingList<VerifiedConditionItem>();

			maximumIssueCount = 0;
			foreach (QualityConditionVerification verification in conditionVerifications)
			{
				if (verification.ErrorCount > maximumIssueCount)
				{
					maximumIssueCount = verification.ErrorCount;
				}

				result.Add(new VerifiedConditionItem(verification));
			}

			return result;
		}

		[Browsable(false)]
		public int SelectionCount => _gridHandler.SelectedRowCount;

		[Browsable(false)]
		[NotNull]
		public IList<QualityConditionVerification> SelectedConditionVerifications
		{
			get
			{
				return _gridHandler.GetSelectedRows()
				                   .Select(item => item.QualityConditionVerification)
				                   .ToList();
			}
		}

		public void RestoreSortState([CanBeNull] DataGridViewSortState sortState)
		{
			if (sortState == null)
			{
				sortState = new DataGridViewSortState(_columnName.Name);
			}

			sortState.TryApplyState(_dataGridView);
		}

		[NotNull]
		public DataGridViewSortState GetSortState()
		{
			return new DataGridViewSortState(_dataGridView);
		}

		public bool FilterRows
		{
			get { return _dataGridViewFindToolStrip.FilterRows; }
			set { _dataGridViewFindToolStrip.FilterRows = value; }
		}

		public bool MatchCase
		{
			get { return _dataGridViewFindToolStrip.MatchCase; }
			set { _dataGridViewFindToolStrip.MatchCase = value; }
		}

		protected virtual void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			_latch.RunLatchedOperation(OnSelectionChanged);
		}

		private class VerifiedConditionItem
		{
			private static readonly Image _statusImageNoIssues = VerificationResultImages.OK;
			private static readonly Image _statusImageHasWarnings = VerificationResultImages.Warning;
			private static readonly Image _statusImageHasErrors = VerificationResultImages.Error;

			public VerifiedConditionItem(
				[NotNull] QualityConditionVerification qualityConditionVerification)
			{
				Assert.ArgumentNotNull(qualityConditionVerification,
				                       nameof(qualityConditionVerification));

				QualityConditionVerification = qualityConditionVerification;
				QualityCondition qualityCondition =
					Assert.NotNull(qualityConditionVerification.DisplayableCondition);
				TestDescriptor testDescriptor = qualityCondition.TestDescriptor;

				Name = qualityCondition.Name;
				Category = qualityCondition.Category?.GetQualifiedName();
				TestDescriptorName = testDescriptor.Name;
				TestCategories = GetTestCategories(testDescriptor);
				DatasetNames = GetDatasetNames(qualityCondition);

				IssueCount = qualityConditionVerification.ErrorCount;

				IssueType = qualityConditionVerification.AllowErrors
					            ? IssueType.Warning
					            : IssueType.Error;

				StatusImage = IssueCount == 0
					              ? _statusImageNoIssues
					              : qualityConditionVerification.AllowErrors
						              ? _statusImageHasWarnings
						              : _statusImageHasErrors;
			}

			[UsedImplicitly]
			public Image StatusImage { get; }

			[UsedImplicitly]
			public string Name { get; }

			[UsedImplicitly]
			public string Category { get; private set; }

			[UsedImplicitly]
			public string TestDescriptorName { get; }

			[UsedImplicitly]
			public string TestCategories { get; }

			[UsedImplicitly]
			public string DatasetNames { get; }

			[UsedImplicitly]
			public int IssueCount { get; }

			[NotNull]
			public QualityConditionVerification QualityConditionVerification { get; }

			private IssueType IssueType { get; }

			[NotNull]
			private static string GetTestCategories([NotNull] TestDescriptor testDescriptor)
			{
				var testInfo = InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

				return testInfo == null
					       ? string.Empty
					       : StringUtils.ConcatenateSorted(testInfo.TestCategories, ", ");
			}
			
			[NotNull]
			private static string GetDatasetNames([NotNull] QualityCondition qualityCondition)
			{
				var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues(
					         includeSourceDatasets: true))
				{
					names.Add(dataset.Name);
				}

				var list = new List<string>(names);

				list.Sort();

				return StringUtils.Concatenate(list, ", ");
			}

			public class DataGridViewIssueCountColumn : DataGridViewColumn
			{
				public DataGridViewIssueCountColumn()
				{
					Margin = 5;
					ErrorColor = Color.Red;
					WarningColor = Color.Yellow;

					// ReSharper disable once RedundantBaseQualifier
					base.CellTemplate = new Cell();
				}

				[Browsable(false)]
				[UsedImplicitly]
				public int MaximumIssueCount { get; set; }

				[UsedImplicitly]
				public int Margin { get; set; }

				[UsedImplicitly]
				public Color WarningColor { get; set; }

				[UsedImplicitly]
				public Color ErrorColor { get; set; }

				private static Color GetColor(IssueType issueType,
				                              [NotNull] DataGridViewIssueCountColumn column)
				{
					switch (issueType)
					{
						case IssueType.Warning:
							return column.WarningColor;

						case IssueType.Error:
							return column.ErrorColor;

						default:
							throw new ArgumentOutOfRangeException(nameof(issueType));
					}
				}

				#region Nested type: Cell

				private class Cell : DataGridViewCell
				{
					protected override void Paint(
						Graphics graphics, Rectangle clipBounds,
						Rectangle cellBounds,
						int rowIndex,
						DataGridViewElementStates cellState,
						object value, object formattedValue,
						string errorText,
						DataGridViewCellStyle cellStyle,
						DataGridViewAdvancedBorderStyle advancedBorderStyle,
						DataGridViewPaintParts paintParts)
					{
						using (Brush brush = new SolidBrush(cellStyle.BackColor))
						{
							graphics.FillRectangle(brush, cellBounds);
						}

						VerifiedConditionItem item = GetItem(rowIndex);
						var column =
							DataGridView?.Columns[ColumnIndex] as DataGridViewIssueCountColumn;

						if (column == null || item == null)
						{
							return;
						}

						if (column.MaximumIssueCount <= 0)
						{
							return;
						}

						double factor = (cellBounds.Width - column.Margin - column.Margin) /
						                (double) column.MaximumIssueCount;

						Draw(cellBounds.Top + 2, cellBounds.Height - 4,
						     column.Margin + cellBounds.Left, item.IssueCount,
						     graphics, GetColor(item.IssueType, column), factor);
					}

					[CanBeNull]
					private VerifiedConditionItem GetItem(int rowIndex)
					{
						return (VerifiedConditionItem) DataGridView?.Rows[rowIndex].DataBoundItem;
					}

					protected override object GetFormattedValue(
						object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
						TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter,
						DataGridViewDataErrorContexts context)
					{
						VerifiedConditionItem item = GetItem(rowIndex);
						return item?.IssueCount ?? 0;
					}

					private static void Draw(int top, int height, int leftMargin,
					                         int issueCount,
					                         [NotNull] Graphics graphics,
					                         Color color, double factor)
					{
						int width = leftMargin + (int) (issueCount * factor) - leftMargin;

						var rect = new Rectangle(leftMargin, top + 1, width, height - 2);

						using (Brush brush = new SolidBrush(color))
						{
							graphics.FillRectangle(brush, rect);
						}
					}
				}

				#endregion
			}
		}
	}
}
