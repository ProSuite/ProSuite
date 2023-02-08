using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.UI.QA.Controls;

namespace ProSuite.UI.QA.Customize
{
    partial class CustomizeQASpecForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxSpecification = new System.Windows.Forms.TextBox();
			this._labelSpecification = new System.Windows.Forms.Label();
			this._toolTips = new System.Windows.Forms.ToolTip(this.components);
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._buttonOK = new System.Windows.Forms.Button();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._splitContainerSpecification = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._splitContainerConditions = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._groupBoxConditions = new System.Windows.Forms.GroupBox();
			this._panelConditions = new System.Windows.Forms.Panel();
			this._conditionsLayerView = new ConditionsLayerViewControl();
			this._conditionDatasetsControl = new ConditionDatasetsControl();
			this._conditionListControl = new ConditionListControl();
			this._toolStripConditionList = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this._toolStripComboBoxView = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this._groupBoxSelected = new System.Windows.Forms.GroupBox();
			this._labelEnabledConditions = new System.Windows.Forms.Label();
			this._dataGridViewEnabledConditions = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this.dgcSelType = new System.Windows.Forms.DataGridViewImageColumn();
			this.dgcSelTest = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._bindingSourceEnabledConditions = new System.Windows.Forms.BindingSource(this.components);
			this._groupBoxSelectedParameters = new System.Windows.Forms.GroupBox();
			this._toolStripButtonWarningConditions = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonErrorConditions = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonStopConditions = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonEnableAll = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonEnableNone = new System.Windows.Forms.ToolStripButton();
			this._splitContainer = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._qualityConditionControl = new QualityConditionControl();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageParameterValues = new System.Windows.Forms.TabPage();
			this._qualityConditionTableViewControl = new QualityConditionTableViewControl();
			this._toolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripButtonCustomizeTestParameterValues = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonReset = new System.Windows.Forms.ToolStripButton();
			this._tabPageTestDescriptor = new System.Windows.Forms.TabPage();
			this._testDescriptorControl = new TestDescriptorControl();
			this.dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewImageColumn2 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerSpecification)).BeginInit();
			this._splitContainerSpecification.Panel1.SuspendLayout();
			this._splitContainerSpecification.Panel2.SuspendLayout();
			this._splitContainerSpecification.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerConditions)).BeginInit();
			this._splitContainerConditions.Panel1.SuspendLayout();
			this._splitContainerConditions.Panel2.SuspendLayout();
			this._splitContainerConditions.SuspendLayout();
			this._groupBoxConditions.SuspendLayout();
			this._panelConditions.SuspendLayout();
			this._toolStripConditionList.SuspendLayout();
			this._groupBoxSelected.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewEnabledConditions)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceEnabledConditions)).BeginInit();
			this._groupBoxSelectedParameters.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			this._tabControl.SuspendLayout();
			this._tabPageParameterValues.SuspendLayout();
			this._toolStrip.SuspendLayout();
			this._tabPageTestDescriptor.SuspendLayout();
			this.SuspendLayout();
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxDescription.Location = new System.Drawing.Point(124, 38);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ReadOnly = true;
			this._textBoxDescription.Size = new System.Drawing.Size(848, 44);
			this._textBoxDescription.TabIndex = 5;
			this._textBoxDescription.TabStop = false;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(55, 41);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 4;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSpecification
			// 
			this._textBoxSpecification.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxSpecification.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxSpecification.Location = new System.Drawing.Point(124, 12);
			this._textBoxSpecification.Name = "_textBoxSpecification";
			this._textBoxSpecification.ReadOnly = true;
			this._textBoxSpecification.Size = new System.Drawing.Size(848, 20);
			this._textBoxSpecification.TabIndex = 3;
			this._textBoxSpecification.TabStop = false;
			// 
			// _labelSpecification
			// 
			this._labelSpecification.AutoSize = true;
			this._labelSpecification.Location = new System.Drawing.Point(12, 15);
			this._labelSpecification.Name = "_labelSpecification";
			this._labelSpecification.Size = new System.Drawing.Size(106, 13);
			this._labelSpecification.TabIndex = 2;
			this._labelSpecification.Text = "Quality Specification:";
			this._labelSpecification.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _statusStrip
			// 
			this._statusStrip.Location = new System.Drawing.Point(0, 739);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(984, 22);
			this._statusStrip.TabIndex = 7;
			this._statusStrip.Text = "_statusStrip";
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(907, 189);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(53, 30);
			this._buttonOK.TabIndex = 0;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(907, 225);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(53, 29);
			this._buttonCancel.TabIndex = 1;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _splitContainerSpecification
			// 
			this._splitContainerSpecification.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainerSpecification.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this._splitContainerSpecification.Location = new System.Drawing.Point(12, 88);
			this._splitContainerSpecification.Name = "_splitContainerSpecification";
			this._splitContainerSpecification.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerSpecification.Panel1
			// 
			this._splitContainerSpecification.Panel1.Controls.Add(this._splitContainerConditions);
			// 
			// _splitContainerSpecification.Panel2
			// 
			this._splitContainerSpecification.Panel2.Controls.Add(this._groupBoxSelectedParameters);
			this._splitContainerSpecification.Panel2.Controls.Add(this._buttonCancel);
			this._splitContainerSpecification.Panel2.Controls.Add(this._buttonOK);
			this._splitContainerSpecification.Size = new System.Drawing.Size(960, 645);
			this._splitContainerSpecification.SplitterDistance = 387;
			this._splitContainerSpecification.TabIndex = 6;
			// 
			// _splitContainerConditions
			// 
			this._splitContainerConditions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainerConditions.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this._splitContainerConditions.Location = new System.Drawing.Point(0, 0);
			this._splitContainerConditions.Name = "_splitContainerConditions";
			// 
			// _splitContainerConditions.Panel1
			// 
			this._splitContainerConditions.Panel1.Controls.Add(this._groupBoxConditions);
			// 
			// _splitContainerConditions.Panel2
			// 
			this._splitContainerConditions.Panel2.Controls.Add(this._groupBoxSelected);
			this._splitContainerConditions.Size = new System.Drawing.Size(960, 384);
			this._splitContainerConditions.SplitterDistance = 706;
			this._splitContainerConditions.TabIndex = 0;
			// 
			// _groupBoxConditions
			// 
			this._groupBoxConditions.Controls.Add(this._panelConditions);
			this._groupBoxConditions.Controls.Add(this._toolStripConditionList);
			this._groupBoxConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxConditions.Location = new System.Drawing.Point(0, 0);
			this._groupBoxConditions.Name = "_groupBoxConditions";
			this._groupBoxConditions.Size = new System.Drawing.Size(706, 384);
			this._groupBoxConditions.TabIndex = 0;
			this._groupBoxConditions.TabStop = false;
			this._groupBoxConditions.Text = "Available Quality Conditions";
			// 
			// _panelConditions
			// 
			this._panelConditions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._panelConditions.Controls.Add(this._conditionsLayerView);
			this._panelConditions.Controls.Add(this._conditionDatasetsControl);
			this._panelConditions.Controls.Add(this._conditionListControl);
			this._panelConditions.Location = new System.Drawing.Point(11, 51);
			this._panelConditions.Name = "_panelConditions";
			this._panelConditions.Size = new System.Drawing.Size(689, 327);
			this._panelConditions.TabIndex = 13;
			// 
			// _conditionsLayerView
			// 
			this._conditionsLayerView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._conditionsLayerView.CustomizeView = null;
			this._conditionsLayerView.Location = new System.Drawing.Point(72, 53);
			this._conditionsLayerView.Name = "_conditionsLayerView";
			this._conditionsLayerView.Size = new System.Drawing.Size(480, 245);
			this._conditionsLayerView.TabIndex = 12;
			// 
			// _conditionDatasetsControl
			// 
			this._conditionDatasetsControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._conditionDatasetsControl.CustomizeView = null;
			this._conditionDatasetsControl.FilterRows = false;
			this._conditionDatasetsControl.Location = new System.Drawing.Point(14, 7);
			this._conditionDatasetsControl.MatchCase = false;
			this._conditionDatasetsControl.Name = "_conditionDatasetsControl";
			this._conditionDatasetsControl.Size = new System.Drawing.Size(646, 256);
			this._conditionDatasetsControl.TabIndex = 11;
			// 
			// _conditionListControl
			// 
			this._conditionListControl.CustomizeView = null;
			this._conditionListControl.FilterRows = false;
			this._conditionListControl.Location = new System.Drawing.Point(3, 42);
			this._conditionListControl.MatchCase = false;
			this._conditionListControl.Name = "_conditionListControl";
			this._conditionListControl.Size = new System.Drawing.Size(575, 161);
			this._conditionListControl.TabIndex = 15;
			// 
			// _toolStripConditionList
			// 
			this._toolStripConditionList.ClickThrough = true;
			this._toolStripConditionList.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripConditionList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripLabel1,
            this._toolStripComboBoxView,
            this._toolStripButtonWarningConditions,
            this._toolStripButtonErrorConditions,
            this._toolStripButtonStopConditions,
            this.toolStripSeparator1,
            this._toolStripButtonEnableAll,
            this._toolStripButtonEnableNone});
			this._toolStripConditionList.Location = new System.Drawing.Point(3, 16);
			this._toolStripConditionList.Name = "_toolStripConditionList";
			this._toolStripConditionList.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripConditionList.Size = new System.Drawing.Size(700, 25);
			this._toolStripConditionList.TabIndex = 15;
			this._toolStripConditionList.Text = "toolStrip1";
			// 
			// _toolStripLabel1
			// 
			this._toolStripLabel1.Name = "_toolStripLabel1";
			this._toolStripLabel1.Size = new System.Drawing.Size(32, 22);
			this._toolStripLabel1.Text = "View";
			// 
			// _toolStripComboBoxView
			// 
			this._toolStripComboBoxView.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._toolStripComboBoxView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this._toolStripComboBoxView.Name = "_toolStripComboBoxView";
			this._toolStripComboBoxView.Size = new System.Drawing.Size(220, 25);
			this._toolStripComboBoxView.SelectedIndexChanged += new System.EventHandler(this._toolStripComboBoxView_SelectedIndexChanged);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// _groupBoxSelected
			// 
			this._groupBoxSelected.Controls.Add(this._labelEnabledConditions);
			this._groupBoxSelected.Controls.Add(this._dataGridViewEnabledConditions);
			this._groupBoxSelected.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxSelected.Location = new System.Drawing.Point(0, 0);
			this._groupBoxSelected.Name = "_groupBoxSelected";
			this._groupBoxSelected.Size = new System.Drawing.Size(250, 384);
			this._groupBoxSelected.TabIndex = 0;
			this._groupBoxSelected.TabStop = false;
			this._groupBoxSelected.Text = "Enabled Quality Conditions";
			// 
			// _labelEnabledConditions
			// 
			this._labelEnabledConditions.AutoSize = true;
			this._labelEnabledConditions.Location = new System.Drawing.Point(6, 26);
			this._labelEnabledConditions.Name = "_labelEnabledConditions";
			this._labelEnabledConditions.Size = new System.Drawing.Size(189, 13);
			this._labelEnabledConditions.TabIndex = 0;
			this._labelEnabledConditions.Text = "### of ### Tests selected to execute";
			// 
			// _dataGridViewEnabledConditions
			// 
			this._dataGridViewEnabledConditions.AllowUserToAddRows = false;
			this._dataGridViewEnabledConditions.AllowUserToDeleteRows = false;
			this._dataGridViewEnabledConditions.AllowUserToResizeRows = false;
			this._dataGridViewEnabledConditions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewEnabledConditions.AutoGenerateColumns = false;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridViewEnabledConditions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this._dataGridViewEnabledConditions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewEnabledConditions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgcSelType,
            this.dgcSelTest});
			this._dataGridViewEnabledConditions.DataSource = this._bindingSourceEnabledConditions;
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewEnabledConditions.DefaultCellStyle = dataGridViewCellStyle5;
			this._dataGridViewEnabledConditions.Location = new System.Drawing.Point(6, 49);
			this._dataGridViewEnabledConditions.Name = "_dataGridViewEnabledConditions";
			this._dataGridViewEnabledConditions.ReadOnly = true;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridViewEnabledConditions.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
			this._dataGridViewEnabledConditions.RowHeadersVisible = false;
			this._dataGridViewEnabledConditions.RowHeadersWidth = 20;
			this._dataGridViewEnabledConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewEnabledConditions.Size = new System.Drawing.Size(238, 329);
			this._dataGridViewEnabledConditions.TabIndex = 2;
			this._dataGridViewEnabledConditions.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridViewEnabledConditions_CellFormatting);
			this._dataGridViewEnabledConditions.SelectionChanged += new System.EventHandler(this._dataGridViewEnabledConditions_SelectionChanged);
			// 
			// dgcSelType
			// 
			this.dgcSelType.DataPropertyName = "Type";
			this.dgcSelType.HeaderText = "";
			this.dgcSelType.Name = "dgcSelType";
			this.dgcSelType.ReadOnly = true;
			this.dgcSelType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dgcSelType.Width = 20;
			// 
			// dgcSelTest
			// 
			this.dgcSelTest.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dgcSelTest.DataPropertyName = "TestName";
			this.dgcSelTest.HeaderText = "Quality Condition";
			this.dgcSelTest.Name = "dgcSelTest";
			this.dgcSelTest.ReadOnly = true;
			this.dgcSelTest.ToolTipText = "Quality Condition Name";
			// 
			// _bindingSourceEnabledConditions
			// 
			this._bindingSourceEnabledConditions.DataSource = typeof(global::ProSuite.UI.QA.Controls.SpecificationDataset);
			// 
			// _groupBoxSelectedParameters
			// 
			this._groupBoxSelectedParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxSelectedParameters.Controls.Add(this._splitContainer);
			this._groupBoxSelectedParameters.Location = new System.Drawing.Point(0, 3);
			this._groupBoxSelectedParameters.Name = "_groupBoxSelectedParameters";
			this._groupBoxSelectedParameters.Size = new System.Drawing.Size(901, 251);
			this._groupBoxSelectedParameters.TabIndex = 0;
			this._groupBoxSelectedParameters.TabStop = false;
			this._groupBoxSelectedParameters.Text = "Selected Quality Condition";
			this._groupBoxSelectedParameters.EnabledChanged += new System.EventHandler(this._groupBoxSelectedParameters_EnabledChanged);
			// 
			// _toolStripButtonWarningConditions
			// 
			this._toolStripButtonWarningConditions.CheckOnClick = true;
			this._toolStripButtonWarningConditions.Image = global::ProSuite.UI.Properties.TestTypeImages.TestTypeWarning;
			this._toolStripButtonWarningConditions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonWarningConditions.Name = "_toolStripButtonWarningConditions";
			this._toolStripButtonWarningConditions.Size = new System.Drawing.Size(77, 22);
			this._toolStripButtonWarningConditions.Text = "Warnings";
			this._toolStripButtonWarningConditions.ToolTipText = "Enable/disable all soft quality conditions (warnings)";
			this._toolStripButtonWarningConditions.CheckedChanged += new System.EventHandler(this._toolStripButtonWarningConditions_CheckedChanged);
			// 
			// _toolStripButtonErrorConditions
			// 
			this._toolStripButtonErrorConditions.CheckOnClick = true;
			this._toolStripButtonErrorConditions.Image = global::ProSuite.UI.Properties.TestTypeImages.TestTypeError;
			this._toolStripButtonErrorConditions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonErrorConditions.Name = "_toolStripButtonErrorConditions";
			this._toolStripButtonErrorConditions.Size = new System.Drawing.Size(57, 22);
			this._toolStripButtonErrorConditions.Text = "Errors";
			this._toolStripButtonErrorConditions.ToolTipText = "Enable/disable all hard quality conditions (errors)";
			this._toolStripButtonErrorConditions.CheckedChanged += new System.EventHandler(this._toolStripButtonErrorConditions_CheckedChanged);
			// 
			// _toolStripButtonStopConditions
			// 
			this._toolStripButtonStopConditions.CheckOnClick = true;
			this._toolStripButtonStopConditions.Image = global::ProSuite.UI.Properties.TestTypeImages.TestTypeStop;
			this._toolStripButtonStopConditions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonStopConditions.Name = "_toolStripButtonStopConditions";
			this._toolStripButtonStopConditions.Size = new System.Drawing.Size(112, 22);
			this._toolStripButtonStopConditions.Text = "Stop Conditions";
			this._toolStripButtonStopConditions.ToolTipText = "Enable/disable all stop conditions";
			this._toolStripButtonStopConditions.CheckedChanged += new System.EventHandler(this._toolStripButtonStopConditions_CheckedChanged);
			// 
			// _toolStripButtonEnableAll
			// 
			this._toolStripButtonEnableAll.Image = global::ProSuite.UI.Properties.Resources.CheckAll;
			this._toolStripButtonEnableAll.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonEnableAll.Name = "_toolStripButtonEnableAll";
			this._toolStripButtonEnableAll.Size = new System.Drawing.Size(41, 22);
			this._toolStripButtonEnableAll.Text = "All";
			this._toolStripButtonEnableAll.Click += new System.EventHandler(this._toolStripButtonEnableAll_Click);
			// 
			// _toolStripButtonEnableNone
			// 
			this._toolStripButtonEnableNone.Image = global::ProSuite.UI.Properties.Resources.UncheckAll;
			this._toolStripButtonEnableNone.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonEnableNone.Name = "_toolStripButtonEnableNone";
			this._toolStripButtonEnableNone.Size = new System.Drawing.Size(56, 22);
			this._toolStripButtonEnableNone.Text = "None";
			this._toolStripButtonEnableNone.Click += new System.EventHandler(this._toolStripButtonEnableNone_Click);
			// 
			// _splitContainer
			// 
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this._splitContainer.Location = new System.Drawing.Point(3, 16);
			this._splitContainer.Name = "_splitContainer";
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._qualityConditionControl);
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.Controls.Add(this._tabControl);
			this._splitContainer.Size = new System.Drawing.Size(895, 232);
			this._splitContainer.SplitterDistance = 352;
			this._splitContainer.TabIndex = 2;
			// 
			// _qualityConditionControl
			// 
			this._qualityConditionControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionControl.Location = new System.Drawing.Point(0, 0);
			this._qualityConditionControl.Name = "_qualityConditionControl";
			this._qualityConditionControl.QualityCondition = null;
			this._qualityConditionControl.ReadOnly = true;
			this._qualityConditionControl.Size = new System.Drawing.Size(352, 232);
			this._qualityConditionControl.TabIndex = 0;
			// 
			// _tabControl
			// 
			this._tabControl.Controls.Add(this._tabPageParameterValues);
			this._tabControl.Controls.Add(this._tabPageTestDescriptor);
			this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControl.Location = new System.Drawing.Point(0, 0);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(539, 232);
			this._tabControl.TabIndex = 0;
			// 
			// _tabPageParameterValues
			// 
			this._tabPageParameterValues.Controls.Add(this._qualityConditionTableViewControl);
			this._tabPageParameterValues.Controls.Add(this._toolStrip);
			this._tabPageParameterValues.Location = new System.Drawing.Point(4, 22);
			this._tabPageParameterValues.Name = "_tabPageParameterValues";
			this._tabPageParameterValues.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageParameterValues.Size = new System.Drawing.Size(531, 206);
			this._tabPageParameterValues.TabIndex = 0;
			this._tabPageParameterValues.Text = "Parameter Values";
			this._tabPageParameterValues.UseVisualStyleBackColor = true;
			// 
			// _qualityConditionTableViewControl
			// 
			this._qualityConditionTableViewControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionTableViewControl.Location = new System.Drawing.Point(3, 28);
			this._qualityConditionTableViewControl.Name = "_qualityConditionTableViewControl";
			this._qualityConditionTableViewControl.Size = new System.Drawing.Size(525, 175);
			this._qualityConditionTableViewControl.TabIndex = 2;
			// 
			// _toolStrip
			// 
			this._toolStrip.ClickThrough = true;
			this._toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonCustomizeTestParameterValues,
            this._toolStripButtonReset});
			this._toolStrip.Location = new System.Drawing.Point(3, 3);
			this._toolStrip.Name = "_toolStrip";
			this._toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStrip.Size = new System.Drawing.Size(525, 25);
			this._toolStrip.TabIndex = 4;
			this._toolStrip.Text = "Parameter Value Tools";
			// 
			// _toolStripButtonCustomizeTestParameterValues
			// 
			this._toolStripButtonCustomizeTestParameterValues.Image = global::ProSuite.UI.Properties.Resources.Edit;
			this._toolStripButtonCustomizeTestParameterValues.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonCustomizeTestParameterValues.Name = "_toolStripButtonCustomizeTestParameterValues";
			this._toolStripButtonCustomizeTestParameterValues.Size = new System.Drawing.Size(92, 22);
			this._toolStripButtonCustomizeTestParameterValues.Text = "Customize...";
			this._toolStripButtonCustomizeTestParameterValues.Click += new System.EventHandler(this._toolStripButtonCustomizeTestParameterValues_Click);
			// 
			// _toolStripButtonReset
			// 
			this._toolStripButtonReset.Image = global::ProSuite.UI.Properties.Resources.Undo_16x;
			this._toolStripButtonReset.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonReset.Name = "_toolStripButtonReset";
			this._toolStripButtonReset.Size = new System.Drawing.Size(55, 22);
			this._toolStripButtonReset.Text = "Reset";
			this._toolStripButtonReset.ToolTipText = "Reset to original parameter values";
			this._toolStripButtonReset.Click += new System.EventHandler(this._toolStripButtonReset_Click);
			// 
			// _tabPageTestDescriptor
			// 
			this._tabPageTestDescriptor.Controls.Add(this._testDescriptorControl);
			this._tabPageTestDescriptor.Location = new System.Drawing.Point(4, 22);
			this._tabPageTestDescriptor.Name = "_tabPageTestDescriptor";
			this._tabPageTestDescriptor.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageTestDescriptor.Size = new System.Drawing.Size(483, 197);
			this._tabPageTestDescriptor.TabIndex = 1;
			this._tabPageTestDescriptor.Text = "Test";
			this._tabPageTestDescriptor.UseVisualStyleBackColor = true;
			// 
			// _testDescriptorControl
			// 
			this._testDescriptorControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._testDescriptorControl.Location = new System.Drawing.Point(3, 3);
			this._testDescriptorControl.Name = "_testDescriptorControl";
			this._testDescriptorControl.Size = new System.Drawing.Size(477, 191);
			this._testDescriptorControl.TabIndex = 1;
			this._testDescriptorControl.TestDescriptor = null;
			// 

			// dataGridViewCheckBoxColumn1
			// 
			this.dataGridViewCheckBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
			this.dataGridViewCheckBoxColumn1.DataPropertyName = "Enabled";
			this.dataGridViewCheckBoxColumn1.HeaderText = "";
			this.dataGridViewCheckBoxColumn1.MinimumWidth = 25;
			this.dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
			this.dataGridViewCheckBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewCheckBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.DataPropertyName = "Type";
			this.dataGridViewImageColumn1.HeaderText = "";
			this.dataGridViewImageColumn1.MinimumWidth = 20;
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			this.dataGridViewImageColumn1.ReadOnly = true;
			this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn1.Width = 20;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn1.DataPropertyName = "TestType";
			this.dataGridViewTextBoxColumn1.HeaderText = "Test Type";
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "DatasetName";
			this.dataGridViewTextBoxColumn2.HeaderText = "Dataset";
			this.dataGridViewTextBoxColumn2.MinimumWidth = 50;
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			// 
			// dataGridViewImageColumn2
			// 
			this.dataGridViewImageColumn2.DataPropertyName = "DatasetType";
			this.dataGridViewImageColumn2.HeaderText = "";
			this.dataGridViewImageColumn2.MinimumWidth = 20;
			this.dataGridViewImageColumn2.Name = "dataGridViewImageColumn2";
			this.dataGridViewImageColumn2.ReadOnly = true;
			this.dataGridViewImageColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewImageColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn2.Width = 20;
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn3.DataPropertyName = "DatasetName";
			this.dataGridViewTextBoxColumn3.HeaderText = "Dataset";
			this.dataGridViewTextBoxColumn3.MinimumWidth = 50;
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			// 
			// CustomizeQASpecForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(984, 761);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._splitContainerSpecification);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxSpecification);
			this.Controls.Add(this._labelSpecification);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(700, 500);
			this.Name = "CustomizeQASpecForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Customize Quality Specification";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CustomizeQASpecForm_FormClosed);
			this.Load += new System.EventHandler(this.CustomizeQASpecForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CustomizeQASpecForm_KeyDown);
			this._splitContainerSpecification.Panel1.ResumeLayout(false);
			this._splitContainerSpecification.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerSpecification)).EndInit();
			this._splitContainerSpecification.ResumeLayout(false);
			this._splitContainerConditions.Panel1.ResumeLayout(false);
			this._splitContainerConditions.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerConditions)).EndInit();
			this._splitContainerConditions.ResumeLayout(false);
			this._groupBoxConditions.ResumeLayout(false);
			this._groupBoxConditions.PerformLayout();
			this._panelConditions.ResumeLayout(false);
			this._toolStripConditionList.ResumeLayout(false);
			this._toolStripConditionList.PerformLayout();
			this._groupBoxSelected.ResumeLayout(false);
			this._groupBoxSelected.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewEnabledConditions)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceEnabledConditions)).EndInit();
			this._groupBoxSelectedParameters.ResumeLayout(false);
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			this._tabControl.ResumeLayout(false);
			this._tabPageParameterValues.ResumeLayout(false);
			this._tabPageParameterValues.PerformLayout();
			this._toolStrip.ResumeLayout(false);
			this._toolStrip.PerformLayout();
			this._tabPageTestDescriptor.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerConditions;
        private System.Windows.Forms.GroupBox _groupBoxConditions;
        private System.Windows.Forms.GroupBox _groupBoxSelected;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxSpecification;
        private System.Windows.Forms.Label _labelSpecification;
        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.ToolTip _toolTips;
		private System.Windows.Forms.GroupBox _groupBoxSelectedParameters;
        private System.Windows.Forms.BindingSource _bindingSourceEnabledConditions;
		private System.Windows.Forms.Label _labelEnabledConditions;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerSpecification;
		private QualityConditionControl _qualityConditionControl;
    	private TestDescriptorControl _testDescriptorControl;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewEnabledConditions;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private System.Windows.Forms.TabControl _tabControl;
		private System.Windows.Forms.TabPage _tabPageParameterValues;
		private System.Windows.Forms.TabPage _tabPageTestDescriptor;
		private System.Windows.Forms.DataGridViewImageColumn dgcSelType;
		private System.Windows.Forms.DataGridViewTextBoxColumn dgcSelTest;
		private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private QualityConditionTableViewControl _qualityConditionTableViewControl;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.ToolStripButton _toolStripButtonCustomizeTestParameterValues;
		private ToolStripEx _toolStrip;
		private ConditionDatasetsControl _conditionDatasetsControl;
		private ConditionsLayerViewControl _conditionsLayerView;
		private System.Windows.Forms.Panel _panelConditions;
		private ToolStripEx _toolStripConditionList;
		private System.Windows.Forms.ToolStripLabel _toolStripLabel1;
		private System.Windows.Forms.ToolStripComboBox _toolStripComboBoxView;
		private ConditionListControl _conditionListControl;
		private System.Windows.Forms.ToolStripButton _toolStripButtonEnableAll;
		private System.Windows.Forms.ToolStripButton _toolStripButtonEnableNone;
		private System.Windows.Forms.ToolStripButton _toolStripButtonWarningConditions;
		private System.Windows.Forms.ToolStripButton _toolStripButtonErrorConditions;
		private System.Windows.Forms.ToolStripButton _toolStripButtonStopConditions;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton _toolStripButtonReset;
	}
}
