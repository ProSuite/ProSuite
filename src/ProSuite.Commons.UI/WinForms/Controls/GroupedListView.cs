using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// A UserControl listing items by group. Four columns:
	/// checkbox (optional), icon (can be empty), name, info.
	/// Appearance similar to a ListView with grouping turned on.
	/// Reason for existence: ListView doesn't show grouping
	/// if VisualStyles are turned off.
	/// </summary>
	public partial class GroupedListView : UserControl
	{
		private const string _defaultNullGroupHeadingText = "Others";
		private bool _listNullGroupHeadingFirst;

		private string _nullGroupHeadingText;
		private Font _groupHeadingFont;
		private Color _groupHeadingColor;
		private Color _statusTextColor = Color.Red;
		private ImageList _smallImageList;
		private bool _showCheckBoxes;

		private bool _inUpdateCycle;
		private bool _handleEvents;
		private bool _needLayout;

		private TableLayoutPanel _tableLayoutPanel;
		private Control _bottomFillerLabel;

		private readonly List<InternalItem> _itemList;
		private readonly IDictionary<string, Control> _groupNameToControlMap;

		private bool _isFirstPaintEvent = true;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="GroupedListView"/> class.
		/// </summary>
		public GroupedListView()
		{
			InitializeComponent();

			_itemList = new List<InternalItem>();

			_groupHeadingColor = SystemColors.ControlText;
			_groupHeadingFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);

			_inUpdateCycle = false;
			_handleEvents = true;
			_needLayout = false;

			_groupNameToControlMap = new Dictionary<string, Control>();
		}

		#region Appearance Properties

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ImageList SmallImageList
		{
			get { return _smallImageList; }
			set { _smallImageList = value; }
		}

		[UsedImplicitly]
		public Font GroupHeadingFont
		{
			get { return _groupHeadingFont; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));
				_groupHeadingFont = value;
			}
		}

		[UsedImplicitly]
		public Color GroupHeadingColor
		{
			get { return _groupHeadingColor; }
			set { _groupHeadingColor = value; }
		}

		[UsedImplicitly]
		public Color StatusTextColor
		{
			get { return _statusTextColor; }
			set { _statusTextColor = value; }
		}

		[UsedImplicitly]
		public string NullGroupHeadingText
		{
			get { return _nullGroupHeadingText ?? _defaultNullGroupHeadingText; }
			set { _nullGroupHeadingText = value; }
		}

		[UsedImplicitly]
		public bool ListNullGroupHeadingFirst
		{
			get { return _listNullGroupHeadingFirst; }
			set { _listNullGroupHeadingFirst = value; }
		}

		public bool ShowCheckBoxes
		{
			get { return _showCheckBoxes; }
			set
			{
				_showCheckBoxes = value;
				PerformLayout();
			}
		}

		#endregion

		#region Item Management

		public void AddItem([NotNull] IGroupedListViewItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			_itemList.Add(new InternalItem(item, this));
			ListChanged();
		}

		public void AddItem([NotNull] string name, [CanBeNull] string group,
		                    [NotNull] string imageKey)
		{
			AddItem(new GroupedListViewItem(name, group, imageKey));
		}

		// Note: add AddItem overloads as needed

		public IGroupedListViewItem GetItem(int index)
		{
			return _itemList[index].UserItem;
		}

		public int CountItems()
		{
			return _itemList.Count;
		}

		public void ClearItems()
		{
			_itemList.Clear();
			ListChanged();
		}

		#endregion

		// A dictionary's Keys are a read-only collection,
		// so there is no danger of the client fiddling with it.
		public ICollection<string> Groups => _groupNameToControlMap.Keys;

		public void ScrollToGroup(string groupName)
		{
			if (_tableLayoutPanel == null)
			{
				return;
			}

			Control groupLabel;
			if (! _groupNameToControlMap.TryGetValue(groupName, out groupLabel))
			{
				return;
			}

			// Scroll to end of list (always the _bottomFillerLabel),
			// then scroll up to desired group label control:
			_tableLayoutPanel.ScrollControlIntoView(_bottomFillerLabel);
			_tableLayoutPanel.ScrollControlIntoView(groupLabel);
		}

		public void BeginUpdate()
		{
			_inUpdateCycle = true;
		}

		public void EndUpdate()
		{
			_inUpdateCycle = false;
			PerformLayout();
		}

		public void RefreshControlStates()
		{
			try
			{
				_handleEvents = false;

				foreach (InternalItem item in _itemList)
				{
					item.RefreshControls();
				}
			}
			finally
			{
				_handleEvents = true;
			}
		}

		public event ItemCheckEventHandler ItemCheck;

		#region Non-public members

		private void ListChanged()
		{
			_needLayout = true;
			PerformLayout();
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			if (_needLayout && ! _inUpdateCycle)
			{
				_needLayout = false;

				RebuildTable();
			}

			base.OnLayout(e);
		}

		#region Table Building

		private void CreateTableLayoutPanel()
		{
			if (_tableLayoutPanel != null)
			{
				Controls.Remove(_tableLayoutPanel);
				_tableLayoutPanel.Dispose();

				_tableLayoutPanel = null;
				_bottomFillerLabel = null;
			}

			_tableLayoutPanel =
				new TableLayoutPanel
				{
					Name = "_tableLayoutPanel",
					Margin = new Padding(0, 0, 0, 0),
					Padding = new Padding(8, 0, 0, 0),
					Dock = DockStyle.Fill,
					GrowStyle = TableLayoutPanelGrowStyle.AddRows
				};

			_tableLayoutPanel.Controls.Clear();
			_tableLayoutPanel.ColumnStyles.Clear();
			_tableLayoutPanel.RowStyles.Clear();

			_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

			_tableLayoutPanel.AutoScroll = true;
			_tableLayoutPanel.AutoSize = false;

			_tableLayoutPanel.BackColor = SystemColors.Window;
			_tableLayoutPanel.BorderStyle = BorderStyle.Fixed3D;
			_tableLayoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

			_tableLayoutPanel.CellPaint += _tableLayoutPanel_CellPaint;
			_tableLayoutPanel.MouseDown += _tableLayoutPanel_MouseDown;
			_tableLayoutPanel.Paint += _tableLayoutPanel_Paint;
			_tableLayoutPanel.Resize += _tableLayoutPanel_Resize;

			Controls.Add(_tableLayoutPanel);
		}

		private void RebuildTable()
		{
			Stopwatch stopwatch1 = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				stopwatch1 = _msg.DebugStartTiming();
			}

			CreateTableLayoutPanel();

			_msg.DebugStopTiming(stopwatch1, "GroupedListView: CreateTableLayoutPanel");

			Stopwatch stopwatch2 = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				stopwatch2 = _msg.DebugStartTiming();
			}

			_tableLayoutPanel.SuspendLayout();

			_tableLayoutPanel.RowCount = 0; // grow as needed
			_tableLayoutPanel.ColumnCount = _showCheckBoxes
				                                ? 4
				                                : 3;

			_itemList.Sort(); // by Group, then by Name
			_groupNameToControlMap.Clear(); // reset

			var rowNumber = 0;
			string currentHeading = null;

			foreach (InternalItem item in _itemList)
			{
				IGroupedListViewItem userItem = item.UserItem;

				string headingText = userItem.Group ?? NullGroupHeadingText;

				if (string.CompareOrdinal(headingText, currentHeading) != 0)
				{
					currentHeading = headingText;
					AddGroupHeading(currentHeading, rowNumber++);
				}

				AddItemEntry(item, rowNumber++);
			}

			AddFillerRow(rowNumber);

			_tableLayoutPanel.ResumeLayout();

			_msg.DebugStopTiming(stopwatch2, "GroupedListView: rebuilt table layout");
		}

		private void AddItemEntry([NotNull] InternalItem internalItem, int rowNumber)
		{
			var columnNumber = 0;

			if (_showCheckBoxes)
			{
				internalItem.CheckBox.CheckStateChanged += item_CheckStateChanged;
				internalItem.CheckBox.Anchor = AnchorStyles.None;
				internalItem.CheckBox.Margin = new Padding(3, 3, 3, 3);
				internalItem.CheckBox.Padding = new Padding(0, 0, 0, 0);
				_tableLayoutPanel.Controls.Add(internalItem.CheckBox,
				                               columnNumber++, rowNumber);

				internalItem.PictureBox.DoubleClick += item_DoubleClick;
				internalItem.NameLabel.DoubleClick += item_DoubleClick;
			}

			if (SmallImageList != null &&
			    ! string.IsNullOrEmpty(internalItem.UserItem.ImageKey))
			{
				internalItem.PictureBox.Image =
					SmallImageList.Images[internalItem.UserItem.ImageKey];
			}

			_tableLayoutPanel.Controls.Add(internalItem.PictureBox,
			                               columnNumber++, rowNumber);

			internalItem.NameLabel.Anchor = AnchorStyles.Left;
			internalItem.NameLabel.Margin = new Padding(1, 4, 1, 1);
			internalItem.NameLabel.Padding = new Padding(0, 0, 0, 0);
			_tableLayoutPanel.Controls.Add(internalItem.NameLabel,
			                               columnNumber++, rowNumber);

			internalItem.InfoLabel.Anchor = AnchorStyles.Left;
			internalItem.InfoLabel.Margin = new Padding(1, 4, 1, 1);
			internalItem.InfoLabel.Padding = new Padding(0, 0, 0, 0);
			_tableLayoutPanel.Controls.Add(internalItem.InfoLabel,
			                               columnNumber, rowNumber);

			string toolTipText = internalItem.UserItem.ToolTip;
			if (! string.IsNullOrEmpty(toolTipText))
			{
				_toolTip.SetToolTip(internalItem.NameLabel, toolTipText);
			}
		}

		private void AddGroupHeading([NotNull] string headingText, int rowNumber)
		{
			var headingLabel =
				new Label
				{
					AutoSize = true,
					Text = headingText,
					Font = GroupHeadingFont,
					ForeColor = GroupHeadingColor,
					Margin = new Padding(3, 5, 3, 2),
					Padding = new Padding(0, 10, 0, 0),
					Anchor = AnchorStyles.Left | AnchorStyles.Bottom
				};

			int columnCount = _tableLayoutPanel.ColumnCount;
			_tableLayoutPanel.Controls.Add(headingLabel, 0, rowNumber);
			_tableLayoutPanel.SetColumnSpan(headingLabel, columnCount);

			_groupNameToControlMap.Add(headingText, headingLabel);
		}

		private void AddFillerRow(int rowNumber)
		{
			var bottomFillerLabel = new Label
			                        {
				                        AutoSize = true,
				                        Text = string.Empty,
				                        Margin = new Padding(0)
			                        };

			int columnCount = _tableLayoutPanel.ColumnCount;
			_tableLayoutPanel.Controls.Add(bottomFillerLabel, 0, rowNumber);
			_tableLayoutPanel.SetColumnSpan(bottomFillerLabel, columnCount);

			_bottomFillerLabel = bottomFillerLabel;
		}

		#endregion

		#region Event handling

		private void _tableLayoutPanel_CellPaint(object sender,
		                                         TableLayoutCellPaintEventArgs e)
		{
			// Draw those horizontal lines with the nifty gradient:
			// thin and gray below item lines, thick and blue below headings.

			Assert.NotNull(_tableLayoutPanel, "_tableLayoutPanel");

			Rectangle cellBounds = e.CellBounds;

			if (e.Column != 0)
			{
				return;
			}

			int rowWidth = GetRowWidth(_tableLayoutPanel);

			Control control = _tableLayoutPanel.GetControlFromPosition(0, e.Row);

			if (control == _bottomFillerLabel)
			{
				// no line for filler label at bottom
			}
			else if (control != null && _tableLayoutPanel.GetColumnSpan(control) > 1)
			{
				var lineRectangle = new Rectangle(
					cellBounds.X, cellBounds.Bottom - 2, rowWidth, 2);
				var lineBrush = new LinearGradientBrush(
					lineRectangle, Color.Blue, _tableLayoutPanel.BackColor,
					LinearGradientMode.Horizontal);

				e.Graphics.FillRectangle(lineBrush, lineRectangle);
			}
			else
			{
				var lineRectangle = new Rectangle(
					cellBounds.X, cellBounds.Bottom - 1, rowWidth, 1);
				var lineBrush = new LinearGradientBrush(
					lineRectangle, Color.LightGray, _tableLayoutPanel.BackColor,
					LinearGradientMode.Horizontal);

				e.Graphics.FillRectangle(lineBrush, lineRectangle);
			}
		}

		private void _tableLayoutPanel_MouseDown(object sender, MouseEventArgs e)
		{
			// It seems TableLayoutPanels don't get the focus on mouse clicks.
			// Workaround: listen for MouseDown events and Focus() manually.

			Assert.NotNull(_tableLayoutPanel, "_tableLayoutPanel");

			_tableLayoutPanel.Focus();
		}

		private void _tableLayoutPanel_Resize(object sender, EventArgs e)
		{
			// Hack: Give focus to table layout (and thus taking it away
			// from wherever it was) to prevent the table layout from
			// automatically scrolling to the control with the focus.

			Assert.NotNull(_tableLayoutPanel, "_tableLayoutPanel");

			_tableLayoutPanel.Focus();
		}

		private void _tableLayoutPanel_Paint(object sender, PaintEventArgs e)
		{
			// Hack: Give focus to table layout (and thus taking it away
			// from wherever it was) to prevent the table layout from
			// automatically scrolling to the control with the focus.

			Assert.NotNull(_tableLayoutPanel, "_tableLayoutPanel");

			if (_isFirstPaintEvent)
			{
				_tableLayoutPanel.Focus();
				_isFirstPaintEvent = false;

				if (_tableLayoutPanel.Controls.Count > 0)
				{
					Control firstControl = _tableLayoutPanel.Controls[0];
					_tableLayoutPanel.ScrollControlIntoView(firstControl);
				}
			}
		}

		private static int GetRowWidth([NotNull] TableLayoutPanel tableLayoutPanel)
		{
			Assert.ArgumentNotNull(tableLayoutPanel, nameof(tableLayoutPanel));

			var rowWidth = 0;
			int[] columnWidths = tableLayoutPanel.GetColumnWidths();

			foreach (int columnWidth in columnWidths)
			{
				rowWidth += columnWidth;
			}

			return rowWidth;
		}

		private void item_CheckStateChanged(object sender, EventArgs e)
		{
			if (! _handleEvents || ! (sender is CheckBox))
			{
				return;
			}

			var checkBox = (CheckBox) sender;

			if (checkBox.Tag is InternalItem item && checkBox.Enabled)
			{
				ItemCheckEventArgs eventArgs;

				int itemIndex = _itemList.IndexOf(item);

				if (checkBox.CheckState == CheckState.Checked)
				{
					eventArgs = new ItemCheckEventArgs(
						itemIndex, CheckState.Checked, CheckState.Unchecked);
				}
				else
				{
					eventArgs = new ItemCheckEventArgs(
						itemIndex, CheckState.Unchecked, CheckState.Checked);
				}

				if (ItemCheck != null)
				{
					try
					{
						_handleEvents = false;

						ItemCheck.Invoke(this, eventArgs);

						// Note: client might changed NewValue:
						checkBox.CheckState = eventArgs.NewValue;
					}
					finally
					{
						_handleEvents = true;
					}
				}
			}
		}

		private void item_DoubleClick(object sender, EventArgs e)
		{
			if (! _handleEvents || ! (sender is Control))
			{
				return;
			}

			var control = (Control) sender;

			if (! control.Enabled || ! (control.Tag is InternalItem))
			{
				return;
			}

			var item = (InternalItem) control.Tag;

			switch (item.CheckBox.CheckState)
			{
				case CheckState.Checked:
					item.CheckBox.CheckState = CheckState.Unchecked;
					break;

				case CheckState.Unchecked:
					item.CheckBox.CheckState = CheckState.Checked;
					break;
			}
		}

		#endregion

		#region Internal representation of list items

		private class InternalItem : IComparable<InternalItem>
		{
			private readonly GroupedListView _owner;
			private readonly IGroupedListViewItem _userItem;

			private readonly CheckBox _checkBox = new CheckBox();
			private readonly PictureBox _pictureBox = new PictureBox();
			private readonly Label _labelName = new Label();
			private readonly Label _labelInfo = new Label();

			/// <summary>
			/// Initializes a new instance of the <see cref="InternalItem"/> class.
			/// </summary>
			/// <param name="userItem">The user item.</param>
			/// <param name="owner">The owner.</param>
			public InternalItem([NotNull] IGroupedListViewItem userItem,
			                    [NotNull] GroupedListView owner)
			{
				Assert.ArgumentNotNull(owner, nameof(owner));
				Assert.ArgumentNotNull(userItem, nameof(userItem));
				Assert.NotNullOrEmpty(userItem.Name,
				                      "userItem.Name must not be null nor empty");

				_owner = owner;
				_userItem = userItem;

				_checkBox.Text = string.Empty;
				_checkBox.AutoSize = true;
				_checkBox.Tag = this;

				_pictureBox.Size = new Size(1, 1); // very small
				_pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
				_pictureBox.Tag = this;

				_labelName.AutoSize = true;
				_labelName.Tag = this;

				_labelInfo.AutoSize = true;
				_labelInfo.Tag = this;

				RefreshControls();
			}

			public IGroupedListViewItem UserItem => _userItem;

			public CheckBox CheckBox => _checkBox;

			public PictureBox PictureBox => _pictureBox;

			public Label NameLabel => _labelName;

			public Label InfoLabel => _labelInfo;

			public void RefreshControls()
			{
				_checkBox.Checked = _userItem.Checked;
				_checkBox.Enabled = _userItem.Enabled;

				_labelName.Text = _userItem.Name;
				_labelName.ForeColor = _userItem.Color;

				_labelInfo.Text = _userItem.Status ?? string.Empty;
				_labelInfo.ForeColor = _owner.StatusTextColor;
			}

			public int CompareTo(InternalItem other)
			{
				if (other == null)
				{
					return 1;
				}

				int groupOrder = CompareGroup(_userItem.Group, other.UserItem.Group);

				if (groupOrder == 0)
				{
					int nameOrder = string.Compare(
						_userItem.Name, other.UserItem.Name,
						StringComparison.CurrentCultureIgnoreCase);

					return nameOrder;
				}

				return groupOrder;
			}

			private int CompareGroup(string group1, string group2)
			{
				if (group1 == null && group2 == null)
				{
					return 0;
				}

				if (group1 == null)
				{
					return _owner.ListNullGroupHeadingFirst
						       ? -1
						       : 1;
				}

				if (group2 == null)
				{
					return _owner.ListNullGroupHeadingFirst
						       ? 1
						       : -1;
				}

				return string.Compare(
					group1, group2,
					StringComparison.CurrentCultureIgnoreCase);
			}
		}

		#endregion

		#region Default implementation of IGroupedListViewItem

		private class GroupedListViewItem : IGroupedListViewItem
		{
			private readonly string _name;
			private readonly string _group;
			private readonly string _imageKey;
			private readonly string _toolTip;
			private readonly string _status;
			private readonly Color _color;
			private readonly bool _enabled;
			private readonly bool _checked;

			/// <summary>
			/// Initializes a new instance of the <see cref="GroupedListViewItem"/> class.
			/// </summary>
			/// <param name="name">The name.</param>
			/// <param name="group">The group.</param>
			/// <param name="imageKey">The image key.</param>
			public GroupedListViewItem([NotNull] string name,
			                           [CanBeNull] string group,
			                           [CanBeNull] string imageKey)
			{
				Assert.ArgumentNotNullOrEmpty(name, nameof(name));

				_name = name;
				_group = group;
				_imageKey = imageKey;

				_toolTip = null;
				_status = string.Empty;
				_color = SystemColors.ControlText;
				_checked = false;
				_enabled = false;
			}

			// Note: add other constructors as needed

			public bool Checked => _checked;

			public bool Enabled => _enabled;

			public string Name => _name;

			public string Status => _status;

			public Color Color => _color;

			public string Group => _group;

			public string ImageKey => _imageKey;

			public string ToolTip => _toolTip;
		}

		#endregion

		#endregion
	}
}
