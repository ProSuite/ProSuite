using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
    partial class TestDescriptorControl
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
			this._groupBoxClass = new System.Windows.Forms.GroupBox();
			this._comboBoxConstructorIndex = new System.Windows.Forms.ComboBox();
			this._objectReferenceControlTestClass = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._labelParameter = new System.Windows.Forms.Label();
			this._dataGridViewParameter = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._objectReferenceControlTestFactory = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._booleanComboboxStopOnError = new global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this._labelStopOnError = new System.Windows.Forms.Label();
			this._booleanComboboxAllowErrors = new global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this._labelAllowErrors = new System.Windows.Forms.Label();
			this._groupBoxFactory = new System.Windows.Forms.GroupBox();
			this._labelFactoryClass = new System.Windows.Forms.Label();
			this._groupBoxClassOrFactory = new System.Windows.Forms.GroupBox();
			this._groupBoxTestConfigurator = new System.Windows.Forms.GroupBox();
			this._labelConfiguratorClass = new System.Windows.Forms.Label();
			this._objectReferenceControlTestConfigurator = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageImplementation = new System.Windows.Forms.TabPage();
			this._splitContainerDescription = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._textBoxCategories = new System.Windows.Forms.TextBox();
			this._labelTestCategories = new System.Windows.Forms.Label();
			this._tabPageQualityConditions = new System.Windows.Forms.TabPage();
			this._dataGridViewQualityConditions = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnQualityConditionImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnStopOnError = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnQualityConditionDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this._toolStripElements = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this._numericUpDownExecutionPriority = new global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable();
			this._labelExecutionPriority = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this._groupBoxClass.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParameter)).BeginInit();
			this._groupBoxFactory.SuspendLayout();
			this._groupBoxClassOrFactory.SuspendLayout();
			this._groupBoxTestConfigurator.SuspendLayout();
			this._tabControl.SuspendLayout();
			this._tabPageImplementation.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDescription)).BeginInit();
			this._splitContainerDescription.Panel1.SuspendLayout();
			this._splitContainerDescription.Panel2.SuspendLayout();
			this._splitContainerDescription.SuspendLayout();
			this._tabPageQualityConditions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualityConditions)).BeginInit();
			this._toolStripElements.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(37, 44);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 2;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(106, 41);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(520, 83);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(62, 17);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(106, 14);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(520, 20);
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
			this._labelTestClass.Location = new System.Drawing.Point(39, 22);
			this._labelTestClass.Name = "_labelTestClass";
			this._labelTestClass.Size = new System.Drawing.Size(35, 13);
			this._labelTestClass.TabIndex = 0;
			this._labelTestClass.Text = "Class:";
			this._labelTestClass.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelConstructorId
			// 
			this._labelConstructorId.AutoSize = true;
			this._labelConstructorId.Location = new System.Drawing.Point(10, 48);
			this._labelConstructorId.Name = "_labelConstructorId";
			this._labelConstructorId.Size = new System.Drawing.Size(64, 13);
			this._labelConstructorId.TabIndex = 2;
			this._labelConstructorId.Text = "Constructor:";
			this._labelConstructorId.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelTestDescription
			// 
			this._labelTestDescription.AutoSize = true;
			this._labelTestDescription.Location = new System.Drawing.Point(1, 32);
			this._labelTestDescription.Name = "_labelTestDescription";
			this._labelTestDescription.Size = new System.Drawing.Size(85, 13);
			this._labelTestDescription.TabIndex = 10;
			this._labelTestDescription.Text = "Test description:";
			this._labelTestDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxTestDescription
			// 
			this._textBoxTestDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTestDescription.Location = new System.Drawing.Point(92, 29);
			this._textBoxTestDescription.Multiline = true;
			this._textBoxTestDescription.Name = "_textBoxTestDescription";
			this._textBoxTestDescription.ReadOnly = true;
			this._textBoxTestDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxTestDescription.Size = new System.Drawing.Size(532, 38);
			this._textBoxTestDescription.TabIndex = 11;
			this._textBoxTestDescription.TabStop = false;
			// 
			// _groupBoxClass
			// 
			this._groupBoxClass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxClass.Controls.Add(this._comboBoxConstructorIndex);
			this._groupBoxClass.Controls.Add(this._objectReferenceControlTestClass);
			this._groupBoxClass.Controls.Add(this._labelTestClass);
			this._groupBoxClass.Controls.Add(this._labelConstructorId);
			this._groupBoxClass.Location = new System.Drawing.Point(13, 85);
			this._groupBoxClass.Name = "_groupBoxClass";
			this._groupBoxClass.Size = new System.Drawing.Size(599, 79);
			this._groupBoxClass.TabIndex = 1;
			this._groupBoxClass.TabStop = false;
			this._groupBoxClass.Text = "Test Class";
			// 
			// _comboBoxConstructorIndex
			// 
			this._comboBoxConstructorIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._comboBoxConstructorIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._comboBoxConstructorIndex.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._comboBoxConstructorIndex.FormattingEnabled = true;
			this._comboBoxConstructorIndex.Location = new System.Drawing.Point(80, 45);
			this._comboBoxConstructorIndex.Name = "_comboBoxConstructorIndex";
			this._comboBoxConstructorIndex.Size = new System.Drawing.Size(497, 22);
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
			this._objectReferenceControlTestClass.Location = new System.Drawing.Point(80, 19);
			this._objectReferenceControlTestClass.Name = "_objectReferenceControlTestClass";
			this._objectReferenceControlTestClass.ReadOnly = false;
			this._objectReferenceControlTestClass.Size = new System.Drawing.Size(497, 20);
			this._objectReferenceControlTestClass.TabIndex = 1;
			// 
			// _labelParameter
			// 
			this._labelParameter.AutoSize = true;
			this._labelParameter.Location = new System.Drawing.Point(0, 5);
			this._labelParameter.Name = "_labelParameter";
			this._labelParameter.Size = new System.Drawing.Size(86, 13);
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
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewParameter.DefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridViewParameter.Location = new System.Drawing.Point(92, 3);
			this._dataGridViewParameter.Name = "_dataGridViewParameter";
			this._dataGridViewParameter.ReadOnly = true;
			this._dataGridViewParameter.RowHeadersVisible = false;
			this._dataGridViewParameter.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewParameter.Size = new System.Drawing.Size(532, 96);
			this._dataGridViewParameter.ShowCellToolTips = false;
			this._dataGridViewParameter.TabIndex = 13;
			// 
			// _objectReferenceControlTestFactory
			// 
			this._objectReferenceControlTestFactory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlTestFactory.DataSource = null;
			this._objectReferenceControlTestFactory.DisplayMember = null;
			this._objectReferenceControlTestFactory.FindObjectDelegate = null;
			this._objectReferenceControlTestFactory.FormatTextDelegate = null;
			this._objectReferenceControlTestFactory.Location = new System.Drawing.Point(80, 22);
			this._objectReferenceControlTestFactory.Name = "_objectReferenceControlTestFactory";
			this._objectReferenceControlTestFactory.ReadOnly = false;
			this._objectReferenceControlTestFactory.Size = new System.Drawing.Size(497, 20);
			this._objectReferenceControlTestFactory.TabIndex = 1;
			// 
			// _booleanComboboxStopOnError
			// 
			this._booleanComboboxStopOnError.FalseText = "No";
			this._booleanComboboxStopOnError.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._booleanComboboxStopOnError.Location = new System.Drawing.Point(279, 130);
			this._booleanComboboxStopOnError.Name = "_booleanComboboxStopOnError";
			this._booleanComboboxStopOnError.Size = new System.Drawing.Size(89, 21);
			this._booleanComboboxStopOnError.TabIndex = 3;
			this._booleanComboboxStopOnError.TrueText = "Yes";
			this._booleanComboboxStopOnError.Value = false;
			// 
			// _labelStopOnError
			// 
			this._labelStopOnError.AutoSize = true;
			this._labelStopOnError.Location = new System.Drawing.Point(202, 134);
			this._labelStopOnError.Name = "_labelStopOnError";
			this._labelStopOnError.Size = new System.Drawing.Size(71, 13);
			this._labelStopOnError.TabIndex = 4;
			this._labelStopOnError.Text = "Stop on error:";
			this._labelStopOnError.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _booleanComboboxAllowErrors
			// 
			this._booleanComboboxAllowErrors.FalseText = "Error";
			this._booleanComboboxAllowErrors.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._booleanComboboxAllowErrors.Location = new System.Drawing.Point(106, 130);
			this._booleanComboboxAllowErrors.Name = "_booleanComboboxAllowErrors";
			this._booleanComboboxAllowErrors.Size = new System.Drawing.Size(89, 21);
			this._booleanComboboxAllowErrors.TabIndex = 2;
			this._booleanComboboxAllowErrors.TrueText = "Warning";
			this._booleanComboboxAllowErrors.Value = false;
			// 
			// _labelAllowErrors
			// 
			this._labelAllowErrors.AutoSize = true;
			this._labelAllowErrors.Location = new System.Drawing.Point(42, 134);
			this._labelAllowErrors.Name = "_labelAllowErrors";
			this._labelAllowErrors.Size = new System.Drawing.Size(58, 13);
			this._labelAllowErrors.TabIndex = 6;
			this._labelAllowErrors.Text = "Issue type:";
			this._labelAllowErrors.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _groupBoxFactory
			// 
			this._groupBoxFactory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxFactory.Controls.Add(this._labelFactoryClass);
			this._groupBoxFactory.Controls.Add(this._objectReferenceControlTestFactory);
			this._groupBoxFactory.Location = new System.Drawing.Point(13, 19);
			this._groupBoxFactory.Name = "_groupBoxFactory";
			this._groupBoxFactory.Size = new System.Drawing.Size(599, 60);
			this._groupBoxFactory.TabIndex = 0;
			this._groupBoxFactory.TabStop = false;
			this._groupBoxFactory.Text = "Test Factory";
			// 
			// _labelFactoryClass
			// 
			this._labelFactoryClass.AutoSize = true;
			this._labelFactoryClass.Location = new System.Drawing.Point(39, 25);
			this._labelFactoryClass.Name = "_labelFactoryClass";
			this._labelFactoryClass.Size = new System.Drawing.Size(35, 13);
			this._labelFactoryClass.TabIndex = 0;
			this._labelFactoryClass.Text = "Class:";
			this._labelFactoryClass.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _groupBoxClassOrFactory
			// 
			this._groupBoxClassOrFactory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxClassOrFactory.Controls.Add(this._groupBoxFactory);
			this._groupBoxClassOrFactory.Controls.Add(this._groupBoxClass);
			this._groupBoxClassOrFactory.Location = new System.Drawing.Point(6, 6);
			this._groupBoxClassOrFactory.Name = "_groupBoxClassOrFactory";
			this._groupBoxClassOrFactory.Size = new System.Drawing.Size(624, 172);
			this._groupBoxClassOrFactory.TabIndex = 8;
			this._groupBoxClassOrFactory.TabStop = false;
			this._groupBoxClassOrFactory.Text = "Specify either a Test Factory or a Test Class and Constructor Index";
			// 
			// _groupBoxTestConfigurator
			// 
			this._groupBoxTestConfigurator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxTestConfigurator.Controls.Add(this._labelConfiguratorClass);
			this._groupBoxTestConfigurator.Controls.Add(this._objectReferenceControlTestConfigurator);
			this._groupBoxTestConfigurator.Location = new System.Drawing.Point(6, 184);
			this._groupBoxTestConfigurator.Name = "_groupBoxTestConfigurator";
			this._groupBoxTestConfigurator.Size = new System.Drawing.Size(624, 57);
			this._groupBoxTestConfigurator.TabIndex = 9;
			this._groupBoxTestConfigurator.TabStop = false;
			this._groupBoxTestConfigurator.Text = "Test Configurator (optional, sets also either Test Class or Test Factory)";
			// 
			// _labelConfiguratorClass
			// 
			this._labelConfiguratorClass.AutoSize = true;
			this._labelConfiguratorClass.Location = new System.Drawing.Point(52, 25);
			this._labelConfiguratorClass.Name = "_labelConfiguratorClass";
			this._labelConfiguratorClass.Size = new System.Drawing.Size(35, 13);
			this._labelConfiguratorClass.TabIndex = 0;
			this._labelConfiguratorClass.Text = "Class:";
			this._labelConfiguratorClass.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _objectReferenceControlTestConfigurator
			// 
			this._objectReferenceControlTestConfigurator.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlTestConfigurator.DataSource = null;
			this._objectReferenceControlTestConfigurator.DisplayMember = null;
			this._objectReferenceControlTestConfigurator.FindObjectDelegate = null;
			this._objectReferenceControlTestConfigurator.FormatTextDelegate = null;
			this._objectReferenceControlTestConfigurator.Location = new System.Drawing.Point(93, 22);
			this._objectReferenceControlTestConfigurator.Name = "_objectReferenceControlTestConfigurator";
			this._objectReferenceControlTestConfigurator.ReadOnly = false;
			this._objectReferenceControlTestConfigurator.Size = new System.Drawing.Size(510, 20);
			this._objectReferenceControlTestConfigurator.TabIndex = 1;
			// 
			// _tabControl
			// 
			this._tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControl.Controls.Add(this._tabPageImplementation);
			this._tabControl.Controls.Add(this._tabPageQualityConditions);
			this._tabControl.Location = new System.Drawing.Point(4, 163);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(644, 452);
			this._tabControl.TabIndex = 5;
			this._tabControl.SelectedIndexChanged += new System.EventHandler(this._tabControl_SelectedIndexChanged);
			// 
			// _tabPageImplementation
			// 
			this._tabPageImplementation.Controls.Add(this._splitContainerDescription);
			this._tabPageImplementation.Controls.Add(this._groupBoxClassOrFactory);
			this._tabPageImplementation.Controls.Add(this._groupBoxTestConfigurator);
			this._tabPageImplementation.Location = new System.Drawing.Point(4, 22);
			this._tabPageImplementation.Name = "_tabPageImplementation";
			this._tabPageImplementation.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageImplementation.Size = new System.Drawing.Size(636, 426);
			this._tabPageImplementation.TabIndex = 0;
			this._tabPageImplementation.Text = "Implementation";
			this._tabPageImplementation.UseVisualStyleBackColor = true;
			// 
			// _splitContainerDescription
			// 
			this._splitContainerDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainerDescription.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainerDescription.Location = new System.Drawing.Point(6, 247);
			this._splitContainerDescription.Name = "_splitContainerDescription";
			this._splitContainerDescription.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerDescription.Panel1
			// 
			this._splitContainerDescription.Panel1.Controls.Add(this._textBoxCategories);
			this._splitContainerDescription.Panel1.Controls.Add(this._textBoxTestDescription);
			this._splitContainerDescription.Panel1.Controls.Add(this._labelTestCategories);
			this._splitContainerDescription.Panel1.Controls.Add(this._labelTestDescription);
			this._splitContainerDescription.Panel1MinSize = 60;
			// 
			// _splitContainerDescription.Panel2
			// 
			this._splitContainerDescription.Panel2.Controls.Add(this._dataGridViewParameter);
			this._splitContainerDescription.Panel2.Controls.Add(this._labelParameter);
			this._splitContainerDescription.Panel2MinSize = 50;
			this._splitContainerDescription.Size = new System.Drawing.Size(624, 171);
			this._splitContainerDescription.SplitterDistance = 70;
			this._splitContainerDescription.TabIndex = 14;
			this._splitContainerDescription.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this._splitContainerDescription_SplitterMoved);
			// 
			// _textBoxCategories
			// 
			this._textBoxCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategories.Location = new System.Drawing.Point(93, 3);
			this._textBoxCategories.Name = "_textBoxCategories";
			this._textBoxCategories.ReadOnly = true;
			this._textBoxCategories.Size = new System.Drawing.Size(528, 20);
			this._textBoxCategories.TabIndex = 11;
			this._textBoxCategories.TabStop = false;
			// 
			// _labelTestCategories
			// 
			this._labelTestCategories.AutoSize = true;
			this._labelTestCategories.Location = new System.Drawing.Point(4, 6);
			this._labelTestCategories.Name = "_labelTestCategories";
			this._labelTestCategories.Size = new System.Drawing.Size(83, 13);
			this._labelTestCategories.TabIndex = 10;
			this._labelTestCategories.Text = "Test categories:";
			this._labelTestCategories.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _tabPageQualityConditions
			// 
			this._tabPageQualityConditions.Controls.Add(this._dataGridViewQualityConditions);
			this._tabPageQualityConditions.Controls.Add(this._dataGridViewFindToolStrip);
			this._tabPageQualityConditions.Controls.Add(this._toolStripElements);
			this._tabPageQualityConditions.Location = new System.Drawing.Point(4, 22);
			this._tabPageQualityConditions.Name = "_tabPageQualityConditions";
			this._tabPageQualityConditions.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageQualityConditions.Size = new System.Drawing.Size(636, 426);
			this._tabPageQualityConditions.TabIndex = 1;
			this._tabPageQualityConditions.Text = "Quality Conditions";
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
			this._dataGridViewQualityConditions.Location = new System.Drawing.Point(3, 53);
			this._dataGridViewQualityConditions.Name = "_dataGridViewQualityConditions";
			this._dataGridViewQualityConditions.RowHeadersVisible = false;
			this._dataGridViewQualityConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewQualityConditions.Size = new System.Drawing.Size(630, 370);
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
			this._columnIssueType.Width = 84;
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
			this._columnStopOnError.Width = 96;
			// 
			// _columnQualityConditionDescription
			// 
			this._columnQualityConditionDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnQualityConditionDescription.DataPropertyName = "Description";
			this._columnQualityConditionDescription.HeaderText = "Description";
			this._columnQualityConditionDescription.Name = "_columnQualityConditionDescription";
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(3, 28);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(630, 25);
			this._dataGridViewFindToolStrip.TabIndex = 28;
			this._dataGridViewFindToolStrip.Text = "_dataGridViewFindToolStrip";
			// 
			// _toolStripElements
			// 
			this._toolStripElements.AutoSize = false;
			this._toolStripElements.ClickThrough = true;
			this._toolStripElements.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripElements.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
			this._toolStripElements.Location = new System.Drawing.Point(3, 3);
			this._toolStripElements.Name = "_toolStripElements";
			this._toolStripElements.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripElements.Size = new System.Drawing.Size(630, 25);
			this._toolStripElements.TabIndex = 27;
			this._toolStripElements.Text = "Element Tools";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(106, 22);
			this.toolStripLabel1.Text = "Quality Conditions";
			// 
			// _numericUpDownExecutionPriority
			// 
			this._numericUpDownExecutionPriority.DecimalPlaces = 0;
			this._numericUpDownExecutionPriority.Increment = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this._numericUpDownExecutionPriority.Location = new System.Drawing.Point(468, 132);
			this._numericUpDownExecutionPriority.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            0});
			this._numericUpDownExecutionPriority.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
			this._numericUpDownExecutionPriority.Name = "_numericUpDownExecutionPriority";
			this._numericUpDownExecutionPriority.Size = new System.Drawing.Size(129, 20);
			this._numericUpDownExecutionPriority.TabIndex = 4;
			this._numericUpDownExecutionPriority.ThousandsSeparator = false;
			this._numericUpDownExecutionPriority.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
			// 
			// _labelExecutionPriority
			// 
			this._labelExecutionPriority.AutoSize = true;
			this._labelExecutionPriority.Location = new System.Drawing.Point(374, 134);
			this._labelExecutionPriority.Name = "_labelExecutionPriority";
			this._labelExecutionPriority.Size = new System.Drawing.Size(90, 13);
			this._labelExecutionPriority.TabIndex = 16;
			this._labelExecutionPriority.Text = "Execution priority:";
			this._labelExecutionPriority.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// TestDescriptorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelExecutionPriority);
			this.Controls.Add(this._numericUpDownExecutionPriority);
			this.Controls.Add(this._tabControl);
			this.Controls.Add(this._labelAllowErrors);
			this.Controls.Add(this._labelStopOnError);
			this.Controls.Add(this._booleanComboboxAllowErrors);
			this.Controls.Add(this._booleanComboboxStopOnError);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxName);
			this.Name = "TestDescriptorControl";
			this.Size = new System.Drawing.Size(651, 618);
			this.Load += new System.EventHandler(this.TestDescriptorControl_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.TestDescriptorControl_Paint);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this._groupBoxClass.ResumeLayout(false);
			this._groupBoxClass.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewParameter)).EndInit();
			this._groupBoxFactory.ResumeLayout(false);
			this._groupBoxFactory.PerformLayout();
			this._groupBoxClassOrFactory.ResumeLayout(false);
			this._groupBoxTestConfigurator.ResumeLayout(false);
			this._groupBoxTestConfigurator.PerformLayout();
			this._tabControl.ResumeLayout(false);
			this._tabPageImplementation.ResumeLayout(false);
			this._splitContainerDescription.Panel1.ResumeLayout(false);
			this._splitContainerDescription.Panel1.PerformLayout();
			this._splitContainerDescription.Panel2.ResumeLayout(false);
			this._splitContainerDescription.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDescription)).EndInit();
			this._splitContainerDescription.ResumeLayout(false);
			this._tabPageQualityConditions.ResumeLayout(false);
			this._tabPageQualityConditions.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewQualityConditions)).EndInit();
			this._toolStripElements.ResumeLayout(false);
			this._toolStripElements.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

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
        private ObjectReferenceControl _objectReferenceControlTestFactory;
        private System.Windows.Forms.GroupBox _groupBoxClass;
        private ObjectReferenceControl _objectReferenceControlTestClass;
		private System.Windows.Forms.Label _labelParameter;
        private System.Windows.Forms.Label _labelStopOnError;
        private global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox _booleanComboboxStopOnError;
        private System.Windows.Forms.Label _labelAllowErrors;
        private global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox _booleanComboboxAllowErrors;
        private System.Windows.Forms.GroupBox _groupBoxFactory;
        private System.Windows.Forms.Label _labelFactoryClass;
        private System.Windows.Forms.GroupBox _groupBoxClassOrFactory;
        private System.Windows.Forms.GroupBox _groupBoxTestConfigurator;
        private System.Windows.Forms.Label _labelConfiguratorClass;
        private ObjectReferenceControl _objectReferenceControlTestConfigurator;
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabPageImplementation;
		private System.Windows.Forms.TabPage _tabPageQualityConditions;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private NumericUpDownNullable _numericUpDownExecutionPriority;
        private System.Windows.Forms.Label _labelExecutionPriority;
		private System.Windows.Forms.ComboBox _comboBoxConstructorIndex;
		private DoubleBufferedDataGridView _dataGridViewParameter;
		private DoubleBufferedDataGridView _dataGridViewQualityConditions;
		private ToolStripEx _toolStripElements;
		private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerDescription;
		private System.Windows.Forms.TextBox _textBoxCategories;
		private System.Windows.Forms.Label _labelTestCategories;
		private System.Windows.Forms.DataGridViewImageColumn _columnQualityConditionImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnIssueType;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnStopOnError;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnQualityConditionDescription;
    }
}
