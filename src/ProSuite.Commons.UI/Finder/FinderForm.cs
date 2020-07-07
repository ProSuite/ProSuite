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
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.Properties;
using ProSuite.Commons.UI.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Xml;

namespace ProSuite.Commons.UI.Finder
{
	public partial class FinderForm<T> : Form, IFinderView<T>,
	                                     IFormStateAware<FinderFormState> where T : class
	{
		[CanBeNull] private readonly IList<T> _list;

		[NotNull] private readonly FormStateManager<FinderFormState> _formStateManager;
		[NotNull] private readonly BoundDataGridHandler<T> _gridHandler;
		[NotNull] private readonly Latch _latch = new Latch();
		[NotNull] private readonly DataGridViewFindController _findController;

		[CanBeNull] private readonly ToolStripButton _toolStripButtonSelectFindResultRows;

		[CanBeNull] private readonly ToolStripComboBox
			_toolStripStretchComboBoxFinderQueries;

		[CanBeNull] private readonly ISettingsPersister<ContextSpecificSettings>
			_contextSettingsPersister;

		[CanBeNull] private FinderFormState _restoredState;

		[CanBeNull] private readonly ContextSpecificSettings
			_restoredContextSpecificSettings;

		#region Constructors

		private FinderForm([NotNull] IEnumerable<ColumnDescriptor> columnDescriptors,
		                   bool allowMultipleSelection = false,
		                   bool keepFindTextBetweenCalls = true,
		                   [CanBeNull] string filterSettingsContext = null)
		{
			Assert.ArgumentNotNull(columnDescriptors, nameof(columnDescriptors));

			InitializeComponent();

			KeepFindTextBetweenCalls = keepFindTextBetweenCalls;

			_toolStripStatusLabelMessage.Text = string.Empty;

			Type type = typeof(T);
			string typeName = Assert.NotNull(type.FullName,
			                                 "type has no name: {0}", type);

			_formStateManager = new FormStateManager<FinderFormState>(this, typeName);

			_formStateManager.RestoreState();

			if (! StringUtils.IsNullOrEmptyOrBlank(filterSettingsContext))
			{
				_contextSettingsPersister = CreateContextSettingsPersister(typeName,
				                                                           filterSettingsContext);
				_restoredContextSpecificSettings = _contextSettingsPersister.Read();
			}

			_gridHandler = new BoundDataGridHandler<T>(
				_dataGridView,
				restoreSelectionAfterUserSort : true);
			_gridHandler.SelectionChanged += _gridHandler_SelectionChanged;

			// configure the datagrid
			_dataGridView.SuspendLayout();

			_dataGridView.MultiSelect = allowMultipleSelection;

			_gridHandler.AddColumns(columnDescriptors);

			_dataGridView.AutoGenerateColumns = false;

			_findController = new DataGridViewFindController(_dataGridView,
			                                                 _dataGridViewFindToolStrip);
			_findController.FindResultChanged += _findController_FindResultChanged;

			if (allowMultipleSelection)
			{
				_toolStripButtonSelectFindResultRows =
					new ToolStripButton(LocalizableStrings.FinderForm_SelectRows)
					{
						Enabled = false,
						ImageScaling = ToolStripItemImageScaling.None,
						Image = Resources.SelectAll
					};

				_toolStripButtonSelectFindResultRows.Click +=
					_toolStripButtonSelectFindResultRows_Click;

				_dataGridViewFindToolStrip.Items.Add(
					_toolStripButtonSelectFindResultRows);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FinderForm{T}"/> class.
		/// </summary>
		/// <param name="columnDescriptors">The column descriptors.</param>
		/// <param name="finderQueries">The finder queries.</param>
		/// <param name="allowMultipleSelection">if set to <c>true</c> selecting multiple rows is allowed.</param>
		/// <param name="keepFindTextBetweenCalls">if set to <c>true</c> the find text is persisted and restored 
		/// between usages of the finder (individually per type of item to find) </param>
		/// <param name="filterSettingsContext"></param>
		internal FinderForm([NotNull] IEnumerable<ColumnDescriptor> columnDescriptors,
		                    [NotNull] IEnumerable<FinderQuery<T>> finderQueries,
		                    bool allowMultipleSelection = false,
		                    bool keepFindTextBetweenCalls = true,
		                    [CanBeNull] string filterSettingsContext = null)
			: this(columnDescriptors,
			       allowMultipleSelection,
			       keepFindTextBetweenCalls,
			       filterSettingsContext)
		{
			Assert.ArgumentNotNull(finderQueries, nameof(finderQueries));
			List<FinderQuery<T>> queryList = finderQueries.ToList();
			Assert.ArgumentCondition(queryList.Count > 0,
			                         "at least one finder query must be specified");

			_toolStripStretchComboBoxFinderQueries = CreateFinderQueryComboBox(queryList);

			ToolStripItemCollection toolStripItems = _dataGridViewFindToolStrip.Items;

			toolStripItems.Add(new ToolStripSeparator());
			toolStripItems.Add(new ToolStripLabel {Text = @"Query:"});
			toolStripItems.Add(_toolStripStretchComboBoxFinderQueries);

			_toolStripStretchComboBoxFinderQueries.SelectedIndex =
				GetDefaultFinderQueryItemIndex(
					_restoredContextSpecificSettings?.FinderQueryId);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FinderForm{T}"/> class.
		/// </summary>
		/// <param name="columnDescriptors">The column descriptors.</param>
		/// <param name="list">The list.</param>
		/// <param name="allowMultipleSelection">if set to <c>true</c> selecting multiple rows is allowed.</param>
		/// <param name="keepFindTextBetweenCalls">if set to <c>true</c> the find text is persisted and restored 
		/// between usages of the finder (individually per type of item to find) </param>
		/// <param name="filterSettingsContext"></param>
		internal FinderForm([NotNull] IEnumerable<ColumnDescriptor> columnDescriptors,
		                    [NotNull] IList<T> list,
		                    bool allowMultipleSelection = false,
		                    bool keepFindTextBetweenCalls = true,
		                    [CanBeNull] string filterSettingsContext = null)
			: this(columnDescriptors,
			       allowMultipleSelection,
			       keepFindTextBetweenCalls,
			       filterSettingsContext)
		{
			Assert.ArgumentNotNull(list, nameof(list));

			_list = list;
		}

		#endregion

		[PublicAPI]
		public bool KeepFindTextBetweenCalls { get; set; }

		#region IFinderView<T> Members

		public IFinderObserver Observer { get; set; }

		public bool OKEnabled
		{
			get { return _buttonOK.Enabled; }
			set { _buttonOK.Enabled = value; }
		}

		public int TotalCount => _dataGridView.Rows.Count;

		public IEnumerable<T> GetSelection()
		{
			foreach (DataGridViewRow row in _dataGridView.SelectedRows)
			{
				yield return (T) row.DataBoundItem;
			}
		}

		[CanBeNull]
		public IList<T> Selection { get; set; }

		public string StatusMessage
		{
			get { return _toolStripStatusLabelMessage.Text; }
			set { _toolStripStatusLabelMessage.Text = value; }
		}

		public int SelectionCount => _dataGridView.SelectedRows.Count;

		public bool HasSelection => _dataGridView.SelectedRows.Count > 0;

		#endregion

		#region IFormStateAware<FinderFormState> Members

		void IFormStateAware<FinderFormState>.RestoreState(FinderFormState formState)
		{
			_restoredState = formState;
		}

		void IFormStateAware<FinderFormState>.GetState(FinderFormState formState)
		{
			formState.DataGridViewSortState = new DataGridViewSortState(_dataGridView);

			formState.FilterRows = _dataGridViewFindToolStrip.FilterRows;

			if (KeepFindTextBetweenCalls && _contextSettingsPersister == null)
			{
				// persist the find text, to the "global" (per type) form state
				formState.FindText = _dataGridViewFindToolStrip.FindText;
			}
			else if (_restoredState != null)
			{
				// write the original restored value
				formState.FindText = _restoredState.FindText;
			}

			formState.MatchCase = _dataGridViewFindToolStrip.MatchCase;

			// save the scroll position
			formState.FirstDisplayedScrollingRowIndex =
				_dataGridView.FirstDisplayedScrollingRowIndex;
			formState.FirstDisplayedScrollingColumnIndex =
				_dataGridView.FirstDisplayedScrollingColumnIndex;
		}

		#endregion

		private int GetDefaultFinderQueryItemIndex([CanBeNull] string finderQueryId)
		{
			const int defaultIndex = 0;

			if (StringUtils.IsNullOrEmptyOrBlank(finderQueryId))
			{
				return defaultIndex;
			}

			string idToSelect = finderQueryId.Trim();

			ComboBox.ObjectCollection finderQueryItems =
				Assert.NotNull(_toolStripStretchComboBoxFinderQueries).Items;

			for (var i = 0; i < finderQueryItems.Count; i++)
			{
				var item = (FinderQueryItem) finderQueryItems[i];

				if (string.Equals(item.FinderQuery.Id.Trim(),
				                  idToSelect,
				                  StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return defaultIndex;
		}

		[NotNull]
		private static ToolStripComboBox CreateFinderQueryComboBox(
			[NotNull] IEnumerable<FinderQuery<T>> finderQueries)
		{
			var result = new ToolStripStretchComboBox
			             {
				             DropDownStyle = ComboBoxStyle.DropDownList,
				             BackColor = Color.FromKnownColor(KnownColor.Info),
				             ForeColor = Color.FromKnownColor(KnownColor.InfoText)
			             };

			result.BeginUpdate();
			foreach (FinderQuery<T> finderQuery in finderQueries)
			{
				result.Items.Add(new FinderQueryItem(finderQuery));
			}

			result.EndUpdate();

			return result;
		}

		[NotNull]
		private static ISettingsPersister<ContextSpecificSettings>
			CreateContextSettingsPersister(
				[NotNull] string typeName,
				[NotNull] string filterSettingsContext)
		{
			const string directory = "finder";
			const string fileExtension = "xml";

			string directoryPath = EnvironmentUtils.ConfigurationDirectoryProvider
			                                       .GetDirectory(AppDataFolder.Roaming,
			                                                     directory);

			string fileName = string.Format("{0}_{1}.{2}",
			                                typeName, filterSettingsContext,
			                                fileExtension);

			return new XmlSettingsPersister<ContextSpecificSettings>(
				directoryPath, fileName);
		}

		private bool IsSelectable(int rowIndex)
		{
			var selectable = _dataGridView.Rows[rowIndex].DataBoundItem as ISelectable;

			return selectable == null || selectable.Selectable;
		}

		private void ApplyState([CanBeNull] FinderFormState state,
		                        [CanBeNull] ContextSpecificSettings contextSpecificSettings,
		                        bool skipSorting = false)
		{
			if (state == null)
			{
				_dataGridViewFindToolStrip.FilterRows = true;

				if (! skipSorting) ApplyDefaultSortOrder();

				return;
			}

			_dataGridViewFindToolStrip.FilterRows = state.FilterRows;
			_dataGridViewFindToolStrip.MatchCase = state.MatchCase;

			if (! skipSorting)
			{
				// try to restore the saved grid sort state
				DataGridViewSortState sortState = state.DataGridViewSortState;

				if (sortState == null || ! sortState.TryApplyState(_dataGridView))
				{
					ApplyDefaultSortOrder();
				}
			}

			if (state.FirstDisplayedScrollingRowIndex > 0)
			{
				DataGridViewUtils.TrySetFirstDisplayedScrollingRow(
					_dataGridView, state.FirstDisplayedScrollingRowIndex);
			}

			if (state.FirstDisplayedScrollingColumnIndex > 0)
			{
				DataGridViewUtils.TrySetFirstDisplayedScrollinColumn(
					_dataGridView, state.FirstDisplayedScrollingColumnIndex);
			}

			if (KeepFindTextBetweenCalls)
			{
				_dataGridViewFindToolStrip.FindText = GetRestoredFindText(state,
				                                                          contextSpecificSettings);
			}
		}

		[CanBeNull]
		private static string GetRestoredFindText(
			[NotNull] FinderFormState formState,
			[CanBeNull] ContextSpecificSettings contextSpecificSettings)
		{
			if (contextSpecificSettings == null)
			{
				return StringUtils.IsNotEmpty(formState.FindText)
					       ? formState.FindText
					       : null;
			}

			return StringUtils.IsNotEmpty(contextSpecificSettings.FindText)
				       ? contextSpecificSettings.FindText
				       : null;
		}

		[CanBeNull]
		private static DataGridViewSortState GetDefaultSortState(
			[NotNull] DataGridView grid)
		{
			// get default sort order: sort ascending on the first text column
			// with sort mode = Automatic
			var column = grid.Columns
			                 .OfType<DataGridViewTextBoxColumn>()
			                 .FirstOrDefault(
				                 c => c.SortMode ==
				                      DataGridViewColumnSortMode.Automatic &&
				                      c.IsDataBound);

			return column == null ? null : new DataGridViewSortState(column.Name);
		}

		private void ApplyDefaultSortOrder()
		{
			// apply default sort order: sort ascending on the first text column
			// with sort mode = Automatic
			foreach (DataGridViewColumn column in _dataGridView.Columns)
			{
				if (column is DataGridViewTextBoxColumn &&
				    column.SortMode == DataGridViewColumnSortMode.Automatic &&
				    column.IsDataBound)
				{
					_dataGridView.Sort(column, ListSortDirection.Ascending);
					break;
				}
			}
		}

		[NotNull]
		private IList<T> GetSelectedQueryResult()
		{
			FinderQuery<T> query = GetSelectedFinderQuery();

			return query?.GetResult() ?? new List<T>();
		}

		[CanBeNull]
		private FinderQuery<T> GetSelectedFinderQuery()
		{
			if (_toolStripStretchComboBoxFinderQueries == null)
			{
				return null;
			}

			int index = _toolStripStretchComboBoxFinderQueries.SelectedIndex;
			if (index < 0)
			{
				return null;
			}

			var item =
				(FinderQueryItem) _toolStripStretchComboBoxFinderQueries.Items[index];

			return item.FinderQuery;
		}

		private void SaveContextSpecificSettings()
		{
			if (_contextSettingsPersister == null)
			{
				return;
			}

			string findText;
			if (KeepFindTextBetweenCalls)
			{
				// save the current find text
				findText = _dataGridViewFindToolStrip.FindText;
			}
			else
			{
				// save the original restored value
				findText = _restoredContextSpecificSettings?.FindText;
			}

			FinderQuery<T> query = GetSelectedFinderQuery();

			string finderQueryId = query?.Id;

			_contextSettingsPersister?.Write(new ContextSpecificSettings
			                                 {
				                                 FindText = findText,
				                                 FinderQueryId = finderQueryId
			                                 });
		}

		#region Event handlers

		private void FinderForm_Load(object sender, EventArgs e)
		{
			_latch.RunInsideLatch(
				delegate
				{
					bool presorted = _gridHandler.BindTo(
						new SortableBindingList<T>(_list ?? GetSelectedQueryResult()),
						sortStateOverride : _restoredState?.DataGridViewSortState,
						defaultSortState : GetDefaultSortState(_dataGridView));

					ApplyState(_restoredState, _restoredContextSpecificSettings,
					           skipSorting : presorted);
				});

			_dataGridView.ResumeLayout(true);

			Observer.ViewLoaded();

			_dataGridViewFindToolStrip.ActivateFindField(this, selectAllText : true);

			// start listening to selection changes in the list of finder queries (if defined)
			if (_toolStripStretchComboBoxFinderQueries != null)
			{
				_toolStripStretchComboBoxFinderQueries.SelectedIndexChanged +=
					_toolStripStretchComboBoxFinderQueries_SelectedIndexChanged;
			}
		}

		private void FinderForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager.SaveState();

			SaveContextSpecificSettings();
		}

		private void _toolStripStretchComboBoxFinderQueries_SelectedIndexChanged(
			object sender, EventArgs e)
		{
			using (new WaitCursor())
			{
				_latch.RunInsideLatch(
					delegate
					{
						BoundDataGridSelectionState<T> selection =
							_gridHandler.GetSelectionState();
						ColumnSortState sortState =
							DataGridViewUtils.GetSortState(_dataGridView);

						var list = new SortableBindingList<T>(GetSelectedQueryResult(),
						                                      raiseListChangedEventAfterSort :
						                                      false);

						bool presorted = DataGridViewUtils.TrySortBindingList<T>(
							list, _dataGridView, sortState);

						_gridHandler.BindTo(list);

						if (! presorted)
						{
							DataGridViewUtils.TryApplySortState(_dataGridView, sortState);
						}

						_gridHandler.RestoreSelection(selection);
					});

				_dataGridViewFindToolStrip.ActivateFindField(this, selectAllText : true);

				Observer.SelectionChanged();
			}
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			Observer.OKClicked();
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			Observer.CancelClicked();
		}

		private void _gridHandler_SelectionChanged(object sender, EventArgs e)
		{
			if (_latch.IsLatched)
			{
				return;
			}

			// unselect any rows that the user selected, but which should
			// not be selectable
			_latch.RunInsideLatch(
				delegate
				{
					foreach (var row in _dataGridView.SelectedRows
					                                 .Cast<DataGridViewRow>()
					                                 .Where(r => ! IsSelectable(r.Index)))
					{
						row.Selected = false;
					}
				});

			Observer.SelectionChanged();
		}

		private void _dataGridView_CellDoubleClick(object sender,
		                                           DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
			{
				// ignore double-click on column headers
				return;
			}

			Observer.ListDoubleClicked();
		}

		private void _dataGridView_CellFormatting(object sender,
		                                          DataGridViewCellFormattingEventArgs e)
		{
			if (! IsSelectable(e.RowIndex))
			{
				e.CellStyle.BackColor = Color.LightGray;
			}
		}

		private void _toolStripButtonSelectFindResultRows_Click(
			object sender, EventArgs e)
		{
			_findController.SelectAllRows();
		}

		private void _findController_FindResultChanged(object sender, EventArgs e)
		{
			if (_toolStripButtonSelectFindResultRows != null)
			{
				_toolStripButtonSelectFindResultRows.Enabled =
					_findController.FindResultCount > 0;
			}
		}

		#endregion

		private class FinderQueryItem
		{
			public FinderQueryItem([NotNull] FinderQuery<T> finderQuery)
			{
				Assert.ArgumentNotNull(finderQuery, nameof(finderQuery));

				FinderQuery = finderQuery;
			}

			[NotNull]
			public FinderQuery<T> FinderQuery { get; }

			public override string ToString()
			{
				return FinderQuery.Name;
			}
		}
	}
}
