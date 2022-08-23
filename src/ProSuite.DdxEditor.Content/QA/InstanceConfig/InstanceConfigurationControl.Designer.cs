using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.AO.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
    partial class InstanceConfigurationControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._labelInstanceDescriptor = new System.Windows.Forms.LinkLabel();
			this.openFileDialogImport = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogExport = new System.Windows.Forms.SaveFileDialog();
			this._textBoxQualitySpecifications = new System.Windows.Forms.TextBox();
			this._labelQualitySpecifications = new System.Windows.Forms.Label();
			this._textBoxUrl = new System.Windows.Forms.TextBox();
			this._labelUrl = new System.Windows.Forms.Label();
			this._buttonOpenUrl = new System.Windows.Forms.Button();
			this._textBoxCategory = new System.Windows.Forms.TextBox();
			this._labelCategory = new System.Windows.Forms.Label();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this._objectReferenceControlInstanceDescriptor = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._tabPageNotes = new System.Windows.Forms.TabPage();
			this._textBoxNotes = new System.Windows.Forms.TextBox();
			this._tabPageReferencing = new System.Windows.Forms.TabPage();
			this._dataGridViewReferences = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnAlgorithm = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._toolStripElements = new ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.miniToolStrip = new ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._tabPageParameters = new System.Windows.Forms.TabPage();
			this._instanceConfigTableViewControlPanel = new System.Windows.Forms.Panel();
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
			this._textBoxDescGrid = new System.Windows.Forms.TextBox();
			this.labelDescGrid = new System.Windows.Forms.Label();
			this._dataGridViewParamGrid = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this.labelParamGrid = new System.Windows.Forms.Label();
			this._tabControlDetails = new System.Windows.Forms.TabControl();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this._tabPageNotes.SuspendLayout();
			this._tabPageReferencing.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewReferences)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this._tabPageParameters.SuspendLayout();
			this._instanceConfigTableViewControlPanel.SuspendLayout();
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
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(159, 25);
			this._textBoxName.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(375, 31);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(92, 29);
			this._labelName.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(63, 25);
			this._labelName.TabIndex = 1;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(159, 77);
			this._textBoxDescription.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(830, 91);
			this._textBoxDescription.TabIndex = 2;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(50, 83);
			this._labelDescription.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(106, 25);
			this._labelDescription.TabIndex = 3;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _labelInstanceDescriptor
			// 
			this._labelInstanceDescriptor.AutoSize = true;
			this._labelInstanceDescriptor.Location = new System.Drawing.Point(12, 233);
			this._labelInstanceDescriptor.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelInstanceDescriptor.Name = "_labelInstanceDescriptor";
			this._labelInstanceDescriptor.Size = new System.Drawing.Size(142, 25);
			this._labelInstanceDescriptor.TabIndex = 9;
			this._labelInstanceDescriptor.TabStop = true;
			this._labelInstanceDescriptor.Text = "Implementation:";
			this._labelInstanceDescriptor.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this._labelInstanceDescriptor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._labelInstanceDescriptor_LinkClicked);
			// 
			// _textBoxQualitySpecifications
			// 
			this._textBoxQualitySpecifications.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxQualitySpecifications.Location = new System.Drawing.Point(159, 274);
			this._textBoxQualitySpecifications.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxQualitySpecifications.Name = "_textBoxQualitySpecifications";
			this._textBoxQualitySpecifications.ReadOnly = true;
			this._textBoxQualitySpecifications.Size = new System.Drawing.Size(834, 31);
			this._textBoxQualitySpecifications.TabIndex = 8;
			this._textBoxQualitySpecifications.TabStop = false;
			// 
			// _labelQualitySpecifications
			// 
			this._labelQualitySpecifications.AutoSize = true;
			this._labelQualitySpecifications.Location = new System.Drawing.Point(77, 279);
			this._labelQualitySpecifications.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelQualitySpecifications.Name = "_labelQualitySpecifications";
			this._labelQualitySpecifications.Size = new System.Drawing.Size(75, 25);
			this._labelQualitySpecifications.TabIndex = 32;
			this._labelQualitySpecifications.Text = "Used in:";
			this._labelQualitySpecifications.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUrl
			// 
			this._textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxUrl.Location = new System.Drawing.Point(159, 183);
			this._textBoxUrl.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxUrl.Name = "_textBoxUrl";
			this._textBoxUrl.Size = new System.Drawing.Size(779, 31);
			this._textBoxUrl.TabIndex = 3;
			// 
			// _labelUrl
			// 
			this._labelUrl.AutoSize = true;
			this._labelUrl.Location = new System.Drawing.Point(106, 188);
			this._labelUrl.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelUrl.Name = "_labelUrl";
			this._labelUrl.Size = new System.Drawing.Size(47, 25);
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
			this._buttonOpenUrl.Location = new System.Drawing.Point(950, 177);
			this._buttonOpenUrl.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._buttonOpenUrl.Name = "_buttonOpenUrl";
			this._buttonOpenUrl.Size = new System.Drawing.Size(43, 50);
			this._buttonOpenUrl.TabIndex = 4;
			this._buttonOpenUrl.UseVisualStyleBackColor = true;
			this._buttonOpenUrl.Click += new System.EventHandler(this._buttonOpenUrl_Click);
			// 
			// _textBoxCategory
			// 
			this._textBoxCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategory.Location = new System.Drawing.Point(664, 25);
			this._textBoxCategory.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxCategory.Name = "_textBoxCategory";
			this._textBoxCategory.ReadOnly = true;
			this._textBoxCategory.Size = new System.Drawing.Size(323, 31);
			this._textBoxCategory.TabIndex = 1;
			this._textBoxCategory.TabStop = false;
			// 
			// _labelCategory
			// 
			this._labelCategory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelCategory.AutoSize = true;
			this._labelCategory.Location = new System.Drawing.Point(569, 30);
			this._labelCategory.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelCategory.Name = "_labelCategory";
			this._labelCategory.Size = new System.Drawing.Size(88, 25);
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
			// _objectReferenceControlInstanceDescriptor
			// 
			this._objectReferenceControlInstanceDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlInstanceDescriptor.DataSource = null;
			this._objectReferenceControlInstanceDescriptor.DisplayMember = null;
			this._objectReferenceControlInstanceDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlInstanceDescriptor.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._objectReferenceControlInstanceDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlInstanceDescriptor.Location = new System.Drawing.Point(159, 233);
			this._objectReferenceControlInstanceDescriptor.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._objectReferenceControlInstanceDescriptor.Name = "_objectReferenceControlInstanceDescriptor";
			this._objectReferenceControlInstanceDescriptor.ReadOnly = false;
			this._objectReferenceControlInstanceDescriptor.Size = new System.Drawing.Size(834, 37);
			this._objectReferenceControlInstanceDescriptor.TabIndex = 10;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.MinimumWidth = 8;
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.Width = 150;
			// 
			// _tabPageNotes
			// 
			this._tabPageNotes.Controls.Add(this._textBoxNotes);
			this._tabPageNotes.Location = new System.Drawing.Point(4, 34);
			this._tabPageNotes.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageNotes.Name = "_tabPageNotes";
			this._tabPageNotes.Padding = new System.Windows.Forms.Padding(10, 12, 10, 12);
			this._tabPageNotes.Size = new System.Drawing.Size(1001, 388);
			this._tabPageNotes.TabIndex = 3;
			this._tabPageNotes.Text = "Notes";
			this._tabPageNotes.UseVisualStyleBackColor = true;
			// 
			// _textBoxNotes
			// 
			this._textBoxNotes.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxNotes.Location = new System.Drawing.Point(10, 12);
			this._textBoxNotes.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxNotes.Multiline = true;
			this._textBoxNotes.Name = "_textBoxNotes";
			this._textBoxNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxNotes.Size = new System.Drawing.Size(981, 364);
			this._textBoxNotes.TabIndex = 0;
			// 
			// _tabPageReferencing
			// 
			this._tabPageReferencing.Controls.Add(this._dataGridViewReferences);
			this._tabPageReferencing.Controls.Add(this._toolStripElements);
			this._tabPageReferencing.Location = new System.Drawing.Point(4, 34);
			this._tabPageReferencing.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageReferencing.Name = "_tabPageReferencing";
			this._tabPageReferencing.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageReferencing.Size = new System.Drawing.Size(1001, 388);
			this._tabPageReferencing.TabIndex = 1;
			this._tabPageReferencing.Text = "Usage";
			// 
			// _dataGridViewReferences
			// 
			this._dataGridViewReferences.AllowUserToAddRows = false;
			this._dataGridViewReferences.AllowUserToDeleteRows = false;
			this._dataGridViewReferences.AllowUserToResizeRows = false;
			this._dataGridViewReferences.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewReferences.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnImage,
            this._columnName,
            this._columnType,
            this._columnAlgorithm,
            this._columnDescription,
            this._columnCategory});
			this._dataGridViewReferences.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewReferences.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridViewReferences.Location = new System.Drawing.Point(6, 53);
			this._dataGridViewReferences.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._dataGridViewReferences.Name = "_dataGridViewReferences";
			this._dataGridViewReferences.RowHeadersVisible = false;
			this._dataGridViewReferences.RowHeadersWidth = 62;
			this._dataGridViewReferences.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewReferences.Size = new System.Drawing.Size(989, 330);
			this._dataGridViewReferences.TabIndex = 24;
			this._dataGridViewReferences.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellDoubleClick);
			this._dataGridViewReferences.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellEndEdit);
			this._dataGridViewReferences.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewQualitySpecifications_CellValueChanged);
			// 
			// _columnImage
			// 
			this._columnImage.DataPropertyName = "Image";
			this._columnImage.HeaderText = "";
			this._columnImage.MinimumWidth = 20;
			this._columnImage.Name = "_columnImage";
			this._columnImage.ReadOnly = true;
			this._columnImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnImage.Width = 30;
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
			// _columnType
			// 
			this._columnType.DataPropertyName = "Type";
			this._columnType.HeaderText = "Type";
			this._columnType.MinimumWidth = 8;
			this._columnType.Name = "_columnType";
			this._columnType.ReadOnly = true;
			this._columnType.Width = 180;
			// 
			// _columnAlgorithm
			// 
			this._columnAlgorithm.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnAlgorithm.DataPropertyName = "AlgorithmImplementation";
			this._columnAlgorithm.HeaderText = "Algorithm";
			this._columnAlgorithm.MinimumWidth = 8;
			this._columnAlgorithm.Name = "_columnAlgorithm";
			this._columnAlgorithm.Width = 128;
			// 
			// _columnDescription
			// 
			this._columnDescription.DataPropertyName = "Description";
			this._columnDescription.HeaderText = "Description";
			this._columnDescription.MinimumWidth = 8;
			this._columnDescription.Name = "_columnDescription";
			this._columnDescription.Width = 150;
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
			// _toolStripElements
			// 
			this._toolStripElements.AutoSize = false;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.ImageScalingSize = new System.Drawing.Size(24, 24);
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
			this._toolStripElements.Location = new System.Drawing.Point(6, 5);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this._toolStripElements.Size = new System.Drawing.Size(989, 48);
			this._toolStripElements.TabIndex = 25;
			this._toolStripElements.Text = "Element Tools";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(235, 43);
			this.toolStripLabel1.Text = "This instance is reference by:";
			// 
			// miniToolStrip
			// 
			this.miniToolStrip.AccessibleName = "New item selection";
			this.miniToolStrip.AccessibleRole = System.Windows.Forms.AccessibleRole.ButtonDropDown;
			this.miniToolStrip.AutoSize = false;
			this.miniToolStrip.CanOverflow = false;
			this.miniToolStrip.ClickThrough = true;
			this.miniToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.miniToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.miniToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.miniToolStrip.Location = new System.Drawing.Point(159, 10);
			this.miniToolStrip.Name = "miniToolStrip";
			this.miniToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.miniToolStrip.Size = new System.Drawing.Size(989, 48);
			this.miniToolStrip.TabIndex = 25;
			// 
			// _tabPageParameters
			// 
			this._tabPageParameters.Controls.Add(this._instanceConfigTableViewControlPanel);
			this._tabPageParameters.Location = new System.Drawing.Point(4, 34);
			this._tabPageParameters.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageParameters.Name = "_tabPageParameters";
			this._tabPageParameters.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageParameters.Size = new System.Drawing.Size(1001, 388);
			this._tabPageParameters.TabIndex = 0;
			this._tabPageParameters.Text = "Parameters";
			// 
			// _instanceConfigTableViewControlPanel
			// 
			this._instanceConfigTableViewControlPanel.Controls.Add(this._tabControlParameterValues);
			this._instanceConfigTableViewControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._instanceConfigTableViewControlPanel.Location = new System.Drawing.Point(6, 5);
			this._instanceConfigTableViewControlPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this._instanceConfigTableViewControlPanel.Name = "_instanceConfigTableViewControlPanel";
			this._instanceConfigTableViewControlPanel.Size = new System.Drawing.Size(989, 378);
			this._instanceConfigTableViewControlPanel.TabIndex = 29;
			// 
			// _tabControlParameterValues
			// 
			this._tabControlParameterValues.Controls.Add(this.tabPageProperties);
			this._tabControlParameterValues.Controls.Add(this._tabPageTableView);
			this._tabControlParameterValues.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControlParameterValues.Location = new System.Drawing.Point(0, 0);
			this._tabControlParameterValues.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabControlParameterValues.Name = "_tabControlParameterValues";
			this._tabControlParameterValues.SelectedIndex = 0;
			this._tabControlParameterValues.Size = new System.Drawing.Size(989, 378);
			this._tabControlParameterValues.TabIndex = 28;
			this._tabControlParameterValues.SelectedIndexChanged += new System.EventHandler(this._tabControlParameterValues_SelectedIndexChanged);
			// 
			// tabPageProperties
			// 
			this.tabPageProperties.Controls.Add(this._splitContainerProperties);
			this.tabPageProperties.Location = new System.Drawing.Point(4, 34);
			this.tabPageProperties.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this.tabPageProperties.Name = "tabPageProperties";
			this.tabPageProperties.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this.tabPageProperties.Size = new System.Drawing.Size(981, 340);
			this.tabPageProperties.TabIndex = 0;
			this.tabPageProperties.Text = "Parameter Values";
			this.tabPageProperties.UseVisualStyleBackColor = true;
			// 
			// _splitContainerProperties
			// 
			this._splitContainerProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerProperties.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerProperties.Location = new System.Drawing.Point(6, 5);
			this._splitContainerProperties.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
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
			this._splitContainerProperties.Size = new System.Drawing.Size(969, 330);
			this._splitContainerProperties.SplitterDistance = 74;
			this._splitContainerProperties.SplitterWidth = 8;
			this._splitContainerProperties.TabIndex = 31;
			// 
			// _textBoxDescProps
			// 
			this._textBoxDescProps.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxDescProps.Location = new System.Drawing.Point(123, 0);
			this._textBoxDescProps.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxDescProps.Multiline = true;
			this._textBoxDescProps.Name = "_textBoxDescProps";
			this._textBoxDescProps.ReadOnly = true;
			this._textBoxDescProps.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescProps.Size = new System.Drawing.Size(846, 74);
			this._textBoxDescProps.TabIndex = 0;
			// 
			// _panelDescriptionLabel
			// 
			this._panelDescriptionLabel.Controls.Add(this._labelParameterDescription);
			this._panelDescriptionLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this._panelDescriptionLabel.Location = new System.Drawing.Point(0, 0);
			this._panelDescriptionLabel.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._panelDescriptionLabel.MaximumSize = new System.Drawing.Size(123, 19230);
			this._panelDescriptionLabel.MinimumSize = new System.Drawing.Size(123, 0);
			this._panelDescriptionLabel.Name = "_panelDescriptionLabel";
			this._panelDescriptionLabel.Size = new System.Drawing.Size(123, 74);
			this._panelDescriptionLabel.TabIndex = 23;
			// 
			// _labelParameterDescription
			// 
			this._labelParameterDescription.AutoSize = true;
			this._labelParameterDescription.Location = new System.Drawing.Point(9, 5);
			this._labelParameterDescription.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelParameterDescription.Name = "_labelParameterDescription";
			this._labelParameterDescription.Size = new System.Drawing.Size(106, 25);
			this._labelParameterDescription.TabIndex = 22;
			this._labelParameterDescription.Text = "Description:";
			this._labelParameterDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _propertyGrid
			// 
			this._propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._propertyGrid.Location = new System.Drawing.Point(0, 0);
			this._propertyGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._propertyGrid.Name = "_propertyGrid";
			this._propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this._propertyGrid.Size = new System.Drawing.Size(969, 248);
			this._propertyGrid.TabIndex = 0;
			this._propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this._propertyGrid_PropertyValueChanged);
			// 
			// _tabPageTableView
			// 
			this._tabPageTableView.Controls.Add(this._splitContainer);
			this._tabPageTableView.Location = new System.Drawing.Point(4, 34);
			this._tabPageTableView.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageTableView.Name = "_tabPageTableView";
			this._tabPageTableView.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageTableView.Size = new System.Drawing.Size(981, 340);
			this._tabPageTableView.TabIndex = 1;
			this._tabPageTableView.Text = "Table View";
			this._tabPageTableView.UseVisualStyleBackColor = true;
			// 
			// _splitContainer
			// 
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.Location = new System.Drawing.Point(6, 5);
			this._splitContainer.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._splitContainer.Name = "_splitContainer";
			this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._splitContainerHeader);
			this._splitContainer.Size = new System.Drawing.Size(969, 330);
			this._splitContainer.SplitterDistance = 179;
			this._splitContainer.SplitterWidth = 7;
			this._splitContainer.TabIndex = 30;
			// 
			// _splitContainerHeader
			// 
			this._splitContainerHeader.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerHeader.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerHeader.Location = new System.Drawing.Point(0, 0);
			this._splitContainerHeader.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._splitContainerHeader.Name = "_splitContainerHeader";
			this._splitContainerHeader.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerHeader.Panel1
			// 
			this._splitContainerHeader.Panel1.Controls.Add(this._textBoxDescGrid);
			this._splitContainerHeader.Panel1.Controls.Add(this.labelDescGrid);
			// 
			// _splitContainerHeader.Panel2
			// 
			this._splitContainerHeader.Panel2.Controls.Add(this._dataGridViewParamGrid);
			this._splitContainerHeader.Panel2.Controls.Add(this.labelParamGrid);
			this._splitContainerHeader.Panel2MinSize = 50;
			this._splitContainerHeader.Size = new System.Drawing.Size(969, 179);
			this._splitContainerHeader.SplitterDistance = 48;
			this._splitContainerHeader.SplitterWidth = 8;
			this._splitContainerHeader.TabIndex = 26;
			// 
			// _textBoxDescGrid
			// 
			this._textBoxDescGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxDescGrid.Location = new System.Drawing.Point(133, 0);
			this._textBoxDescGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxDescGrid.Multiline = true;
			this._textBoxDescGrid.Name = "_textBoxDescGrid";
			this._textBoxDescGrid.ReadOnly = true;
			this._textBoxDescGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescGrid.Size = new System.Drawing.Size(836, 48);
			this._textBoxDescGrid.TabIndex = 21;
			// 
			// labelDescGrid
			// 
			this.labelDescGrid.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelDescGrid.Location = new System.Drawing.Point(0, 0);
			this.labelDescGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 0);
			this.labelDescGrid.Name = "labelDescGrid";
			this.labelDescGrid.Size = new System.Drawing.Size(133, 48);
			this.labelDescGrid.TabIndex = 22;
			this.labelDescGrid.Text = "Description:";
			this.labelDescGrid.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _dataGridViewParamGrid
			// 
			this._dataGridViewParamGrid.AllowUserToAddRows = false;
			this._dataGridViewParamGrid.AllowUserToDeleteRows = false;
			this._dataGridViewParamGrid.AllowUserToResizeRows = false;
			this._dataGridViewParamGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewParamGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridViewParamGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewParamGrid.DefaultCellStyle = dataGridViewCellStyle2;
			this._dataGridViewParamGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewParamGrid.Location = new System.Drawing.Point(133, 0);
			this._dataGridViewParamGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._dataGridViewParamGrid.Name = "_dataGridViewParamGrid";
			this._dataGridViewParamGrid.ReadOnly = true;
			this._dataGridViewParamGrid.RowHeadersVisible = false;
			this._dataGridViewParamGrid.RowHeadersWidth = 62;
			this._dataGridViewParamGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewParamGrid.Size = new System.Drawing.Size(836, 123);
			this._dataGridViewParamGrid.TabIndex = 24;
			this._dataGridViewParamGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this._dataGridViewParamGrid_DataBindingComplete);
			// 
			// labelParamGrid
			// 
			this.labelParamGrid.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelParamGrid.Location = new System.Drawing.Point(0, 0);
			this.labelParamGrid.Margin = new System.Windows.Forms.Padding(0, 5, 6, 0);
			this.labelParamGrid.Name = "labelParamGrid";
			this.labelParamGrid.Size = new System.Drawing.Size(133, 123);
			this.labelParamGrid.TabIndex = 23;
			this.labelParamGrid.Text = "Parameters:";
			this.labelParamGrid.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControlDetails
			// 
			this._tabControlDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControlDetails.Controls.Add(this._tabPageParameters);
			this._tabControlDetails.Controls.Add(this._tabPageReferencing);
			this._tabControlDetails.Controls.Add(this._tabPageNotes);
			this._tabControlDetails.Location = new System.Drawing.Point(14, 320);
			this._tabControlDetails.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabControlDetails.Name = "_tabControlDetails";
			this._tabControlDetails.SelectedIndex = 0;
			this._tabControlDetails.Size = new System.Drawing.Size(1009, 426);
			this._tabControlDetails.TabIndex = 0;
			this._tabControlDetails.SelectedIndexChanged += new System.EventHandler(this._tabControlDetails_SelectedIndexChanged);
			// 
			// InstanceConfigurationControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._buttonOpenUrl);
			this.Controls.Add(this._tabControlDetails);
			this.Controls.Add(this._labelInstanceDescriptor);
			this.Controls.Add(this._labelQualitySpecifications);
			this.Controls.Add(this._labelCategory);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelUrl);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxCategory);
			this.Controls.Add(this._textBoxUrl);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._objectReferenceControlInstanceDescriptor);
			this.Controls.Add(this._textBoxQualitySpecifications);
			this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this.Name = "InstanceConfigurationControl";
			this.Size = new System.Drawing.Size(1029, 751);
			this.Load += new System.EventHandler(this.QualityConditionControl_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.QualityConditionControl_Paint);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this._tabPageNotes.ResumeLayout(false);
			this._tabPageNotes.PerformLayout();
			this._tabPageReferencing.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewReferences)).EndInit();
			this._toolStripElements.ResumeLayout(false);
			this._toolStripElements.PerformLayout();
			this._tabPageParameters.ResumeLayout(false);
			this._instanceConfigTableViewControlPanel.ResumeLayout(false);
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
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private ObjectReferenceControl _objectReferenceControlInstanceDescriptor;
        private System.Windows.Forms.Label labelParamGrid;
        private System.Windows.Forms.Label labelDescGrid;
        private System.Windows.Forms.TextBox _textBoxDescGrid;
        private System.Windows.Forms.LinkLabel _labelInstanceDescriptor;
        private System.Windows.Forms.OpenFileDialog openFileDialogImport;
        private System.Windows.Forms.SaveFileDialog saveFileDialogExport;
        private System.Windows.Forms.TabControl _tabControlParameterValues;
        private System.Windows.Forms.TabPage tabPageProperties;
        private System.Windows.Forms.TabPage _tabPageTableView;
        private System.Windows.Forms.PropertyGrid _propertyGrid;
        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerProperties;
        private System.Windows.Forms.Label _labelParameterDescription;
        private System.Windows.Forms.TextBox _textBoxDescProps;
        private System.Windows.Forms.TabControl _tabControlDetails;
        private System.Windows.Forms.TabPage _tabPageParameters;
		private System.Windows.Forms.TabPage _tabPageReferencing;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.TextBox _textBoxQualitySpecifications;
        private System.Windows.Forms.Label _labelQualitySpecifications;
		private DoubleBufferedDataGridView _dataGridViewParamGrid;
		private DoubleBufferedDataGridView _dataGridViewReferences;
		private ToolStripEx _toolStripElements;
		private System.Windows.Forms.Label _labelUrl;
		private System.Windows.Forms.TextBox _textBoxUrl;
		private System.Windows.Forms.Button _buttonOpenUrl;
		private System.Windows.Forms.Panel _panelDescriptionLabel;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerHeader;
		private System.Windows.Forms.Label _labelCategory;
		private System.Windows.Forms.TextBox _textBoxCategory;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.TabPage _tabPageNotes;
		private System.Windows.Forms.TextBox _textBoxNotes;
		private System.Windows.Forms.Panel _instanceConfigTableViewControlPanel;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnAlgorithm;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDescription;
		private ToolStripEx miniToolStrip;
	}
}
