using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Properties;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public sealed class DataGridViewFindToolStrip : ToolStripEx,
	                                                IDataGridViewFindToolsView
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ToolStripTextBox _toolStripTextBoxFind;
		private readonly ToolStripButton _toolStripButtonClearFilter;
		private readonly ToolStripButton _toolStripButtonMoveNext;
		private readonly ToolStripButton _toolStripButtonMovePrevious;
		private readonly ToolStripLabel _toolStripLabelCount;
		private readonly ToolStripCheckBox _toolStripCheckBoxMatchCase;
		private readonly ToolStripButton _toolStripButtonFilterRows;

		private Image _filterImage;
		private Image _orangeFilterImage;

		private readonly Color _defaultBackgroundColor;
		private readonly Color _findTextDefaultBackColor;
		private readonly Color _findTextDefaultForeColor;

		//private Color _findTextHighLightColor = Color.Empty;
		private bool _highlightFindText = true;

		// At 96 DPI, like the values the designer writes; applied scaled to the screen DPI.
		private int _findTextBoxLogicalWidth = 150;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DataGridViewFindToolStrip"/> class.
		/// </summary>
		public DataGridViewFindToolStrip()
		{
			_filterImage = Resources.Filter;
			_orangeFilterImage = Resources.FilterOrange;

			SuspendLayout();

			var toolStripLabelFind = new ToolStripLabel(
				string.Format(
					"{0}: ", LocalizableStrings.DataGridViewFindToolStrip_Find));

			_toolStripTextBoxFind = new ToolStripTextBox
			                        {
				                        Width = LogicalToDeviceUnits(_findTextBoxLogicalWidth),
				                        AutoSize = false
			                        };

			_toolStripTextBoxFind.TextChanged += _toolStripTextBoxFind_TextChanged;
			_toolStripTextBoxFind.KeyDown += _toolStripTextBoxFind_KeyDown;

			_toolStripButtonClearFilter =
				new ToolStripButton
				{
					Text =
						LocalizableStrings.DataGridViewFindToolStrip_ClearFilter,
					ImageScaling = ToolStripItemImageScaling.SizeToFit,
					Image = Resources.ClearFilter,
					DisplayStyle = ToolStripItemDisplayStyle.Image,
					Enabled = false
				};
			_toolStripButtonClearFilter.Click += _toolStripButtonClearFilter_Click;

			_toolStripLabelCount =
				new ToolStripLabel
				{
					Width = 50,
					Padding = new Padding(4, 0, 0, 0)
				};

			_toolStripButtonMoveNext =
				new ToolStripButton
				{
					Text =
						LocalizableStrings.DataGridViewFindToolStrip_FindNext,
					ImageScaling = ToolStripItemImageScaling.SizeToFit,
					Image = Resources.DownArrow
				};
			_toolStripButtonMoveNext.Click += _toolStripButtonMoveNext_Click;

			_toolStripButtonMovePrevious =
				new ToolStripButton
				{
					Text = LocalizableStrings.DataGridViewFindToolStrip_FindPrevious,
					ImageScaling = ToolStripItemImageScaling.SizeToFit,
					Image = Resources.UpArrow
				};
			_toolStripButtonMovePrevious.Click += _toolStripButtonMovePrevious_Click;

			_toolStripCheckBoxMatchCase = new ToolStripCheckBox();
			_toolStripCheckBoxMatchCase.CheckBoxControl.Text =
				LocalizableStrings.DataGridViewFindToolStrip_MatchCase;
			_toolStripCheckBoxMatchCase.CheckBoxControl.Padding = new Padding(0, 2, 0, 0);
			_toolStripCheckBoxMatchCase.CheckedChanged +=
				_toolStripCheckBoxMatchCase_CheckChanged;

			_toolStripButtonFilterRows =
				new ToolStripButton
				{
					Text = LocalizableStrings.DataGridViewFindToolStrip_FilterRows,
					ImageScaling = ToolStripItemImageScaling.SizeToFit,
					Image = Resources.Filter,
					CheckOnClick = true,
				};
			_toolStripButtonFilterRows.CheckedChanged +=
				_toolStripButtonFilterRows_CheckChanged;

			Items.Add(toolStripLabelFind);
			Items.Add(_toolStripTextBoxFind);
			Items.Add(_toolStripButtonClearFilter);
			Items.Add(_toolStripLabelCount);
			Items.Add(_toolStripButtonMoveNext);
			Items.Add(_toolStripButtonMovePrevious);
			Items.Add(_toolStripCheckBoxMatchCase);
			Items.Add(_toolStripButtonFilterRows);

			const bool performLayout = true;
			ResumeLayout(performLayout);

			_defaultBackgroundColor = _toolStripLabelCount.BackColor;
			_findTextDefaultBackColor = _toolStripTextBoxFind.BackColor;
			_findTextDefaultForeColor = _toolStripTextBoxFind.ForeColor;
		}

		#endregion

		[DefaultValue(true)]
		public bool HighlightFindText
		{
			get { return _highlightFindText; }
			set
			{
				if (_highlightFindText == value)
				{
					return;
				}

				_highlightFindText = value;

				ApplyHighlightFindText();
			}
		}

		/// <summary>
		/// Whether the find next/previous buttons show their text next to the image. If not,
		/// the text is the tooltip.
		/// </summary>
		[DefaultValue(true)]
		public bool ShowFindButtonText
		{
			get { return _toolStripButtonMoveNext.DisplayStyle != ToolStripItemDisplayStyle.Image; }
			set
			{
				ToolStripItemDisplayStyle style = value
					                                  ? ToolStripItemDisplayStyle.ImageAndText
					                                  : ToolStripItemDisplayStyle.Image;
				_toolStripButtonMoveNext.DisplayStyle = style;
				_toolStripButtonMovePrevious.DisplayStyle = style;
			}
		}

		/// <summary>
		/// The width of the find text box at 96 DPI. It is scaled to the screen DPI.
		/// </summary>
		public int FindTextBoxWidth
		{
			get { return _findTextBoxLogicalWidth; }
			set
			{
				_findTextBoxLogicalWidth = value;
				_toolStripTextBoxFind.Width = LogicalToDeviceUnits(value);
			}
		}

		public string FindText
		{
			get { return _toolStripTextBoxFind.Text; }
			set { _toolStripTextBoxFind.Text = value; }
		}

		public bool FilterRows
		{
			get { return _toolStripButtonFilterRows.Checked; }
			set { _toolStripButtonFilterRows.Checked = value; }
		}

		public bool MatchCase
		{
			get { return _toolStripCheckBoxMatchCase.Checked; }
			set { _toolStripCheckBoxMatchCase.Checked = value; }
		}

		public void ActivateFindField([NotNull] ContainerControl containerControl,
		                              bool selectAllText = false)
		{
			Assert.ArgumentNotNull(containerControl, nameof(containerControl));

			containerControl.ActiveControl = _toolStripTextBoxFind.TextBox;

			if (selectAllText)
			{
				_toolStripTextBoxFind.SelectAll();
			}
		}

		public IDataGridViewFindObserver Observer { get; set; }

		bool IDataGridViewFindToolsView.ClearFilterEnabled
		{
			get { return _toolStripButtonClearFilter.Enabled; }
			set { _toolStripButtonClearFilter.Enabled = value; }
		}

		bool IDataGridViewFindToolsView.MoveNextEnabled
		{
			get { return _toolStripButtonMoveNext.Enabled; }
			set { _toolStripButtonMoveNext.Enabled = value; }
		}

		bool IDataGridViewFindToolsView.MovePreviousEnabled
		{
			get { return _toolStripButtonMovePrevious.Enabled; }
			set { _toolStripButtonMovePrevious.Enabled = value; }
		}

		bool IDataGridViewFindToolsView.FilterRowsButtonVisible
		{
			get { return _toolStripButtonFilterRows.Enabled; }
			set
			{
				if (value == _toolStripButtonFilterRows.Enabled)
				{
					return;
				}

				// _toolStripButtonFilterRows.Enabled = value;
				_toolStripButtonFilterRows.Visible = value;

				// HideFilterRowsButton(value);
				// HideFilterRowsButton(! value);
			}
		}

		string IDataGridViewFindToolsView.FindResultStatusText
		{
			get { return _toolStripLabelCount.Text; }
			set { _toolStripLabelCount.Text = value; }
		}

		void IDataGridViewFindToolsView.SetFindResultStatusColor(Color color)
		{
			_toolStripLabelCount.BackColor = color;
		}

		void IDataGridViewFindToolsView.ClearFindResultStatusColor()
		{
			_toolStripLabelCount.BackColor = _defaultBackgroundColor;
		}

		public void DisplayFilteredRowsState(bool hasFilteredRows)
		{
			_toolStripButtonFilterRows.Image = hasFilteredRows
				                                   ? _orangeFilterImage
				                                   : _filterImage;
		}

		#region Non-public

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			ScaleToDpi();
		}

		protected override void OnDpiChangedAfterParent(EventArgs e)
		{
			base.OnDpiChangedAfterParent(e);

			ScaleToDpi();
		}

		// The images are 48 px, drawn at 16 logical px. Set, not multiplied, so a form that
		// already scaled the ImageScalingSize of its tool strips gets the same size.
		private void ScaleToDpi()
		{
			int size = LogicalToDeviceUnits(16);
			ImageScalingSize = new Size(size, size);

			_toolStripTextBoxFind.Width = LogicalToDeviceUnits(_findTextBoxLogicalWidth);
		}

		private void ApplyHighlightFindText()
		{
			if (HighlightFindText && StringUtils.IsNotEmpty(_toolStripTextBoxFind.Text))
			{
				_toolStripTextBoxFind.BackColor = Color.FromKnownColor(KnownColor.Info);
				_toolStripTextBoxFind.ForeColor =
					Color.FromKnownColor(KnownColor.InfoText);
			}
			else
			{
				_toolStripTextBoxFind.BackColor = _findTextDefaultBackColor;
				_toolStripTextBoxFind.ForeColor = _findTextDefaultForeColor;
			}
		}

		//private void HideFilterRowsButton(bool hide)
		//{
		//	if (hide && Items.Contains(_toolStripButtonFilterRows))
		//	{
		//		Items.Remove(_toolStripButtonFilterRows);
		//	}
		//	else if (!Items.Contains(_toolStripButtonFilterRows))
		//	{
		//		Items.Add(_toolStripButtonFilterRows);
		//	}
		//}

		#region Event handlers

		private void _toolStripTextBoxFind_TextChanged(object sender, EventArgs e)
		{
			Try(() =>
			{
				ApplyHighlightFindText();
				try
				{
					Observer?.Find(_toolStripTextBoxFind.Text);
				}
				catch (Exception exception)
				{
					// ignored
				}
			});
		}

		private void _toolStripTextBoxFind_KeyDown(object sender, KeyEventArgs e)
		{
			Try(() => { Observer?.HandleFindKeyEvent(e); });
		}

		private void _toolStripButtonClearFilter_Click(object sender, EventArgs e)
		{
			Try(() => { Observer?.ClearFilter(); });
		}

		private void _toolStripButtonMovePrevious_Click(object sender, EventArgs e)
		{
			Try(() => { Observer?.MoveToPrevious(); });
		}

		private void _toolStripButtonMoveNext_Click(object sender, EventArgs e)
		{
			Try(() => { Observer?.MoveToNext(); });
		}

		private void _toolStripCheckBoxMatchCase_CheckChanged(object sender, EventArgs e)
		{
			Try(() =>
			{
				if (Observer != null)
				{
					Observer.MatchCase = _toolStripCheckBoxMatchCase.Checked;
				}
			});
		}

		private void _toolStripButtonFilterRows_CheckChanged(object sender, EventArgs e)
		{
			Try(() =>
			{
				if (Observer != null)
				{
					Observer.FilterRows = _toolStripButtonFilterRows.Checked;
				}
			});
		}

		#endregion

		#endregion

		private static void Try([NotNull] Action procedure,
		                        [CallerMemberName] string method = null)
		{
			try
			{
				_msg.VerboseDebug(() => $"QueryHistoryForm.{method}");

				procedure();
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, _msg);
			}
		}
	}
}
