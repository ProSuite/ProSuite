using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
    partial class InstanceDescriptorControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._labelTestClass = new System.Windows.Forms.Label();
			this._labelConstructorId = new System.Windows.Forms.Label();
			this._labelTestDescription = new System.Windows.Forms.Label();
			this._textBoxTestDescription = new System.Windows.Forms.TextBox();
			this._comboBoxConstructorIndex = new System.Windows.Forms.ComboBox();
			this._objectReferenceControlTestClass = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._labelParameter = new System.Windows.Forms.Label();
			this._dataGridViewParameter = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._groupBoxClassOrFactory = new System.Windows.Forms.GroupBox();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageImplementation = new System.Windows.Forms.TabPage();
			this._splitContainerDescription = new System.Windows.Forms.SplitContainer();
			this._textBoxCategories = new System.Windows.Forms.TextBox();
			this._labelTestCategories = new System.Windows.Forms.Label();
			this._panelImplementationTop = new System.Windows.Forms.Panel();
			this._tabPageQualityConditions = new System.Windows.Forms.TabPage();
			this._dataGridViewQualityConditions = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnQualityConditionImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnStopOnError = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnQualityConditionDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewFindToolStrip = new ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this._toolStripElements = new ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this._panelTop = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParameter)).BeginInit();
			this._groupBoxClassOrFactory.SuspendLayout();
			this._tabControl.SuspendLayout();
			this._tabPageImplementation.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDescription)).BeginInit();
			this._splitContainerDescription.Panel1.SuspendLayout();
			this._splitContainerDescription.Panel2.SuspendLayout();
			this._splitContainerDescription.SuspendLayout();
			this._panelImplementationTop.SuspendLayout();
			this._tabPageQualityConditions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualityConditions)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this._panelTop.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(51, 49);
			this._labelDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(70, 15);
			this._labelDescription.TabIndex = 2;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(127, 44);
			this._textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(620, 48);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(80, 17);
			this._labelName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(42, 15);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(127, 14);
			this._textBoxName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(620, 23);
			this._textBoxName.TabIndex = 0;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _labelTestClass
			// 
			this._labelTestClass.AutoSize = true;
			this._labelTestClass.Location = new System.Drawing.Point(58, 24);
			this._labelTestClass.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelTestClass.Name = "_labelTestClass";
			this._labelTestClass.Size = new System.Drawing.Size(37, 15);
			this._labelTestClass.TabIndex = 0;
			this._labelTestClass.Text = "Class:";
			this._labelTestClass.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelConstructorId
			// 
			this._labelConstructorId.AutoSize = true;
			this._labelConstructorId.Location = new System.Drawing.Point(24, 54);
			this._labelConstructorId.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelConstructorId.Name = "_labelConstructorId";
			this._labelConstructorId.Size = new System.Drawing.Size(73, 15);
			this._labelConstructorId.TabIndex = 2;
			this._labelConstructorId.Text = "Constructor:";
			this._labelConstructorId.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelTestDescription
			// 
			this._labelTestDescription.AutoSize = true;
			this._labelTestDescription.Location = new System.Drawing.Point(7, 36);
			this._labelTestDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelTestDescription.Name = "_labelTestDescription";
			this._labelTestDescription.Size = new System.Drawing.Size(92, 15);
			this._labelTestDescription.TabIndex = 10;
			this._labelTestDescription.Text = "Test description:";
			this._labelTestDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxTestDescription
			// 
			this._textBoxTestDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTestDescription.Location = new System.Drawing.Point(104, 33);
			this._textBoxTestDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxTestDescription.Multiline = true;
			this._textBoxTestDescription.Name = "_textBoxTestDescription";
			this._textBoxTestDescription.ReadOnly = true;
			this._textBoxTestDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxTestDescription.Size = new System.Drawing.Size(630, 87);
			this._textBoxTestDescription.TabIndex = 11;
			this._textBoxTestDescription.TabStop = false;
			// 
			// _comboBoxConstructorIndex
			// 
			this._comboBoxConstructorIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._comboBoxConstructorIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboBoxConstructorIndex.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._comboBoxConstructorIndex.FormattingEnabled = true;
			this._comboBoxConstructorIndex.Location = new System.Drawing.Point(104, 56);
			this._comboBoxConstructorIndex.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._comboBoxConstructorIndex.Name = "_comboBoxConstructorIndex";
			this._comboBoxConstructorIndex.Size = new System.Drawing.Size(614, 22);
			this._comboBoxConstructorIndex.TabIndex = 4;
			// 
			// _objectReferenceControlTestClass
			// 
			this._objectReferenceControlTestClass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlTestClass.DataSource = null;
			this._objectReferenceControlTestClass.DisplayMember = null;
			this._objectReferenceControlTestClass.FindObjectDelegate = null;
			this._objectReferenceControlTestClass.FormatTextDelegate = null;
			this._objectReferenceControlTestClass.Location = new System.Drawing.Point(104, 21);
			this._objectReferenceControlTestClass.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._objectReferenceControlTestClass.Name = "_objectReferenceControlTestClass";
			this._objectReferenceControlTestClass.ReadOnly = false;
			this._objectReferenceControlTestClass.Size = new System.Drawing.Size(614, 23);
			this._objectReferenceControlTestClass.TabIndex = 1;
			// 
			// _labelParameter
			// 
			this._labelParameter.AutoSize = true;
			this._labelParameter.Location = new System.Drawing.Point(7, 10);
			this._labelParameter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelParameter.Name = "_labelParameter";
			this._labelParameter.Size = new System.Drawing.Size(92, 15);
			this._labelParameter.TabIndex = 12;
			this._labelParameter.Text = "Test parameters:";
			this._labelParameter.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _dataGridViewParameter
			// 
			this._dataGridViewParameter.AllowUserToAddRows = false;
			this._dataGridViewParameter.AllowUserToDeleteRows = false;
			this._dataGridViewParameter.AllowUserToResizeRows = false;
			this._dataGridViewParameter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewParameter.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewParameter.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridViewParameter.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewParameter.DefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridViewParameter.Location = new System.Drawing.Point(104, 7);
			this._dataGridViewParameter.Name = "_dataGridViewParameter";
			this._dataGridViewParameter.ReadOnly = true;
			this._dataGridViewParameter.RowHeadersVisible = false;
			this._dataGridViewParameter.RowHeadersWidth = 62;
			this._dataGridViewParameter.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewParameter.Size = new System.Drawing.Size(630, 73);
			this._dataGridViewParameter.ShowCellToolTips = false;
			this._dataGridViewParameter.TabIndex = 13;
			// 
			// _groupBoxClassOrFactory
			// 
			this._groupBoxClassOrFactory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxClassOrFactory.Controls.Add(this._comboBoxConstructorIndex);
			this._groupBoxClassOrFactory.Controls.Add(this._objectReferenceControlTestClass);
			this._groupBoxClassOrFactory.Controls.Add(this._labelTestClass);
			this._groupBoxClassOrFactory.Controls.Add(this._labelConstructorId);
			this._groupBoxClassOrFactory.Location = new System.Drawing.Point(0, -1);
			this._groupBoxClassOrFactory.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._groupBoxClassOrFactory.Name = "_groupBoxClassOrFactory";
			this._groupBoxClassOrFactory.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._groupBoxClassOrFactory.Size = new System.Drawing.Size(734, 89);
			this._groupBoxClassOrFactory.TabIndex = 8;
			this._groupBoxClassOrFactory.TabStop = false;
			this._groupBoxClassOrFactory.Text = "Specify a Class and Constructor Index";
			// 
			// _tabControl
			// 
			this._tabControl.Controls.Add(this._tabPageImplementation);
			this._tabControl.Controls.Add(this._tabPageQualityConditions);
			this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControl.Location = new System.Drawing.Point(0, 103);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(760, 376);
			this._tabControl.TabIndex = 5;
			this._tabControl.SelectedIndexChanged += new System.EventHandler(this._tabControl_SelectedIndexChanged);
			// 
			// _tabPageImplementation
			// 
			this._tabPageImplementation.Controls.Add(this._splitContainerDescription);
			this._tabPageImplementation.Controls.Add(this._panelImplementationTop);
			this._tabPageImplementation.Location = new System.Drawing.Point(4, 24);
			this._tabPageImplementation.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._tabPageImplementation.Name = "_tabPageImplementation";
			this._tabPageImplementation.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._tabPageImplementation.Size = new System.Drawing.Size(752, 348);
			this._tabPageImplementation.TabIndex = 0;
			this._tabPageImplementation.Text = "Implementation";
			this._tabPageImplementation.UseVisualStyleBackColor = true;
			// 
			// _splitContainerDescription
			// 
			this._splitContainerDescription.BackColor = System.Drawing.SystemColors.ControlLight;
			this._splitContainerDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerDescription.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerDescription.Location = new System.Drawing.Point(6, 95);
			this._splitContainerDescription.Name = "_splitContainerDescription";
			this._splitContainerDescription.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerDescription.Panel1
			// 
			this._splitContainerDescription.Panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._splitContainerDescription.Panel1.Controls.Add(this._textBoxCategories);
			this._splitContainerDescription.Panel1.Controls.Add(this._textBoxTestDescription);
			this._splitContainerDescription.Panel1.Controls.Add(this._labelTestCategories);
			this._splitContainerDescription.Panel1.Controls.Add(this._labelTestDescription);
			this._splitContainerDescription.Panel1MinSize = 60;
			// 
			// _splitContainerDescription.Panel2
			// 
			this._splitContainerDescription.Panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._splitContainerDescription.Panel2.Controls.Add(this._dataGridViewParameter);
			this._splitContainerDescription.Panel2.Controls.Add(this._labelParameter);
			this._splitContainerDescription.Panel2MinSize = 50;
			this._splitContainerDescription.Size = new System.Drawing.Size(740, 246);
			this._splitContainerDescription.SplitterDistance = 130;
			this._splitContainerDescription.SplitterWidth = 5;
			this._splitContainerDescription.TabIndex = 14;
			this._splitContainerDescription.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this._splitContainerDescription_SplitterMoved);
			// 
			// _textBoxCategories
			// 
			this._textBoxCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategories.Location = new System.Drawing.Point(104, 7);
			this._textBoxCategories.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._textBoxCategories.Name = "_textBoxCategories";
			this._textBoxCategories.ReadOnly = true;
			this._textBoxCategories.Size = new System.Drawing.Size(630, 23);
			this._textBoxCategories.TabIndex = 11;
			this._textBoxCategories.TabStop = false;
			// 
			// _labelTestCategories
			// 
			this._labelTestCategories.AutoSize = true;
			this._labelTestCategories.Location = new System.Drawing.Point(33, 10);
			this._labelTestCategories.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelTestCategories.Name = "_labelTestCategories";
			this._labelTestCategories.Size = new System.Drawing.Size(66, 15);
			this._labelTestCategories.TabIndex = 10;
			this._labelTestCategories.Text = "Categories:";
			this._labelTestCategories.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _panelImplementationTop
			// 
			this._panelImplementationTop.Controls.Add(this._groupBoxClassOrFactory);
			this._panelImplementationTop.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelImplementationTop.Location = new System.Drawing.Point(6, 7);
			this._panelImplementationTop.Margin = new System.Windows.Forms.Padding(0);
			this._panelImplementationTop.Name = "_panelImplementationTop";
			this._panelImplementationTop.Size = new System.Drawing.Size(740, 88);
			this._panelImplementationTop.TabIndex = 15;
			// 
			// _tabPageQualityConditions
			// 
			this._tabPageQualityConditions.Controls.Add(this._dataGridViewQualityConditions);
			this._tabPageQualityConditions.Controls.Add(this._dataGridViewFindToolStrip);
			this._tabPageQualityConditions.Controls.Add(this._toolStripElements);
			this._tabPageQualityConditions.Location = new System.Drawing.Point(4, 24);
			this._tabPageQualityConditions.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._tabPageQualityConditions.Name = "_tabPageQualityConditions";
			this._tabPageQualityConditions.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._tabPageQualityConditions.Size = new System.Drawing.Size(752, 324);
			this._tabPageQualityConditions.TabIndex = 1;
			this._tabPageQualityConditions.Text = "Configurations";
			this._tabPageQualityConditions.UseVisualStyleBackColor = true;
			// 
			// _dataGridViewQualityConditions
			// 
			this._dataGridViewQualityConditions.AllowUserToAddRows = false;
			this._dataGridViewQualityConditions.AllowUserToDeleteRows = false;
			this._dataGridViewQualityConditions.AllowUserToResizeRows = false;
			this._dataGridViewQualityConditions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewQualityConditions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnQualityConditionImage,
            this._columnName,
            this._columnIssueType,
            this._columnStopOnError,
            this._columnQualityConditionDescription});
			this._dataGridViewQualityConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewQualityConditions.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridViewQualityConditions.Location = new System.Drawing.Point(6, 61);
			this._dataGridViewQualityConditions.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this._dataGridViewQualityConditions.Name = "_dataGridViewQualityConditions";
			this._dataGridViewQualityConditions.RowHeadersVisible = false;
			this._dataGridViewQualityConditions.RowHeadersWidth = 62;
			this._dataGridViewQualityConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewQualityConditions.Size = new System.Drawing.Size(740, 256);
			this._dataGridViewQualityConditions.TabIndex = 26;
			this._dataGridViewQualityConditions.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualityConditions_CellDoubleClick);
			this._dataGridViewQualityConditions.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualityConditions_CellEndEdit);
			this._dataGridViewQualityConditions.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualityConditions_CellValueChanged);
			// 
			// _columnQualityConditionImage
			// 
			this._columnQualityConditionImage.DataPropertyName = "Image";
			this._columnQualityConditionImage.HeaderText = "";
			this._columnQualityConditionImage.MinimumWidth = 20;
			this._columnQualityConditionImage.Name = "_columnQualityConditionImage";
			this._columnQualityConditionImage.Width = 20;
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnName.DataPropertyName = "Name";
			this._columnName.HeaderText = "Name";
			this._columnName.MinimumWidth = 200;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			this._columnName.Width = 200;
			// 
			// _columnIssueType
			// 
			this._columnIssueType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnIssueType.DataPropertyName = "AllowErrorsOverride";
			this._columnIssueType.DisplayStyleForCurrentCellOnly = true;
			this._columnIssueType.HeaderText = "Issue Type";
			this._columnIssueType.MinimumWidth = 80;
			this._columnIssueType.Name = "_columnIssueType";
			this._columnIssueType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnIssueType.Width = 80;
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
			this._columnStopOnError.Width = 95;
			// 
			// _columnQualityConditionDescription
			// 
			this._columnQualityConditionDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnQualityConditionDescription.DataPropertyName = "Description";
			this._columnQualityConditionDescription.HeaderText = "Description";
			this._columnQualityConditionDescription.MinimumWidth = 8;
			this._columnQualityConditionDescription.Name = "_columnQualityConditionDescription";
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 288;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(6, 36);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(740, 25);
			this._dataGridViewFindToolStrip.TabIndex = 28;
			this._dataGridViewFindToolStrip.Text = "_dataGridViewFindToolStrip";
			// 
			// _toolStripElements
			// 
			this._toolStripElements.AutoSize = false;
			this._toolStripElements.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.ImageScalingSize = new System.Drawing.Size(24, 24);
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
			this._toolStripElements.Location = new System.Drawing.Point(6, 7);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this._toolStripElements.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripElements.Size = new System.Drawing.Size(740, 29);
			this._toolStripElements.TabIndex = 27;
			this._toolStripElements.Text = "Element Tools";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(228, 26);
			this.toolStripLabel1.Text = "Configurations using this implementation";
			// 
			// _panelTop
			// 
			this._panelTop.Controls.Add(this._labelName);
			this._panelTop.Controls.Add(this._textBoxName);
			this._panelTop.Controls.Add(this._labelDescription);
			this._panelTop.Controls.Add(this._textBoxDescription);
			this._panelTop.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelTop.Location = new System.Drawing.Point(0, 0);
			this._panelTop.Margin = new System.Windows.Forms.Padding(2);
			this._panelTop.Name = "_panelTop";
			this._panelTop.Size = new System.Drawing.Size(760, 103);
			this._panelTop.TabIndex = 6;
			// 
			// InstanceDescriptorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._tabControl);
			this.Controls.Add(this._panelTop);
			this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
			this.Name = "InstanceDescriptorControl";
			this.Size = new System.Drawing.Size(760, 455);
			this.Load += new System.EventHandler(this.TestDescriptorControl_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.TestDescriptorControl_Paint);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParameter)).EndInit();
			this._groupBoxClassOrFactory.ResumeLayout(false);
			this._groupBoxClassOrFactory.PerformLayout();
			this._tabControl.ResumeLayout(false);
			this._tabPageImplementation.ResumeLayout(false);
			this._splitContainerDescription.Panel1.ResumeLayout(false);
			this._splitContainerDescription.Panel1.PerformLayout();
			this._splitContainerDescription.Panel2.ResumeLayout(false);
			this._splitContainerDescription.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDescription)).EndInit();
			this._splitContainerDescription.ResumeLayout(false);
			this._panelImplementationTop.ResumeLayout(false);
			this._tabPageQualityConditions.ResumeLayout(false);
			this._tabPageQualityConditions.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualityConditions)).EndInit();
			this._toolStripElements.ResumeLayout(false);
			this._toolStripElements.PerformLayout();
			this._panelTop.ResumeLayout(false);
			this._panelTop.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.ErrorProvider _errorProvider;
        private System.Windows.Forms.Label _labelTestClass;
        private System.Windows.Forms.Label _labelTestDescription;
        private System.Windows.Forms.TextBox _textBoxTestDescription;
		private System.Windows.Forms.Label _labelConstructorId;
        private ObjectReferenceControl _objectReferenceControlTestClass;
		private System.Windows.Forms.Label _labelParameter;
        private System.Windows.Forms.GroupBox _groupBoxClassOrFactory;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabPageImplementation;
		private System.Windows.Forms.TabPage _tabPageQualityConditions;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ComboBox _comboBoxConstructorIndex;
		private DoubleBufferedDataGridView _dataGridViewParameter;
		private DoubleBufferedDataGridView _dataGridViewQualityConditions;
		private ToolStripEx _toolStripElements;
		private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.SplitContainer _splitContainerDescription;
		private System.Windows.Forms.TextBox _textBoxCategories;
		private System.Windows.Forms.Label _labelTestCategories;
		private System.Windows.Forms.DataGridViewImageColumn _columnQualityConditionImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnIssueType;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnStopOnError;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnQualityConditionDescription;
		private System.Windows.Forms.Panel _panelImplementationTop;
		private System.Windows.Forms.Panel _panelTop;
	}
}
