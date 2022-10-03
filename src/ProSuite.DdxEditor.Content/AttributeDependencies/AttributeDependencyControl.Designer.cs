using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
    partial class AttributeDependencyControl
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
			this._objectReferenceControlDataset = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageAttributes = new System.Windows.Forms.TabPage();
			this._splitContainerAttributes = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._labelAvailableAttr = new System.Windows.Forms.Label();
			this._dataGridViewAvailable = new System.Windows.Forms.DataGridView();
			this._dataGridViewAvailableImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this._dataGridViewAvailableTextBoxColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewAvailableTextBoxColumnFieldType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._splitContainerSourceTarget = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._dataGridViewSource = new System.Windows.Forms.DataGridView();
			this._dataGridViewSourceImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this._dataGridViewSourceTextBoxColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewSourceTextBoxColumnFieldType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._labelSource = new System.Windows.Forms.Label();
			this._buttonRemoveFromSource = new System.Windows.Forms.Button();
			this._buttonAddToSource = new System.Windows.Forms.Button();
			this._dataGridViewTarget = new System.Windows.Forms.DataGridView();
			this._dataGridViewTargetImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this._dataGridViewTargetTextBoxColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewTargetTextBoxColumnFieldType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._labelTarget = new System.Windows.Forms.Label();
			this._buttonRemoveFromTarget = new System.Windows.Forms.Button();
			this._buttonAddToTarget = new System.Windows.Forms.Button();
			this._tabPageMappings = new System.Windows.Forms.TabPage();
			this._labelRightClickInfo = new System.Windows.Forms.Label();
			this._buttonExportMappings = new System.Windows.Forms.Button();
			this._buttonImportMappings = new System.Windows.Forms.Button();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this._dataGridViewMappings = new global::ProSuite.Commons.UI.WinForms.Controls.FilterableDataGridView();
			this._dataGridViewMappingsTextBoxColumnDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._labelDataset = new System.Windows.Forms.Label();
			this._tabControl.SuspendLayout();
			this._tabPageAttributes.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerAttributes)).BeginInit();
			this._splitContainerAttributes.Panel1.SuspendLayout();
			this._splitContainerAttributes.Panel2.SuspendLayout();
			this._splitContainerAttributes.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewAvailable)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerSourceTarget)).BeginInit();
			this._splitContainerSourceTarget.Panel1.SuspendLayout();
			this._splitContainerSourceTarget.Panel2.SuspendLayout();
			this._splitContainerSourceTarget.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewSource)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewTarget)).BeginInit();
			this._tabPageMappings.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewMappings)).BeginInit();
			this.SuspendLayout();
			// 
			// _objectReferenceControlDataset
			// 
			this._objectReferenceControlDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlDataset.DataSource = null;
			this._objectReferenceControlDataset.DisplayMember = null;
			this._objectReferenceControlDataset.FindObjectDelegate = null;
			this._objectReferenceControlDataset.FormatTextDelegate = null;
			this._objectReferenceControlDataset.Location = new System.Drawing.Point(85, 12);
			this._objectReferenceControlDataset.Name = "_objectReferenceControlDataset";
			this._objectReferenceControlDataset.ReadOnly = false;
			this._objectReferenceControlDataset.Size = new System.Drawing.Size(546, 20);
			this._objectReferenceControlDataset.TabIndex = 13;
			// 
			// _tabControl
			// 
			this._tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControl.Controls.Add(this._tabPageAttributes);
			this._tabControl.Controls.Add(this._tabPageMappings);
			this._tabControl.Location = new System.Drawing.Point(17, 38);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(614, 493);
			this._tabControl.TabIndex = 15;
			this._tabControl.SelectedIndexChanged += new System.EventHandler(this._tabControl_SelectedIndexChanged);
			// 
			// _tabPageAttributes
			// 
			this._tabPageAttributes.Controls.Add(this._splitContainerAttributes);
			this._tabPageAttributes.Location = new System.Drawing.Point(4, 22);
			this._tabPageAttributes.Name = "_tabPageAttributes";
			this._tabPageAttributes.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageAttributes.Size = new System.Drawing.Size(606, 467);
			this._tabPageAttributes.TabIndex = 0;
			this._tabPageAttributes.Text = "Attributes";
			this._tabPageAttributes.UseVisualStyleBackColor = true;
			// 
			// _splitContainerAttributes
			// 
			this._splitContainerAttributes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainerAttributes.Location = new System.Drawing.Point(0, 0);
			this._splitContainerAttributes.Name = "_splitContainerAttributes";
			// 
			// _splitContainerAttributes.Panel1
			// 
			this._splitContainerAttributes.Panel1.Controls.Add(this._labelAvailableAttr);
			this._splitContainerAttributes.Panel1.Controls.Add(this._dataGridViewAvailable);
			// 
			// _splitContainerAttributes.Panel2
			// 
			this._splitContainerAttributes.Panel2.Controls.Add(this._splitContainerSourceTarget);
			this._splitContainerAttributes.Size = new System.Drawing.Size(606, 464);
			this._splitContainerAttributes.SplitterDistance = 301;
			this._splitContainerAttributes.TabIndex = 0;
			// 
			// _labelAvailableAttr
			// 
			this._labelAvailableAttr.AutoSize = true;
			this._labelAvailableAttr.Location = new System.Drawing.Point(3, 10);
			this._labelAvailableAttr.Name = "_labelAvailableAttr";
			this._labelAvailableAttr.Size = new System.Drawing.Size(100, 13);
			this._labelAvailableAttr.TabIndex = 0;
			this._labelAvailableAttr.Text = "Available Attributes:";
			// 
			// _dataGridViewAvailable
			// 
			this._dataGridViewAvailable.AllowUserToAddRows = false;
			this._dataGridViewAvailable.AllowUserToDeleteRows = false;
			this._dataGridViewAvailable.AllowUserToResizeRows = false;
			this._dataGridViewAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewAvailable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewAvailable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._dataGridViewAvailableImageColumn,
            this._dataGridViewAvailableTextBoxColumnName,
            this._dataGridViewAvailableTextBoxColumnFieldType});
			this._dataGridViewAvailable.Location = new System.Drawing.Point(3, 29);
			this._dataGridViewAvailable.MultiSelect = false;
			this._dataGridViewAvailable.Name = "_dataGridViewAvailable";
			this._dataGridViewAvailable.ReadOnly = true;
			this._dataGridViewAvailable.RowHeadersVisible = false;
			this._dataGridViewAvailable.RowHeadersWidth = 15;
			this._dataGridViewAvailable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewAvailable.Size = new System.Drawing.Size(295, 432);
			this._dataGridViewAvailable.TabIndex = 1;
			this._dataGridViewAvailable.SelectionChanged += new System.EventHandler(this._dataGridViewAvailable_SelectionChanged);
			// 
			// _dataGridViewAvailableImageColumn
			// 
			this._dataGridViewAvailableImageColumn.DataPropertyName = "Image";
			this._dataGridViewAvailableImageColumn.HeaderText = "";
			this._dataGridViewAvailableImageColumn.MinimumWidth = 20;
			this._dataGridViewAvailableImageColumn.Name = "_dataGridViewAvailableImageColumn";
			this._dataGridViewAvailableImageColumn.ReadOnly = true;
			this._dataGridViewAvailableImageColumn.Width = 30;
			// 
			// _dataGridViewAvailableTextBoxColumnName
			// 
			this._dataGridViewAvailableTextBoxColumnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewAvailableTextBoxColumnName.DataPropertyName = "Name";
			this._dataGridViewAvailableTextBoxColumnName.HeaderText = "Name";
			this._dataGridViewAvailableTextBoxColumnName.Name = "_dataGridViewAvailableTextBoxColumnName";
			this._dataGridViewAvailableTextBoxColumnName.ReadOnly = true;
			// 
			// _dataGridViewAvailableTextBoxColumnFieldType
			// 
			this._dataGridViewAvailableTextBoxColumnFieldType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewAvailableTextBoxColumnFieldType.DataPropertyName = "FieldType";
			this._dataGridViewAvailableTextBoxColumnFieldType.HeaderText = "Field Type";
			this._dataGridViewAvailableTextBoxColumnFieldType.Name = "_dataGridViewAvailableTextBoxColumnFieldType";
			this._dataGridViewAvailableTextBoxColumnFieldType.ReadOnly = true;
			// 
			// _splitContainerSourceTarget
			// 
			this._splitContainerSourceTarget.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerSourceTarget.Location = new System.Drawing.Point(0, 0);
			this._splitContainerSourceTarget.Name = "_splitContainerSourceTarget";
			this._splitContainerSourceTarget.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerSourceTarget.Panel1
			// 
			this._splitContainerSourceTarget.Panel1.Controls.Add(this._dataGridViewSource);
			this._splitContainerSourceTarget.Panel1.Controls.Add(this._labelSource);
			this._splitContainerSourceTarget.Panel1.Controls.Add(this._buttonRemoveFromSource);
			this._splitContainerSourceTarget.Panel1.Controls.Add(this._buttonAddToSource);
			// 
			// _splitContainerSourceTarget.Panel2
			// 
			this._splitContainerSourceTarget.Panel2.Controls.Add(this._dataGridViewTarget);
			this._splitContainerSourceTarget.Panel2.Controls.Add(this._labelTarget);
			this._splitContainerSourceTarget.Panel2.Controls.Add(this._buttonRemoveFromTarget);
			this._splitContainerSourceTarget.Panel2.Controls.Add(this._buttonAddToTarget);
			this._splitContainerSourceTarget.Size = new System.Drawing.Size(301, 464);
			this._splitContainerSourceTarget.SplitterDistance = 211;
			this._splitContainerSourceTarget.TabIndex = 0;
			// 
			// _dataGridViewSource
			// 
			this._dataGridViewSource.AllowUserToAddRows = false;
			this._dataGridViewSource.AllowUserToDeleteRows = false;
			this._dataGridViewSource.AllowUserToResizeRows = false;
			this._dataGridViewSource.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewSource.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewSource.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._dataGridViewSourceImageColumn,
            this._dataGridViewSourceTextBoxColumnName,
            this._dataGridViewSourceTextBoxColumnFieldType});
			this._dataGridViewSource.Location = new System.Drawing.Point(33, 26);
			this._dataGridViewSource.MultiSelect = false;
			this._dataGridViewSource.Name = "_dataGridViewSource";
			this._dataGridViewSource.ReadOnly = true;
			this._dataGridViewSource.RowHeadersVisible = false;
			this._dataGridViewSource.RowHeadersWidth = 15;
			this._dataGridViewSource.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewSource.Size = new System.Drawing.Size(266, 182);
			this._dataGridViewSource.TabIndex = 1;
			this._dataGridViewSource.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this._dataGridViewSource_DataBindingComplete);
			this._dataGridViewSource.SelectionChanged += new System.EventHandler(this._dataGridViewSource_SelectionChanged);
			// 
			// _dataGridViewSourceImageColumn
			// 
			this._dataGridViewSourceImageColumn.DataPropertyName = "Image";
			this._dataGridViewSourceImageColumn.HeaderText = "";
			this._dataGridViewSourceImageColumn.MinimumWidth = 20;
			this._dataGridViewSourceImageColumn.Name = "_dataGridViewSourceImageColumn";
			this._dataGridViewSourceImageColumn.ReadOnly = true;
			this._dataGridViewSourceImageColumn.Width = 30;
			// 
			// _dataGridViewSourceTextBoxColumnName
			// 
			this._dataGridViewSourceTextBoxColumnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewSourceTextBoxColumnName.DataPropertyName = "Name";
			this._dataGridViewSourceTextBoxColumnName.HeaderText = "Name";
			this._dataGridViewSourceTextBoxColumnName.Name = "_dataGridViewSourceTextBoxColumnName";
			this._dataGridViewSourceTextBoxColumnName.ReadOnly = true;
			// 
			// _dataGridViewSourceTextBoxColumnFieldType
			// 
			this._dataGridViewSourceTextBoxColumnFieldType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewSourceTextBoxColumnFieldType.DataPropertyName = "FieldType";
			this._dataGridViewSourceTextBoxColumnFieldType.HeaderText = "Field Type";
			this._dataGridViewSourceTextBoxColumnFieldType.Name = "_dataGridViewSourceTextBoxColumnFieldType";
			this._dataGridViewSourceTextBoxColumnFieldType.ReadOnly = true;
			// 
			// _labelSource
			// 
			this._labelSource.AutoSize = true;
			this._labelSource.Location = new System.Drawing.Point(30, 10);
			this._labelSource.Name = "_labelSource";
			this._labelSource.Size = new System.Drawing.Size(91, 13);
			this._labelSource.TabIndex = 0;
			this._labelSource.Text = "Source Attributes:";
			// 
			// _buttonRemoveFromSource
			// 
			this._buttonRemoveFromSource.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._buttonRemoveFromSource.Location = new System.Drawing.Point(4, 82);
			this._buttonRemoveFromSource.Name = "_buttonRemoveFromSource";
			this._buttonRemoveFromSource.Size = new System.Drawing.Size(23, 23);
			this._buttonRemoveFromSource.TabIndex = 3;
			this._buttonRemoveFromSource.UseVisualStyleBackColor = true;
			this._buttonRemoveFromSource.Click += new System.EventHandler(this._buttonRemoveFromSource_Click);
			// 
			// _buttonAddToSource
			// 
			this._buttonAddToSource.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._buttonAddToSource.Location = new System.Drawing.Point(4, 53);
			this._buttonAddToSource.Name = "_buttonAddToSource";
			this._buttonAddToSource.Size = new System.Drawing.Size(23, 23);
			this._buttonAddToSource.TabIndex = 2;
			this._buttonAddToSource.UseVisualStyleBackColor = true;
			this._buttonAddToSource.Click += new System.EventHandler(this._buttonAddToSource_Click);
			// 
			// _dataGridViewTarget
			// 
			this._dataGridViewTarget.AllowUserToAddRows = false;
			this._dataGridViewTarget.AllowUserToDeleteRows = false;
			this._dataGridViewTarget.AllowUserToResizeRows = false;
			this._dataGridViewTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewTarget.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewTarget.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._dataGridViewTargetImageColumn,
            this._dataGridViewTargetTextBoxColumnName,
            this._dataGridViewTargetTextBoxColumnFieldType});
			this._dataGridViewTarget.Location = new System.Drawing.Point(33, 26);
			this._dataGridViewTarget.MultiSelect = false;
			this._dataGridViewTarget.Name = "_dataGridViewTarget";
			this._dataGridViewTarget.ReadOnly = true;
			this._dataGridViewTarget.RowHeadersVisible = false;
			this._dataGridViewTarget.RowHeadersWidth = 15;
			this._dataGridViewTarget.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewTarget.Size = new System.Drawing.Size(266, 220);
			this._dataGridViewTarget.TabIndex = 1;
			this._dataGridViewTarget.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this._dataGridViewTarget_DataBindingComplete);
			this._dataGridViewTarget.SelectionChanged += new System.EventHandler(this._dataGridViewTarget_SelectionChanged);
			// 
			// _dataGridViewTargetImageColumn
			// 
			this._dataGridViewTargetImageColumn.DataPropertyName = "Image";
			this._dataGridViewTargetImageColumn.HeaderText = "";
			this._dataGridViewTargetImageColumn.MinimumWidth = 20;
			this._dataGridViewTargetImageColumn.Name = "_dataGridViewTargetImageColumn";
			this._dataGridViewTargetImageColumn.ReadOnly = true;
			this._dataGridViewTargetImageColumn.Width = 30;
			// 
			// _dataGridViewTargetTextBoxColumnName
			// 
			this._dataGridViewTargetTextBoxColumnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewTargetTextBoxColumnName.DataPropertyName = "Name";
			this._dataGridViewTargetTextBoxColumnName.HeaderText = "Name";
			this._dataGridViewTargetTextBoxColumnName.Name = "_dataGridViewTargetTextBoxColumnName";
			this._dataGridViewTargetTextBoxColumnName.ReadOnly = true;
			// 
			// _dataGridViewTargetTextBoxColumnFieldType
			// 
			this._dataGridViewTargetTextBoxColumnFieldType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewTargetTextBoxColumnFieldType.DataPropertyName = "FieldType";
			this._dataGridViewTargetTextBoxColumnFieldType.HeaderText = "Field Type";
			this._dataGridViewTargetTextBoxColumnFieldType.Name = "_dataGridViewTargetTextBoxColumnFieldType";
			this._dataGridViewTargetTextBoxColumnFieldType.ReadOnly = true;
			// 
			// _labelTarget
			// 
			this._labelTarget.AutoSize = true;
			this._labelTarget.Location = new System.Drawing.Point(30, 10);
			this._labelTarget.Name = "_labelTarget";
			this._labelTarget.Size = new System.Drawing.Size(88, 13);
			this._labelTarget.TabIndex = 0;
			this._labelTarget.Text = "Target Attributes:";
			// 
			// _buttonRemoveFromTarget
			// 
			this._buttonRemoveFromTarget.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._buttonRemoveFromTarget.Location = new System.Drawing.Point(3, 89);
			this._buttonRemoveFromTarget.Name = "_buttonRemoveFromTarget";
			this._buttonRemoveFromTarget.Size = new System.Drawing.Size(23, 23);
			this._buttonRemoveFromTarget.TabIndex = 3;
			this._buttonRemoveFromTarget.UseVisualStyleBackColor = true;
			this._buttonRemoveFromTarget.Click += new System.EventHandler(this._buttonRemoveFromTarget_Click);
			// 
			// _buttonAddToTarget
			// 
			this._buttonAddToTarget.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._buttonAddToTarget.Location = new System.Drawing.Point(3, 60);
			this._buttonAddToTarget.Name = "_buttonAddToTarget";
			this._buttonAddToTarget.Size = new System.Drawing.Size(23, 23);
			this._buttonAddToTarget.TabIndex = 2;
			this._buttonAddToTarget.UseVisualStyleBackColor = true;
			this._buttonAddToTarget.Click += new System.EventHandler(this._buttonAddToTarget_Click);
			// 
			// _tabPageMappings
			// 
			this._tabPageMappings.Controls.Add(this._labelRightClickInfo);
			this._tabPageMappings.Controls.Add(this._buttonExportMappings);
			this._tabPageMappings.Controls.Add(this._buttonImportMappings);
			this._tabPageMappings.Controls.Add(this._dataGridViewFindToolStrip);
			this._tabPageMappings.Controls.Add(this._dataGridViewMappings);
			this._tabPageMappings.Location = new System.Drawing.Point(4, 22);
			this._tabPageMappings.Name = "_tabPageMappings";
			this._tabPageMappings.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageMappings.Size = new System.Drawing.Size(606, 467);
			this._tabPageMappings.TabIndex = 1;
			this._tabPageMappings.Text = "Mappings";
			this._tabPageMappings.UseVisualStyleBackColor = true;
			// 
			// _labelRightClickInfo
			// 
			this._labelRightClickInfo.AutoSize = true;
			this._labelRightClickInfo.Location = new System.Drawing.Point(6, 11);
			this._labelRightClickInfo.Name = "_labelRightClickInfo";
			this._labelRightClickInfo.Size = new System.Drawing.Size(289, 13);
			this._labelRightClickInfo.TabIndex = 4;
			this._labelRightClickInfo.Text = "(Right-click column headers for field type and coded values)";
			// 
			// _buttonExportMappings
			// 
			this._buttonExportMappings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonExportMappings.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Export;
			this._buttonExportMappings.Location = new System.Drawing.Point(450, 6);
			this._buttonExportMappings.Name = "_buttonExportMappings";
			this._buttonExportMappings.Size = new System.Drawing.Size(150, 23);
			this._buttonExportMappings.TabIndex = 3;
			this._buttonExportMappings.Text = "Export Mappings...";
			this._buttonExportMappings.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this._buttonExportMappings.UseVisualStyleBackColor = true;
			this._buttonExportMappings.Click += new System.EventHandler(this._buttonExportMappings_Click);
			// 
			// _buttonImportMappings
			// 
			this._buttonImportMappings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonImportMappings.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Import;
			this._buttonImportMappings.Location = new System.Drawing.Point(294, 6);
			this._buttonImportMappings.Name = "_buttonImportMappings";
			this._buttonImportMappings.Size = new System.Drawing.Size(150, 23);
			this._buttonImportMappings.TabIndex = 2;
			this._buttonImportMappings.Text = "Import Mappings...";
			this._buttonImportMappings.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this._buttonImportMappings.UseVisualStyleBackColor = true;
			this._buttonImportMappings.Click += new System.EventHandler(this._buttonImportMappings_Click);
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewFindToolStrip.AutoSize = false;
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(6, 34);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(594, 27);
			this._dataGridViewFindToolStrip.TabIndex = 27;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
			// 
			// _dataGridViewMappings
			// 
			this._dataGridViewMappings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewMappings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewMappings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._dataGridViewMappingsTextBoxColumnDescription});
			this._dataGridViewMappings.Location = new System.Drawing.Point(6, 64);
			this._dataGridViewMappings.Name = "_dataGridViewMappings";
			this._dataGridViewMappings.RowHeadersWidth = 30;
			this._dataGridViewMappings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewMappings.Size = new System.Drawing.Size(594, 397);
			this._dataGridViewMappings.TabIndex = 0;
			this._dataGridViewMappings.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridViewMappings_CellFormatting);
			this._dataGridViewMappings.CellParsing += new System.Windows.Forms.DataGridViewCellParsingEventHandler(this._dataGridViewMappings_CellParsing);
			this._dataGridViewMappings.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewMappings_CellValueChanged);
			this._dataGridViewMappings.Sorted += new System.EventHandler(this._dataGridViewMappings_Sorted);
			this._dataGridViewMappings.UserAddedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this._dataGridViewMappings_UserAddedRow);
			this._dataGridViewMappings.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this._dataGridViewMappings_UserDeletedRow);
			// 
			// _dataGridViewMappingsTextBoxColumnDescription
			// 
			this._dataGridViewMappingsTextBoxColumnDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._dataGridViewMappingsTextBoxColumnDescription.DataPropertyName = "Description";
			this._dataGridViewMappingsTextBoxColumnDescription.HeaderText = "Description";
			this._dataGridViewMappingsTextBoxColumnDescription.Name = "_dataGridViewMappingsTextBoxColumnDescription";
			// 
			// _labelDataset
			// 
			this._labelDataset.AutoSize = true;
			this._labelDataset.Location = new System.Drawing.Point(21, 15);
			this._labelDataset.Name = "_labelDataset";
			this._labelDataset.Size = new System.Drawing.Size(47, 13);
			this._labelDataset.TabIndex = 16;
			this._labelDataset.Text = "Dataset:";
			// 
			// AttributeDependencyControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelDataset);
			this.Controls.Add(this._tabControl);
			this.Controls.Add(this._objectReferenceControlDataset);
			this.Name = "AttributeDependencyControl";
			this.Size = new System.Drawing.Size(655, 544);
			this._tabControl.ResumeLayout(false);
			this._tabPageAttributes.ResumeLayout(false);
			this._splitContainerAttributes.Panel1.ResumeLayout(false);
			this._splitContainerAttributes.Panel1.PerformLayout();
			this._splitContainerAttributes.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerAttributes)).EndInit();
			this._splitContainerAttributes.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewAvailable)).EndInit();
			this._splitContainerSourceTarget.Panel1.ResumeLayout(false);
			this._splitContainerSourceTarget.Panel1.PerformLayout();
			this._splitContainerSourceTarget.Panel2.ResumeLayout(false);
			this._splitContainerSourceTarget.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerSourceTarget)).EndInit();
			this._splitContainerSourceTarget.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewSource)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewTarget)).EndInit();
			this._tabPageMappings.ResumeLayout(false);
			this._tabPageMappings.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewMappings)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelDataset;
        private global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl _objectReferenceControlDataset;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabPageAttributes;
        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerAttributes;
        private System.Windows.Forms.Label _labelAvailableAttr;
        private System.Windows.Forms.DataGridView _dataGridViewAvailable;
        private System.Windows.Forms.DataGridViewImageColumn _dataGridViewAvailableImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewAvailableTextBoxColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewAvailableTextBoxColumnFieldType;
        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerSourceTarget;
        private System.Windows.Forms.DataGridView _dataGridViewSource;
        private System.Windows.Forms.DataGridViewImageColumn _dataGridViewSourceImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewSourceTextBoxColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewSourceTextBoxColumnFieldType;
        private System.Windows.Forms.Label _labelSource;
        private System.Windows.Forms.Button _buttonRemoveFromSource;
        private System.Windows.Forms.Button _buttonAddToSource;
        private System.Windows.Forms.DataGridView _dataGridViewTarget;
        private System.Windows.Forms.DataGridViewImageColumn _dataGridViewTargetImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewTargetTextBoxColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewTargetTextBoxColumnFieldType;
        private System.Windows.Forms.Label _labelTarget;
        private System.Windows.Forms.Button _buttonRemoveFromTarget;
        private System.Windows.Forms.Button _buttonAddToTarget;
        private System.Windows.Forms.TabPage _tabPageMappings;
        private System.Windows.Forms.Label _labelRightClickInfo;
		private FilterableDataGridView _dataGridViewMappings;
        private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridViewMappingsTextBoxColumnDescription;
        private System.Windows.Forms.Button _buttonExportMappings;
        private System.Windows.Forms.Button _buttonImportMappings;
		private global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip _dataGridViewFindToolStrip;
    }
}
