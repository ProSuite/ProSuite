using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.QA.Controls;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	partial class QualityConditionControl
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

				ITestConfigurator old = _propertyGrid.SelectedObject as ITestConfigurator;
				if (old != null)
				{
					old.DataChanged -= configurator_DataChanged;
				}

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
			components=new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			_textBoxName=new System.Windows.Forms.TextBox();
			_labelName=new System.Windows.Forms.Label();
			_textBoxDescription=new System.Windows.Forms.TextBox();
			_labelDescription=new System.Windows.Forms.Label();
			_errorProvider=new System.Windows.Forms.ErrorProvider(components);
			_textBoxDescGrid=new System.Windows.Forms.TextBox();
			_labelTestDescriptor=new System.Windows.Forms.LinkLabel();
			_buttonExport=new System.Windows.Forms.Button();
			_buttonImport=new System.Windows.Forms.Button();
			openFileDialogImport=new System.Windows.Forms.OpenFileDialog();
			saveFileDialogExport=new System.Windows.Forms.SaveFileDialog();
			_tabControlParameterValues=new System.Windows.Forms.TabControl();
			tabPageProperties=new System.Windows.Forms.TabPage();
			_splitContainerProperties=new SplitContainerEx();
			_textBoxDescProps=new System.Windows.Forms.TextBox();
			_panelDescriptionLabel=new System.Windows.Forms.Panel();
			_labelParameterDescription=new System.Windows.Forms.Label();
			_propertyGrid=new System.Windows.Forms.PropertyGrid();
			_tabPageTableView=new System.Windows.Forms.TabPage();
			_splitContainer=new SplitContainerEx();
			_splitContainerHeader=new SplitContainerEx();
			_dataGridViewParamGrid=new DoubleBufferedDataGridView();
			_labelStopOnError=new System.Windows.Forms.Label();
			_labelAllowErrors=new System.Windows.Forms.Label();
			_textBoxStopOnErrorDefault=new System.Windows.Forms.TextBox();
			_textBoxIssueTypeDefault=new System.Windows.Forms.TextBox();
			_labelTestDescriptorDefaultStopOnError=new System.Windows.Forms.Label();
			_labelTestDescriptorDefaultAllowErrors=new System.Windows.Forms.Label();
			_tabControlDetails=new System.Windows.Forms.TabControl();
			_tabPageParameters=new System.Windows.Forms.TabPage();
			_panelParameters=new System.Windows.Forms.Panel();
			_qualityConditionTableViewControlPanel=new System.Windows.Forms.Panel();
			_exportButtonPanel=new System.Windows.Forms.Panel();
			_instanceParameterConfigControl=new InstanceConfig.InstanceParameterConfigControl();
			_tabPageIssueFilters=new System.Windows.Forms.TabPage();
			_issueFilterPanelBottom=new System.Windows.Forms.Panel();
			_textBoxFilterExpression=new System.Windows.Forms.TextBox();
			_labelFilterExpression=new System.Windows.Forms.Label();
			_dataGridViewIssueFilters=new DoubleBufferedDataGridView();
			_dataGridIssueColumnImage=new System.Windows.Forms.DataGridViewImageColumn();
			_dataGridIssueColumnName=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_dataGridIssueColumnType=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_dataGridIssueColumnAlgorithm=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_dataGridIssueColumnCategory=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_dataGridIssueColumnDescription=new System.Windows.Forms.DataGridViewTextBoxColumn();
			toolStripEx1=new ToolStripEx();
			_toolStripLabelIssueFilters=new System.Windows.Forms.ToolStripLabel();
			_toolStripButtonRemoveIssueFilter=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonAddIssueFilter=new System.Windows.Forms.ToolStripButton();
			_tabPageQualitySpecifications=new System.Windows.Forms.TabPage();
			_dataGridViewQualitySpecifications=new DoubleBufferedDataGridView();
			_columnName=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnCategory=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnIssueType=new System.Windows.Forms.DataGridViewComboBoxColumn();
			_columnStopOnError=new System.Windows.Forms.DataGridViewComboBoxColumn();
			_toolStripElements=new ToolStripEx();
			toolStripLabel1=new System.Windows.Forms.ToolStripLabel();
			_toolStripButtonRemoveFromQualitySpecifications=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonAssignToQualitySpecifications=new System.Windows.Forms.ToolStripButton();
			_tabPageOptions=new System.Windows.Forms.TabPage();
			_groupBoxIdentification=new System.Windows.Forms.GroupBox();
			_textBoxUuid=new System.Windows.Forms.TextBox();
			_buttonNewVersionGuid=new System.Windows.Forms.Button();
			_labelUuid=new System.Windows.Forms.Label();
			_labelVersionUuid=new System.Windows.Forms.Label();
			_textBoxVersionUuid=new System.Windows.Forms.TextBox();
			_groupBoxTablesWithoutGeometry=new System.Windows.Forms.GroupBox();
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry=new System.Windows.Forms.CheckBox();
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues=new System.Windows.Forms.CheckBox();
			_tabPageNotes=new System.Windows.Forms.TabPage();
			_textBoxNotes=new System.Windows.Forms.TextBox();
			_textBoxQualitySpecifications=new System.Windows.Forms.TextBox();
			_labelQualitySpecifications=new System.Windows.Forms.Label();
			_textBoxUrl=new System.Windows.Forms.TextBox();
			_labelUrl=new System.Windows.Forms.Label();
			_buttonOpenUrl=new System.Windows.Forms.Button();
			_textBoxCategory=new System.Windows.Forms.TextBox();
			_labelCategory=new System.Windows.Forms.Label();
			_toolTip=new System.Windows.Forms.ToolTip(components);
			_nullableBooleanComboboxIssueType=new NullableBooleanCombobox();
			_nullableBooleanComboboxStopOnError=new NullableBooleanCombobox();
			_objectReferenceControlTestDescriptor=new ObjectReferenceControl();
			dataGridViewTextBoxColumn1=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_panelParametersTop=new System.Windows.Forms.Panel();
			_linkDocumentation=new System.Windows.Forms.LinkLabel();
			_panelParametersEdit=new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)_errorProvider).BeginInit();
			_tabControlParameterValues.SuspendLayout();
			tabPageProperties.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerProperties).BeginInit();
			_splitContainerProperties.Panel1.SuspendLayout();
			_splitContainerProperties.Panel2.SuspendLayout();
			_splitContainerProperties.SuspendLayout();
			_panelDescriptionLabel.SuspendLayout();
			_tabPageTableView.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
			_splitContainer.Panel1.SuspendLayout();
			_splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerHeader).BeginInit();
			_splitContainerHeader.Panel1.SuspendLayout();
			_splitContainerHeader.Panel2.SuspendLayout();
			_splitContainerHeader.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewParamGrid).BeginInit();
			_tabControlDetails.SuspendLayout();
			_tabPageParameters.SuspendLayout();
			_panelParameters.SuspendLayout();
			_qualityConditionTableViewControlPanel.SuspendLayout();
			_exportButtonPanel.SuspendLayout();
			_tabPageIssueFilters.SuspendLayout();
			_issueFilterPanelBottom.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewIssueFilters).BeginInit();
			toolStripEx1.SuspendLayout();
			_tabPageQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewQualitySpecifications).BeginInit();
			_toolStripElements.SuspendLayout();
			_tabPageOptions.SuspendLayout();
			_groupBoxIdentification.SuspendLayout();
			_groupBoxTablesWithoutGeometry.SuspendLayout();
			_tabPageNotes.SuspendLayout();
			_panelParametersTop.SuspendLayout();
			SuspendLayout();
			// 
			// _textBoxName
			// 
			_textBoxName.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxName.Location=new System.Drawing.Point(111, 15);
			_textBoxName.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxName.Name="_textBoxName";
			_textBoxName.Size=new System.Drawing.Size(267, 23);
			_textBoxName.TabIndex=0;
			_toolTip.SetToolTip(_textBoxName, "Press TAB to suggest a name");
			_textBoxName.PreviewKeyDown+=_textBoxName_PreviewKeyDown;
			// 
			// _labelName
			// 
			_labelName.AutoSize=true;
			_labelName.Location=new System.Drawing.Point(59, 18);
			_labelName.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelName.Name="_labelName";
			_labelName.Size=new System.Drawing.Size(42, 15);
			_labelName.TabIndex=1;
			_labelName.Text="Name:";
			_labelName.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			_textBoxDescription.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxDescription.Location=new System.Drawing.Point(111, 46);
			_textBoxDescription.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescription.Multiline=true;
			_textBoxDescription.Name="_textBoxDescription";
			_textBoxDescription.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxDescription.Size=new System.Drawing.Size(582, 56);
			_textBoxDescription.TabIndex=2;
			// 
			// _labelDescription
			// 
			_labelDescription.AutoSize=true;
			_labelDescription.Location=new System.Drawing.Point(30, 50);
			_labelDescription.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelDescription.Name="_labelDescription";
			_labelDescription.Size=new System.Drawing.Size(70, 15);
			_labelDescription.TabIndex=3;
			_labelDescription.Text="Description:";
			_labelDescription.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _errorProvider
			// 
			_errorProvider.BlinkStyle=System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			_errorProvider.ContainerControl=this;
			// 
			// _textBoxDescGrid
			// 
			_textBoxDescGrid.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxDescGrid.Location=new System.Drawing.Point(0, 0);
			_textBoxDescGrid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescGrid.Multiline=true;
			_textBoxDescGrid.Name="_textBoxDescGrid";
			_textBoxDescGrid.ReadOnly=true;
			_textBoxDescGrid.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxDescGrid.Size=new System.Drawing.Size(674, 48);
			_textBoxDescGrid.TabIndex=21;
			// 
			// _labelTestDescriptor
			// 
			_labelTestDescriptor.AutoSize=true;
			_labelTestDescriptor.Location=new System.Drawing.Point(8, 209);
			_labelTestDescriptor.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelTestDescriptor.Name="_labelTestDescriptor";
			_labelTestDescriptor.Size=new System.Drawing.Size(87, 15);
			_labelTestDescriptor.TabIndex=9;
			_labelTestDescriptor.TabStop=true;
			_labelTestDescriptor.Text="Test Descriptor:";
			_labelTestDescriptor.TextAlign=System.Drawing.ContentAlignment.TopRight;
			_labelTestDescriptor.LinkClicked+=_labelTestDescriptor_LinkClicked;
			// 
			// _buttonExport
			// 
			_buttonExport.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_buttonExport.Location=new System.Drawing.Point(506, 3);
			_buttonExport.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonExport.Name="_buttonExport";
			_buttonExport.Size=new System.Drawing.Size(88, 27);
			_buttonExport.TabIndex=5;
			_buttonExport.Text="Export";
			_buttonExport.UseVisualStyleBackColor=true;
			_buttonExport.Click+=_buttonExport_Click;
			// 
			// _buttonImport
			// 
			_buttonImport.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_buttonImport.Location=new System.Drawing.Point(602, 3);
			_buttonImport.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonImport.Name="_buttonImport";
			_buttonImport.Size=new System.Drawing.Size(88, 27);
			_buttonImport.TabIndex=6;
			_buttonImport.Text="Import";
			_buttonImport.UseVisualStyleBackColor=true;
			_buttonImport.Click+=_buttonImport_Click;
			// 
			// _tabControlParameterValues
			// 
			_tabControlParameterValues.Controls.Add(tabPageProperties);
			_tabControlParameterValues.Controls.Add(_tabPageTableView);
			_tabControlParameterValues.Dock=System.Windows.Forms.DockStyle.Fill;
			_tabControlParameterValues.Location=new System.Drawing.Point(0, 0);
			_tabControlParameterValues.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabControlParameterValues.Name="_tabControlParameterValues";
			_tabControlParameterValues.SelectedIndex=0;
			_tabControlParameterValues.Size=new System.Drawing.Size(690, 345);
			_tabControlParameterValues.TabIndex=28;
			_tabControlParameterValues.SelectedIndexChanged+=_tabControlParameterValues_SelectedIndexChanged;
			// 
			// tabPageProperties
			// 
			tabPageProperties.Controls.Add(_splitContainerProperties);
			tabPageProperties.Location=new System.Drawing.Point(4, 24);
			tabPageProperties.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			tabPageProperties.Name="tabPageProperties";
			tabPageProperties.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			tabPageProperties.Size=new System.Drawing.Size(682, 317);
			tabPageProperties.TabIndex=0;
			tabPageProperties.Text="Parameter Values";
			tabPageProperties.UseVisualStyleBackColor=true;
			// 
			// _splitContainerProperties
			// 
			_splitContainerProperties.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainerProperties.FixedPanel=System.Windows.Forms.FixedPanel.Panel1;
			_splitContainerProperties.Location=new System.Drawing.Point(4, 3);
			_splitContainerProperties.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainerProperties.Name="_splitContainerProperties";
			_splitContainerProperties.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerProperties.Panel1
			// 
			_splitContainerProperties.Panel1.Controls.Add(_textBoxDescProps);
			_splitContainerProperties.Panel1.Controls.Add(_panelDescriptionLabel);
			// 
			// _splitContainerProperties.Panel2
			// 
			_splitContainerProperties.Panel2.Controls.Add(_propertyGrid);
			_splitContainerProperties.Size=new System.Drawing.Size(674, 311);
			_splitContainerProperties.SplitterDistance=74;
			_splitContainerProperties.SplitterWidth=5;
			_splitContainerProperties.TabIndex=31;
			// 
			// _textBoxDescProps
			// 
			_textBoxDescProps.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxDescProps.Location=new System.Drawing.Point(86, 0);
			_textBoxDescProps.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescProps.Multiline=true;
			_textBoxDescProps.Name="_textBoxDescProps";
			_textBoxDescProps.ReadOnly=true;
			_textBoxDescProps.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxDescProps.Size=new System.Drawing.Size(588, 74);
			_textBoxDescProps.TabIndex=0;
			// 
			// _panelDescriptionLabel
			// 
			_panelDescriptionLabel.Controls.Add(_labelParameterDescription);
			_panelDescriptionLabel.Dock=System.Windows.Forms.DockStyle.Left;
			_panelDescriptionLabel.Location=new System.Drawing.Point(0, 0);
			_panelDescriptionLabel.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_panelDescriptionLabel.MaximumSize=new System.Drawing.Size(86, 11538);
			_panelDescriptionLabel.MinimumSize=new System.Drawing.Size(86, 0);
			_panelDescriptionLabel.Name="_panelDescriptionLabel";
			_panelDescriptionLabel.Size=new System.Drawing.Size(86, 74);
			_panelDescriptionLabel.TabIndex=23;
			// 
			// _labelParameterDescription
			// 
			_labelParameterDescription.AutoSize=true;
			_labelParameterDescription.Location=new System.Drawing.Point(6, 3);
			_labelParameterDescription.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelParameterDescription.Name="_labelParameterDescription";
			_labelParameterDescription.Size=new System.Drawing.Size(70, 15);
			_labelParameterDescription.TabIndex=22;
			_labelParameterDescription.Text="Description:";
			_labelParameterDescription.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _propertyGrid
			// 
			_propertyGrid.Dock=System.Windows.Forms.DockStyle.Fill;
			_propertyGrid.Location=new System.Drawing.Point(0, 0);
			_propertyGrid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_propertyGrid.Name="_propertyGrid";
			_propertyGrid.PropertySort=System.Windows.Forms.PropertySort.NoSort;
			_propertyGrid.Size=new System.Drawing.Size(674, 232);
			_propertyGrid.TabIndex=0;
			_propertyGrid.PropertyValueChanged+=_propertyGrid_PropertyValueChanged;
			// 
			// _tabPageTableView
			// 
			_tabPageTableView.Controls.Add(_splitContainer);
			_tabPageTableView.Location=new System.Drawing.Point(4, 24);
			_tabPageTableView.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageTableView.Name="_tabPageTableView";
			_tabPageTableView.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageTableView.Size=new System.Drawing.Size(682, 317);
			_tabPageTableView.TabIndex=1;
			_tabPageTableView.Text="Table View";
			_tabPageTableView.UseVisualStyleBackColor=true;
			// 
			// _splitContainer
			// 
			_splitContainer.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainer.Location=new System.Drawing.Point(4, 3);
			_splitContainer.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainer.Name="_splitContainer";
			_splitContainer.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			_splitContainer.Panel1.Controls.Add(_splitContainerHeader);
			_splitContainer.Size=new System.Drawing.Size(674, 311);
			_splitContainer.SplitterDistance=169;
			_splitContainer.TabIndex=30;
			// 
			// _splitContainerHeader
			// 
			_splitContainerHeader.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainerHeader.FixedPanel=System.Windows.Forms.FixedPanel.Panel1;
			_splitContainerHeader.Location=new System.Drawing.Point(0, 0);
			_splitContainerHeader.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainerHeader.Name="_splitContainerHeader";
			_splitContainerHeader.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerHeader.Panel1
			// 
			_splitContainerHeader.Panel1.Controls.Add(_textBoxDescGrid);
			// 
			// _splitContainerHeader.Panel2
			// 
			_splitContainerHeader.Panel2.Controls.Add(_dataGridViewParamGrid);
			_splitContainerHeader.Panel2MinSize=50;
			_splitContainerHeader.Size=new System.Drawing.Size(674, 169);
			_splitContainerHeader.SplitterDistance=48;
			_splitContainerHeader.SplitterWidth=5;
			_splitContainerHeader.TabIndex=26;
			// 
			// _dataGridViewParamGrid
			// 
			_dataGridViewParamGrid.AllowUserToAddRows=false;
			_dataGridViewParamGrid.AllowUserToDeleteRows=false;
			_dataGridViewParamGrid.AllowUserToResizeRows=false;
			_dataGridViewParamGrid.AutoSizeColumnsMode=System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			_dataGridViewParamGrid.AutoSizeRowsMode=System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			_dataGridViewParamGrid.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle1.Alignment=System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor=System.Drawing.SystemColors.ControlLight;
			dataGridViewCellStyle1.Font=new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle1.ForeColor=System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.Padding=new System.Windows.Forms.Padding(0, 2, 0, 2);
			dataGridViewCellStyle1.SelectionBackColor=System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor=System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode=System.Windows.Forms.DataGridViewTriState.False;
			_dataGridViewParamGrid.DefaultCellStyle=dataGridViewCellStyle1;
			_dataGridViewParamGrid.Dock=System.Windows.Forms.DockStyle.Fill;
			_dataGridViewParamGrid.Location=new System.Drawing.Point(0, 0);
			_dataGridViewParamGrid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewParamGrid.Name="_dataGridViewParamGrid";
			_dataGridViewParamGrid.ReadOnly=true;
			_dataGridViewParamGrid.RowHeadersVisible=false;
			_dataGridViewParamGrid.RowHeadersWidth=62;
			_dataGridViewParamGrid.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewParamGrid.Size=new System.Drawing.Size(674, 116);
			_dataGridViewParamGrid.TabIndex=24;
			_dataGridViewParamGrid.DataBindingComplete+=_dataGridViewParamGrid_DataBindingComplete;
			// 
			// _labelStopOnError
			// 
			_labelStopOnError.AutoSize=true;
			_labelStopOnError.Location=new System.Drawing.Point(20, 175);
			_labelStopOnError.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelStopOnError.Name="_labelStopOnError";
			_labelStopOnError.Size=new System.Drawing.Size(79, 15);
			_labelStopOnError.TabIndex=32;
			_labelStopOnError.Text="Stop on error:";
			_labelStopOnError.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAllowErrors
			// 
			_labelAllowErrors.AutoSize=true;
			_labelAllowErrors.Location=new System.Drawing.Point(36, 143);
			_labelAllowErrors.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelAllowErrors.Name="_labelAllowErrors";
			_labelAllowErrors.Size=new System.Drawing.Size(62, 15);
			_labelAllowErrors.TabIndex=32;
			_labelAllowErrors.Text="Issue type:";
			_labelAllowErrors.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxStopOnErrorDefault
			// 
			_textBoxStopOnErrorDefault.Location=new System.Drawing.Point(383, 172);
			_textBoxStopOnErrorDefault.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxStopOnErrorDefault.Name="_textBoxStopOnErrorDefault";
			_textBoxStopOnErrorDefault.ReadOnly=true;
			_textBoxStopOnErrorDefault.Size=new System.Drawing.Size(65, 23);
			_textBoxStopOnErrorDefault.TabIndex=8;
			_textBoxStopOnErrorDefault.TabStop=false;
			// 
			// _textBoxIssueTypeDefault
			// 
			_textBoxIssueTypeDefault.Location=new System.Drawing.Point(383, 140);
			_textBoxIssueTypeDefault.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxIssueTypeDefault.Name="_textBoxIssueTypeDefault";
			_textBoxIssueTypeDefault.ReadOnly=true;
			_textBoxIssueTypeDefault.Size=new System.Drawing.Size(65, 23);
			_textBoxIssueTypeDefault.TabIndex=6;
			_textBoxIssueTypeDefault.TabStop=false;
			// 
			// _labelTestDescriptorDefaultStopOnError
			// 
			_labelTestDescriptorDefaultStopOnError.AutoSize=true;
			_labelTestDescriptorDefaultStopOnError.Location=new System.Drawing.Point(230, 175);
			_labelTestDescriptorDefaultStopOnError.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelTestDescriptorDefaultStopOnError.Name="_labelTestDescriptorDefaultStopOnError";
			_labelTestDescriptorDefaultStopOnError.Size=new System.Drawing.Size(136, 15);
			_labelTestDescriptorDefaultStopOnError.TabIndex=34;
			_labelTestDescriptorDefaultStopOnError.Text="Default (Test Descriptor):";
			_labelTestDescriptorDefaultStopOnError.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelTestDescriptorDefaultAllowErrors
			// 
			_labelTestDescriptorDefaultAllowErrors.AutoSize=true;
			_labelTestDescriptorDefaultAllowErrors.Location=new System.Drawing.Point(230, 143);
			_labelTestDescriptorDefaultAllowErrors.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelTestDescriptorDefaultAllowErrors.Name="_labelTestDescriptorDefaultAllowErrors";
			_labelTestDescriptorDefaultAllowErrors.Size=new System.Drawing.Size(136, 15);
			_labelTestDescriptorDefaultAllowErrors.TabIndex=34;
			_labelTestDescriptorDefaultAllowErrors.Text="Default (Test Descriptor):";
			_labelTestDescriptorDefaultAllowErrors.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControlDetails
			// 
			_tabControlDetails.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_tabControlDetails.Controls.Add(_tabPageParameters);
			_tabControlDetails.Controls.Add(_tabPageIssueFilters);
			_tabControlDetails.Controls.Add(_tabPageQualitySpecifications);
			_tabControlDetails.Controls.Add(_tabPageOptions);
			_tabControlDetails.Controls.Add(_tabPageNotes);
			_tabControlDetails.Location=new System.Drawing.Point(10, 267);
			_tabControlDetails.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabControlDetails.Name="_tabControlDetails";
			_tabControlDetails.SelectedIndex=0;
			_tabControlDetails.Size=new System.Drawing.Size(706, 414);
			_tabControlDetails.TabIndex=0;
			_tabControlDetails.SelectedIndexChanged+=_tabControlDetails_SelectedIndexChanged;
			// 
			// _tabPageParameters
			// 
			_tabPageParameters.Controls.Add(_panelParameters);
			_tabPageParameters.Location=new System.Drawing.Point(4, 24);
			_tabPageParameters.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageParameters.Name="_tabPageParameters";
			_tabPageParameters.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageParameters.Size=new System.Drawing.Size(698, 386);
			_tabPageParameters.TabIndex=0;
			_tabPageParameters.Text="Test Parameters";
			// 
			// _panelParameters
			// 
			_panelParameters.BackColor=System.Drawing.SystemColors.ControlLightLight;
			_panelParameters.Controls.Add(_qualityConditionTableViewControlPanel);
			_panelParameters.Controls.Add(_exportButtonPanel);
			_panelParameters.Controls.Add(_instanceParameterConfigControl);
			_panelParameters.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelParameters.Location=new System.Drawing.Point(4, 3);
			_panelParameters.Name="_panelParameters";
			_panelParameters.Size=new System.Drawing.Size(690, 380);
			_panelParameters.TabIndex=31;
			// 
			// _qualityConditionTableViewControlPanel
			// 
			_qualityConditionTableViewControlPanel.Controls.Add(_tabControlParameterValues);
			_qualityConditionTableViewControlPanel.Dock=System.Windows.Forms.DockStyle.Fill;
			_qualityConditionTableViewControlPanel.Location=new System.Drawing.Point(0, 35);
			_qualityConditionTableViewControlPanel.Name="_qualityConditionTableViewControlPanel";
			_qualityConditionTableViewControlPanel.Size=new System.Drawing.Size(690, 345);
			_qualityConditionTableViewControlPanel.TabIndex=29;
			// 
			// _exportButtonPanel
			// 
			_exportButtonPanel.Controls.Add(_buttonExport);
			_exportButtonPanel.Controls.Add(_buttonImport);
			_exportButtonPanel.Dock=System.Windows.Forms.DockStyle.Top;
			_exportButtonPanel.Location=new System.Drawing.Point(0, 0);
			_exportButtonPanel.Name="_exportButtonPanel";
			_exportButtonPanel.Size=new System.Drawing.Size(690, 35);
			_exportButtonPanel.TabIndex=30;
			// 
			// _instanceParameterConfigControl
			// 
			_instanceParameterConfigControl.Dock=System.Windows.Forms.DockStyle.Fill;
			_instanceParameterConfigControl.Location=new System.Drawing.Point(0, 0);
			_instanceParameterConfigControl.Name="_instanceParameterConfigControl";
			_instanceParameterConfigControl.Size=new System.Drawing.Size(690, 380);
			_instanceParameterConfigControl.TabIndex=7;
			_instanceParameterConfigControl.Visible=false;
			_instanceParameterConfigControl.DocumentationLinkClicked+=_instanceParameterConfigControl_DocumentationLinkClicked;
			// 
			// _tabPageIssueFilters
			// 
			_tabPageIssueFilters.Controls.Add(_issueFilterPanelBottom);
			_tabPageIssueFilters.Controls.Add(_dataGridViewIssueFilters);
			_tabPageIssueFilters.Controls.Add(toolStripEx1);
			_tabPageIssueFilters.Location=new System.Drawing.Point(4, 24);
			_tabPageIssueFilters.Margin=new System.Windows.Forms.Padding(2);
			_tabPageIssueFilters.Name="_tabPageIssueFilters";
			_tabPageIssueFilters.Size=new System.Drawing.Size(698, 386);
			_tabPageIssueFilters.TabIndex=4;
			_tabPageIssueFilters.Text="Issue Filters";
			_tabPageIssueFilters.UseVisualStyleBackColor=true;
			// 
			// _issueFilterPanelBottom
			// 
			_issueFilterPanelBottom.Controls.Add(_textBoxFilterExpression);
			_issueFilterPanelBottom.Controls.Add(_labelFilterExpression);
			_issueFilterPanelBottom.Dock=System.Windows.Forms.DockStyle.Bottom;
			_issueFilterPanelBottom.Location=new System.Drawing.Point(0, 339);
			_issueFilterPanelBottom.Name="_issueFilterPanelBottom";
			_issueFilterPanelBottom.Size=new System.Drawing.Size(698, 47);
			_issueFilterPanelBottom.TabIndex=28;
			// 
			// _textBoxFilterExpression
			// 
			_textBoxFilterExpression.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxFilterExpression.Location=new System.Drawing.Point(108, 12);
			_textBoxFilterExpression.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxFilterExpression.Name="_textBoxFilterExpression";
			_textBoxFilterExpression.Size=new System.Drawing.Size(570, 23);
			_textBoxFilterExpression.TabIndex=1;
			// 
			// _labelFilterExpression
			// 
			_labelFilterExpression.AutoSize=true;
			_labelFilterExpression.Location=new System.Drawing.Point(6, 15);
			_labelFilterExpression.Name="_labelFilterExpression";
			_labelFilterExpression.Size=new System.Drawing.Size(95, 15);
			_labelFilterExpression.TabIndex=0;
			_labelFilterExpression.Text="Filter Expression:";
			// 
			// _dataGridViewIssueFilters
			// 
			_dataGridViewIssueFilters.AllowUserToAddRows=false;
			_dataGridViewIssueFilters.AllowUserToDeleteRows=false;
			_dataGridViewIssueFilters.AllowUserToResizeRows=false;
			_dataGridViewIssueFilters.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewIssueFilters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _dataGridIssueColumnImage, _dataGridIssueColumnName, _dataGridIssueColumnType, _dataGridIssueColumnAlgorithm, _dataGridIssueColumnCategory, _dataGridIssueColumnDescription });
			_dataGridViewIssueFilters.Dock=System.Windows.Forms.DockStyle.Fill;
			_dataGridViewIssueFilters.EditMode=System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			_dataGridViewIssueFilters.Location=new System.Drawing.Point(0, 29);
			_dataGridViewIssueFilters.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewIssueFilters.Name="_dataGridViewIssueFilters";
			_dataGridViewIssueFilters.RowHeadersVisible=false;
			_dataGridViewIssueFilters.RowHeadersWidth=62;
			_dataGridViewIssueFilters.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewIssueFilters.Size=new System.Drawing.Size(698, 357);
			_dataGridViewIssueFilters.TabIndex=27;
			_dataGridViewIssueFilters.CellDoubleClick+=_dataGridViewIssueFilters_CellDoubleClick;
			_dataGridViewIssueFilters.CellEndEdit+=_dataGridViewIssueFilters_CellEndEdit;
			_dataGridViewIssueFilters.CellValueChanged+=_dataGridViewIssueFilters_CellValueChanged;
			// 
			// _dataGridIssueColumnImage
			// 
			_dataGridIssueColumnImage.DataPropertyName="Image";
			_dataGridIssueColumnImage.HeaderText="";
			_dataGridIssueColumnImage.MinimumWidth=20;
			_dataGridIssueColumnImage.Name="_dataGridIssueColumnImage";
			_dataGridIssueColumnImage.ReadOnly=true;
			_dataGridIssueColumnImage.Resizable=System.Windows.Forms.DataGridViewTriState.False;
			_dataGridIssueColumnImage.Width=30;
			// 
			// _dataGridIssueColumnName
			// 
			_dataGridIssueColumnName.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_dataGridIssueColumnName.DataPropertyName="Name";
			_dataGridIssueColumnName.HeaderText="Name";
			_dataGridIssueColumnName.MinimumWidth=200;
			_dataGridIssueColumnName.Name="_dataGridIssueColumnName";
			_dataGridIssueColumnName.ReadOnly=true;
			_dataGridIssueColumnName.Width=200;
			// 
			// _dataGridIssueColumnType
			// 
			_dataGridIssueColumnType.DataPropertyName="Type";
			_dataGridIssueColumnType.HeaderText="Type";
			_dataGridIssueColumnType.MinimumWidth=8;
			_dataGridIssueColumnType.Name="_dataGridIssueColumnType";
			_dataGridIssueColumnType.ReadOnly=true;
			_dataGridIssueColumnType.Width=180;
			// 
			// _dataGridIssueColumnAlgorithm
			// 
			_dataGridIssueColumnAlgorithm.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_dataGridIssueColumnAlgorithm.DataPropertyName="AlgorithmImplementation";
			_dataGridIssueColumnAlgorithm.HeaderText="Algorithm";
			_dataGridIssueColumnAlgorithm.MinimumWidth=8;
			_dataGridIssueColumnAlgorithm.Name="_dataGridIssueColumnAlgorithm";
			_dataGridIssueColumnAlgorithm.Width=86;
			// 
			// _dataGridIssueColumnCategory
			// 
			_dataGridIssueColumnCategory.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_dataGridIssueColumnCategory.DataPropertyName="Category";
			_dataGridIssueColumnCategory.HeaderText="Category";
			_dataGridIssueColumnCategory.MinimumWidth=50;
			_dataGridIssueColumnCategory.Name="_dataGridIssueColumnCategory";
			_dataGridIssueColumnCategory.ReadOnly=true;
			_dataGridIssueColumnCategory.Width=80;
			// 
			// _dataGridIssueColumnDescription
			// 
			_dataGridIssueColumnDescription.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			_dataGridIssueColumnDescription.DataPropertyName="Description";
			_dataGridIssueColumnDescription.HeaderText="Description";
			_dataGridIssueColumnDescription.MinimumWidth=8;
			_dataGridIssueColumnDescription.Name="_dataGridIssueColumnDescription";
			// 
			// toolStripEx1
			// 
			toolStripEx1.AutoSize=false;
			toolStripEx1.ClickThrough=true;
			toolStripEx1.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			toolStripEx1.ImageScalingSize=new System.Drawing.Size(24, 24);
			toolStripEx1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripLabelIssueFilters, _toolStripButtonRemoveIssueFilter, _toolStripButtonAddIssueFilter });
			toolStripEx1.Location=new System.Drawing.Point(0, 0);
			toolStripEx1.Name="toolStripEx1";
			toolStripEx1.Padding=new System.Windows.Forms.Padding(0, 0, 2, 0);
			toolStripEx1.Size=new System.Drawing.Size(698, 29);
			toolStripEx1.TabIndex=26;
			toolStripEx1.Text="Element Tools";
			// 
			// _toolStripLabelIssueFilters
			// 
			_toolStripLabelIssueFilters.Name="_toolStripLabelIssueFilters";
			_toolStripLabelIssueFilters.Size=new System.Drawing.Size(67, 26);
			_toolStripLabelIssueFilters.Text="Issue Filters";
			// 
			// _toolStripButtonRemoveIssueFilter
			// 
			_toolStripButtonRemoveIssueFilter.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonRemoveIssueFilter.Image=Properties.Resources.Remove;
			_toolStripButtonRemoveIssueFilter.Name="_toolStripButtonRemoveIssueFilter";
			_toolStripButtonRemoveIssueFilter.Size=new System.Drawing.Size(78, 26);
			_toolStripButtonRemoveIssueFilter.Text="Remove";
			_toolStripButtonRemoveIssueFilter.Click+=_toolStripButtonRemoveIssueFilter_Click;
			// 
			// _toolStripButtonAddIssueFilter
			// 
			_toolStripButtonAddIssueFilter.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonAddIssueFilter.Image=Properties.Resources.Assign;
			_toolStripButtonAddIssueFilter.Name="_toolStripButtonAddIssueFilter";
			_toolStripButtonAddIssueFilter.Size=new System.Drawing.Size(66, 26);
			_toolStripButtonAddIssueFilter.Text="Add...";
			_toolStripButtonAddIssueFilter.Click+=_toolStripButtonAddIssueFilter_Click;
			// 
			// _tabPageQualitySpecifications
			// 
			_tabPageQualitySpecifications.Controls.Add(_dataGridViewQualitySpecifications);
			_tabPageQualitySpecifications.Controls.Add(_toolStripElements);
			_tabPageQualitySpecifications.Location=new System.Drawing.Point(4, 24);
			_tabPageQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageQualitySpecifications.Name="_tabPageQualitySpecifications";
			_tabPageQualitySpecifications.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageQualitySpecifications.Size=new System.Drawing.Size(698, 386);
			_tabPageQualitySpecifications.TabIndex=1;
			_tabPageQualitySpecifications.Text="Quality Specifications";
			// 
			// _dataGridViewQualitySpecifications
			// 
			_dataGridViewQualitySpecifications.AllowUserToAddRows=false;
			_dataGridViewQualitySpecifications.AllowUserToDeleteRows=false;
			_dataGridViewQualitySpecifications.AllowUserToResizeRows=false;
			_dataGridViewQualitySpecifications.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewQualitySpecifications.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _columnName, _columnCategory, _columnIssueType, _columnStopOnError });
			_dataGridViewQualitySpecifications.Dock=System.Windows.Forms.DockStyle.Fill;
			_dataGridViewQualitySpecifications.EditMode=System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			_dataGridViewQualitySpecifications.Location=new System.Drawing.Point(4, 32);
			_dataGridViewQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewQualitySpecifications.Name="_dataGridViewQualitySpecifications";
			_dataGridViewQualitySpecifications.RowHeadersVisible=false;
			_dataGridViewQualitySpecifications.RowHeadersWidth=62;
			_dataGridViewQualitySpecifications.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewQualitySpecifications.Size=new System.Drawing.Size(690, 351);
			_dataGridViewQualitySpecifications.TabIndex=24;
			_dataGridViewQualitySpecifications.CellDoubleClick+=_dataGridViewQualitySpecifications_CellDoubleClick;
			_dataGridViewQualitySpecifications.CellEndEdit+=_dataGridViewQualitySpecifications_CellEndEdit;
			_dataGridViewQualitySpecifications.CellValueChanged+=_dataGridViewQualitySpecifications_CellValueChanged;
			// 
			// _columnName
			// 
			_columnName.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_columnName.DataPropertyName="QualitySpecificationName";
			_columnName.HeaderText="Quality Specification";
			_columnName.MinimumWidth=200;
			_columnName.Name="_columnName";
			_columnName.ReadOnly=true;
			_columnName.Width=200;
			// 
			// _columnCategory
			// 
			_columnCategory.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			_columnCategory.DataPropertyName="Category";
			_columnCategory.HeaderText="Category";
			_columnCategory.MinimumWidth=50;
			_columnCategory.Name="_columnCategory";
			_columnCategory.ReadOnly=true;
			// 
			// _columnIssueType
			// 
			_columnIssueType.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_columnIssueType.DataPropertyName="AllowErrorsOverride";
			_columnIssueType.DisplayStyleForCurrentCellOnly=true;
			_columnIssueType.HeaderText="Issue Type";
			_columnIssueType.Items.AddRange(new object[] { "Warning", "Error", "Default" });
			_columnIssueType.MinimumWidth=87;
			_columnIssueType.Name="_columnIssueType";
			_columnIssueType.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			_columnIssueType.Width=87;
			// 
			// _columnStopOnError
			// 
			_columnStopOnError.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			_columnStopOnError.DataPropertyName="StopOnErrorOverride";
			_columnStopOnError.DisplayStyleForCurrentCellOnly=true;
			_columnStopOnError.HeaderText="Stop On Error";
			_columnStopOnError.Items.AddRange(new object[] { "Yes", "No", "Default" });
			_columnStopOnError.MinimumWidth=96;
			_columnStopOnError.Name="_columnStopOnError";
			_columnStopOnError.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			_columnStopOnError.Width=96;
			// 
			// _toolStripElements
			// 
			_toolStripElements.AutoSize=false;
			_toolStripElements.ClickThrough=true;
			_toolStripElements.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripElements.ImageScalingSize=new System.Drawing.Size(24, 24);
			_toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, _toolStripButtonRemoveFromQualitySpecifications, _toolStripButtonAssignToQualitySpecifications });
			_toolStripElements.Location=new System.Drawing.Point(4, 3);
			_toolStripElements.Name="_toolStripElements";
			_toolStripElements.Padding=new System.Windows.Forms.Padding(0, 0, 2, 0);
			_toolStripElements.Size=new System.Drawing.Size(690, 29);
			_toolStripElements.TabIndex=25;
			_toolStripElements.Text="Element Tools";
			// 
			// toolStripLabel1
			// 
			toolStripLabel1.Name="toolStripLabel1";
			toolStripLabel1.Size=new System.Drawing.Size(121, 26);
			toolStripLabel1.Text="Quality Specifications";
			// 
			// _toolStripButtonRemoveFromQualitySpecifications
			// 
			_toolStripButtonRemoveFromQualitySpecifications.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonRemoveFromQualitySpecifications.Image=Properties.Resources.Remove;
			_toolStripButtonRemoveFromQualitySpecifications.Name="_toolStripButtonRemoveFromQualitySpecifications";
			_toolStripButtonRemoveFromQualitySpecifications.Size=new System.Drawing.Size(78, 26);
			_toolStripButtonRemoveFromQualitySpecifications.Text="Remove";
			_toolStripButtonRemoveFromQualitySpecifications.Click+=_toolStripButtonRemoveQualityConditions_Click;
			// 
			// _toolStripButtonAssignToQualitySpecifications
			// 
			_toolStripButtonAssignToQualitySpecifications.Alignment=System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonAssignToQualitySpecifications.Image=Properties.Resources.Assign;
			_toolStripButtonAssignToQualitySpecifications.Name="_toolStripButtonAssignToQualitySpecifications";
			_toolStripButtonAssignToQualitySpecifications.Size=new System.Drawing.Size(211, 26);
			_toolStripButtonAssignToQualitySpecifications.Text="Assign To Quality Specifications...";
			_toolStripButtonAssignToQualitySpecifications.Click+=_toolStripButtonAssignToQualitySpecifications_Click;
			// 
			// _tabPageOptions
			// 
			_tabPageOptions.Controls.Add(_groupBoxIdentification);
			_tabPageOptions.Controls.Add(_groupBoxTablesWithoutGeometry);
			_tabPageOptions.Location=new System.Drawing.Point(4, 24);
			_tabPageOptions.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageOptions.Name="_tabPageOptions";
			_tabPageOptions.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageOptions.Size=new System.Drawing.Size(698, 386);
			_tabPageOptions.TabIndex=2;
			_tabPageOptions.Text="Options";
			_tabPageOptions.UseVisualStyleBackColor=true;
			// 
			// _groupBoxIdentification
			// 
			_groupBoxIdentification.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_groupBoxIdentification.Controls.Add(_textBoxUuid);
			_groupBoxIdentification.Controls.Add(_buttonNewVersionGuid);
			_groupBoxIdentification.Controls.Add(_labelUuid);
			_groupBoxIdentification.Controls.Add(_labelVersionUuid);
			_groupBoxIdentification.Controls.Add(_textBoxVersionUuid);
			_groupBoxIdentification.Location=new System.Drawing.Point(19, 122);
			_groupBoxIdentification.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxIdentification.Name="_groupBoxIdentification";
			_groupBoxIdentification.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxIdentification.Size=new System.Drawing.Size(662, 90);
			_groupBoxIdentification.TabIndex=44;
			_groupBoxIdentification.TabStop=false;
			_groupBoxIdentification.Text="Quality condition IDs";
			// 
			// _textBoxUuid
			// 
			_textBoxUuid.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxUuid.Location=new System.Drawing.Point(111, 22);
			_textBoxUuid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxUuid.Name="_textBoxUuid";
			_textBoxUuid.ReadOnly=true;
			_textBoxUuid.Size=new System.Drawing.Size(430, 23);
			_textBoxUuid.TabIndex=41;
			_textBoxUuid.TabStop=false;
			// 
			// _buttonNewVersionGuid
			// 
			_buttonNewVersionGuid.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_buttonNewVersionGuid.Location=new System.Drawing.Point(565, 50);
			_buttonNewVersionGuid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonNewVersionGuid.Name="_buttonNewVersionGuid";
			_buttonNewVersionGuid.Size=new System.Drawing.Size(88, 27);
			_buttonNewVersionGuid.TabIndex=43;
			_buttonNewVersionGuid.Text="Assign New";
			_buttonNewVersionGuid.UseVisualStyleBackColor=true;
			_buttonNewVersionGuid.Click+=_buttonNewVersionUuid_Click;
			// 
			// _labelUuid
			// 
			_labelUuid.AutoSize=true;
			_labelUuid.Location=new System.Drawing.Point(16, 25);
			_labelUuid.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelUuid.Name="_labelUuid";
			_labelUuid.Size=new System.Drawing.Size(37, 15);
			_labelUuid.TabIndex=40;
			_labelUuid.Text="UUID:";
			// 
			// _labelVersionUuid
			// 
			_labelVersionUuid.AutoSize=true;
			_labelVersionUuid.Location=new System.Drawing.Point(16, 55);
			_labelVersionUuid.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelVersionUuid.Name="_labelVersionUuid";
			_labelVersionUuid.Size=new System.Drawing.Size(78, 15);
			_labelVersionUuid.TabIndex=42;
			_labelVersionUuid.Text="Version UUID:";
			// 
			// _textBoxVersionUuid
			// 
			_textBoxVersionUuid.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxVersionUuid.Location=new System.Drawing.Point(111, 52);
			_textBoxVersionUuid.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxVersionUuid.Name="_textBoxVersionUuid";
			_textBoxVersionUuid.ReadOnly=true;
			_textBoxVersionUuid.Size=new System.Drawing.Size(430, 23);
			_textBoxVersionUuid.TabIndex=41;
			_textBoxVersionUuid.TabStop=false;
			// 
			// _groupBoxTablesWithoutGeometry
			// 
			_groupBoxTablesWithoutGeometry.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_groupBoxTablesWithoutGeometry.Controls.Add(_checkBoxNeverFilterTableRowsUsingRelatedGeometry);
			_groupBoxTablesWithoutGeometry.Controls.Add(_checkBoxNeverStoreRelatedGeometryForTableRowIssues);
			_groupBoxTablesWithoutGeometry.Location=new System.Drawing.Point(19, 18);
			_groupBoxTablesWithoutGeometry.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxTablesWithoutGeometry.Name="_groupBoxTablesWithoutGeometry";
			_groupBoxTablesWithoutGeometry.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxTablesWithoutGeometry.Size=new System.Drawing.Size(659, 97);
			_groupBoxTablesWithoutGeometry.TabIndex=39;
			_groupBoxTablesWithoutGeometry.TabStop=false;
			_groupBoxTablesWithoutGeometry.Text="Tables without geometry";
			// 
			// _checkBoxNeverFilterTableRowsUsingRelatedGeometry
			// 
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.AutoEllipsis=true;
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Location=new System.Drawing.Point(20, 32);
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Name="_checkBoxNeverFilterTableRowsUsingRelatedGeometry";
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Size=new System.Drawing.Size(632, 20);
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.TabIndex=0;
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.Text="Never filter table rows to verified perimeter by geometry of related features";
			_checkBoxNeverFilterTableRowsUsingRelatedGeometry.UseVisualStyleBackColor=true;
			// 
			// _checkBoxNeverStoreRelatedGeometryForTableRowIssues
			// 
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.AutoEllipsis=true;
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Location=new System.Drawing.Point(20, 59);
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Name="_checkBoxNeverStoreRelatedGeometryForTableRowIssues";
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Size=new System.Drawing.Size(632, 20);
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.TabIndex=1;
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.Text="Never derive geometry from related features for storing issues";
			_checkBoxNeverStoreRelatedGeometryForTableRowIssues.UseVisualStyleBackColor=true;
			// 
			// _tabPageNotes
			// 
			_tabPageNotes.Controls.Add(_textBoxNotes);
			_tabPageNotes.Location=new System.Drawing.Point(4, 24);
			_tabPageNotes.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageNotes.Name="_tabPageNotes";
			_tabPageNotes.Padding=new System.Windows.Forms.Padding(7);
			_tabPageNotes.Size=new System.Drawing.Size(698, 386);
			_tabPageNotes.TabIndex=3;
			_tabPageNotes.Text="Notes";
			_tabPageNotes.UseVisualStyleBackColor=true;
			// 
			// _textBoxNotes
			// 
			_textBoxNotes.Dock=System.Windows.Forms.DockStyle.Fill;
			_textBoxNotes.Location=new System.Drawing.Point(7, 7);
			_textBoxNotes.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxNotes.Multiline=true;
			_textBoxNotes.Name="_textBoxNotes";
			_textBoxNotes.ScrollBars=System.Windows.Forms.ScrollBars.Vertical;
			_textBoxNotes.Size=new System.Drawing.Size(684, 372);
			_textBoxNotes.TabIndex=0;
			// 
			// _textBoxQualitySpecifications
			// 
			_textBoxQualitySpecifications.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxQualitySpecifications.Location=new System.Drawing.Point(111, 237);
			_textBoxQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxQualitySpecifications.Name="_textBoxQualitySpecifications";
			_textBoxQualitySpecifications.ReadOnly=true;
			_textBoxQualitySpecifications.Size=new System.Drawing.Size(582, 23);
			_textBoxQualitySpecifications.TabIndex=8;
			_textBoxQualitySpecifications.TabStop=false;
			// 
			// _labelQualitySpecifications
			// 
			_labelQualitySpecifications.AutoSize=true;
			_labelQualitySpecifications.Location=new System.Drawing.Point(50, 240);
			_labelQualitySpecifications.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelQualitySpecifications.Name="_labelQualitySpecifications";
			_labelQualitySpecifications.Size=new System.Drawing.Size(49, 15);
			_labelQualitySpecifications.TabIndex=32;
			_labelQualitySpecifications.Text="Used in:";
			_labelQualitySpecifications.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUrl
			// 
			_textBoxUrl.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxUrl.Location=new System.Drawing.Point(111, 110);
			_textBoxUrl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxUrl.Name="_textBoxUrl";
			_textBoxUrl.Size=new System.Drawing.Size(546, 23);
			_textBoxUrl.TabIndex=3;
			// 
			// _labelUrl
			// 
			_labelUrl.AutoSize=true;
			_labelUrl.Location=new System.Drawing.Point(65, 114);
			_labelUrl.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelUrl.Name="_labelUrl";
			_labelUrl.Size=new System.Drawing.Size(31, 15);
			_labelUrl.TabIndex=1;
			_labelUrl.Text="URL:";
			_labelUrl.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _buttonOpenUrl
			// 
			_buttonOpenUrl.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_buttonOpenUrl.FlatAppearance.BorderSize=0;
			_buttonOpenUrl.FlatStyle=System.Windows.Forms.FlatStyle.Flat;
			_buttonOpenUrl.Image=Properties.Resources.OpenUrl;
			_buttonOpenUrl.Location=new System.Drawing.Point(665, 106);
			_buttonOpenUrl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonOpenUrl.Name="_buttonOpenUrl";
			_buttonOpenUrl.Size=new System.Drawing.Size(30, 30);
			_buttonOpenUrl.TabIndex=4;
			_buttonOpenUrl.UseVisualStyleBackColor=true;
			_buttonOpenUrl.Click+=_buttonOpenUrl_Click;
			// 
			// _textBoxCategory
			// 
			_textBoxCategory.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_textBoxCategory.Location=new System.Drawing.Point(465, 15);
			_textBoxCategory.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxCategory.Name="_textBoxCategory";
			_textBoxCategory.ReadOnly=true;
			_textBoxCategory.Size=new System.Drawing.Size(227, 23);
			_textBoxCategory.TabIndex=1;
			_textBoxCategory.TabStop=false;
			// 
			// _labelCategory
			// 
			_labelCategory.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Right;
			_labelCategory.AutoSize=true;
			_labelCategory.Location=new System.Drawing.Point(398, 18);
			_labelCategory.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelCategory.Name="_labelCategory";
			_labelCategory.Size=new System.Drawing.Size(58, 15);
			_labelCategory.TabIndex=3;
			_labelCategory.Text="Category:";
			_labelCategory.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _toolTip
			// 
			_toolTip.AutomaticDelay=100;
			_toolTip.AutoPopDelay=5000;
			_toolTip.InitialDelay=100;
			_toolTip.IsBalloon=true;
			_toolTip.ReshowDelay=20;
			_toolTip.ShowAlways=true;
			// 
			// _nullableBooleanComboboxIssueType
			// 
			_nullableBooleanComboboxIssueType.DefaultText="Use Default";
			_nullableBooleanComboboxIssueType.FalseText="Error";
			_nullableBooleanComboboxIssueType.FlatStyle=System.Windows.Forms.FlatStyle.Standard;
			_nullableBooleanComboboxIssueType.Location=new System.Drawing.Point(111, 140);
			_nullableBooleanComboboxIssueType.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_nullableBooleanComboboxIssueType.Name="_nullableBooleanComboboxIssueType";
			_nullableBooleanComboboxIssueType.Size=new System.Drawing.Size(112, 24);
			_nullableBooleanComboboxIssueType.TabIndex=5;
			_nullableBooleanComboboxIssueType.TrueText="Warning";
			_nullableBooleanComboboxIssueType.Value=null;
			// 
			// _nullableBooleanComboboxStopOnError
			// 
			_nullableBooleanComboboxStopOnError.DefaultText="Use Default";
			_nullableBooleanComboboxStopOnError.FalseText="No";
			_nullableBooleanComboboxStopOnError.FlatStyle=System.Windows.Forms.FlatStyle.Standard;
			_nullableBooleanComboboxStopOnError.Location=new System.Drawing.Point(111, 171);
			_nullableBooleanComboboxStopOnError.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_nullableBooleanComboboxStopOnError.Name="_nullableBooleanComboboxStopOnError";
			_nullableBooleanComboboxStopOnError.Size=new System.Drawing.Size(112, 24);
			_nullableBooleanComboboxStopOnError.TabIndex=7;
			_nullableBooleanComboboxStopOnError.TrueText="Yes";
			_nullableBooleanComboboxStopOnError.Value=null;
			// 
			// _objectReferenceControlTestDescriptor
			// 
			_objectReferenceControlTestDescriptor.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_objectReferenceControlTestDescriptor.DataSource=null;
			_objectReferenceControlTestDescriptor.DisplayMember=null;
			_objectReferenceControlTestDescriptor.FindObjectDelegate=null;
			_objectReferenceControlTestDescriptor.Font=new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			_objectReferenceControlTestDescriptor.FormatTextDelegate=null;
			_objectReferenceControlTestDescriptor.Location=new System.Drawing.Point(111, 205);
			_objectReferenceControlTestDescriptor.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_objectReferenceControlTestDescriptor.Name="_objectReferenceControlTestDescriptor";
			_objectReferenceControlTestDescriptor.ReadOnly=false;
			_objectReferenceControlTestDescriptor.Size=new System.Drawing.Size(582, 35);
			_objectReferenceControlTestDescriptor.TabIndex=10;
			// 
			// dataGridViewTextBoxColumn1
			// 
			dataGridViewTextBoxColumn1.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewTextBoxColumn1.DataPropertyName="QualitySpecificationName";
			dataGridViewTextBoxColumn1.HeaderText="Quality Specification";
			dataGridViewTextBoxColumn1.MinimumWidth=200;
			dataGridViewTextBoxColumn1.Name="dataGridViewTextBoxColumn1";
			dataGridViewTextBoxColumn1.ReadOnly=true;
			// 
			// _panelParametersTop
			// 
			_panelParametersTop.BackColor=System.Drawing.Color.Transparent;
			_panelParametersTop.Controls.Add(_linkDocumentation);
			_panelParametersTop.Dock=System.Windows.Forms.DockStyle.Top;
			_panelParametersTop.Location=new System.Drawing.Point(0, 0);
			_panelParametersTop.Name="_panelParametersTop";
			_panelParametersTop.Size=new System.Drawing.Size(690, 27);
			_panelParametersTop.TabIndex=1;
			// 
			// _linkDocumentation
			// 
			_linkDocumentation.AutoSize=true;
			_linkDocumentation.Location=new System.Drawing.Point(6, 9);
			_linkDocumentation.Margin=new System.Windows.Forms.Padding(6, 0, 6, 0);
			_linkDocumentation.Name="_linkDocumentation";
			_linkDocumentation.Size=new System.Drawing.Size(179, 15);
			_linkDocumentation.TabIndex=23;
			_linkDocumentation.TabStop=true;
			_linkDocumentation.Text="Show Parameter Documentation";
			_linkDocumentation.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _panelParametersEdit
			// 
			_panelParametersEdit.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelParametersEdit.Location=new System.Drawing.Point(0, 27);
			_panelParametersEdit.Name="_panelParametersEdit";
			_panelParametersEdit.Size=new System.Drawing.Size(690, 318);
			_panelParametersEdit.TabIndex=2;
			// 
			// QualityConditionControl
			// 
			AutoScaleDimensions=new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_textBoxQualitySpecifications);
			Controls.Add(_buttonOpenUrl);
			Controls.Add(_tabControlDetails);
			Controls.Add(_labelTestDescriptor);
			Controls.Add(_labelTestDescriptorDefaultAllowErrors);
			Controls.Add(_labelTestDescriptorDefaultStopOnError);
			Controls.Add(_textBoxIssueTypeDefault);
			Controls.Add(_textBoxStopOnErrorDefault);
			Controls.Add(_labelQualitySpecifications);
			Controls.Add(_labelAllowErrors);
			Controls.Add(_labelStopOnError);
			Controls.Add(_nullableBooleanComboboxIssueType);
			Controls.Add(_nullableBooleanComboboxStopOnError);
			Controls.Add(_labelCategory);
			Controls.Add(_labelDescription);
			Controls.Add(_textBoxDescription);
			Controls.Add(_labelUrl);
			Controls.Add(_labelName);
			Controls.Add(_textBoxCategory);
			Controls.Add(_textBoxUrl);
			Controls.Add(_textBoxName);
			Controls.Add(_objectReferenceControlTestDescriptor);
			Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name="QualityConditionControl";
			Size=new System.Drawing.Size(720, 688);
			Load+=QualityConditionControl_Load;
			Paint+=QualityConditionControl_Paint;
			((System.ComponentModel.ISupportInitialize)_errorProvider).EndInit();
			_tabControlParameterValues.ResumeLayout(false);
			tabPageProperties.ResumeLayout(false);
			_splitContainerProperties.Panel1.ResumeLayout(false);
			_splitContainerProperties.Panel1.PerformLayout();
			_splitContainerProperties.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainerProperties).EndInit();
			_splitContainerProperties.ResumeLayout(false);
			_panelDescriptionLabel.ResumeLayout(false);
			_panelDescriptionLabel.PerformLayout();
			_tabPageTableView.ResumeLayout(false);
			_splitContainer.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
			_splitContainer.ResumeLayout(false);
			_splitContainerHeader.Panel1.ResumeLayout(false);
			_splitContainerHeader.Panel1.PerformLayout();
			_splitContainerHeader.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainerHeader).EndInit();
			_splitContainerHeader.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_dataGridViewParamGrid).EndInit();
			_tabControlDetails.ResumeLayout(false);
			_tabPageParameters.ResumeLayout(false);
			_panelParameters.ResumeLayout(false);
			_qualityConditionTableViewControlPanel.ResumeLayout(false);
			_exportButtonPanel.ResumeLayout(false);
			_tabPageIssueFilters.ResumeLayout(false);
			_issueFilterPanelBottom.ResumeLayout(false);
			_issueFilterPanelBottom.PerformLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewIssueFilters).EndInit();
			toolStripEx1.ResumeLayout(false);
			toolStripEx1.PerformLayout();
			_tabPageQualitySpecifications.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_dataGridViewQualitySpecifications).EndInit();
			_toolStripElements.ResumeLayout(false);
			_toolStripElements.PerformLayout();
			_tabPageOptions.ResumeLayout(false);
			_groupBoxIdentification.ResumeLayout(false);
			_groupBoxIdentification.PerformLayout();
			_groupBoxTablesWithoutGeometry.ResumeLayout(false);
			_tabPageNotes.ResumeLayout(false);
			_tabPageNotes.PerformLayout();
			_panelParametersTop.ResumeLayout(false);
			_panelParametersTop.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private ObjectReferenceControl _objectReferenceControlTestDescriptor;
		private System.Windows.Forms.TextBox _textBoxDescGrid;
		private System.Windows.Forms.LinkLabel _labelTestDescriptor;
		private System.Windows.Forms.Button _buttonImport;
		private System.Windows.Forms.Button _buttonExport;
		private System.Windows.Forms.OpenFileDialog openFileDialogImport;
		private System.Windows.Forms.SaveFileDialog saveFileDialogExport;
		private System.Windows.Forms.TabControl _tabControlParameterValues;
		private System.Windows.Forms.TabPage tabPageProperties;
		private System.Windows.Forms.TabPage _tabPageTableView;
		private System.Windows.Forms.PropertyGrid _propertyGrid;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private System.Windows.Forms.Label _labelStopOnError;
		private global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox _nullableBooleanComboboxIssueType;
		private global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox _nullableBooleanComboboxStopOnError;
		private System.Windows.Forms.Label _labelAllowErrors;
		private System.Windows.Forms.Label _labelTestDescriptorDefaultAllowErrors;
		private System.Windows.Forms.Label _labelTestDescriptorDefaultStopOnError;
		private System.Windows.Forms.TextBox _textBoxIssueTypeDefault;
		private System.Windows.Forms.TextBox _textBoxStopOnErrorDefault;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerProperties;
		private System.Windows.Forms.Label _labelParameterDescription;
		private System.Windows.Forms.TextBox _textBoxDescProps;
		private System.Windows.Forms.TabControl _tabControlDetails;
		private System.Windows.Forms.TabPage _tabPageParameters;
		private System.Windows.Forms.TabPage _tabPageQualitySpecifications;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveFromQualitySpecifications;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAssignToQualitySpecifications;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.TextBox _textBoxQualitySpecifications;
		private System.Windows.Forms.Label _labelQualitySpecifications;
		private DoubleBufferedDataGridView _dataGridViewParamGrid;
		private DoubleBufferedDataGridView _dataGridViewQualitySpecifications;
		private ToolStripEx _toolStripElements;
		private System.Windows.Forms.Label _labelUrl;
		private System.Windows.Forms.TextBox _textBoxUrl;
		private System.Windows.Forms.Button _buttonOpenUrl;
		private System.Windows.Forms.Panel _panelDescriptionLabel;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerHeader;
		private System.Windows.Forms.CheckBox _checkBoxNeverFilterTableRowsUsingRelatedGeometry;
		private System.Windows.Forms.CheckBox _checkBoxNeverStoreRelatedGeometryForTableRowIssues;
		private System.Windows.Forms.TabPage _tabPageOptions;
		private System.Windows.Forms.GroupBox _groupBoxTablesWithoutGeometry;
		private System.Windows.Forms.Label _labelVersionUuid;
		private System.Windows.Forms.TextBox _textBoxVersionUuid;
		private System.Windows.Forms.TextBox _textBoxUuid;
		private System.Windows.Forms.Label _labelUuid;
		private System.Windows.Forms.Button _buttonNewVersionGuid;
		private System.Windows.Forms.GroupBox _groupBoxIdentification;
		private System.Windows.Forms.Label _labelCategory;
		private System.Windows.Forms.TextBox _textBoxCategory;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.TabPage _tabPageNotes;
		private System.Windows.Forms.TextBox _textBoxNotes;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnIssueType;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnStopOnError;
		private System.Windows.Forms.Panel _qualityConditionTableViewControlPanel;
		private System.Windows.Forms.Panel _exportButtonPanel;
		private System.Windows.Forms.TabPage _tabPageIssueFilters;
		private ToolStripEx toolStripEx1;
		private System.Windows.Forms.ToolStripLabel _toolStripLabelIssueFilters;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveIssueFilter;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAddIssueFilter;
		private DoubleBufferedDataGridView _dataGridViewIssueFilters;
		private System.Windows.Forms.DataGridViewImageColumn _dataGridIssueColumnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridIssueColumnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridIssueColumnType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridIssueColumnAlgorithm;
		private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridIssueColumnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _dataGridIssueColumnDescription;
		private System.Windows.Forms.Panel _panelParameters;
		private System.Windows.Forms.Panel _panelParametersEdit;
		private System.Windows.Forms.Panel _panelParametersTop;
		private System.Windows.Forms.LinkLabel _linkDocumentation;
		private InstanceConfig.InstanceParameterConfigControl _instanceParameterConfigControl;
		private System.Windows.Forms.Panel _issueFilterPanelBottom;
		private System.Windows.Forms.TextBox _textBoxFilterExpression;
		private System.Windows.Forms.Label _labelFilterExpression;
	}
}
