using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
    partial class QualitySpecificationControl
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
			this.components = new System.ComponentModel.Container();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._numericUpDownListOrder = new System.Windows.Forms.NumericUpDown();
			this._labelListOrder = new System.Windows.Forms.Label();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._numericUpDownTileSize = new global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this._checkBoxHidden = new System.Windows.Forms.CheckBox();
			this._textBoxUuid = new System.Windows.Forms.TextBox();
			this._labelUuid = new System.Windows.Forms.Label();
			this._labelCategory = new System.Windows.Forms.Label();
			this._textBoxCategory = new System.Windows.Forms.TextBox();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this._tabControlDetails = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this._dataGridView = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnTestTypeImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTest = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnAllowErrorsDefault = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnStopOnError = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnStopOnErrorDefault = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this._toolStripButtonAssignToCategory = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonRemoveQualityConditions = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonAssignQualityConditions = new System.Windows.Forms.ToolStripButton();
			this._toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this._tabPageNotes = new System.Windows.Forms.TabPage();
			this._textBoxNotes = new System.Windows.Forms.TextBox();
			this._labelUrl = new System.Windows.Forms.Label();
			this._textBoxUrl = new System.Windows.Forms.TextBox();
			this._buttonOpenUrl = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownListOrder)).BeginInit();
			this._tabControlDetails.SuspendLayout();
			this.tabPage1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this._dataGridViewFindToolStrip.SuspendLayout();
			this._tabPageNotes.SuspendLayout();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(107, 13);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(538, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(63, 16);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 1;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(107, 40);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(724, 55);
			this._textBoxDescription.TabIndex = 2;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(38, 43);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 3;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _numericUpDownListOrder
			// 
			this._numericUpDownListOrder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._numericUpDownListOrder.Location = new System.Drawing.Point(769, 14);
			this._numericUpDownListOrder.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this._numericUpDownListOrder.Name = "_numericUpDownListOrder";
			this._numericUpDownListOrder.Size = new System.Drawing.Size(62, 20);
			this._numericUpDownListOrder.TabIndex = 1;
			// 
			// _labelListOrder
			// 
			this._labelListOrder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelListOrder.AutoSize = true;
			this._labelListOrder.Location = new System.Drawing.Point(671, 16);
			this._labelListOrder.Name = "_labelListOrder";
			this._labelListOrder.Size = new System.Drawing.Size(92, 13);
			this._labelListOrder.TabIndex = 25;
			this._labelListOrder.Text = "Display List Order:";
			this._labelListOrder.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			// 
			// dataGridViewTextBoxColumn4
			// 
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			// 
			// dataGridViewTextBoxColumn5
			// 
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			// 
			// _numericUpDownTileSize
			// 
			this._numericUpDownTileSize.DecimalPlaces = 3;
			this._numericUpDownTileSize.Increment = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this._numericUpDownTileSize.Location = new System.Drawing.Point(107, 153);
			this._numericUpDownTileSize.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this._numericUpDownTileSize.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
			this._numericUpDownTileSize.Name = "_numericUpDownTileSize";
			this._numericUpDownTileSize.Size = new System.Drawing.Size(166, 20);
			this._numericUpDownTileSize.TabIndex = 7;
			this._numericUpDownTileSize.ThousandsSeparator = true;
			this._numericUpDownTileSize.Value = null;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(53, 156);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Tile size:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.AutoEllipsis = true;
			this.label2.Location = new System.Drawing.Point(273, 157);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(492, 17);
			this.label2.TabIndex = 3;
			this.label2.Text = "(optional override to project tile size)";
			// 
			// _checkBoxHidden
			// 
			this._checkBoxHidden.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._checkBoxHidden.AutoSize = true;
			this._checkBoxHidden.Location = new System.Drawing.Point(771, 156);
			this._checkBoxHidden.Name = "_checkBoxHidden";
			this._checkBoxHidden.Size = new System.Drawing.Size(60, 17);
			this._checkBoxHidden.TabIndex = 8;
			this._checkBoxHidden.Text = "Hidden";
			this._checkBoxHidden.UseVisualStyleBackColor = true;
			// 
			// _textBoxUuid
			// 
			this._textBoxUuid.Location = new System.Drawing.Point(107, 127);
			this._textBoxUuid.Name = "_textBoxUuid";
			this._textBoxUuid.ReadOnly = true;
			this._textBoxUuid.Size = new System.Drawing.Size(240, 20);
			this._textBoxUuid.TabIndex = 5;
			// 
			// _labelUuid
			// 
			this._labelUuid.AutoSize = true;
			this._labelUuid.Location = new System.Drawing.Point(64, 130);
			this._labelUuid.Name = "_labelUuid";
			this._labelUuid.Size = new System.Drawing.Size(37, 13);
			this._labelUuid.TabIndex = 1;
			this._labelUuid.Text = "UUID:";
			this._labelUuid.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelCategory
			// 
			this._labelCategory.AutoSize = true;
			this._labelCategory.Location = new System.Drawing.Point(353, 130);
			this._labelCategory.Name = "_labelCategory";
			this._labelCategory.Size = new System.Drawing.Size(52, 13);
			this._labelCategory.TabIndex = 27;
			this._labelCategory.Text = "Category:";
			this._labelCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxCategory
			// 
			this._textBoxCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategory.Location = new System.Drawing.Point(411, 127);
			this._textBoxCategory.Name = "_textBoxCategory";
			this._textBoxCategory.ReadOnly = true;
			this._textBoxCategory.Size = new System.Drawing.Size(420, 20);
			this._textBoxCategory.TabIndex = 6;
			// 
			// _toolTip
			// 
			this._toolTip.AutomaticDelay = 100;
			this._toolTip.AutoPopDelay = 5000;
			this._toolTip.InitialDelay = 100;
			this._toolTip.IsBalloon = true;
			this._toolTip.ReshowDelay = 20;
			this._toolTip.ShowAlways = true;
			// 
			// _tabControlDetails
			// 
			this._tabControlDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControlDetails.Controls.Add(this.tabPage1);
			this._tabControlDetails.Controls.Add(this._tabPageNotes);
			this._tabControlDetails.Location = new System.Drawing.Point(3, 179);
			this._tabControlDetails.Name = "_tabControlDetails";
			this._tabControlDetails.SelectedIndex = 0;
			this._tabControlDetails.Size = new System.Drawing.Size(843, 375);
			this._tabControlDetails.TabIndex = 9;
			this._tabControlDetails.SelectedIndexChanged += new System.EventHandler(this._tabControlDetails_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this._dataGridView);
			this.tabPage1.Controls.Add(this._dataGridViewFindToolStrip);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(835, 349);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Quality Conditions";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnTestTypeImage,
            this._columnName,
            this._columnCategory,
            this._columnTest,
            this._columnIssueType,
            this._columnAllowErrorsDefault,
            this._columnStopOnError,
            this._columnStopOnErrorDefault,
            this._columnUrl,
            this._columnDescription});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridView.Location = new System.Drawing.Point(3, 30);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(829, 316);
			this._dataGridView.TabIndex = 11;
			this._dataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellDoubleClick);
			this._dataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellEndEdit);
			this._dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
			this._dataGridView.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this._dataGridView_CellToolTipTextNeeded);
			this._dataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellValueChanged);
			// 
			// _columnTestTypeImage
			// 
			this._columnTestTypeImage.DataPropertyName = "TestTypeImage";
			this._columnTestTypeImage.HeaderText = "";
			this._columnTestTypeImage.MinimumWidth = 24;
			this._columnTestTypeImage.Name = "_columnTestTypeImage";
			this._columnTestTypeImage.ReadOnly = true;
			this._columnTestTypeImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnTestTypeImage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnTestTypeImage.Width = 24;
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnName.DataPropertyName = "QualityConditionName";
			this._columnName.HeaderText = "Name";
			this._columnName.MinimumWidth = 100;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			this._columnName.Width = 400;
			// 
			// _columnCategory
			// 
			this._columnCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnCategory.DataPropertyName = "Category";
			this._columnCategory.HeaderText = "Category";
			this._columnCategory.MinimumWidth = 150;
			this._columnCategory.Name = "_columnCategory";
			this._columnCategory.ReadOnly = true;
			this._columnCategory.Width = 150;
			// 
			// _columnTest
			// 
			this._columnTest.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTest.DataPropertyName = "Test";
			this._columnTest.HeaderText = "Test";
			this._columnTest.MinimumWidth = 80;
			this._columnTest.Name = "_columnTest";
			this._columnTest.ReadOnly = true;
			this._columnTest.Width = 80;
			// 
			// _columnIssueType
			// 
			this._columnIssueType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnIssueType.DataPropertyName = "AllowErrorsOverride";
			this._columnIssueType.DisplayStyleForCurrentCellOnly = true;
			this._columnIssueType.HeaderText = "Issue Type";
			this._columnIssueType.Items.AddRange(new object[] {
            "Warning",
            "Error",
            "Default"});
			this._columnIssueType.MinimumWidth = 80;
			this._columnIssueType.Name = "_columnIssueType";
			this._columnIssueType.Width = 80;
			// 
			// _columnAllowErrorsDefault
			// 
			this._columnAllowErrorsDefault.DataPropertyName = "AllowErrors";
			this._columnAllowErrorsDefault.HeaderText = "Issue Type (Condition Default)";
			this._columnAllowErrorsDefault.MinimumWidth = 120;
			this._columnAllowErrorsDefault.Name = "_columnAllowErrorsDefault";
			this._columnAllowErrorsDefault.ReadOnly = true;
			this._columnAllowErrorsDefault.Width = 120;
			// 
			// _columnStopOnError
			// 
			this._columnStopOnError.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnStopOnError.DataPropertyName = "StopOnErrorOverride";
			this._columnStopOnError.DisplayStyleForCurrentCellOnly = true;
			this._columnStopOnError.HeaderText = "Stop On Error";
			this._columnStopOnError.MinimumWidth = 80;
			this._columnStopOnError.Name = "_columnStopOnError";
			this._columnStopOnError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnStopOnError.Width = 88;
			// 
			// _columnStopOnErrorDefault
			// 
			this._columnStopOnErrorDefault.DataPropertyName = "StopOnError";
			this._columnStopOnErrorDefault.HeaderText = "Stop On Error (Condition Default)";
			this._columnStopOnErrorDefault.MinimumWidth = 120;
			this._columnStopOnErrorDefault.Name = "_columnStopOnErrorDefault";
			this._columnStopOnErrorDefault.ReadOnly = true;
			this._columnStopOnErrorDefault.Width = 120;
			// 
			// _columnUrl
			// 
			this._columnUrl.DataPropertyName = "Url";
			this._columnUrl.HeaderText = "Url";
			this._columnUrl.Name = "_columnUrl";
			this._columnUrl.Width = 80;
			// 
			// _columnDescription
			// 
			this._columnDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDescription.DataPropertyName = "QualityConditionDescription";
			this._columnDescription.HeaderText = "Condition Description";
			this._columnDescription.MinimumWidth = 50;
			this._columnDescription.Name = "_columnDescription";
			this._columnDescription.ReadOnly = true;
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.AutoSize = false;
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonAssignToCategory,
            this._toolStripButtonRemoveQualityConditions,
            this._toolStripButtonAssignQualityConditions,
            this._toolStripSeparator});
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(3, 3);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(829, 27);
			this._dataGridViewFindToolStrip.TabIndex = 10;
			this._dataGridViewFindToolStrip.Text = "Find";
			// 
			// _toolStripButtonAssignToCategory
			// 
			this._toolStripButtonAssignToCategory.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAssignToCategory.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.AssignToDataQualityCategory;
			this._toolStripButtonAssignToCategory.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonAssignToCategory.Name = "_toolStripButtonAssignToCategory";
			this._toolStripButtonAssignToCategory.Size = new System.Drawing.Size(71, 24);
			this._toolStripButtonAssignToCategory.Text = "Assign...";
			this._toolStripButtonAssignToCategory.ToolTipText = "Assign to Category...";
			this._toolStripButtonAssignToCategory.Click += new System.EventHandler(this._toolStripButtonAssignToCategory_Click);
			// 
			// _toolStripButtonRemoveQualityConditions
			// 
			this._toolStripButtonRemoveQualityConditions.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemoveQualityConditions.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemoveQualityConditions.Name = "_toolStripButtonRemoveQualityConditions";
			this._toolStripButtonRemoveQualityConditions.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this._toolStripButtonRemoveQualityConditions.Size = new System.Drawing.Size(70, 24);
			this._toolStripButtonRemoveQualityConditions.Text = "Remove";
			this._toolStripButtonRemoveQualityConditions.Click += new System.EventHandler(this._toolStripButtonRemoveQualityConditions_Click);
			// 
			// _toolStripButtonAssignQualityConditions
			// 
			this._toolStripButtonAssignQualityConditions.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAssignQualityConditions.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._toolStripButtonAssignQualityConditions.Name = "_toolStripButtonAssignQualityConditions";
			this._toolStripButtonAssignQualityConditions.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this._toolStripButtonAssignQualityConditions.Size = new System.Drawing.Size(58, 24);
			this._toolStripButtonAssignQualityConditions.Text = "Add...";
			this._toolStripButtonAssignQualityConditions.Click += new System.EventHandler(this._toolStripButtonAssignQualityConditions_Click);
			// 
			// _toolStripSeparator
			// 
			this._toolStripSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripSeparator.Name = "_toolStripSeparator";
			this._toolStripSeparator.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this._toolStripSeparator.Size = new System.Drawing.Size(6, 27);
			// 
			// _tabPageNotes
			// 
			this._tabPageNotes.Controls.Add(this._textBoxNotes);
			this._tabPageNotes.Location = new System.Drawing.Point(4, 22);
			this._tabPageNotes.Name = "_tabPageNotes";
			this._tabPageNotes.Padding = new System.Windows.Forms.Padding(6);
			this._tabPageNotes.Size = new System.Drawing.Size(835, 349);
			this._tabPageNotes.TabIndex = 1;
			this._tabPageNotes.Text = "Notes";
			this._tabPageNotes.UseVisualStyleBackColor = true;
			// 
			// _textBoxNotes
			// 
			this._textBoxNotes.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxNotes.Location = new System.Drawing.Point(6, 6);
			this._textBoxNotes.Multiline = true;
			this._textBoxNotes.Name = "_textBoxNotes";
			this._textBoxNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxNotes.Size = new System.Drawing.Size(823, 337);
			this._textBoxNotes.TabIndex = 1;
			// 
			// _labelUrl
			// 
			this._labelUrl.AutoSize = true;
			this._labelUrl.Location = new System.Drawing.Point(69, 104);
			this._labelUrl.Name = "_labelUrl";
			this._labelUrl.Size = new System.Drawing.Size(32, 13);
			this._labelUrl.TabIndex = 38;
			this._labelUrl.Text = "URL:";
			this._labelUrl.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUrl
			// 
			this._textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxUrl.Location = new System.Drawing.Point(107, 101);
			this._textBoxUrl.Name = "_textBoxUrl";
			this._textBoxUrl.Size = new System.Drawing.Size(700, 20);
			this._textBoxUrl.TabIndex = 3;
			// 
			// _buttonOpenUrl
			// 
			this._buttonOpenUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOpenUrl.FlatAppearance.BorderSize = 0;
			this._buttonOpenUrl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this._buttonOpenUrl.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.OpenUrl;
			this._buttonOpenUrl.Location = new System.Drawing.Point(809, 97);
			this._buttonOpenUrl.Name = "_buttonOpenUrl";
			this._buttonOpenUrl.Size = new System.Drawing.Size(26, 26);
			this._buttonOpenUrl.TabIndex = 4;
			this._buttonOpenUrl.UseVisualStyleBackColor = true;
			this._buttonOpenUrl.Click += new System.EventHandler(this._buttonOpenUrl_Click);
			// 
			// QualitySpecificationControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._buttonOpenUrl);
			this.Controls.Add(this._labelUrl);
			this.Controls.Add(this._textBoxUrl);
			this.Controls.Add(this._tabControlDetails);
			this.Controls.Add(this._labelCategory);
			this.Controls.Add(this._textBoxCategory);
			this.Controls.Add(this._checkBoxHidden);
			this.Controls.Add(this._numericUpDownTileSize);
			this.Controls.Add(this._labelListOrder);
			this.Controls.Add(this._numericUpDownListOrder);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelUuid);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxUuid);
			this.Controls.Add(this._textBoxName);
			this.Name = "QualitySpecificationControl";
			this.Size = new System.Drawing.Size(849, 557);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.QualitySpecificationControl_Paint);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownListOrder)).EndInit();
			this._tabControlDetails.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this._dataGridViewFindToolStrip.ResumeLayout(false);
			this._dataGridViewFindToolStrip.PerformLayout();
			this._tabPageNotes.ResumeLayout(false);
			this._tabPageNotes.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.ErrorProvider _errorProvider;
        private System.Windows.Forms.Label _labelListOrder;
        private System.Windows.Forms.NumericUpDown _numericUpDownListOrder;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private NumericUpDownNullable _numericUpDownTileSize;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox _checkBoxHidden;
		private System.Windows.Forms.Label _labelUuid;
		private System.Windows.Forms.TextBox _textBoxUuid;
		private System.Windows.Forms.Label _labelCategory;
		private System.Windows.Forms.TextBox _textBoxCategory;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.TabControl _tabControlDetails;
		private System.Windows.Forms.TabPage tabPage1;
		private DoubleBufferedDataGridView _dataGridView;
		private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveQualityConditions;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAssignQualityConditions;
		private System.Windows.Forms.TabPage _tabPageNotes;
		private System.Windows.Forms.TextBox _textBoxNotes;
		private System.Windows.Forms.ToolStripSeparator _toolStripSeparator;
		private System.Windows.Forms.Button _buttonOpenUrl;
		private System.Windows.Forms.Label _labelUrl;
		private System.Windows.Forms.TextBox _textBoxUrl;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAssignToCategory;
		private System.Windows.Forms.DataGridViewImageColumn _columnTestTypeImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTest;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnIssueType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnAllowErrorsDefault;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnStopOnError;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnStopOnErrorDefault;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnUrl;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDescription;
	}
}
