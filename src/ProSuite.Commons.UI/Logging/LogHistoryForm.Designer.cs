using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Logging
{
    partial class LogHistoryForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this._dataGridViewLogEvents = new DoubleBufferedDataGridView();
            this.logLevelImageDataGridViewImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.LogNummer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.logDateTimeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.logMessageDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._bindingSourceLogEvent = new System.Windows.Forms.BindingSource(this.components);
            this._groupBoxDetails = new System.Windows.Forms.GroupBox();
            this.textBoxDetails = new System.Windows.Forms.TextBox();
            this._buttonCopy = new System.Windows.Forms.Button();
            this._buttonClose = new System.Windows.Forms.Button();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._splitContainer = new SplitContainerEx();
            this._dataGridViewFindToolStrip = new DataGridViewFindToolStrip();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridViewLogEvents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._bindingSourceLogEvent)).BeginInit();
            this._groupBoxDetails.SuspendLayout();
            this._statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
            this._splitContainer.Panel1.SuspendLayout();
            this._splitContainer.Panel2.SuspendLayout();
            this._splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // _dataGridViewLogEvents
            // 
            this._dataGridViewLogEvents.AllowUserToAddRows = false;
            this._dataGridViewLogEvents.AllowUserToDeleteRows = false;
            this._dataGridViewLogEvents.AllowUserToResizeColumns = false;
            this._dataGridViewLogEvents.AllowUserToResizeRows = false;
            this._dataGridViewLogEvents.AutoGenerateColumns = false;
            this._dataGridViewLogEvents.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this._dataGridViewLogEvents.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._dataGridViewLogEvents.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this._dataGridViewLogEvents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGridViewLogEvents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.logLevelImageDataGridViewImageColumn,
            this.LogNummer,
            this.logDateTimeDataGridViewTextBoxColumn,
            this.logMessageDataGridViewTextBoxColumn});
            this._dataGridViewLogEvents.DataSource = this._bindingSourceLogEvent;
            this._dataGridViewLogEvents.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dataGridViewLogEvents.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this._dataGridViewLogEvents.Location = new System.Drawing.Point(5, 30);
            this._dataGridViewLogEvents.MultiSelect = false;
            this._dataGridViewLogEvents.Name = "_dataGridViewLogEvents";
            this._dataGridViewLogEvents.ReadOnly = true;
            this._dataGridViewLogEvents.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this._dataGridViewLogEvents.RowHeadersVisible = false;
            this._dataGridViewLogEvents.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this._dataGridViewLogEvents.RowTemplate.Height = 18;
            this._dataGridViewLogEvents.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this._dataGridViewLogEvents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dataGridViewLogEvents.Size = new System.Drawing.Size(534, 209);
            this._dataGridViewLogEvents.TabIndex = 0;
            this._dataGridViewLogEvents.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
            this._dataGridViewLogEvents.CurrentCellChanged += new System.EventHandler(this._dataGridView_CurrentCellChanged);
            this._dataGridViewLogEvents.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(_dataGridView_DataError);
            this._dataGridViewLogEvents.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
            // 
            // logLevelImageDataGridViewImageColumn
            // 
            this.logLevelImageDataGridViewImageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.logLevelImageDataGridViewImageColumn.DataPropertyName = "LogLevelImage";
            this.logLevelImageDataGridViewImageColumn.FillWeight = 1F;
            this.logLevelImageDataGridViewImageColumn.HeaderText = "";
            this.logLevelImageDataGridViewImageColumn.MinimumWidth = 19;
            this.logLevelImageDataGridViewImageColumn.Name = "logLevelImageDataGridViewImageColumn";
            this.logLevelImageDataGridViewImageColumn.ReadOnly = true;
            this.logLevelImageDataGridViewImageColumn.Width = 19;
            // 
            // LogNummer
            // 
            this.LogNummer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LogNummer.DataPropertyName = "LogNummer";
            this.LogNummer.FillWeight = 1F;
            this.LogNummer.HeaderText = "#";
            this.LogNummer.MinimumWidth = 30;
            this.LogNummer.Name = "LogNummer";
            this.LogNummer.ReadOnly = true;
            this.LogNummer.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.LogNummer.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.LogNummer.Visible = false;
            this.LogNummer.Width = 30;
            // 
            // logDateTimeDataGridViewTextBoxColumn
            // 
            this.logDateTimeDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.logDateTimeDataGridViewTextBoxColumn.DataPropertyName = "LogDateTime";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.Format = "T";
            dataGridViewCellStyle1.NullValue = null;
            this.logDateTimeDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle1;
            this.logDateTimeDataGridViewTextBoxColumn.FillWeight = 1F;
            this.logDateTimeDataGridViewTextBoxColumn.HeaderText = "Date";
            this.logDateTimeDataGridViewTextBoxColumn.MinimumWidth = 60;
            this.logDateTimeDataGridViewTextBoxColumn.Name = "logDateTimeDataGridViewTextBoxColumn";
            this.logDateTimeDataGridViewTextBoxColumn.ReadOnly = true;
            this.logDateTimeDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.logDateTimeDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.logDateTimeDataGridViewTextBoxColumn.Width = 60;
            // 
            // logMessageDataGridViewTextBoxColumn
            // 
            this.logMessageDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.logMessageDataGridViewTextBoxColumn.DataPropertyName = "LogMessage";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.logMessageDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.logMessageDataGridViewTextBoxColumn.HeaderText = "Message";
            this.logMessageDataGridViewTextBoxColumn.MinimumWidth = 200;
            this.logMessageDataGridViewTextBoxColumn.Name = "logMessageDataGridViewTextBoxColumn";
            this.logMessageDataGridViewTextBoxColumn.ReadOnly = true;
            this.logMessageDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // _bindingSourceLogEvent
            // 
            this._bindingSourceLogEvent.DataSource = typeof(LogEventItem);
            // 
            // _groupBoxDetails
            // 
            this._groupBoxDetails.Controls.Add(this.textBoxDetails);
            this._groupBoxDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this._groupBoxDetails.Location = new System.Drawing.Point(5, 5);
            this._groupBoxDetails.Name = "_groupBoxDetails";
            this._groupBoxDetails.Padding = new System.Windows.Forms.Padding(8);
            this._groupBoxDetails.Size = new System.Drawing.Size(534, 146);
            this._groupBoxDetails.TabIndex = 1;
            this._groupBoxDetails.TabStop = false;
            this._groupBoxDetails.Text = "Details";
            // 
            // textBoxDetails
            // 
            this.textBoxDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDetails.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDetails.Location = new System.Drawing.Point(8, 21);
            this.textBoxDetails.Multiline = true;
            this.textBoxDetails.Name = "textBoxDetails";
            this.textBoxDetails.ReadOnly = true;
            this.textBoxDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxDetails.Size = new System.Drawing.Size(518, 117);
            this.textBoxDetails.TabIndex = 1;
            // 
            // _buttonCopy
            // 
            this._buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCopy.Location = new System.Drawing.Point(378, 410);
            this._buttonCopy.Name = "_buttonCopy";
            this._buttonCopy.Size = new System.Drawing.Size(75, 23);
            this._buttonCopy.TabIndex = 2;
            this._buttonCopy.Text = "Copy";
            this._buttonCopy.UseVisualStyleBackColor = true;
            this._buttonCopy.Click += new System.EventHandler(this.buttonCopy_Click);
            // 
            // _buttonClose
            // 
            this._buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonClose.Location = new System.Drawing.Point(459, 410);
            this._buttonClose.Name = "_buttonClose";
            this._buttonClose.Size = new System.Drawing.Size(75, 23);
            this._buttonClose.TabIndex = 2;
            this._buttonClose.Text = "Close";
            this._buttonClose.UseVisualStyleBackColor = true;
            this._buttonClose.Click += new System.EventHandler(this._buttonClose_Click);
            // 
            // _statusStrip
            // 
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
            this._statusStrip.Location = new System.Drawing.Point(0, 436);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(544, 22);
            this._statusStrip.TabIndex = 3;
            this._statusStrip.Text = "statusStrip1";
            // 
            // _toolStripStatusLabel
            // 
            this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
            this._toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // _splitContainer
            // 
            this._splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this._splitContainer.Location = new System.Drawing.Point(0, 0);
            this._splitContainer.Name = "_splitContainer";
            this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            this._splitContainer.Panel1.Controls.Add(this._dataGridViewLogEvents);
            this._splitContainer.Panel1.Controls.Add(this._dataGridViewFindToolStrip);
            this._splitContainer.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // _splitContainer.Panel2
            // 
            this._splitContainer.Panel2.Controls.Add(this._groupBoxDetails);
            this._splitContainer.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this._splitContainer.Size = new System.Drawing.Size(544, 404);
            this._splitContainer.SplitterDistance = 244;
            this._splitContainer.TabIndex = 4;
            // 
            // _dataGridViewFindToolStrip
            // 
            this._dataGridViewFindToolStrip.ClickThrough = true;
            this._dataGridViewFindToolStrip.FilterRows = false;
            this._dataGridViewFindToolStrip.FindText = "";
            this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
            this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(5, 5);
            this._dataGridViewFindToolStrip.MatchCase = false;
            this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
            this._dataGridViewFindToolStrip.Observer = null;
            this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(534, 25);
            this._dataGridViewFindToolStrip.TabIndex = 1;
            this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
            // 
            // LogHistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._buttonClose;
            this.ClientSize = new System.Drawing.Size(544, 458);
            this.Controls.Add(this._splitContainer);
            this.Controls.Add(this._buttonCopy);
            this.Controls.Add(this._statusStrip);
            this.Controls.Add(this._buttonClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "LogHistoryForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Log Messages";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.BackupLogEventsView_FormClosed);
            this.Shown += new System.EventHandler(this.BackupLogEventsView_Shown);
            ((System.ComponentModel.ISupportInitialize)(this._dataGridViewLogEvents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._bindingSourceLogEvent)).EndInit();
            this._groupBoxDetails.ResumeLayout(false);
            this._groupBoxDetails.PerformLayout();
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            this._splitContainer.Panel1.ResumeLayout(false);
            this._splitContainer.Panel1.PerformLayout();
            this._splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
            this._splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.GroupBox _groupBoxDetails;
        private System.Windows.Forms.TextBox textBoxDetails;
        private System.Windows.Forms.Button _buttonCopy;
        private System.Windows.Forms.Button _buttonClose;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.BindingSource _bindingSourceLogEvent;
        private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
        private SplitContainerEx _splitContainer;
        private System.Windows.Forms.DataGridViewImageColumn logLevelImageDataGridViewImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogNummer;
        private System.Windows.Forms.DataGridViewTextBoxColumn logDateTimeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn logMessageDataGridViewTextBoxColumn;
		private DoubleBufferedDataGridView _dataGridViewLogEvents;
        private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
    }
}