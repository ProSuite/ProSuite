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
			this._splitContainer = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._panelParamsDesc = new System.Windows.Forms.Panel();
			this._textBoxDescGrid = new System.Windows.Forms.TextBox();
			this.labelDescGrid = new System.Windows.Forms.Label();
			this._panelParametersEdit = new System.Windows.Forms.Panel();
			this._panelParametersTop = new System.Windows.Forms.Panel();
			this._linkDocumentation = new System.Windows.Forms.LinkLabel();
			this._splitContainerHeader = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._splitContainerProperties = new ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._textBoxDescProps = new System.Windows.Forms.TextBox();
			this._panelDescriptionLabel = new System.Windows.Forms.Panel();
			this._labelParameterDescription = new System.Windows.Forms.Label();
			this._tabControlDetails = new System.Windows.Forms.TabControl();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this._tabPageNotes.SuspendLayout();
			this._tabPageReferencing.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewReferences)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this._tabPageParameters.SuspendLayout();
			this._instanceConfigTableViewControlPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			this._panelParamsDesc.SuspendLayout();
			this._panelParametersTop.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerHeader)).BeginInit();
			this._splitContainerHeader.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerProperties)).BeginInit();
			this._splitContainerProperties.Panel1.SuspendLayout();
			this._splitContainerProperties.SuspendLayout();
			this._panelDescriptionLabel.SuspendLayout();
			this._tabControlDetails.SuspendLayout();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(109, 15);
			this._textBoxName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(256, 23);
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
			this._textBoxDescription.Location = new System.Drawing.Point(109, 46);
			this._textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(592, 56);
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
			// _labelInstanceDescriptor
			// 
			this._labelInstanceDescriptor.AutoSize = true;
			this._labelInstanceDescriptor.Location = new System.Drawing.Point(4, 142);
			this._labelInstanceDescriptor.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._labelInstanceDescriptor.Name = "_labelInstanceDescriptor";
			this._labelInstanceDescriptor.Size = new System.Drawing.Size(95, 15);
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
			this._textBoxQualitySpecifications.Location = new System.Drawing.Point(109, 171);
			this._textBoxQualitySpecifications.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxQualitySpecifications.Name = "_textBoxQualitySpecifications";
			this._textBoxQualitySpecifications.ReadOnly = true;
			this._textBoxQualitySpecifications.Size = new System.Drawing.Size(592, 23);
			this._textBoxQualitySpecifications.TabIndex = 8;
			this._textBoxQualitySpecifications.TabStop = false;
			// 
			// _labelQualitySpecifications
			// 
			this._labelQualitySpecifications.AutoSize = true;
			this._labelQualitySpecifications.Location = new System.Drawing.Point(50, 174);
			this._labelQualitySpecifications.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
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
			this._textBoxUrl.Location = new System.Drawing.Point(109, 110);
			this._textBoxUrl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxUrl.Name = "_textBoxUrl";
			this._textBoxUrl.Size = new System.Drawing.Size(555, 23);
			this._textBoxUrl.TabIndex = 3;
			// 
			// _labelUrl
			// 
			this._labelUrl.AutoSize = true;
			this._labelUrl.Location = new System.Drawing.Point(68, 114);
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
			this._buttonOpenUrl.Location = new System.Drawing.Point(671, 106);
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
			this._textBoxCategory.Location = new System.Drawing.Point(466, 15);
			this._textBoxCategory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxCategory.Name = "_textBoxCategory";
			this._textBoxCategory.ReadOnly = true;
			this._textBoxCategory.Size = new System.Drawing.Size(234, 23);
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
			// _objectReferenceControlInstanceDescriptor
			// 
			this._objectReferenceControlInstanceDescriptor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlInstanceDescriptor.DataSource = null;
			this._objectReferenceControlInstanceDescriptor.DisplayMember = null;
			this._objectReferenceControlInstanceDescriptor.FindObjectDelegate = null;
			this._objectReferenceControlInstanceDescriptor.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this._objectReferenceControlInstanceDescriptor.FormatTextDelegate = null;
			this._objectReferenceControlInstanceDescriptor.Location = new System.Drawing.Point(109, 142);
			this._objectReferenceControlInstanceDescriptor.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._objectReferenceControlInstanceDescriptor.Name = "_objectReferenceControlInstanceDescriptor";
			this._objectReferenceControlInstanceDescriptor.ReadOnly = false;
			this._objectReferenceControlInstanceDescriptor.Size = new System.Drawing.Size(592, 23);
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
			this._tabPageNotes.Location = new System.Drawing.Point(4, 24);
			this._tabPageNotes.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageNotes.Name = "_tabPageNotes";
			this._tabPageNotes.Padding = new System.Windows.Forms.Padding(10, 12, 10, 12);
			this._tabPageNotes.Size = new System.Drawing.Size(693, 322);
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
			this._textBoxNotes.Size = new System.Drawing.Size(673, 298);
			this._textBoxNotes.TabIndex = 0;
			// 
			// _tabPageReferencing
			// 
			this._tabPageReferencing.Controls.Add(this._dataGridViewReferences);
			this._tabPageReferencing.Controls.Add(this._toolStripElements);
			this._tabPageReferencing.Location = new System.Drawing.Point(4, 24);
			this._tabPageReferencing.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageReferencing.Name = "_tabPageReferencing";
			this._tabPageReferencing.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageReferencing.Size = new System.Drawing.Size(693, 322);
			this._tabPageReferencing.TabIndex = 1;
			this._tabPageReferencing.Text = "Usage";
			this._tabPageReferencing.UseVisualStyleBackColor = true;
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
			this._dataGridViewReferences.Size = new System.Drawing.Size(681, 264);
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
			this._columnAlgorithm.Width = 86;
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
			this._toolStripElements.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.ImageScalingSize = new System.Drawing.Size(24, 24);
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
			this._toolStripElements.Location = new System.Drawing.Point(6, 5);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this._toolStripElements.Size = new System.Drawing.Size(681, 48);
			this._toolStripElements.TabIndex = 25;
			this._toolStripElements.Text = "Element Tools";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(164, 45);
			this.toolStripLabel1.Text = "This instance is referenced by:";
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
			this._tabPageParameters.Location = new System.Drawing.Point(4, 24);
			this._tabPageParameters.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageParameters.Name = "_tabPageParameters";
			this._tabPageParameters.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabPageParameters.Size = new System.Drawing.Size(693, 322);
			this._tabPageParameters.TabIndex = 0;
			this._tabPageParameters.Text = "Parameters";
			this._tabPageParameters.UseVisualStyleBackColor = true;
			// 
			// _instanceConfigTableViewControlPanel
			// 
			this._instanceConfigTableViewControlPanel.Controls.Add(this._splitContainer);
			this._instanceConfigTableViewControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._instanceConfigTableViewControlPanel.Location = new System.Drawing.Point(6, 5);
			this._instanceConfigTableViewControlPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this._instanceConfigTableViewControlPanel.Name = "_instanceConfigTableViewControlPanel";
			this._instanceConfigTableViewControlPanel.Size = new System.Drawing.Size(681, 312);
			this._instanceConfigTableViewControlPanel.TabIndex = 29;
			// 
			// _splitContainer
			// 
			this._splitContainer.BackColor = System.Drawing.Color.LightGray;
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.Location = new System.Drawing.Point(0, 0);
			this._splitContainer.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._splitContainer.Name = "_splitContainer";
			this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._splitContainer.Panel1.Controls.Add(this._panelParamsDesc);
			this._splitContainer.Panel1.Padding = new System.Windows.Forms.Padding(5);
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._splitContainer.Panel2.Controls.Add(this._panelParametersEdit);
			this._splitContainer.Panel2.Controls.Add(this._panelParametersTop);
			this._splitContainer.Size = new System.Drawing.Size(681, 312);
			this._splitContainer.SplitterDistance = 40;
			this._splitContainer.SplitterWidth = 7;
			this._splitContainer.TabIndex = 30;
			// 
			// _panelParamsDesc
			// 
			this._panelParamsDesc.Controls.Add(this._textBoxDescGrid);
			this._panelParamsDesc.Controls.Add(this.labelDescGrid);
			this._panelParamsDesc.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelParamsDesc.Location = new System.Drawing.Point(5, 5);
			this._panelParamsDesc.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._panelParamsDesc.Name = "_panelParamsDesc";
			this._panelParamsDesc.Size = new System.Drawing.Size(671, 30);
			this._panelParamsDesc.TabIndex = 27;
			// 
			// _textBoxDescGrid
			// 
			this._textBoxDescGrid.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._textBoxDescGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._textBoxDescGrid.Location = new System.Drawing.Point(75, 0);
			this._textBoxDescGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._textBoxDescGrid.Multiline = true;
			this._textBoxDescGrid.Name = "_textBoxDescGrid";
			this._textBoxDescGrid.ReadOnly = true;
			this._textBoxDescGrid.Size = new System.Drawing.Size(596, 30);
			this._textBoxDescGrid.TabIndex = 21;
			// 
			// labelDescGrid
			// 
			this.labelDescGrid.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelDescGrid.Location = new System.Drawing.Point(0, 0);
			this.labelDescGrid.Margin = new System.Windows.Forms.Padding(6, 5, 6, 0);
			this.labelDescGrid.Name = "labelDescGrid";
			this.labelDescGrid.Size = new System.Drawing.Size(75, 30);
			this.labelDescGrid.TabIndex = 22;
			this.labelDescGrid.Text = "Description:";
			this.labelDescGrid.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _panelParametersEdit
			// 
			this._panelParametersEdit.BackColor = System.Drawing.Color.Transparent;
			this._panelParametersEdit.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelParametersEdit.Location = new System.Drawing.Point(0, 27);
			this._panelParametersEdit.Margin = new System.Windows.Forms.Padding(0);
			this._panelParametersEdit.Name = "_panelParametersEdit";
			this._panelParametersEdit.Size = new System.Drawing.Size(681, 238);
			this._panelParametersEdit.TabIndex = 1;
			// 
			// _panelParametersTop
			// 
			this._panelParametersTop.BackColor = System.Drawing.Color.Transparent;
			this._panelParametersTop.Controls.Add(this._linkDocumentation);
			this._panelParametersTop.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelParametersTop.Location = new System.Drawing.Point(0, 0);
			this._panelParametersTop.Name = "_panelParametersTop";
			this._panelParametersTop.Size = new System.Drawing.Size(681, 27);
			this._panelParametersTop.TabIndex = 0;
			// 
			// _linkDocumentation
			// 
			this._linkDocumentation.AutoSize = true;
			this._linkDocumentation.Location = new System.Drawing.Point(6, 9);
			this._linkDocumentation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._linkDocumentation.Name = "_linkDocumentation";
			this._linkDocumentation.Size = new System.Drawing.Size(179, 15);
			this._linkDocumentation.TabIndex = 23;
			this._linkDocumentation.TabStop = true;
			this._linkDocumentation.Text = "Show Parameter Documentation";
			this._linkDocumentation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this._linkDocumentation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._linkDocumentation_LinkClicked);
			// 
			// _splitContainerHeader
			// 
			this._splitContainerHeader.Location = new System.Drawing.Point(0, 0);
			this._splitContainerHeader.Name = "_splitContainerHeader";
			this._splitContainerHeader.Size = new System.Drawing.Size(150, 100);
			this._splitContainerHeader.TabIndex = 0;
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
			this._splitContainerProperties.Panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this._splitContainerProperties.Size = new System.Drawing.Size(661, 274);
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
			this._textBoxDescProps.Size = new System.Drawing.Size(538, 74);
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
			this._labelParameterDescription.Size = new System.Drawing.Size(70, 15);
			this._labelParameterDescription.TabIndex = 22;
			this._labelParameterDescription.Text = "Description:";
			this._labelParameterDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabControlDetails
			// 
			this._tabControlDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControlDetails.Controls.Add(this._tabPageParameters);
			this._tabControlDetails.Controls.Add(this._tabPageReferencing);
			this._tabControlDetails.Controls.Add(this._tabPageNotes);
			this._tabControlDetails.Location = new System.Drawing.Point(14, 204);
			this._tabControlDetails.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this._tabControlDetails.Name = "_tabControlDetails";
			this._tabControlDetails.SelectedIndex = 0;
			this._tabControlDetails.Size = new System.Drawing.Size(701, 350);
			this._tabControlDetails.TabIndex = 0;
			this._tabControlDetails.SelectedIndexChanged += new System.EventHandler(this._tabControlDetails_SelectedIndexChanged);
			// 
			// InstanceConfigurationControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._textBoxUrl);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._objectReferenceControlInstanceDescriptor);
			this.Controls.Add(this._textBoxQualitySpecifications);
			this.Controls.Add(this._buttonOpenUrl);
			this.Controls.Add(this._tabControlDetails);
			this.Controls.Add(this._labelQualitySpecifications);
			this.Controls.Add(this._labelCategory);
			this.Controls.Add(this._textBoxCategory);
			this.Controls.Add(this._labelInstanceDescriptor);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._labelUrl);
			this.Controls.Add(this._labelName);
			this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
			this.Name = "InstanceConfigurationControl";
			this.Size = new System.Drawing.Size(721, 559);
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
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			this._panelParamsDesc.ResumeLayout(false);
			this._panelParamsDesc.PerformLayout();
			this._panelParametersTop.ResumeLayout(false);
			this._panelParametersTop.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerHeader)).EndInit();
			this._splitContainerHeader.ResumeLayout(false);
			this._splitContainerProperties.Panel1.ResumeLayout(false);
			this._splitContainerProperties.Panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerProperties)).EndInit();
			this._splitContainerProperties.ResumeLayout(false);
			this._panelDescriptionLabel.ResumeLayout(false);
			this._panelDescriptionLabel.PerformLayout();
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
		private System.Windows.Forms.Label labelDescGrid;
        private System.Windows.Forms.TextBox _textBoxDescGrid;
        private System.Windows.Forms.LinkLabel _labelInstanceDescriptor;
        private System.Windows.Forms.OpenFileDialog openFileDialogImport;
        private System.Windows.Forms.SaveFileDialog saveFileDialogExport;
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
		private System.Windows.Forms.LinkLabel _linkDocumentation;
		private System.Windows.Forms.Panel _panelParametersTop;
		private System.Windows.Forms.Panel _panelParametersEdit;
		private System.Windows.Forms.Panel _panelParamsDesc;
	}
}