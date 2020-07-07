using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Logging
{
    partial class LogWindowControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogWindowControl));
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			this._labelPlaceholder = new System.Windows.Forms.Label();
			this._logLevelImages = new System.Windows.Forms.ImageList();
			this._contextMenuStripLogGridBox = new System.Windows.Forms.ContextMenuStrip();
			this._toolStripMenuItemColumns = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItemMsgNumber = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItemMsgDate = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this._toolStripMenuItemShowAll = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this._toolStripMenuItemClearAllMessages = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItemShowLogEventItemDetails = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this._toolStripMenuItemHideDebugMessages = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStripMenuItemVerboseDebugLogging = new System.Windows.Forms.ToolStripMenuItem();
			this._forceRefreshTimer = new System.Windows.Forms.Timer();
			this._dataGridView = new DoubleBufferedDataGridView();
			this._columnLogLevelImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnLogNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnLogDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnLogMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._contextMenuStripLogGridBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// _labelPlaceholder
			// 
			this._labelPlaceholder.Dock = System.Windows.Forms.DockStyle.Fill;
			this._labelPlaceholder.Location = new System.Drawing.Point(0, 0);
			this._labelPlaceholder.Name = "_labelPlaceholder";
			this._labelPlaceholder.Size = new System.Drawing.Size(545, 147);
			this._labelPlaceholder.TabIndex = 0;
			this._labelPlaceholder.Text = "Place controls on the canvas for your dockable window definition";
			this._labelPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _logLevelImages
			// 
			this._logLevelImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_logLevelImages.ImageStream")));
			this._logLevelImages.TransparentColor = System.Drawing.Color.Fuchsia;
			this._logLevelImages.Images.SetKeyName(0, "DebugMessage.bmp");
			this._logLevelImages.Images.SetKeyName(1, "InfoMessage.bmp");
			this._logLevelImages.Images.SetKeyName(2, "WarnMessage.bmp");
			this._logLevelImages.Images.SetKeyName(3, "ErrorMessage.bmp");
			// 
			// _contextMenuStripLogGridBox
			// 
			this._contextMenuStripLogGridBox.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemColumns,
            this._toolStripMenuItem1,
            this._toolStripMenuItemShowAll,
            this._toolStripMenuItem4,
            this._toolStripMenuItemClearAllMessages,
            this._toolStripMenuItemShowLogEventItemDetails,
            this._toolStripMenuItem2,
            this._toolStripMenuItemHideDebugMessages,
            this._toolStripMenuItemVerboseDebugLogging});
			this._contextMenuStripLogGridBox.Name = "contextMenuStrip1";
			this._contextMenuStripLogGridBox.ShowCheckMargin = true;
			this._contextMenuStripLogGridBox.ShowImageMargin = false;
			this._contextMenuStripLogGridBox.Size = new System.Drawing.Size(202, 154);
			this._contextMenuStripLogGridBox.Opening += new System.ComponentModel.CancelEventHandler(this._contextMenuStripLogGridBox_Opening);
			// 
			// _toolStripMenuItemColumns
			// 
			this._toolStripMenuItemColumns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemMsgNumber,
            this._toolStripMenuItemMsgDate});
			this._toolStripMenuItemColumns.Name = "_toolStripMenuItemColumns";
			this._toolStripMenuItemColumns.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemColumns.Text = "Message Columns";
			// 
			// _toolStripMenuItemMsgNumber
			// 
			this._toolStripMenuItemMsgNumber.CheckOnClick = true;
			this._toolStripMenuItemMsgNumber.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._toolStripMenuItemMsgNumber.Name = "_toolStripMenuItemMsgNumber";
			this._toolStripMenuItemMsgNumber.Size = new System.Drawing.Size(118, 22);
			this._toolStripMenuItemMsgNumber.Text = "Number";
			this._toolStripMenuItemMsgNumber.CheckedChanged += new System.EventHandler(this._toolStripMenuItemMsgNumber_CheckedChanged);
			// 
			// _toolStripMenuItemMsgDate
			// 
			this._toolStripMenuItemMsgDate.Checked = true;
			this._toolStripMenuItemMsgDate.CheckOnClick = true;
			this._toolStripMenuItemMsgDate.CheckState = System.Windows.Forms.CheckState.Checked;
			this._toolStripMenuItemMsgDate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._toolStripMenuItemMsgDate.Name = "_toolStripMenuItemMsgDate";
			this._toolStripMenuItemMsgDate.Size = new System.Drawing.Size(118, 22);
			this._toolStripMenuItemMsgDate.Text = "Time";
			this._toolStripMenuItemMsgDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this._toolStripMenuItemMsgDate.CheckedChanged += new System.EventHandler(this._toolStripMenuItemMsgDate_CheckedChanged);
			// 
			// _toolStripMenuItem1
			// 
			this._toolStripMenuItem1.Name = "_toolStripMenuItem1";
			this._toolStripMenuItem1.Size = new System.Drawing.Size(198, 6);
			// 
			// _toolStripMenuItemShowAll
			// 
			this._toolStripMenuItemShowAll.Name = "_toolStripMenuItemShowAll";
			this._toolStripMenuItemShowAll.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemShowAll.Text = "Show Message History";
			this._toolStripMenuItemShowAll.Click += new System.EventHandler(this._toolStripMenuItemShowAll_Click);
			// 
			// _toolStripMenuItem4
			// 
			this._toolStripMenuItem4.Name = "_toolStripMenuItem4";
			this._toolStripMenuItem4.Size = new System.Drawing.Size(198, 6);
			// 
			// _toolStripMenuItemClearAllMessages
			// 
			this._toolStripMenuItemClearAllMessages.Name = "_toolStripMenuItemClearAllMessages";
			this._toolStripMenuItemClearAllMessages.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemClearAllMessages.Text = "Clear All Messages";
			this._toolStripMenuItemClearAllMessages.Click += new System.EventHandler(this._toolStripMenuItemClearAllMessages_Click);
			// 
			// _toolStripMenuItemShowLogEventItemDetails
			// 
			this._toolStripMenuItemShowLogEventItemDetails.Name = "_toolStripMenuItemShowLogEventItemDetails";
			this._toolStripMenuItemShowLogEventItemDetails.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemShowLogEventItemDetails.Text = "Show Message Details";
			this._toolStripMenuItemShowLogEventItemDetails.Click += new System.EventHandler(this._toolStripMenuItemShowLogEventItemDetails_Click);
			// 
			// _toolStripMenuItem2
			// 
			this._toolStripMenuItem2.Name = "_toolStripMenuItem2";
			this._toolStripMenuItem2.Size = new System.Drawing.Size(198, 6);
			// 
			// _toolStripMenuItemHideDebugMessages
			// 
			this._toolStripMenuItemHideDebugMessages.Checked = true;
			this._toolStripMenuItemHideDebugMessages.CheckOnClick = true;
			this._toolStripMenuItemHideDebugMessages.CheckState = System.Windows.Forms.CheckState.Checked;
			this._toolStripMenuItemHideDebugMessages.Name = "_toolStripMenuItemHideDebugMessages";
			this._toolStripMenuItemHideDebugMessages.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemHideDebugMessages.Text = "Hide Debug Messages";
			this._toolStripMenuItemHideDebugMessages.CheckedChanged += new System.EventHandler(this._toolStripMenuItemHideDebugMessages_CheckedChanged);
			// 
			// _toolStripMenuItemVerboseDebugLogging
			// 
			this._toolStripMenuItemVerboseDebugLogging.CheckOnClick = true;
			this._toolStripMenuItemVerboseDebugLogging.Name = "_toolStripMenuItemVerboseDebugLogging";
			this._toolStripMenuItemVerboseDebugLogging.Size = new System.Drawing.Size(201, 22);
			this._toolStripMenuItemVerboseDebugLogging.Text = "Verbose Debug Logging";
			this._toolStripMenuItemVerboseDebugLogging.CheckedChanged += new System.EventHandler(this._toolStripMenuItemVerboseDebugLogging_CheckedChanged);
			// 
			// _forceRefreshTimer
			// 
			this._forceRefreshTimer.Enabled = true;
			this._forceRefreshTimer.Interval = 500;
			this._forceRefreshTimer.Tick += new System.EventHandler(this._forceRefreshTimer_Tick);
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeColumns = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
			this._dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this._dataGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnLogLevelImage,
            this._columnLogNumber,
            this._columnLogDateTime,
            this._columnLogMessage});
			this._dataGridView.ContextMenuStrip = this._contextMenuStripLogGridBox;
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this._dataGridView.GridColor = System.Drawing.SystemColors.Control;
			this._dataGridView.Location = new System.Drawing.Point(0, 0);
			this._dataGridView.MultiSelect = false;
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.RowHeadersWidth = 4;
			this._dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this._dataGridView.RowTemplate.Height = 18;
			this._dataGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(545, 147);
			this._dataGridView.TabIndex = 1;
			this._dataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellDoubleClick);
			this._dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
			this._dataGridView.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this._dataGridView_CellMouseDown);
			this._dataGridView.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this._dataGridView_CellToolTipTextNeeded);
			this._dataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this._dataGridView_DataError);
			this._dataGridView.Paint += new System.Windows.Forms.PaintEventHandler(this._dataGridView_Paint);
			// 
			// _columnLogLevelImage
			// 
			this._columnLogLevelImage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle1.NullValue = ((object)(resources.GetObject("dataGridViewCellStyle1.NullValue")));
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this._columnLogLevelImage.DefaultCellStyle = dataGridViewCellStyle1;
			this._columnLogLevelImage.Description = "Log-Level";
			this._columnLogLevelImage.FillWeight = 1F;
			this._columnLogLevelImage.HeaderText = "";
			this._columnLogLevelImage.MinimumWidth = 19;
			this._columnLogLevelImage.Name = "_columnLogLevelImage";
			this._columnLogLevelImage.ReadOnly = true;
			this._columnLogLevelImage.Width = 19;
			// 
			// _columnLogNumber
			// 
			this._columnLogNumber.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._columnLogNumber.DefaultCellStyle = dataGridViewCellStyle2;
			this._columnLogNumber.FillWeight = 1F;
			this._columnLogNumber.HeaderText = "#";
			this._columnLogNumber.MinimumWidth = 30;
			this._columnLogNumber.Name = "_columnLogNumber";
			this._columnLogNumber.ReadOnly = true;
			this._columnLogNumber.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnLogNumber.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this._columnLogNumber.Visible = false;
			this._columnLogNumber.Width = 30;
			// 
			// _columnLogDateTime
			// 
			this._columnLogDateTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.Format = "T";
			this._columnLogDateTime.DefaultCellStyle = dataGridViewCellStyle3;
			this._columnLogDateTime.FillWeight = 1F;
			this._columnLogDateTime.HeaderText = "Time";
			this._columnLogDateTime.Name = "_columnLogDateTime";
			this._columnLogDateTime.ReadOnly = true;
			this._columnLogDateTime.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnLogDateTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this._columnLogDateTime.Width = 60;
			// 
			// _columnLogMessage
			// 
			this._columnLogMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._columnLogMessage.DefaultCellStyle = dataGridViewCellStyle4;
			this._columnLogMessage.HeaderText = "Message";
			this._columnLogMessage.MinimumWidth = 200;
			this._columnLogMessage.Name = "_columnLogMessage";
			this._columnLogMessage.ReadOnly = true;
			this._columnLogMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// LogWindowControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridView);
			this.Controls.Add(this._labelPlaceholder);
			this.Name = "LogWindowControl";
			this.Size = new System.Drawing.Size(545, 147);
			this.Load += new System.EventHandler(this.LoggingWindow_Load);
			this._contextMenuStripLogGridBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _labelPlaceholder;
		private System.Windows.Forms.ImageList _logLevelImages;
        private System.Windows.Forms.ContextMenuStrip _contextMenuStripLogGridBox;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemColumns;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemMsgNumber;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemMsgDate;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemClearAllMessages;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemShowLogEventItemDetails;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemHideDebugMessages;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemShowAll;
        private System.Windows.Forms.ToolStripSeparator _toolStripMenuItem4;
        //private System.Windows.Forms.DataGridViewImageColumn _columnLogLevelImage;
        //private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogDateTime;
        //private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogMessage;
        //private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogNumber;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemVerboseDebugLogging;
		private System.Windows.Forms.Timer _forceRefreshTimer;
		private DoubleBufferedDataGridView _dataGridView;
		private System.Windows.Forms.DataGridViewImageColumn _columnLogLevelImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogNumber;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogDateTime;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnLogMessage;
    }
}