using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.QA;
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._textBoxDescGrid = new System.Windows.Forms.TextBox();
			this._labelTestDescriptor = new System.Windows.Forms.LinkLabel();
			this._buttonExport = new System.Windows.Forms.Button();
			this._buttonImport = new System.Windows.Forms.Button();
			this.openFileDialogImport = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogExport = new System.Windows.Forms.SaveFileDialog();
			this._tabControlParameterValues = new System.Windows.Forms.TabControl();
			this.tabPageProperties = new System.Windows.Forms.TabPage();
			this._splitContainerProperties = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._textBoxDescProps = new System.Windows.Forms.TextBox();
			this._panelDescriptionLabel = new System.Windows.Forms.Panel();
			this._labelParameterDescription = new System.Windows.Forms.Label();
			this._propertyGrid = new System.Windows.Forms.PropertyGrid();
			this._tabPageTableView = new System.Windows.Forms.TabPage();
			this._splitContainer = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._splitContainerHeader = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._dataGridViewParamGrid = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._labelStopOnError = new System.Windows.Forms.Label();
			this._labelAllowErrors = new System.Windows.Forms.Label();
			this._textBoxStopOnErrorDefault = new System.Windows.Forms.TextBox();
			this._textBoxIssueTypeDefault = new System.Windows.Forms.TextBox();
			this._labelTestDescriptorDefaultStopOnError = new System.Windows.Forms.Label();
			this._labelTestDescriptorDefaultAllowErrors = new System.Windows.Forms.Label();
			this._tabControlDetails = new System.Windows.Forms.TabControl();
			this._tabPageParameters = new System.Windows.Forms.TabPage();
			this._qualityConditionTableViewControlPanel = new System.Windows.Forms.Panel();
			this._exportButtonPanel = new System.Windows.Forms.Panel();
			this._tabPageQualitySpecifications = new System.Windows.Forms.TabPage();
			this._dataGridViewQualitySpecifications = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnStopOnError = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._toolStripElements = new ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this._toolStripButtonRemoveFromQualitySpecifications = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonAssignToQualitySpecifications = new System.Windows.Forms.ToolStripButton();
			this._tabPageOptions = new System.Windows.Forms.TabPage();
			this._groupBoxIdentification = new System.Windows.Forms.GroupBox();
			this._textBoxUuid = new System.Windows.Forms.TextBox();
			this._buttonNewVersionGuid = new System.Windows.Forms.Button();
			this._labelUuid = new System.Windows.Forms.Label();
			this._labelVersionUuid = new System.Windows.Forms.Label();
			this._textBoxVersionUuid = new System.Windows.Forms.TextBox();
			this._groupBoxTablesWithoutGeometry = new System.Windows.Forms.GroupBox();
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry = new System.Windows.Forms.CheckBox();
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues = new System.Windows.Forms.CheckBox();
			this._tabPageNotes = new System.Windows.Forms.TabPage();
			this._textBoxNotes = new System.Windows.Forms.TextBox();
			this._textBoxQualitySpecifications = new System.Windows.Forms.TextBox();
			this._labelQualitySpecifications = new System.Windows.Forms.Label();
			this._textBoxUrl = new System.Windows.Forms.TextBox();
			this._labelUrl = new System.Windows.Forms.Label();
			this._buttonOpenUrl = new System.Windows.Forms.Button();
			this._textBoxCategory = new System.Windows.Forms.TextBox();
			this._labelCategory = new System.Windows.Forms.Label();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this._nullableBooleanComboboxIssueType = new ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox();
			this._nullableBooleanComboboxStopOnError = new ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox();
			this._objectReferenceControlTestDescriptor = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this._tabControlParameterValues.SuspendLayout();
			this.tabPageProperties.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerProperties)).BeginInit();
			this._splitContainerProperties.Panel1.SuspendLayout();
			this._splitContainerProperties.Panel2.SuspendLayout();
			this._splitContainerProperties.SuspendLayout();
			this._panelDescriptionLabel.SuspendLayout();
			this._tabPageTableView.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerHeader)).BeginInit();
			this._splitContainerHeader.Panel1.SuspendLayout();
			this._splitContainerHeader.Panel2.SuspendLayout();
			this._splitContainerHeader.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParamGrid)).BeginInit();
			this._tabControlDetails.SuspendLayout();
			this._tabPageParameters.SuspendLayout();
			this._qualityConditionTableViewControlPanel.SuspendLayout();
			this._exportButtonPanel.SuspendLayout();
			this._tabPageQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualitySpecifications)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this._tabPageOptions.SuspendLayout();
			this._groupBoxIdentification.SuspendLayout();
			this._groupBoxTablesWithoutGeometry.SuspendLayout();
			this._tabPageNotes.SuspendLayout();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(111, 15);
			this._textBoxName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(264, 23);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(59, 18);
			this._labelName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(42, 15);
			this._labelName.TabIndex = 1;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(111, 46);
			this._textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(582, 56);
			this._textBoxDescription.TabIndex = 2;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(30, 50);
			this._labelDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(70, 15);
			this._labelDescription.TabIndex = 3;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _textBoxDescGrid
			// 
			this._textBoxDescGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxDescGrid.Location = new System.Drawing.Point(0, 0);
			this._textBoxDescGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescGrid.Multiline = true;
			this._textBoxDescGrid.Name = "_textBoxDescGrid";
			this._textBoxDescGrid.ReadOnly = true;
			this._textBoxDescGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescGrid.Size = new System.Drawing.Size(674, 48);
			this._textBoxDescGrid.TabIndex = 21;
			// 
			// _labelTestDescriptor
			// 
			this._labelTestDescriptor.AutoSize = true;
			this._labelTestDescriptor.Location = new System.Drawing.Point(8, 209);
			this._labelTestDescriptor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelTestDescriptor.Name = "_labelTestDescriptor";
			this._labelTestDescriptor.Size = new System.Drawing.Size(87, 15);
			this._labelTestDescriptor.TabIndex = 9;
			this._labelTestDescriptor.TabStop = true;
			this._labelTestDescriptor.Text = "Test Descriptor:";
			this._labelTestDescriptor.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this._labelTestDescriptor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._labelTestDescriptor_LinkClicked);
			// 
			// _buttonExport
			// 
			this._buttonExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonExport.Location = new System.Drawing.Point(506, 3);
			this._buttonExport.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonExport.Name = "_buttonExport";
			this._buttonExport.Size = new System.Drawing.Size(88, 27);
			this._buttonExport.TabIndex = 5;
			this._buttonExport.Text = "Export";
			this._buttonExport.UseVisualStyleBackColor = true;
			this._buttonExport.Click += new System.EventHandler(this._buttonExport_Click);
			// 
			// _buttonImport
			// 
			this._buttonImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonImport.Location = new System.Drawing.Point(602, 3);
			this._buttonImport.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonImport.Name = "_buttonImport";
			this._buttonImport.Size = new System.Drawing.Size(88, 27);
			this._buttonImport.TabIndex = 6;
			this._buttonImport.Text = "Import";
			this._buttonImport.UseVisualStyleBackColor = true;
			this._buttonImport.Click += new System.EventHandler(this._buttonImport_Click);
			// 
			// _tabControlParameterValues
			// 
			this._tabControlParameterValues.Controls.Add(this.tabPageProperties);
			this._tabControlParameterValues.Controls.Add(this._tabPageTableView);
			this._tabControlParameterValues.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControlParameterValues.Location = new System.Drawing.Point(0, 0);
			this._tabControlParameterValues.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabControlParameterValues.Name = "_tabControlParameterValues";
			this._tabControlParameterValues.SelectedIndex = 0;
			this._tabControlParameterValues.Size = new System.Drawing.Size(690, 345);
			this._tabControlParameterValues.TabIndex = 28;
			this._tabControlParameterValues.SelectedIndexChanged += new System.EventHandler(this._tabControlParameterValues_SelectedIndexChanged);
			// 
			// tabPageProperties
			// 
			this.tabPageProperties.Controls.Add(this._splitContainerProperties);
			this.tabPageProperties.Location = new System.Drawing.Point(4, 24);
			this.tabPageProperties.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.tabPageProperties.Name = "tabPageProperties";
			this.tabPageProperties.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.tabPageProperties.Size = new System.Drawing.Size(682, 317);
			this.tabPageProperties.TabIndex = 0;
			this.tabPageProperties.Text = "Parameter Values";
			this.tabPageProperties.UseVisualStyleBackColor = true;
			// 
			// _splitContainerProperties
			// 
			this._splitContainerProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerProperties.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerProperties.Location = new System.Drawing.Point(4, 3);
			this._splitContainerProperties.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._splitContainerProperties.Name = "_splitContainerProperties";
			this._splitContainerProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerProperties.Panel1
			// 
			this._splitContainerProperties.Panel1.Controls.Add(this._textBoxDescProps);
			this._splitContainerProperties.Panel1.Controls.Add(this._panelDescriptionLabel);
			// 
			// _splitContainerProperties.Panel2
			// 
			this._splitContainerProperties.Panel2.Controls.Add(this._propertyGrid);
			this._splitContainerProperties.Size = new System.Drawing.Size(674, 311);
			this._splitContainerProperties.SplitterDistance = 74;
			this._splitContainerProperties.SplitterWidth = 5;
			this._splitContainerProperties.TabIndex = 31;
			// 
			// _textBoxDescProps
			// 
			this._textBoxDescProps.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxDescProps.Location = new System.Drawing.Point(86, 0);
			this._textBoxDescProps.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescProps.Multiline = true;
			this._textBoxDescProps.Name = "_textBoxDescProps";
			this._textBoxDescProps.ReadOnly = true;
			this._textBoxDescProps.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescProps.Size = new System.Drawing.Size(588, 74);
			this._textBoxDescProps.TabIndex = 0;
			// 
			// _panelDescriptionLabel
			// 
			this._panelDescriptionLabel.Controls.Add(this._labelParameterDescription);
			this._panelDescriptionLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this._panelDescriptionLabel.Location = new System.Drawing.Point(0, 0);
			this._panelDescriptionLabel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._panelDescriptionLabel.MaximumSize = new System.Drawing.Size(86, 11538);
			this._panelDescriptionLabel.MinimumSize = new System.Drawing.Size(86, 0);
			this._panelDescriptionLabel.Name = "_panelDescriptionLabel";
			this._panelDescriptionLabel.Size = new System.Drawing.Size(86, 74);
			this._panelDescriptionLabel.TabIndex = 23;
			// 
			// _labelParameterDescription
			// 
			this._labelParameterDescription.AutoSize = true;
			this._labelParameterDescription.Location = new System.Drawing.Point(6, 3);
			this._labelParameterDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelParameterDescription.Name = "_labelParameterDescription";
			this._labelParameterDescription.Size = new System.Drawing.Size(70, 15);
			this._labelParameterDescription.TabIndex = 22;
			this._labelParameterDescription.Text = "Description:";
			this._labelParameterDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _propertyGrid
			// 
			this._propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._propertyGrid.Location = new System.Drawing.Point(0, 0);
			this._propertyGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._propertyGrid.Name = "_propertyGrid";
			this._propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this._propertyGrid.Size = new System.Drawing.Size(674, 232);
			this._propertyGrid.TabIndex = 0;
			this._propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this._propertyGrid_PropertyValueChanged);
			// 
			// _tabPageTableView
			// 
			this._tabPageTableView.Controls.Add(this._splitContainer);
			this._tabPageTableView.Location = new System.Drawing.Point(4, 24);
			this._tabPageTableView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageTableView.Name = "_tabPageTableView";
			this._tabPageTableView.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageTableView.Size = new System.Drawing.Size(682, 317);
			this._tabPageTableView.TabIndex = 1;
			this._tabPageTableView.Text = "Table View";
			this._tabPageTableView.UseVisualStyleBackColor = true;
			// 
			// _splitContainer
			// 
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.Location = new System.Drawing.Point(4, 3);
			this._splitContainer.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._splitContainer.Name = "_splitContainer";
			this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._splitContainerHeader);
			this._splitContainer.Size = new System.Drawing.Size(674, 311);
			this._splitContainer.SplitterDistance = 170;
			this._splitContainer.TabIndex = 30;
			// 
			// _splitContainerHeader
			// 
			this._splitContainerHeader.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerHeader.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerHeader.Location = new System.Drawing.Point(0, 0);
			this._splitContainerHeader.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._splitContainerHeader.Name = "_splitContainerHeader";
			this._splitContainerHeader.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerHeader.Panel1
			// 
			this._splitContainerHeader.Panel1.Controls.Add(this._textBoxDescGrid);
			// 
			// _splitContainerHeader.Panel2
			// 
			this._splitContainerHeader.Panel2.Controls.Add(this._dataGridViewParamGrid);
			this._splitContainerHeader.Panel2MinSize = 50;
			this._splitContainerHeader.Size = new System.Drawing.Size(674, 170);
			this._splitContainerHeader.SplitterDistance = 48;
			this._splitContainerHeader.SplitterWidth = 5;
			this._splitContainerHeader.TabIndex = 26;
			// 
			// _dataGridViewParamGrid
			// 
			this._dataGridViewParamGrid.AllowUserToAddRows = false;
			this._dataGridViewParamGrid.AllowUserToDeleteRows = false;
			this._dataGridViewParamGrid.AllowUserToResizeRows = false;
			this._dataGridViewParamGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewParamGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridViewParamGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewParamGrid.DefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridViewParamGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewParamGrid.Location = new System.Drawing.Point(0, 0);
			this._dataGridViewParamGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._dataGridViewParamGrid.Name = "_dataGridViewParamGrid";
			this._dataGridViewParamGrid.ReadOnly = true;
			this._dataGridViewParamGrid.RowHeadersVisible = false;
			this._dataGridViewParamGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewParamGrid.Size = new System.Drawing.Size(674, 117);
			this._dataGridViewParamGrid.TabIndex = 24;
			this._dataGridViewParamGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this._dataGridViewParamGrid_DataBindingComplete);
			// 
			// _labelStopOnError
			// 
			this._labelStopOnError.AutoSize = true;
			this._labelStopOnError.Location = new System.Drawing.Point(20, 175);
			this._labelStopOnError.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelStopOnError.Name = "_labelStopOnError";
			this._labelStopOnError.Size = new System.Drawing.Size(79, 15);
			this._labelStopOnError.TabIndex = 32;
			this._labelStopOnError.Text = "Stop on error:";
			this._labelStopOnError.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAllowErrors
			// 
			this._labelAllowErrors.AutoSize = true;
			this._labelAllowErrors.Location = new System.Drawing.Point(36, 143);
			this._labelAllowErrors.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelAllowErrors.Name = "_labelAllowErrors";
			this._labelAllowErrors.Size = new System.Drawing.Size(62, 15);
			this._labelAllowErrors.TabIndex = 32;
			this._labelAllowErrors.Text = "Issue type:";
			this._labelAllowErrors.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxStopOnErrorDefault
			// 
			this._textBoxStopOnErrorDefault.Location = new System.Drawing.Point(383, 172);
			this._textBoxStopOnErrorDefault.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxStopOnErrorDefault.Name = "_textBoxStopOnErrorDefault";
			this._textBoxStopOnErrorDefault.ReadOnly = true;
			this._textBoxStopOnErrorDefault.Size = new System.Drawing.Size(65, 23);
			this._textBoxStopOnErrorDefault.TabIndex = 8;
			this._textBoxStopOnErrorDefault.TabStop = false;
			// 
			// _textBoxIssueTypeDefault
			// 
			this._textBoxIssueTypeDefault.Location = new System.Drawing.Point(383, 140);
			this._textBoxIssueTypeDefault.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxIssueTypeDefault.Name = "_textBoxIssueTypeDefault";
			this._textBoxIssueTypeDefault.ReadOnly = true;
			this._textBoxIssueTypeDefault.Size = new System.Drawing.Size(65, 23);
			this._textBoxIssueTypeDefault.TabIndex = 6;
			this._textBoxIssueTypeDefault.TabStop = false;
			// 
			// _labelTestDescriptorDefaultStopOnError
			// 
			this._labelTestDescriptorDefaultStopOnError.AutoSize = true;
			this._labelTestDescriptorDefaultStopOnError.Location = new System.Drawing.Point(230, 175);
			this._labelTestDescriptorDefaultStopOnError.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelTestDescriptorDefaultStopOnError.Name = "_labelTestDescriptorDefaultStopOnError";
			this._labelTestDescriptorDefaultStopOnError.Size = new System.Drawing.Size(136, 15);
			this._labelTestDescriptorDefaultStopOnError.TabIndex = 34;
			this._labelTestDescriptorDefaultStopOnError.Text = "Default (Test Descriptor):";
			this._labelTestDescriptorDefaultStopOnError.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelTestDescriptorDefaultAllowErrors
			// 
			this._labelTestDescriptorDefaultAllowErrors.AutoSize = true;
			this._labelTestDescriptorDefaultAllowErrors.Location = new System.Drawing.Point(230, 143);
			this._labelTestDescriptorDefaultAllowErrors.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelTestDescriptorDefaultAllowErrors.Name = "_labelTestDescriptorDefaultAllowErrors";
			this._labelTestDescriptorDefaultAllowErrors.Size = new System.Drawing.Size(136, 15);
			this._labelTestDescriptorDefaultAllowErrors.TabIndex = 34;
			this._labelTestDescriptorDefaultAllowErrors.Text = "Default (Test Descriptor):";
			this._labelTestDescriptorDefaultAllowErrors.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControlDetails
			// 
			this._tabControlDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControlDetails.Controls.Add(this._tabPageParameters);
			this._tabControlDetails.Controls.Add(this._tabPageQualitySpecifications);
			this._tabControlDetails.Controls.Add(this._tabPageOptions);
			this._tabControlDetails.Controls.Add(this._tabPageNotes);
			this._tabControlDetails.Location = new System.Drawing.Point(10, 267);
			this._tabControlDetails.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabControlDetails.Name = "_tabControlDetails";
			this._tabControlDetails.SelectedIndex = 0;
			this._tabControlDetails.Size = new System.Drawing.Size(706, 414);
			this._tabControlDetails.TabIndex = 0;
			this._tabControlDetails.SelectedIndexChanged += new System.EventHandler(this._tabControlDetails_SelectedIndexChanged);
			// 
			// _tabPageParameters
			// 
			this._tabPageParameters.Controls.Add(this._qualityConditionTableViewControlPanel);
			this._tabPageParameters.Controls.Add(this._exportButtonPanel);
			this._tabPageParameters.Location = new System.Drawing.Point(4, 24);
			this._tabPageParameters.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageParameters.Name = "_tabPageParameters";
			this._tabPageParameters.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageParameters.Size = new System.Drawing.Size(698, 386);
			this._tabPageParameters.TabIndex = 0;
			this._tabPageParameters.Text = "Test Parameters";
			// 
			// _qualityConditionTableViewControlPanel
			// 
			this._qualityConditionTableViewControlPanel.Controls.Add(this._tabControlParameterValues);
			this._qualityConditionTableViewControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionTableViewControlPanel.Location = new System.Drawing.Point(4, 38);
			this._qualityConditionTableViewControlPanel.Name = "_qualityConditionTableViewControlPanel";
			this._qualityConditionTableViewControlPanel.Size = new System.Drawing.Size(690, 345);
			this._qualityConditionTableViewControlPanel.TabIndex = 29;
			// 
			// _exportButtonPanel
			// 
			this._exportButtonPanel.Controls.Add(this._buttonExport);
			this._exportButtonPanel.Controls.Add(this._buttonImport);
			this._exportButtonPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this._exportButtonPanel.Location = new System.Drawing.Point(4, 3);
			this._exportButtonPanel.Name = "_exportButtonPanel";
			this._exportButtonPanel.Size = new System.Drawing.Size(690, 35);
			this._exportButtonPanel.TabIndex = 30;
			// 
			// _tabPageQualitySpecifications
			// 
			this._tabPageQualitySpecifications.Controls.Add(this._dataGridViewQualitySpecifications);
			this._tabPageQualitySpecifications.Controls.Add(this._toolStripElements);
			this._tabPageQualitySpecifications.Location = new System.Drawing.Point(4, 24);
			this._tabPageQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageQualitySpecifications.Name = "_tabPageQualitySpecifications";
			this._tabPageQualitySpecifications.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageQualitySpecifications.Size = new System.Drawing.Size(698, 386);
			this._tabPageQualitySpecifications.TabIndex = 1;
			this._tabPageQualitySpecifications.Text = "Quality Specifications";
			// 
			// _dataGridViewQualitySpecifications
			// 
			this._dataGridViewQualitySpecifications.AllowUserToAddRows = false;
			this._dataGridViewQualitySpecifications.AllowUserToDeleteRows = false;
			this._dataGridViewQualitySpecifications.AllowUserToResizeRows = false;
			this._dataGridViewQualitySpecifications.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewQualitySpecifications.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnName,
            this._columnCategory,
            this._columnIssueType,
            this._columnStopOnError});
			this._dataGridViewQualitySpecifications.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewQualitySpecifications.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridViewQualitySpecifications.Location = new System.Drawing.Point(4, 32);
			this._dataGridViewQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._dataGridViewQualitySpecifications.Name = "_dataGridViewQualitySpecifications";
			this._dataGridViewQualitySpecifications.RowHeadersVisible = false;
			this._dataGridViewQualitySpecifications.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewQualitySpecifications.Size = new System.Drawing.Size(690, 351);
			this._dataGridViewQualitySpecifications.TabIndex = 24;
			this._dataGridViewQualitySpecifications.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellDoubleClick);
			this._dataGridViewQualitySpecifications.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellEndEdit);
			this._dataGridViewQualitySpecifications.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellValueChanged);
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnName.DataPropertyName = "QualitySpecificationName";
			this._columnName.HeaderText = "Quality Specification";
			this._columnName.MinimumWidth = 200;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			this._columnName.Width = 200;
			// 
			// _columnCategory
			// 
			this._columnCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnCategory.DataPropertyName = "Category";
			this._columnCategory.HeaderText = "Category";
			this._columnCategory.MinimumWidth = 50;
			this._columnCategory.Name = "_columnCategory";
			this._columnCategory.ReadOnly = true;
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
			this._columnIssueType.MinimumWidth = 87;
			this._columnIssueType.Name = "_columnIssueType";
			this._columnIssueType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnIssueType.Width = 87;
			// 
			// _columnStopOnError
			// 
			this._columnStopOnError.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnStopOnError.DataPropertyName = "StopOnErrorOverride";
			this._columnStopOnError.DisplayStyleForCurrentCellOnly = true;
			this._columnStopOnError.HeaderText = "Stop On Error";
			this._columnStopOnError.Items.AddRange(new object[] {
            "Yes",
            "No",
            "Default"});
			this._columnStopOnError.MinimumWidth = 96;
			this._columnStopOnError.Name = "_columnStopOnError";
			this._columnStopOnError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnStopOnError.Width = 96;
			// 
			// _toolStripElements
			// 
			this._toolStripElements.AutoSize = false;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this._toolStripButtonRemoveFromQualitySpecifications,
            this._toolStripButtonAssignToQualitySpecifications});
			this._toolStripElements.Location = new System.Drawing.Point(4, 3);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.Size = new System.Drawing.Size(690, 29);
			this._toolStripElements.TabIndex = 25;
			this._toolStripElements.Text = "Element Tools";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(121, 26);
			this.toolStripLabel1.Text = "Quality Specifications";
			// 
			// _toolStripButtonRemoveFromQualitySpecifications
			// 
			this._toolStripButtonRemoveFromQualitySpecifications.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemoveFromQualitySpecifications.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemoveFromQualitySpecifications.Name = "_toolStripButtonRemoveFromQualitySpecifications";
			this._toolStripButtonRemoveFromQualitySpecifications.Size = new System.Drawing.Size(70, 26);
			this._toolStripButtonRemoveFromQualitySpecifications.Text = "Remove";
			this._toolStripButtonRemoveFromQualitySpecifications.Click += new System.EventHandler(this._toolStripButtonRemoveQualityConditions_Click);
			// 
			// _toolStripButtonAssignToQualitySpecifications
			// 
			this._toolStripButtonAssignToQualitySpecifications.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAssignToQualitySpecifications.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._toolStripButtonAssignToQualitySpecifications.Name = "_toolStripButtonAssignToQualitySpecifications";
			this._toolStripButtonAssignToQualitySpecifications.Size = new System.Drawing.Size(203, 26);
			this._toolStripButtonAssignToQualitySpecifications.Text = "Assign To Quality Specifications...";
			this._toolStripButtonAssignToQualitySpecifications.Click += new System.EventHandler(this._toolStripButtonAssignToQualitySpecifications_Click);
			// 
			// _tabPageOptions
			// 
			this._tabPageOptions.Controls.Add(this._groupBoxIdentification);
			this._tabPageOptions.Controls.Add(this._groupBoxTablesWithoutGeometry);
			this._tabPageOptions.Location = new System.Drawing.Point(4, 24);
			this._tabPageOptions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageOptions.Name = "_tabPageOptions";
			this._tabPageOptions.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageOptions.Size = new System.Drawing.Size(698, 386);
			this._tabPageOptions.TabIndex = 2;
			this._tabPageOptions.Text = "Options";
			this._tabPageOptions.UseVisualStyleBackColor = true;
			// 
			// _groupBoxIdentification
			// 
			this._groupBoxIdentification.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxIdentification.Controls.Add(this._textBoxUuid);
			this._groupBoxIdentification.Controls.Add(this._buttonNewVersionGuid);
			this._groupBoxIdentification.Controls.Add(this._labelUuid);
			this._groupBoxIdentification.Controls.Add(this._labelVersionUuid);
			this._groupBoxIdentification.Controls.Add(this._textBoxVersionUuid);
			this._groupBoxIdentification.Location = new System.Drawing.Point(19, 122);
			this._groupBoxIdentification.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxIdentification.Name = "_groupBoxIdentification";
			this._groupBoxIdentification.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxIdentification.Size = new System.Drawing.Size(662, 90);
			this._groupBoxIdentification.TabIndex = 44;
			this._groupBoxIdentification.TabStop = false;
			this._groupBoxIdentification.Text = "Quality condition IDs";
			// 
			// _textBoxUuid
			// 
			this._textBoxUuid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxUuid.Location = new System.Drawing.Point(111, 22);
			this._textBoxUuid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxUuid.Name = "_textBoxUuid";
			this._textBoxUuid.ReadOnly = true;
			this._textBoxUuid.Size = new System.Drawing.Size(430, 23);
			this._textBoxUuid.TabIndex = 41;
			this._textBoxUuid.TabStop = false;
			// 
			// _buttonNewVersionGuid
			// 
			this._buttonNewVersionGuid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonNewVersionGuid.Location = new System.Drawing.Point(565, 50);
			this._buttonNewVersionGuid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonNewVersionGuid.Name = "_buttonNewVersionGuid";
			this._buttonNewVersionGuid.Size = new System.Drawing.Size(88, 27);
			this._buttonNewVersionGuid.TabIndex = 43;
			this._buttonNewVersionGuid.Text = "Assign New";
			this._buttonNewVersionGuid.UseVisualStyleBackColor = true;
			this._buttonNewVersionGuid.Click += new System.EventHandler(this._buttonNewVersionUuid_Click);
			// 
			// _labelUuid
			// 
			this._labelUuid.AutoSize = true;
			this._labelUuid.Location = new System.Drawing.Point(16, 25);
			this._labelUuid.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelUuid.Name = "_labelUuid";
			this._labelUuid.Size = new System.Drawing.Size(37, 15);
			this._labelUuid.TabIndex = 40;
			this._labelUuid.Text = "UUID:";
			// 
			// _labelVersionUuid
			// 
			this._labelVersionUuid.AutoSize = true;
			this._labelVersionUuid.Location = new System.Drawing.Point(16, 55);
			this._labelVersionUuid.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelVersionUuid.Name = "_labelVersionUuid";
			this._labelVersionUuid.Size = new System.Drawing.Size(78, 15);
			this._labelVersionUuid.TabIndex = 42;
			this._labelVersionUuid.Text = "Version UUID:";
			// 
			// _textBoxVersionUuid
			// 
			this._textBoxVersionUuid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxVersionUuid.Location = new System.Drawing.Point(111, 52);
			this._textBoxVersionUuid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxVersionUuid.Name = "_textBoxVersionUuid";
			this._textBoxVersionUuid.ReadOnly = true;
			this._textBoxVersionUuid.Size = new System.Drawing.Size(430, 23);
			this._textBoxVersionUuid.TabIndex = 41;
			this._textBoxVersionUuid.TabStop = false;
			// 
			// _groupBoxTablesWithoutGeometry
			// 
			this._groupBoxTablesWithoutGeometry.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxTablesWithoutGeometry.Controls.Add(this._checkBoxNeverFilterTableRowsUsingRelatedGeometry);
			this._groupBoxTablesWithoutGeometry.Controls.Add(this._checkBoxNeverStoreRelatedGeometryForTableRowIssues);
			this._groupBoxTablesWithoutGeometry.Location = new System.Drawing.Point(19, 18);
			this._groupBoxTablesWithoutGeometry.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxTablesWithoutGeometry.Name = "_groupBoxTablesWithoutGeometry";
			this._groupBoxTablesWithoutGeometry.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxTablesWithoutGeometry.Size = new System.Drawing.Size(659, 97);
			this._groupBoxTablesWithoutGeometry.TabIndex = 39;
			this._groupBoxTablesWithoutGeometry.TabStop = false;
			this._groupBoxTablesWithoutGeometry.Text = "Tables without geometry";
			// 
			// _checkBoxNeverFilterTableRowsUsingRelatedGeometry
			// 
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.AutoEllipsis = true;
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Location = new System.Drawing.Point(20, 32);
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Name = "_checkBoxNeverFilterTableRowsUsingRelatedGeometry";
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Size = new System.Drawing.Size(632, 20);
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.TabIndex = 0;
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.Text = "Never filter table rows to verified perimeter by geometry of related features";
			this._checkBoxNeverFilterTableRowsUsingRelatedGeometry.UseVisualStyleBackColor = true;
			// 
			// _checkBoxNeverStoreRelatedGeometryForTableRowIssues
			// 
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.AutoEllipsis = true;
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Location = new System.Drawing.Point(20, 59);
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Name = "_checkBoxNeverStoreRelatedGeometryForTableRowIssues";
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Size = new System.Drawing.Size(632, 20);
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.TabIndex = 1;
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.Text = "Never derive geometry from related features for storing issues";
			this._checkBoxNeverStoreRelatedGeometryForTableRowIssues.UseVisualStyleBackColor = true;
			// 
			// _tabPageNotes
			// 
			this._tabPageNotes.Controls.Add(this._textBoxNotes);
			this._tabPageNotes.Location = new System.Drawing.Point(4, 24);
			this._tabPageNotes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._tabPageNotes.Name = "_tabPageNotes";
			this._tabPageNotes.Padding = new System.Windows.Forms.Padding(7);
			this._tabPageNotes.Size = new System.Drawing.Size(698, 386);
			this._tabPageNotes.TabIndex = 3;
			this._tabPageNotes.Text = "Notes";
			this._tabPageNotes.UseVisualStyleBackColor = true;
			// 
			// _textBoxNotes
			// 
			this._textBoxNotes.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxNotes.Location = new System.Drawing.Point(7, 7);
			this._textBoxNotes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxNotes.Multiline = true;
			this._textBoxNotes.Name = "_textBoxNotes";
			this._textBoxNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxNotes.Size = new System.Drawing.Size(684, 372);
			this._textBoxNotes.TabIndex = 0;
			// 
			// _textBoxQualitySpecifications
			// 
			this._textBoxQualitySpecifications.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxQualitySpecifications.Location = new System.Drawing.Point(111, 237);
			this._textBoxQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxQualitySpecifications.Name = "_textBoxQualitySpecifications";
			this._textBoxQualitySpecifications.ReadOnly = true;
			this._textBoxQualitySpecifications.Size = new System.Drawing.Size(582, 23);
			this._textBoxQualitySpecifications.TabIndex = 8;
			this._textBoxQualitySpecifications.TabStop = false;
			// 
			// _labelQualitySpecifications
			// 
			this._labelQualitySpecifications.AutoSize = true;
			this._labelQualitySpecifications.Location = new System.Drawing.Point(50, 240);
			this._labelQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelQualitySpecifications.Name = "_labelQualitySpecifications";
			this._labelQualitySpecifications.Size = new System.Drawing.Size(49, 15);
			this._labelQualitySpecifications.TabIndex = 32;
			this._labelQualitySpecifications.Text = "Used in:";
			this._labelQualitySpecifications.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUrl
			// 
			this._textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxUrl.Location = new System.Drawing.Point(111, 110);
			this._textBoxUrl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxUrl.Name = "_textBoxUrl";
			this._textBoxUrl.Size = new System.Drawing.Size(558, 23);
			this._textBoxUrl.TabIndex = 3;
			// 
			// _labelUrl
			// 
			this._labelUrl.AutoSize = true;
			this._labelUrl.Location = new System.Drawing.Point(65, 114);
			this._labelUrl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelUrl.Name = "_labelUrl";
			this._labelUrl.Size = new System.Drawing.Size(31, 15);
			this._labelUrl.TabIndex = 1;
			this._labelUrl.Text = "URL:";
			this._labelUrl.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _buttonOpenUrl
			// 
			this._buttonOpenUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOpenUrl.FlatAppearance.BorderSize = 0;
			this._buttonOpenUrl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this._buttonOpenUrl.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.OpenUrl;
			this._buttonOpenUrl.Location = new System.Drawing.Point(665, 106);
			this._buttonOpenUrl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonOpenUrl.Name = "_buttonOpenUrl";
			this._buttonOpenUrl.Size = new System.Drawing.Size(30, 30);
			this._buttonOpenUrl.TabIndex = 4;
			this._buttonOpenUrl.UseVisualStyleBackColor = true;
			this._buttonOpenUrl.Click += new System.EventHandler(this._buttonOpenUrl_Click);
			// 
			// _textBoxCategory
			// 
			this._textBoxCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategory.Location = new System.Drawing.Point(465, 15);
			this._textBoxCategory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxCategory.Name = "_textBoxCategory";
			this._textBoxCategory.ReadOnly = true;
			this._textBoxCategory.Size = new System.Drawing.Size(227, 23);
			this._textBoxCategory.TabIndex = 1;
			this._textBoxCategory.TabStop = false;
			// 
			// _labelCategory
			// 
			this._labelCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelCategory.AutoSize = true;
			this._labelCategory.Location = new System.Drawing.Point(398, 18);
			this._labelCategory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelCategory.Name = "_labelCategory";
			this._labelCategory.Size = new System.Drawing.Size(58, 15);
			this._labelCategory.TabIndex = 3;
			this._labelCategory.Text = "Category:";
			this._labelCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
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
			// _nullableBooleanComboboxIssueType
			// 
			this._nullableBooleanComboboxIssueType.DefaultText = "Use Default";
			this._nullableBooleanComboboxIssueType.FalseText = "Error";
			this._nullableBooleanComboboxIssueType.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._nullableBooleanComboboxIssueType.Location = new System.Drawing.Point(111, 140);
			this._nullableBooleanComboboxIssueType.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
			this._nullableBooleanComboboxIssueType.Name = "_nullableBooleanComboboxIssueType";
			this._nullableBooleanComboboxIssueType.Size = new System.Drawing.Size(112, 24);
			this._nullableBooleanComboboxIssueType.TabIndex = 5;
			this._nullableBooleanComboboxIssueType.TrueText = "Warning";
			this._nullableBooleanComboboxIssueType.Value = null;
			// 
			// _nullableBooleanComboboxStopOnError
			// 
			this._nullableBooleanComboboxStopOnError.DefaultText = "Use Default";
			this._nullableBooleanComboboxStopOnError.FalseText = "No";
			this._nullableBooleanComboboxStopOnError.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._nullableBooleanComboboxStopOnError.Location = new System.Drawing.Point(111, 171);
			this._nullableBooleanComboboxStopOnError.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
			this._nullableBooleanComboboxStopOnError.Name = "_nullableBooleanComboboxStopOnError";
			this._nullableBooleanComboboxStopOnError.Size = new System.Drawing.Size(112, 24);
			this._nullableBooleanComboboxStopOnError.TabIndex = 7;
			this._nullableBooleanComboboxStopOnError.TrueText = "Yes";
			this._nullableBooleanComboboxStopOnError.Value = null;
			// 
			// _objectReferenceControlTestDescriptor
			// 
			this._objectReferenceControlTestDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlTestDescriptor.DataSource = null;
			this._objectReferenceControlTestDescriptor.DisplayMember = null;
			this._objectReferenceControlTestDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlTestDescriptor.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._objectReferenceControlTestDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlTestDescriptor.Location = new System.Drawing.Point(111, 205);
			this._objectReferenceControlTestDescriptor.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._objectReferenceControlTestDescriptor.Name = "_objectReferenceControlTestDescriptor";
			this._objectReferenceControlTestDescriptor.ReadOnly = false;
			this._objectReferenceControlTestDescriptor.Size = new System.Drawing.Size(582, 35);
			this._objectReferenceControlTestDescriptor.TabIndex = 10;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn1.DataPropertyName = "QualitySpecificationName";
			this.dataGridViewTextBoxColumn1.HeaderText = "Quality Specification";
			this.dataGridViewTextBoxColumn1.MinimumWidth = 200;
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			// 
			// QualityConditionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._buttonOpenUrl);
			this.Controls.Add(this._tabControlDetails);
			this.Controls.Add(this._labelTestDescriptor);
			this.Controls.Add(this._labelTestDescriptorDefaultAllowErrors);
			this.Controls.Add(this._labelTestDescriptorDefaultStopOnError);
			this.Controls.Add(this._textBoxIssueTypeDefault);
			this.Controls.Add(this._textBoxStopOnErrorDefault);
			this.Controls.Add(this._labelQualitySpecifications);
			this.Controls.Add(this._labelAllowErrors);
			this.Controls.Add(this._labelStopOnError);
			this.Controls.Add(this._nullableBooleanComboboxIssueType);
			this.Controls.Add(this._nullableBooleanComboboxStopOnError);
			this.Controls.Add(this._labelCategory);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelUrl);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxCategory);
			this.Controls.Add(this._textBoxUrl);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._objectReferenceControlTestDescriptor);
			this.Controls.Add(this._textBoxQualitySpecifications);
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "QualityConditionControl";
			this.Size = new System.Drawing.Size(720, 688);
			this.Load += new System.EventHandler(this.QualityConditionControl_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.QualityConditionControl_Paint);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this._tabControlParameterValues.ResumeLayout(false);
			this.tabPageProperties.ResumeLayout(false);
			this._splitContainerProperties.Panel1.ResumeLayout(false);
			this._splitContainerProperties.Panel1.PerformLayout();
			this._splitContainerProperties.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerProperties)).EndInit();
			this._splitContainerProperties.ResumeLayout(false);
			this._panelDescriptionLabel.ResumeLayout(false);
			this._panelDescriptionLabel.PerformLayout();
			this._tabPageTableView.ResumeLayout(false);
			this._splitContainer.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			this._splitContainerHeader.Panel1.ResumeLayout(false);
			this._splitContainerHeader.Panel1.PerformLayout();
			this._splitContainerHeader.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerHeader)).EndInit();
			this._splitContainerHeader.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParamGrid)).EndInit();
			this._tabControlDetails.ResumeLayout(false);
			this._tabPageParameters.ResumeLayout(false);
			this._qualityConditionTableViewControlPanel.ResumeLayout(false);
			this._exportButtonPanel.ResumeLayout(false);
			this._tabPageQualitySpecifications.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualitySpecifications)).EndInit();
			this._toolStripElements.ResumeLayout(false);
			this._toolStripElements.PerformLayout();
			this._tabPageOptions.ResumeLayout(false);
			this._groupBoxIdentification.ResumeLayout(false);
			this._groupBoxIdentification.PerformLayout();
			this._groupBoxTablesWithoutGeometry.ResumeLayout(false);
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
	}
}
