namespace ProSuite.DdxEditor.Content.QA.TestDescriptors.CreateQualityConditions
{
	partial class CreateQualityConditionsForm
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
			this._dataGridView = new System.Windows.Forms.DataGridView();
			this._contextMenuStripDataGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._toolStripElements = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripButtonRemove = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonAdd = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonSelectAll = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonSelectNone = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonApplyNamingConventionToSelection = new System.Windows.Forms.ToolStripButton();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxQualityConditionNames = new System.Windows.Forms.TextBox();
			this._textBoxTestDescriptorName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._groupBoxDatasets = new System.Windows.Forms.GroupBox();
			this._checkBoxExcludeDatasetsUsingThisTest = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this._textBoxSupportedVariables = new System.Windows.Forms.TextBox();
			this._groupBoxQualitySpecifications = new System.Windows.Forms.GroupBox();
			this._dataGridViewQualitySpecifications = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._toolStripQualitySpecifications = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripButtonRemoveFromQualitySpecifications = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonAssignToQualitySpecifications = new System.Windows.Forms.ToolStripButton();
			this._splitContainer = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._objectReferenceControlCategory = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._labelTargetCategory = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this._groupBoxDatasets.SuspendLayout();
			this._groupBoxQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualitySpecifications)).BeginInit();
			this._toolStripQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.ContextMenuStrip = this._contextMenuStripDataGrid;
			this._dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this._dataGridView.Location = new System.Drawing.Point(6, 44);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.RowHeadersWidth = 30;
			this._dataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this._dataGridView.Size = new System.Drawing.Size(548, 122);
			this._dataGridView.TabIndex = 1;
			this._dataGridView.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellEnter);
			this._dataGridView.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellValidated);
			this._dataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this._dataGridView_DataError);
			this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
			// 
			// _contextMenuStripDataGrid
			// 
			this._contextMenuStripDataGrid.Name = "_contextMenuStripDataGrid";
			this._contextMenuStripDataGrid.Size = new System.Drawing.Size(61, 4);
			this._contextMenuStripDataGrid.Opening += new System.ComponentModel.CancelEventHandler(this._contextMenuStripDataGrid_Opening);
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(497, 529);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 5;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(416, 529);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 4;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _toolStripElements
			// 
			this._toolStripElements.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._toolStripElements.AutoSize = false;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.Dock = System.Windows.Forms.DockStyle.None;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonRemove,
            this._toolStripButtonAdd,
            this._toolStripButtonSelectAll,
            this._toolStripButtonSelectNone,
            this._toolStripButtonApplyNamingConventionToSelection});
			this._toolStripElements.Location = new System.Drawing.Point(6, 21);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripElements.Size = new System.Drawing.Size(548, 25);
			this._toolStripElements.TabIndex = 0;
			// 
			// _toolStripButtonRemove
			// 
			this._toolStripButtonRemove.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemove.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemove.Name = "_toolStripButtonRemove";
			this._toolStripButtonRemove.Size = new System.Drawing.Size(70, 22);
			this._toolStripButtonRemove.Text = "Remove";
			this._toolStripButtonRemove.Click += new System.EventHandler(this._toolStripButtonRemove_Click);
			// 
			// _toolStripButtonAdd
			// 
			this._toolStripButtonAdd.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAdd.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._toolStripButtonAdd.Name = "_toolStripButtonAdd";
			this._toolStripButtonAdd.Size = new System.Drawing.Size(58, 22);
			this._toolStripButtonAdd.Text = "Add...";
			this._toolStripButtonAdd.Click += new System.EventHandler(this._toolStripButtonAdd_Click);
			// 
			// _toolStripButtonSelectAll
			// 
			this._toolStripButtonSelectAll.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.SelectAll;
			this._toolStripButtonSelectAll.Name = "_toolStripButtonSelectAll";
			this._toolStripButtonSelectAll.Size = new System.Drawing.Size(75, 22);
			this._toolStripButtonSelectAll.Text = "Select All";
			this._toolStripButtonSelectAll.Click += new System.EventHandler(this._toolStripButtonSelectAll_Click);
			// 
			// _toolStripButtonSelectNone
			// 
			this._toolStripButtonSelectNone.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.SelectNone;
			this._toolStripButtonSelectNone.Name = "_toolStripButtonSelectNone";
			this._toolStripButtonSelectNone.Size = new System.Drawing.Size(90, 22);
			this._toolStripButtonSelectNone.Text = "Select None";
			this._toolStripButtonSelectNone.Click += new System.EventHandler(this._toolStripButtonSelectNone_Click);
			// 
			// _toolStripButtonApplyNamingConventionToSelection
			// 
			this._toolStripButtonApplyNamingConventionToSelection.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Refresh;
			this._toolStripButtonApplyNamingConventionToSelection.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonApplyNamingConventionToSelection.Name = "_toolStripButtonApplyNamingConventionToSelection";
			this._toolStripButtonApplyNamingConventionToSelection.Size = new System.Drawing.Size(234, 22);
			this._toolStripButtonApplyNamingConventionToSelection.Text = "Apply Naming Convention to Selection";
			this._toolStripButtonApplyNamingConventionToSelection.Click += new System.EventHandler(this._toolStripButtonApplyNamingConventionToSelection_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(17, 67);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(103, 13);
			this.label1.TabIndex = 25;
			this.label1.Text = "Naming Convention:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxQualityConditionNames
			// 
			this._textBoxQualityConditionNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxQualityConditionNames.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxQualityConditionNames.Location = new System.Drawing.Point(126, 64);
			this._textBoxQualityConditionNames.Name = "_textBoxQualityConditionNames";
			this._textBoxQualityConditionNames.Size = new System.Drawing.Size(446, 20);
			this._textBoxQualityConditionNames.TabIndex = 2;
			this._textBoxQualityConditionNames.TextChanged += new System.EventHandler(this._textBoxQualityConditionNames_TextChanged);
			// 
			// _textBoxTestDescriptorName
			// 
			this._textBoxTestDescriptorName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTestDescriptorName.Location = new System.Drawing.Point(126, 12);
			this._textBoxTestDescriptorName.Name = "_textBoxTestDescriptorName";
			this._textBoxTestDescriptorName.ReadOnly = true;
			this._textBoxTestDescriptorName.Size = new System.Drawing.Size(446, 20);
			this._textBoxTestDescriptorName.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(38, 15);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(82, 13);
			this.label2.TabIndex = 28;
			this.label2.Text = "Test Descriptor:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _statusStrip
			// 
			this._statusStrip.Location = new System.Drawing.Point(0, 562);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(584, 22);
			this._statusStrip.TabIndex = 29;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _groupBoxDatasets
			// 
			this._groupBoxDatasets.Controls.Add(this._checkBoxExcludeDatasetsUsingThisTest);
			this._groupBoxDatasets.Controls.Add(this._dataGridView);
			this._groupBoxDatasets.Controls.Add(this._toolStripElements);
			this._groupBoxDatasets.Controls.Add(this.label3);
			this._groupBoxDatasets.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxDatasets.ForeColor = System.Drawing.SystemColors.ControlText;
			this._groupBoxDatasets.Location = new System.Drawing.Point(0, 0);
			this._groupBoxDatasets.Name = "_groupBoxDatasets";
			this._groupBoxDatasets.Size = new System.Drawing.Size(560, 221);
			this._groupBoxDatasets.TabIndex = 0;
			this._groupBoxDatasets.TabStop = false;
			this._groupBoxDatasets.Text = "Datasets to create Quality Conditions for";
			// 
			// _checkBoxExcludeDatasetsUsingThisTest
			// 
			this._checkBoxExcludeDatasetsUsingThisTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._checkBoxExcludeDatasetsUsingThisTest.AutoSize = true;
			this._checkBoxExcludeDatasetsUsingThisTest.Checked = true;
			this._checkBoxExcludeDatasetsUsingThisTest.CheckState = System.Windows.Forms.CheckState.Checked;
			this._checkBoxExcludeDatasetsUsingThisTest.Location = new System.Drawing.Point(240, 199);
			this._checkBoxExcludeDatasetsUsingThisTest.Name = "_checkBoxExcludeDatasetsUsingThisTest";
			this._checkBoxExcludeDatasetsUsingThisTest.Size = new System.Drawing.Size(314, 17);
			this._checkBoxExcludeDatasetsUsingThisTest.TabIndex = 2;
			this._checkBoxExcludeDatasetsUsingThisTest.Text = "Exclude datasets for which this test descriptor is already used";
			this._checkBoxExcludeDatasetsUsingThisTest.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.BackColor = System.Drawing.SystemColors.Info;
			this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label3.Location = new System.Drawing.Point(6, 166);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(548, 29);
			this.label3.TabIndex = 25;
			this.label3.Text = "To fill in parameter values, select cells in one or more parameter columns and ri" +
    "ght click to select \"Fill down\"";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(15, 93);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(105, 13);
			this.label5.TabIndex = 25;
			this.label5.Text = "Supported Variables:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSupportedVariables
			// 
			this._textBoxSupportedVariables.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxSupportedVariables.BackColor = System.Drawing.SystemColors.Info;
			this._textBoxSupportedVariables.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxSupportedVariables.Location = new System.Drawing.Point(126, 90);
			this._textBoxSupportedVariables.Multiline = true;
			this._textBoxSupportedVariables.Name = "_textBoxSupportedVariables";
			this._textBoxSupportedVariables.ReadOnly = true;
			this._textBoxSupportedVariables.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxSupportedVariables.Size = new System.Drawing.Size(446, 37);
			this._textBoxSupportedVariables.TabIndex = 3;
			this._textBoxSupportedVariables.TextChanged += new System.EventHandler(this._textBoxQualityConditionNames_TextChanged);
			// 
			// _groupBoxQualitySpecifications
			// 
			this._groupBoxQualitySpecifications.Controls.Add(this._dataGridViewQualitySpecifications);
			this._groupBoxQualitySpecifications.Controls.Add(this._toolStripQualitySpecifications);
			this._groupBoxQualitySpecifications.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxQualitySpecifications.Location = new System.Drawing.Point(0, 0);
			this._groupBoxQualitySpecifications.Name = "_groupBoxQualitySpecifications";
			this._groupBoxQualitySpecifications.Size = new System.Drawing.Size(560, 161);
			this._groupBoxQualitySpecifications.TabIndex = 0;
			this._groupBoxQualitySpecifications.TabStop = false;
			this._groupBoxQualitySpecifications.Text = "Quality Specifications";
			// 
			// _dataGridViewQualitySpecifications
			// 
			this._dataGridViewQualitySpecifications.AllowUserToAddRows = false;
			this._dataGridViewQualitySpecifications.AllowUserToDeleteRows = false;
			this._dataGridViewQualitySpecifications.AllowUserToResizeRows = false;
			this._dataGridViewQualitySpecifications.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewQualitySpecifications.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnName});
			this._dataGridViewQualitySpecifications.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewQualitySpecifications.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this._dataGridViewQualitySpecifications.Location = new System.Drawing.Point(3, 41);
			this._dataGridViewQualitySpecifications.Name = "_dataGridViewQualitySpecifications";
			this._dataGridViewQualitySpecifications.ReadOnly = true;
			this._dataGridViewQualitySpecifications.RowHeadersVisible = false;
			this._dataGridViewQualitySpecifications.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewQualitySpecifications.Size = new System.Drawing.Size(554, 117);
			this._dataGridViewQualitySpecifications.TabIndex = 1;
			this._dataGridViewQualitySpecifications.SelectionChanged += new System.EventHandler(this._dataGridViewQualitySpecifications_SelectionChanged);
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnName.DataPropertyName = "Name";
			this._columnName.HeaderText = "Quality Specification";
			this._columnName.MinimumWidth = 200;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			// 
			// _toolStripQualitySpecifications
			// 
			this._toolStripQualitySpecifications.AutoSize = false;
			this._toolStripQualitySpecifications.ClickThrough = true;
			this._toolStripQualitySpecifications.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripQualitySpecifications.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonRemoveFromQualitySpecifications,
            this._toolStripButtonAssignToQualitySpecifications});
			this._toolStripQualitySpecifications.Location = new System.Drawing.Point(3, 16);
			this._toolStripQualitySpecifications.Name = "_toolStripQualitySpecifications";
			this._toolStripQualitySpecifications.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripQualitySpecifications.Size = new System.Drawing.Size(554, 25);
			this._toolStripQualitySpecifications.TabIndex = 0;
			this._toolStripQualitySpecifications.Text = "Element Tools";
			// 
			// _toolStripButtonRemoveFromQualitySpecifications
			// 
			this._toolStripButtonRemoveFromQualitySpecifications.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemoveFromQualitySpecifications.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemoveFromQualitySpecifications.Name = "_toolStripButtonRemoveFromQualitySpecifications";
			this._toolStripButtonRemoveFromQualitySpecifications.Size = new System.Drawing.Size(70, 22);
			this._toolStripButtonRemoveFromQualitySpecifications.Text = "Remove";
			this._toolStripButtonRemoveFromQualitySpecifications.Click += new System.EventHandler(this._toolStripButtonRemoveFromQualitySpecifications_Click);
			// 
			// _toolStripButtonAssignToQualitySpecifications
			// 
			this._toolStripButtonAssignToQualitySpecifications.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAssignToQualitySpecifications.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._toolStripButtonAssignToQualitySpecifications.Name = "_toolStripButtonAssignToQualitySpecifications";
			this._toolStripButtonAssignToQualitySpecifications.Size = new System.Drawing.Size(205, 22);
			this._toolStripButtonAssignToQualitySpecifications.Text = "Assign To Quality Specifications...";
			this._toolStripButtonAssignToQualitySpecifications.Click += new System.EventHandler(this._toolStripButtonAssignToQualitySpecifications_Click);
			// 
			// _splitContainer
			// 
			this._splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainer.Location = new System.Drawing.Point(12, 137);
			this._splitContainer.Name = "_splitContainer";
			this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._groupBoxDatasets);
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.Controls.Add(this._groupBoxQualitySpecifications);
			this._splitContainer.Size = new System.Drawing.Size(560, 386);
			this._splitContainer.SplitterDistance = 221;
			this._splitContainer.TabIndex = 33;
			// 
			// _objectReferenceControlCategory
			// 
			this._objectReferenceControlCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlCategory.DataSource = null;
			this._objectReferenceControlCategory.DisplayMember = null;
			this._objectReferenceControlCategory.FindObjectDelegate = null;
			this._objectReferenceControlCategory.FormatTextDelegate = null;
			this._objectReferenceControlCategory.Location = new System.Drawing.Point(126, 38);
			this._objectReferenceControlCategory.Name = "_objectReferenceControlCategory";
			this._objectReferenceControlCategory.ReadOnly = false;
			this._objectReferenceControlCategory.Size = new System.Drawing.Size(446, 20);
			this._objectReferenceControlCategory.TabIndex = 1;
			// 
			// _labelTargetCategory
			// 
			this._labelTargetCategory.AutoSize = true;
			this._labelTargetCategory.Location = new System.Drawing.Point(34, 41);
			this._labelTargetCategory.Name = "_labelTargetCategory";
			this._labelTargetCategory.Size = new System.Drawing.Size(86, 13);
			this._labelTargetCategory.TabIndex = 25;
			this._labelTargetCategory.Text = "Target Category:";
			this._labelTargetCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// CreateQualityConditionsForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(584, 584);
			this.Controls.Add(this._labelTargetCategory);
			this.Controls.Add(this._objectReferenceControlCategory);
			this.Controls.Add(this._splitContainer);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._textBoxTestDescriptorName);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._textBoxQualityConditionNames);
			this.Controls.Add(this._textBoxSupportedVariables);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label5);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(600, 600);
			this.Name = "CreateQualityConditionsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Create Quality Conditions";
			this.Load += new System.EventHandler(this.CreateQualityConditionsForm_Load);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this._toolStripElements.ResumeLayout(false);
			this._toolStripElements.PerformLayout();
			this._groupBoxDatasets.ResumeLayout(false);
			this._groupBoxDatasets.PerformLayout();
			this._groupBoxQualitySpecifications.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualitySpecifications)).EndInit();
			this._toolStripQualitySpecifications.ResumeLayout(false);
			this._toolStripQualitySpecifications.PerformLayout();
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView _dataGridView;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripElements;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemove;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAdd;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox _textBoxQualityConditionNames;
		private System.Windows.Forms.TextBox _textBoxTestDescriptorName;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ContextMenuStrip _contextMenuStripDataGrid;
		private System.Windows.Forms.GroupBox _groupBoxDatasets;
		private System.Windows.Forms.ToolStripButton _toolStripButtonSelectAll;
		private System.Windows.Forms.ToolStripButton _toolStripButtonSelectNone;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox _textBoxSupportedVariables;
		private System.Windows.Forms.ToolStripButton _toolStripButtonApplyNamingConventionToSelection;
		private System.Windows.Forms.GroupBox _groupBoxQualitySpecifications;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripQualitySpecifications;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveFromQualitySpecifications;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAssignToQualitySpecifications;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewQualitySpecifications;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.CheckBox _checkBoxExcludeDatasetsUsingThisTest;
		private global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl _objectReferenceControlCategory;
		private System.Windows.Forms.Label _labelTargetCategory;
	}
}
