using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.Commons.UI.ScreenBinding.Lists;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using ProSuite.UI.Core.Properties;
using ProSuite.UI.Core.QA;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public partial class VerifiedDatasetsControl : UserControl
	{
		private readonly BoundDataGridHandler<VerifiedDatasetItem> _gridHandler;

		private readonly Color _loadColor = Color.Blue;
		private readonly Color _testColor = Color.Red;

		public event EventHandler SelectionChanged;
		private readonly Latch _latch = new Latch();

		public VerifiedDatasetsControl()
		{
			InitializeComponent();

			_gridHandler = new BoundDataGridHandler<VerifiedDatasetItem>(_dataGridView);
			new DataGridViewFindController(_dataGridView, _dataGridViewFindToolStrip);

			_columnFullTime.LoadColor = _loadColor;
			_columnFullTime.ExecColor = _testColor;
		}

		public void Bind(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] Predicate<QualityConditionVerification> includeConditionVerification)
		{
			double maximumTime;
			SortableBindingList<VerifiedDatasetItem> bindingList =
				GetVerifiedDatasetItems(qualityVerification, includeConditionVerification,
				                        out maximumTime);

			_dataGridView.SuspendLayout();

			try
			{
				_columnFullTime.MaximumTime = maximumTime;

				_latch.RunInsideLatch(() =>
				{
					DataGridViewSortState sortState = GetSortState();
					_gridHandler.BindTo(bindingList);
					RestoreSortState(sortState);
				});
			}
			finally
			{
				_dataGridView.ResumeLayout();
			}

			OnSelectionChanged();
		}

		public void Clear()
		{
			_gridHandler.ClearRows();
		}

		[NotNull]
		private static SortableBindingList<VerifiedDatasetItem> GetVerifiedDatasetItems(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] Predicate<QualityConditionVerification> includeConditionVerification,
			out double maximumTime)
		{
			var result = new SortableBindingList<VerifiedDatasetItem>();

			maximumTime = 0;
			foreach (
				QualityConditionVerification conditionVerification in
				qualityVerification.ConditionVerifications)
			{
				if (! includeConditionVerification(conditionVerification))
				{
					continue;
				}

				var datasets = new SimpleSet<Dataset>();

				QualityCondition condition = conditionVerification.DisplayableCondition;

				foreach (Dataset dataset in condition.GetDatasetParameterValues(
					         includeSourceDatasets: true))
				{
					if (datasets.Contains(dataset))
					{
						continue;
					}

					datasets.Add(dataset);

					VerifiedDatasetItem item = CreateVerifiedDatasetItem(
						qualityVerification, dataset,
						conditionVerification);

					if (item.TotalTime > maximumTime)
					{
						maximumTime = item.TotalTime;
					}

					result.Add(item);
				}
			}

			return result;
		}

		[NotNull]
		private static VerifiedDatasetItem CreateVerifiedDatasetItem(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] Dataset dataset,
			[NotNull] QualityConditionVerification conditionVerification)
		{
			QualityVerificationDataset verifiedDataset =
				qualityVerification.GetVerificationDataset(dataset);

			return new VerifiedDatasetItem(conditionVerification, dataset,
			                               verifiedDataset?.LoadTime ?? 0);
		}

		[Browsable(false)]
		public int SelectionCount
		{
			get { return _gridHandler.SelectedRowCount; }
		}

		[Browsable(false)]
		[NotNull]
		public IList<QualityConditionVerification> SelectedConditionVerifications
		{
			get
			{
				return _gridHandler.GetSelectedRows()
				                   .Select(item => item.ConditionVerification)
				                   .ToList();
			}
		}

		public void RestoreSortState([CanBeNull] DataGridViewSortState sortState)
		{
			if (sortState == null)
			{
				sortState = new DataGridViewSortState(_columnTestName.Name);
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
			EventHandler handler = SelectionChanged;
			handler?.Invoke(this, EventArgs.Empty);
		}

		private static double Draw(int top, int height, int leftMargin,
		                           double startTime, double dTime,
		                           [NotNull] Graphics graphics,
		                           Color color, double factor)
		{
			int left = leftMargin + (int) (startTime * factor);
			int width = leftMargin + (int) ((startTime + dTime) * factor) - left;

			var rect = new Rectangle(left, top + 1, width, height - 2);

			using (Brush brush = new SolidBrush(color))
			{
				graphics.FillRectangle(brush, rect);
			}

			return startTime + dTime;
		}

		private void _dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			_latch.RunLatchedOperation(OnSelectionChanged);
		}

		#region Nested type: DataGridViewTimeColumn

		public class DataGridViewTimeColumn : DataGridViewColumn
		{
			private Color _execColor;
			private Color _loadColor;
			private int _margin = 5;

			public DataGridViewTimeColumn()
			{
				base.CellTemplate = new Cell();
			}

			public int Margin
			{
				get { return _margin; }
				set { _margin = value; }
			}

			public Color LoadColor
			{
				get { return _loadColor; }
				set { _loadColor = value; }
			}

			public Color ExecColor
			{
				get { return _execColor; }
				set { _execColor = value; }
			}

			public double MaximumTime { get; set; }

			#region Nested type: Cell

			private class Cell : DataGridViewCell
			{
				protected override void Paint(
					Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
					DataGridViewElementStates cellState, object value, object formattedValue,
					string errorText, DataGridViewCellStyle cellStyle,
					DataGridViewAdvancedBorderStyle advancedBorderStyle,
					DataGridViewPaintParts paintParts)
				{
					DataGridViewTimeColumn column = null;
					VerifiedDatasetItem item = null;

					if (DataGridView != null)
					{
						column = DataGridView.Columns[ColumnIndex] as DataGridViewTimeColumn;
						item = DataGridView.Rows[rowIndex].DataBoundItem as
							       VerifiedDatasetItem;
					}

					using (Brush brush = new SolidBrush(cellStyle.BackColor))
					{
						graphics.FillRectangle(brush, cellBounds);
					}

					if (column == null || item == null)
					{
						return;
					}

					if (column.MaximumTime <= 0)
					{
						return;
					}

					double startTime = 0;
					double factor = (cellBounds.Width - column._margin - column._margin) /
					                column.MaximumTime;

					startTime = Draw(cellBounds.Top + 2, cellBounds.Height - 4,
					                 column._margin + cellBounds.Left, startTime,
					                 item.DataLoadTime, graphics, column._loadColor, factor);

					Draw(cellBounds.Top + 2, cellBounds.Height - 4,
					     column._margin + cellBounds.Left, startTime,
					     item.ExecuteTime, graphics, column._execColor, factor);
				}

				protected override object GetFormattedValue(
					object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
					TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter,
					DataGridViewDataErrorContexts context)
				{
					return 1; // TODO return sum of times?
				}
			}

			#endregion
		}

		private class VerifiedDatasetItem
		{
			private static readonly Image _allowImage =
				(Bitmap) TestTypeImages.TestTypeWarning.Clone();

			private static readonly Image _continueImage =
				(Bitmap) TestTypeImages.TestTypeError.Clone();

			private static readonly Image _stopImage =
				(Bitmap) TestTypeImages.TestTypeStop.Clone();

			private static readonly Image _noIssuesImage = (Bitmap) VerificationResultImages.OK.Clone();
			private static readonly Image _warningsImage = (Bitmap) VerificationResultImages.Warning.Clone();
			private static readonly Image _errorsImage = (Bitmap) VerificationResultImages.Error.Clone();

			private static readonly SortedList<string, Image> _datasetImageList;

			static VerifiedDatasetItem()
			{
				ImageList list = DatasetTypeImageLookup.CreateImageList();
				_datasetImageList = new SortedList<string, Image>();

				foreach (string key in list.Images.Keys)
				{
					Image image = Assert.NotNull(list.Images[key], "image");

					var clonedImage = (Image) image.Clone();

					clonedImage.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(key);

					_datasetImageList.Add(key, clonedImage);
				}

				_allowImage.Tag = QualityConditionType.Allowed;
				_continueImage.Tag = QualityConditionType.ContinueOnError;
				_stopImage.Tag = QualityConditionType.StopOnError;

				_noIssuesImage.Tag = VerificationResultType.NoIssues;
				_warningsImage.Tag = VerificationResultType.Warnings;
				_errorsImage.Tag = VerificationResultType.Errors;
			}

			public VerifiedDatasetItem(
				[NotNull] QualityConditionVerification conditionVerification,
				[NotNull] IDdxDataset dataset,
				double loadTime)
			{
				ConditionVerification = conditionVerification;

				QualityCondition condition = conditionVerification.DisplayableCondition;

				Type = conditionVerification.AllowErrors
					        ? _allowImage
					        : ! conditionVerification.StopOnError
						        ? _continueImage
						        : _stopImage;

				Status = conditionVerification.ErrorCount == 0
					          ? _noIssuesImage
					          : conditionVerification.AllowErrors
						          ? _warningsImage
						          : _errorsImage;

				TestName = condition.Name;
				TestType = condition.TestDescriptor.Name;
				DataLoadTime = loadTime;
				DatasetName = dataset.AliasName;

				string datasetImageKey = DatasetTypeImageLookup.GetImageKey(dataset);
				DatasetType = _datasetImageList[datasetImageKey];
			}

			[NotNull]
			public QualityConditionVerification ConditionVerification { get; }

			public double DataLoadTime { get; }

			[UsedImplicitly]
			public Image Type { get; }

			[UsedImplicitly]
			public Image Status { get; }

			[UsedImplicitly]
			public string TestName { get; }

			[UsedImplicitly]
			public string TestType { get; }

			[UsedImplicitly]
			public string DatasetName { get; }

			[UsedImplicitly]
			public Image DatasetType { get; }

			[UsedImplicitly]
			public double ExecuteTime => ConditionVerification.TotalExecuteTime;

			[UsedImplicitly]
			public double RowExecuteTime => ConditionVerification.RowExecuteTime;

			[UsedImplicitly]
			public double TileExecuteTime => ConditionVerification.TileExecuteTime;

			public double TotalTime => ConditionVerification.TotalExecuteTime + DataLoadTime;
		}

		#endregion
	}
}
