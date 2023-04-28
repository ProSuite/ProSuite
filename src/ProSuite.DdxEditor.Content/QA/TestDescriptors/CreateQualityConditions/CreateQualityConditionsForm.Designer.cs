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
			components=new System.ComponentModel.Container();
			_dataGridView=new System.Windows.Forms.DataGridView();
			_contextMenuStripDataGrid=new System.Windows.Forms.ContextMenuStrip(components);
			_buttonCancel=new System.Windows.Forms.Button();
			_buttonOK=new System.Windows.Forms.Button();
			_toolStripElements=new Commons.UI.WinForms.Controls.ToolStripEx();
			_toolStripButtonRemove=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonAdd=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonSelectAll=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonSelectNone=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonApplyNamingConventionToSelection=new System.Windows.Forms.ToolStripButton();
			label1=new System.Windows.Forms.Label();
			_textBoxQualityConditionNames=new System.Windows.Forms.TextBox();
			_textBoxTestDescriptorName=new System.Windows.Forms.TextBox();
			label2=new System.Windows.Forms.Label();
			_statusStrip=new System.Windows.Forms.StatusStrip();
			_groupBoxDatasets=new System.Windows.Forms.GroupBox();
			_checkBoxExcludeDatasetsUsingThisTest=new System.Windows.Forms.CheckBox();
			label3=new System.Windows.Forms.Label();
			label5=new System.Windows.Forms.Label();
			_textBoxSupportedVariables=new System.Windows.Forms.TextBox();
			_groupBoxQualitySpecifications=new System.Windows.Forms.GroupBox();
			_dataGridViewQualitySpecifications=new Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			_columnName=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_toolStripQualitySpecifications=new Commons.UI.WinForms.Controls.ToolStripEx();
			_toolStripButtonRemoveFromQualitySpecifications=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonAssignToQualitySpecifications=new System.Windows.Forms.ToolStripButton();
			_splitContainer=new Commons.UI.WinForms.Controls.SplitContainerEx();
			_objectReferenceControlCategory=new Commons.UI.WinForms.Controls.ObjectReferenceControl();
			_labelTargetCategory=new System.Windows.Forms.Label();
			_panelMain=new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)_dataGridView).BeginInit();
			_toolStripElements.SuspendLayout();
			_groupBoxDatasets.SuspendLayout();
			_groupBoxQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewQualitySpecifications).BeginInit();
			_toolStripQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
			_splitContainer.Panel1.SuspendLayout();
			_splitContainer.Panel2.SuspendLayout();
			_splitContainer.SuspendLayout();
			_panelMain.SuspendLayout();
			SuspendLayout();
			// 
			// _dataGridViewf
			// 
			_dataGridView.AllowUserToAddRows=false;
			_dataGridView.AllowUserToDeleteRows=false;
			_dataGridView.AllowUserToResizeRows=false;
			_dataGridView.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_dataGridView.AutoSizeRowsMode=System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			_dataGridView.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridView.ContextMenuStrip=_contextMenuStripDataGrid;
			_dataGridView.EditMode=System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			_dataGridView.Location=new System.Drawing.Point(7, 51);
			_dataGridView.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridView.Name="_dataGridView";
			_dataGridView.RowHeadersWidth=30;
			_dataGridView.RowHeadersWidthSizeMode=System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			_dataGridView.Size=new System.Drawing.Size(639, 140);
			_dataGridView.TabIndex=1;
			_dataGridView.CellEnter+=_dataGridView_CellEnter;
			_dataGridView.CellValidated+=_dataGridView_CellValidated;
			_dataGridView.DataError+=_dataGridView_DataError;
			_dataGridView.SelectionChanged+=_dataGridView_SelectionChanged;
			// 
			// _contextMenuStripDataGrid
			// 
			_contextMenuStripDataGrid.Name="_contextMenuStripDataGrid";
			_contextMenuStripDataGrid.Size=new System.Drawing.Size(61, 4);
			_contextMenuStripDataGrid.Opening+=_contextMenuStripDataGrid_Opening;
			// 
			// _buttonCancel
			// 
			_buttonCancel.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Right;
			_buttonCancel.DialogResult=System.Windows.Forms.DialogResult.Cancel;
			_buttonCancel.Location=new System.Drawing.Point(580, 610);
			_buttonCancel.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonCancel.Name="_buttonCancel";
			_buttonCancel.Size=new System.Drawing.Size(88, 27);
			_buttonCancel.TabIndex=5;
			_buttonCancel.Text="Cancel";
			_buttonCancel.UseVisualStyleBackColor=true;
			_buttonCancel.Click+=_buttonCancel_Click;
			// 
			// _buttonOK
			// 
			_buttonOK.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Right;
			_buttonOK.Location=new System.Drawing.Point(485, 610);
			_buttonOK.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonOK.Name="_buttonOK";
			_buttonOK.Size=new System.Drawing.Size(88, 27);
			_buttonOK.TabIndex=4;
			_buttonOK.Text="OK";
			_buttonOK.UseVisualStyleBackColor=true;
			_buttonOK.Click+=_buttonOK_Click;
			// 
			// _toolStripElements
			// 
			_toolStripElements.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_toolStripElements.AutoSize=false;
			_toolStripElements.ClickThrough=true;
			_toolStripElements.Dock=System.Windows.Forms.DockStyle.None;
			_toolStripElements.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripButtonRemove, _toolStripButtonAdd, _toolStripButtonSelectAll, _toolStripButtonSelectNone, _toolStripButtonApplyNamingConventionToSelection });
			_toolStripElements.Location=new System.Drawing.Point(7, 24);
			_toolStripElements.Name="_toolStripElements";
			_toolStripElements.RenderMode=System.Windows.Forms.ToolStripRenderMode.System;
			_toolStripElements.Size=new System.Drawing.Size(639, 29);
			_toolStripElements.TabIndex=0;
			// 
			// _toolStripButtonRemove
			// 
			_toolStripButtonRemove.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonRemove.Image=Properties.Resources.Remove;
			_toolStripButtonRemove.Name="_toolStripButtonRemove";
			_toolStripButtonRemove.Size=new System.Drawing.Size(70, 26);
			_toolStripButtonRemove.Text="Remove";
			_toolStripButtonRemove.Click+=_toolStripButtonRemove_Click;
			// 
			// _toolStripButtonAdd
			// 
			_toolStripButtonAdd.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonAdd.Image=Properties.Resources.Assign;
			_toolStripButtonAdd.Name="_toolStripButtonAdd";
			_toolStripButtonAdd.Size=new System.Drawing.Size(58, 26);
			_toolStripButtonAdd.Text="Add...";
			_toolStripButtonAdd.Click+=_toolStripButtonAdd_Click;
			// 
			// _toolStripButtonSelectAll
			// 
			_toolStripButtonSelectAll.Image=Properties.Resources.SelectAll;
			_toolStripButtonSelectAll.Name="_toolStripButtonSelectAll";
			_toolStripButtonSelectAll.Size=new System.Drawing.Size(75, 26);
			_toolStripButtonSelectAll.Text="Select All";
			_toolStripButtonSelectAll.Click+=_toolStripButtonSelectAll_Click;
			// 
			// _toolStripButtonSelectNone
			// 
			_toolStripButtonSelectNone.Image=Properties.Resources.SelectNone;
			_toolStripButtonSelectNone.Name="_toolStripButtonSelectNone";
			_toolStripButtonSelectNone.Size=new System.Drawing.Size(90, 26);
			_toolStripButtonSelectNone.Text="Select None";
			_toolStripButtonSelectNone.Click+=_toolStripButtonSelectNone_Click;
			// 
			// _toolStripButtonApplyNamingConventionToSelection
			// 
			_toolStripButtonApplyNamingConventionToSelection.Image=Properties.Resources.Refresh;
			_toolStripButtonApplyNamingConventionToSelection.ImageScaling=System.Windows.Forms.ToolStripItemImageScaling.None;
			_toolStripButtonApplyNamingConventionToSelection.Name="_toolStripButtonApplyNamingConventionToSelection";
			_toolStripButtonApplyNamingConventionToSelection.Size=new System.Drawing.Size(234, 26);
			_toolStripButtonApplyNamingConventionToSelection.Text="Apply Naming Convention to Selection";
			_toolStripButtonApplyNamingConventionToSelection.Click+=_toolStripButtonApplyNamingConventionToSelection_Click;
			// 
			// label1
			// 
			label1.AutoSize=true;
			label1.Location=new System.Drawing.Point(20, 77);
			label1.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			label1.Name="label1";
			label1.Size=new System.Drawing.Size(118, 15);
			label1.TabIndex=25;
			label1.Text="Naming Convention:";
			label1.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxQualityConditionNames
			// 
			_textBoxQualityConditionNames.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxQualityConditionNames.Font=new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			_textBoxQualityConditionNames.Location=new System.Drawing.Point(147, 74);
			_textBoxQualityConditionNames.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxQualityConditionNames.Name="_textBoxQualityConditionNames";
			_textBoxQualityConditionNames.Size=new System.Drawing.Size(520, 20);
			_textBoxQualityConditionNames.TabIndex=2;
			_textBoxQualityConditionNames.TextChanged+=_textBoxQualityConditionNames_TextChanged;
			// 
			// _textBoxTestDescriptorName
			// 
			_textBoxTestDescriptorName.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxTestDescriptorName.Location=new System.Drawing.Point(147, 14);
			_textBoxTestDescriptorName.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxTestDescriptorName.Name="_textBoxTestDescriptorName";
			_textBoxTestDescriptorName.ReadOnly=true;
			_textBoxTestDescriptorName.Size=new System.Drawing.Size(520, 23);
			_textBoxTestDescriptorName.TabIndex=0;
			// 
			// label2
			// 
			label2.AutoSize=true;
			label2.Location=new System.Drawing.Point(44, 17);
			label2.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			label2.Name="label2";
			label2.Size=new System.Drawing.Size(87, 15);
			label2.TabIndex=28;
			label2.Text="Test Descriptor:";
			label2.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _statusStrip
			// 
			_statusStrip.Location=new System.Drawing.Point(0, 652);
			_statusStrip.Name="_statusStrip";
			_statusStrip.Padding=new System.Windows.Forms.Padding(1, 0, 16, 0);
			_statusStrip.Size=new System.Drawing.Size(681, 22);
			_statusStrip.TabIndex=29;
			_statusStrip.Text="statusStrip1";
			// 
			// _groupBoxDatasets
			// 
			_groupBoxDatasets.Controls.Add(_checkBoxExcludeDatasetsUsingThisTest);
			_groupBoxDatasets.Controls.Add(_dataGridView);
			_groupBoxDatasets.Controls.Add(_toolStripElements);
			_groupBoxDatasets.Controls.Add(label3);
			_groupBoxDatasets.Dock=System.Windows.Forms.DockStyle.Fill;
			_groupBoxDatasets.ForeColor=System.Drawing.SystemColors.ControlText;
			_groupBoxDatasets.Location=new System.Drawing.Point(0, 0);
			_groupBoxDatasets.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxDatasets.Name="_groupBoxDatasets";
			_groupBoxDatasets.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxDatasets.Size=new System.Drawing.Size(653, 254);
			_groupBoxDatasets.TabIndex=0;
			_groupBoxDatasets.TabStop=false;
			_groupBoxDatasets.Text="Datasets to create Quality Conditions for";
			// 
			// _checkBoxExcludeDatasetsUsingThisTest
			// 
			_checkBoxExcludeDatasetsUsingThisTest.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Right;
			_checkBoxExcludeDatasetsUsingThisTest.AutoSize=true;
			_checkBoxExcludeDatasetsUsingThisTest.Checked=true;
			_checkBoxExcludeDatasetsUsingThisTest.CheckState=System.Windows.Forms.CheckState.Checked;
			_checkBoxExcludeDatasetsUsingThisTest.Location=new System.Drawing.Point(301, 229);
			_checkBoxExcludeDatasetsUsingThisTest.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_checkBoxExcludeDatasetsUsingThisTest.Name="_checkBoxExcludeDatasetsUsingThisTest";
			_checkBoxExcludeDatasetsUsingThisTest.Size=new System.Drawing.Size(346, 19);
			_checkBoxExcludeDatasetsUsingThisTest.TabIndex=2;
			_checkBoxExcludeDatasetsUsingThisTest.Text="Exclude datasets for which this test descriptor is already used";
			_checkBoxExcludeDatasetsUsingThisTest.UseVisualStyleBackColor=true;
			// 
			// label3
			// 
			label3.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			label3.BackColor=System.Drawing.SystemColors.Info;
			label3.BorderStyle=System.Windows.Forms.BorderStyle.Fixed3D;
			label3.Location=new System.Drawing.Point(7, 191);
			label3.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			label3.Name="label3";
			label3.Size=new System.Drawing.Size(639, 33);
			label3.TabIndex=25;
			label3.Text="To fill in parameter values, select cells in one or more parameter columns and right click to select \"Fill down\"";
			label3.TextAlign=System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label5
			// 
			label5.AutoSize=true;
			label5.Location=new System.Drawing.Point(18, 107);
			label5.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			label5.Name="label5";
			label5.Size=new System.Drawing.Size(114, 15);
			label5.TabIndex=25;
			label5.Text="Supported Variables:";
			label5.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSupportedVariables
			// 
			_textBoxSupportedVariables.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxSupportedVariables.BackColor=System.Drawing.SystemColors.Info;
			_textBoxSupportedVariables.Font=new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			_textBoxSupportedVariables.Location=new System.Drawing.Point(147, 104);
			_textBoxSupportedVariables.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxSupportedVariables.Multiline=true;
			_textBoxSupportedVariables.Name="_textBoxSupportedVariables";
			_textBoxSupportedVariables.ReadOnly=true;
			_textBoxSupportedVariables.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxSupportedVariables.Size=new System.Drawing.Size(520, 42);
			_textBoxSupportedVariables.TabIndex=3;
			_textBoxSupportedVariables.TextChanged+=_textBoxQualityConditionNames_TextChanged;
			// 
			// _groupBoxQualitySpecifications
			// 
			_groupBoxQualitySpecifications.Controls.Add(_dataGridViewQualitySpecifications);
			_groupBoxQualitySpecifications.Controls.Add(_toolStripQualitySpecifications);
			_groupBoxQualitySpecifications.Dock=System.Windows.Forms.DockStyle.Fill;
			_groupBoxQualitySpecifications.Location=new System.Drawing.Point(0, 0);
			_groupBoxQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxQualitySpecifications.Name="_groupBoxQualitySpecifications";
			_groupBoxQualitySpecifications.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxQualitySpecifications.Size=new System.Drawing.Size(653, 186);
			_groupBoxQualitySpecifications.TabIndex=0;
			_groupBoxQualitySpecifications.TabStop=false;
			_groupBoxQualitySpecifications.Text="Quality Specifications";
			// 
			// _dataGridViewQualitySpecifications
			// 
			_dataGridViewQualitySpecifications.AllowUserToAddRows=false;
			_dataGridViewQualitySpecifications.AllowUserToDeleteRows=false;
			_dataGridViewQualitySpecifications.AllowUserToResizeRows=false;
			_dataGridViewQualitySpecifications.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewQualitySpecifications.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _columnName });
			_dataGridViewQualitySpecifications.Dock=System.Windows.Forms.DockStyle.Fill;
			_dataGridViewQualitySpecifications.EditMode=System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			_dataGridViewQualitySpecifications.Location=new System.Drawing.Point(4, 48);
			_dataGridViewQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewQualitySpecifications.Name="_dataGridViewQualitySpecifications";
			_dataGridViewQualitySpecifications.ReadOnly=true;
			_dataGridViewQualitySpecifications.RowHeadersVisible=false;
			_dataGridViewQualitySpecifications.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewQualitySpecifications.Size=new System.Drawing.Size(645, 135);
			_dataGridViewQualitySpecifications.TabIndex=1;
			_dataGridViewQualitySpecifications.SelectionChanged+=_dataGridViewQualitySpecifications_SelectionChanged;
			// 
			// _columnName
			// 
			_columnName.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			_columnName.DataPropertyName="Name";
			_columnName.HeaderText="Quality Specification";
			_columnName.MinimumWidth=200;
			_columnName.Name="_columnName";
			_columnName.ReadOnly=true;
			// 
			// _toolStripQualitySpecifications
			// 
			_toolStripQualitySpecifications.AutoSize=false;
			_toolStripQualitySpecifications.ClickThrough=true;
			_toolStripQualitySpecifications.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripQualitySpecifications.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripButtonRemoveFromQualitySpecifications, _toolStripButtonAssignToQualitySpecifications });
			_toolStripQualitySpecifications.Location=new System.Drawing.Point(4, 19);
			_toolStripQualitySpecifications.Name="_toolStripQualitySpecifications";
			_toolStripQualitySpecifications.RenderMode=System.Windows.Forms.ToolStripRenderMode.System;
			_toolStripQualitySpecifications.Size=new System.Drawing.Size(645, 29);
			_toolStripQualitySpecifications.TabIndex=0;
			_toolStripQualitySpecifications.Text="Element Tools";
			// 
			// _toolStripButtonRemoveFromQualitySpecifications
			// 
			_toolStripButtonRemoveFromQualitySpecifications.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonRemoveFromQualitySpecifications.Image=Properties.Resources.Remove;
			_toolStripButtonRemoveFromQualitySpecifications.Name="_toolStripButtonRemoveFromQualitySpecifications";
			_toolStripButtonRemoveFromQualitySpecifications.Size=new System.Drawing.Size(70, 26);
			_toolStripButtonRemoveFromQualitySpecifications.Text="Remove";
			_toolStripButtonRemoveFromQualitySpecifications.Click+=_toolStripButtonRemoveFromQualitySpecifications_Click;
			// 
			// _toolStripButtonAssignToQualitySpecifications
			// 
			_toolStripButtonAssignToQualitySpecifications.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonAssignToQualitySpecifications.Image=Properties.Resources.Assign;
			_toolStripButtonAssignToQualitySpecifications.Name="_toolStripButtonAssignToQualitySpecifications";
			_toolStripButtonAssignToQualitySpecifications.Size=new System.Drawing.Size(203, 26);
			_toolStripButtonAssignToQualitySpecifications.Text="Assign To Quality Specifications...";
			_toolStripButtonAssignToQualitySpecifications.Click+=_toolStripButtonAssignToQualitySpecifications_Click;
			// 
			// _splitContainer
			// 
			_splitContainer.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_splitContainer.Location=new System.Drawing.Point(14, 158);
			_splitContainer.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainer.Name="_splitContainer";
			_splitContainer.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			_splitContainer.Panel1.Controls.Add(_groupBoxDatasets);
			// 
			// _splitContainer.Panel2
			// 
			_splitContainer.Panel2.Controls.Add(_groupBoxQualitySpecifications);
			_splitContainer.Size=new System.Drawing.Size(653, 445);
			_splitContainer.SplitterDistance=254;
			_splitContainer.SplitterWidth=5;
			_splitContainer.TabIndex=33;
			// 
			// _objectReferenceControlCategory
			// 
			_objectReferenceControlCategory.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_objectReferenceControlCategory.DataSource=null;
			_objectReferenceControlCategory.DisplayMember=null;
			_objectReferenceControlCategory.FindObjectDelegate=null;
			_objectReferenceControlCategory.FormatTextDelegate=null;
			_objectReferenceControlCategory.Location=new System.Drawing.Point(147, 44);
			_objectReferenceControlCategory.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_objectReferenceControlCategory.Name="_objectReferenceControlCategory";
			_objectReferenceControlCategory.ReadOnly=false;
			_objectReferenceControlCategory.Size=new System.Drawing.Size(520, 23);
			_objectReferenceControlCategory.TabIndex=1;
			// 
			// _labelTargetCategory
			// 
			_labelTargetCategory.AutoSize=true;
			_labelTargetCategory.Location=new System.Drawing.Point(40, 47);
			_labelTargetCategory.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelTargetCategory.Name="_labelTargetCategory";
			_labelTargetCategory.Size=new System.Drawing.Size(93, 15);
			_labelTargetCategory.TabIndex=25;
			_labelTargetCategory.Text="Target Category:";
			_labelTargetCategory.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _panelMain
			// 
			_panelMain.AutoScroll=true;
			_panelMain.Controls.Add(label2);
			_panelMain.Controls.Add(_labelTargetCategory);
			_panelMain.Controls.Add(_objectReferenceControlCategory);
			_panelMain.Controls.Add(_textBoxTestDescriptorName);
			_panelMain.Controls.Add(label1);
			_panelMain.Controls.Add(_textBoxQualityConditionNames);
			_panelMain.Controls.Add(label5);
			_panelMain.Controls.Add(_textBoxSupportedVariables);
			_panelMain.Controls.Add(_splitContainer);
			_panelMain.Controls.Add(_buttonOK);
			_panelMain.Controls.Add(_buttonCancel);
			_panelMain.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelMain.Location=new System.Drawing.Point(0, 0);
			_panelMain.Name="_panelMain";
			_panelMain.Size=new System.Drawing.Size(681, 652);
			_panelMain.TabIndex=34;
			// 
			// CreateQualityConditionsForm
			// 
			AcceptButton=_buttonOK;
			AutoScaleDimensions=new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
			CancelButton=_buttonCancel;
			ClientSize=new System.Drawing.Size(681, 674);
			Controls.Add(_panelMain);
			Controls.Add(_statusStrip);
			FormBorderStyle=System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			MinimizeBox=false;
			MinimumSize=new System.Drawing.Size(697, 578);
			Name="CreateQualityConditionsForm";
			ShowIcon=false;
			ShowInTaskbar=false;
			Text="Create Quality Conditions";
			Load+=CreateQualityConditionsForm_Load;
			((System.ComponentModel.ISupportInitialize)_dataGridView).EndInit();
			_toolStripElements.ResumeLayout(false);
			_toolStripElements.PerformLayout();
			_groupBoxDatasets.ResumeLayout(false);
			_groupBoxDatasets.PerformLayout();
			_groupBoxQualitySpecifications.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_dataGridViewQualitySpecifications).EndInit();
			_toolStripQualitySpecifications.ResumeLayout(false);
			_toolStripQualitySpecifications.PerformLayout();
			_splitContainer.Panel1.ResumeLayout(false);
			_splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
			_splitContainer.ResumeLayout(false);
			_panelMain.ResumeLayout(false);
			_panelMain.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
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
		private System.Windows.Forms.Panel _panelMain;
	}
}
