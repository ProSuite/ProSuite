using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.UI.Core.QA.Controls;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.Customize
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
			components=new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			_textBoxDescription=new System.Windows.Forms.TextBox();
			_labelDescription=new System.Windows.Forms.Label();
			_textBoxSpecification=new System.Windows.Forms.TextBox();
			_labelSpecification=new System.Windows.Forms.Label();
			_toolTips=new System.Windows.Forms.ToolTip(components);
			_statusStrip=new System.Windows.Forms.StatusStrip();
			_buttonOK=new System.Windows.Forms.Button();
			_buttonCancel=new System.Windows.Forms.Button();
			_splitContainerSpecification=new SplitContainerEx();
			_splitContainerConditions=new SplitContainerEx();
			_groupBoxConditions=new System.Windows.Forms.GroupBox();
			_panelConditions=new System.Windows.Forms.Panel();
			_conditionsLayerView=new ConditionsLayerViewControl();
			_conditionDatasetsControl=new ConditionDatasetsControl();
			_conditionListControl=new ConditionListControl();
			_toolStripConditionList=new ToolStripEx();
			_toolStripLabel1=new System.Windows.Forms.ToolStripLabel();
			_toolStripComboBoxView=new System.Windows.Forms.ToolStripComboBox();
			_toolStripButtonWarningConditions=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonErrorConditions=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonStopConditions=new System.Windows.Forms.ToolStripButton();
			toolStripSeparator1=new System.Windows.Forms.ToolStripSeparator();
			_toolStripButtonEnableAll=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonEnableNone=new System.Windows.Forms.ToolStripButton();
			_panelSelectedConditions=new System.Windows.Forms.Panel();
			_groupBoxSelected=new System.Windows.Forms.GroupBox();
			_dataGridViewEnabledConditions=new DoubleBufferedDataGridView();
			dgcSelType=new System.Windows.Forms.DataGridViewImageColumn();
			dgcSelTest=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_bindingSourceEnabledConditions=new System.Windows.Forms.BindingSource(components);
			_labelEnabledConditions=new System.Windows.Forms.Label();
			_groupBoxSelectedParameters=new System.Windows.Forms.GroupBox();
			_splitContainer=new SplitContainerEx();
			_qualityConditionControl=new QualityConditionControl();
			_tabControl=new System.Windows.Forms.TabControl();
			_tabPageParameterValues=new System.Windows.Forms.TabPage();
			_qualityConditionTableViewControl=new QualityConditionTableViewControl();
			_toolStrip=new ToolStripEx();
			_toolStripButtonCustomizeTestParameterValues=new System.Windows.Forms.ToolStripButton();
			_toolStripButtonReset=new System.Windows.Forms.ToolStripButton();
			_tabPageTestDescriptor=new System.Windows.Forms.TabPage();
			_testDescriptorControl=new TestDescriptorControl();
			_mainSplitterBottomPanel=new System.Windows.Forms.Panel();
			dataGridViewCheckBoxColumn1=new System.Windows.Forms.DataGridViewCheckBoxColumn();
			dataGridViewImageColumn1=new System.Windows.Forms.DataGridViewImageColumn();
			dataGridViewTextBoxColumn1=new System.Windows.Forms.DataGridViewTextBoxColumn();
			dataGridViewTextBoxColumn2=new System.Windows.Forms.DataGridViewTextBoxColumn();
			dataGridViewImageColumn2=new System.Windows.Forms.DataGridViewImageColumn();
			dataGridViewTextBoxColumn3=new System.Windows.Forms.DataGridViewTextBoxColumn();
			_panelTop=new System.Windows.Forms.Panel();
			_panelConditionViews=new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)_splitContainerSpecification).BeginInit();
			_splitContainerSpecification.Panel1.SuspendLayout();
			_splitContainerSpecification.Panel2.SuspendLayout();
			_splitContainerSpecification.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainerConditions).BeginInit();
			_splitContainerConditions.Panel1.SuspendLayout();
			_splitContainerConditions.Panel2.SuspendLayout();
			_splitContainerConditions.SuspendLayout();
			_groupBoxConditions.SuspendLayout();
			_panelConditions.SuspendLayout();
			_toolStripConditionList.SuspendLayout();
			_panelSelectedConditions.SuspendLayout();
			_groupBoxSelected.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewEnabledConditions).BeginInit();
			((System.ComponentModel.ISupportInitialize)_bindingSourceEnabledConditions).BeginInit();
			_groupBoxSelectedParameters.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
			_splitContainer.Panel1.SuspendLayout();
			_splitContainer.Panel2.SuspendLayout();
			_splitContainer.SuspendLayout();
			_tabControl.SuspendLayout();
			_tabPageParameterValues.SuspendLayout();
			_toolStrip.SuspendLayout();
			_tabPageTestDescriptor.SuspendLayout();
			_mainSplitterBottomPanel.SuspendLayout();
			_panelTop.SuspendLayout();
			_panelConditionViews.SuspendLayout();
			SuspendLayout();
			// 
			// _textBoxDescription
			// 
			_textBoxDescription.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxDescription.BackColor=System.Drawing.SystemColors.Control;
			_textBoxDescription.Location=new System.Drawing.Point(145, 44);
			_textBoxDescription.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescription.Multiline=true;
			_textBoxDescription.Name="_textBoxDescription";
			_textBoxDescription.ReadOnly=true;
			_textBoxDescription.Size=new System.Drawing.Size(989, 50);
			_textBoxDescription.TabIndex=5;
			_textBoxDescription.TabStop=false;
			// 
			// _labelDescription
			// 
			_labelDescription.AutoSize=true;
			_labelDescription.Location=new System.Drawing.Point(64, 47);
			_labelDescription.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelDescription.Name="_labelDescription";
			_labelDescription.Size=new System.Drawing.Size(70, 15);
			_labelDescription.TabIndex=4;
			_labelDescription.Text="Description:";
			_labelDescription.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSpecification
			// 
			_textBoxSpecification.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_textBoxSpecification.BackColor=System.Drawing.SystemColors.Control;
			_textBoxSpecification.Location=new System.Drawing.Point(145, 14);
			_textBoxSpecification.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxSpecification.Name="_textBoxSpecification";
			_textBoxSpecification.ReadOnly=true;
			_textBoxSpecification.Size=new System.Drawing.Size(989, 23);
			_textBoxSpecification.TabIndex=3;
			_textBoxSpecification.TabStop=false;
			// 
			// _labelSpecification
			// 
			_labelSpecification.AutoSize=true;
			_labelSpecification.Location=new System.Drawing.Point(14, 17);
			_labelSpecification.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelSpecification.Name="_labelSpecification";
			_labelSpecification.Size=new System.Drawing.Size(119, 15);
			_labelSpecification.TabIndex=2;
			_labelSpecification.Text="Quality Specification:";
			_labelSpecification.TextAlign=System.Drawing.ContentAlignment.TopRight;
			// 
			// _statusStrip
			// 
			_statusStrip.Location=new System.Drawing.Point(0, 856);
			_statusStrip.Name="_statusStrip";
			_statusStrip.Padding=new System.Windows.Forms.Padding(1, 0, 16, 0);
			_statusStrip.Size=new System.Drawing.Size(1148, 22);
			_statusStrip.TabIndex=7;
			_statusStrip.Text="_statusStrip";
			// 
			// _buttonOK
			// 
			_buttonOK.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Right;
			_buttonOK.Location=new System.Drawing.Point(1075, 178);
			_buttonOK.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonOK.Name="_buttonOK";
			_buttonOK.Size=new System.Drawing.Size(62, 35);
			_buttonOK.TabIndex=0;
			_buttonOK.Text="OK";
			_buttonOK.UseVisualStyleBackColor=true;
			_buttonOK.Click+=_buttonOK_Click;
			// 
			// _buttonCancel
			// 
			_buttonCancel.Anchor=System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Right;
			_buttonCancel.DialogResult=System.Windows.Forms.DialogResult.Cancel;
			_buttonCancel.Location=new System.Drawing.Point(1075, 219);
			_buttonCancel.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_buttonCancel.Name="_buttonCancel";
			_buttonCancel.Size=new System.Drawing.Size(62, 33);
			_buttonCancel.TabIndex=1;
			_buttonCancel.Text="Cancel";
			_buttonCancel.UseVisualStyleBackColor=true;
			_buttonCancel.Click+=_buttonCancel_Click;
			// 
			// _splitContainerSpecification
			// 
			_splitContainerSpecification.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainerSpecification.FixedPanel=System.Windows.Forms.FixedPanel.Panel2;
			_splitContainerSpecification.Location=new System.Drawing.Point(0, 100);
			_splitContainerSpecification.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainerSpecification.Name="_splitContainerSpecification";
			_splitContainerSpecification.Orientation=System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerSpecification.Panel1
			// 
			_splitContainerSpecification.Panel1.Controls.Add(_splitContainerConditions);
			// 
			// _splitContainerSpecification.Panel2
			// 
			_splitContainerSpecification.Panel2.Controls.Add(_mainSplitterBottomPanel);
			_splitContainerSpecification.Size=new System.Drawing.Size(1148, 756);
			_splitContainerSpecification.SplitterDistance=488;
			_splitContainerSpecification.SplitterWidth=5;
			_splitContainerSpecification.TabIndex=6;
			// 
			// _splitContainerConditions
			// 
			_splitContainerConditions.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainerConditions.FixedPanel=System.Windows.Forms.FixedPanel.Panel2;
			_splitContainerConditions.Location=new System.Drawing.Point(0, 0);
			_splitContainerConditions.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainerConditions.Name="_splitContainerConditions";
			// 
			// _splitContainerConditions.Panel1
			// 
			_splitContainerConditions.Panel1.Controls.Add(_panelConditionViews);
			// 
			// _splitContainerConditions.Panel2
			// 
			_splitContainerConditions.Panel2.Controls.Add(_panelSelectedConditions);
			_splitContainerConditions.Size=new System.Drawing.Size(1148, 488);
			_splitContainerConditions.SplitterDistance=884;
			_splitContainerConditions.SplitterWidth=5;
			_splitContainerConditions.TabIndex=0;
			// 
			// _groupBoxConditions
			// 
			_groupBoxConditions.Controls.Add(_panelConditions);
			_groupBoxConditions.Controls.Add(_toolStripConditionList);
			_groupBoxConditions.Dock=System.Windows.Forms.DockStyle.Fill;
			_groupBoxConditions.Location=new System.Drawing.Point(0, 0);
			_groupBoxConditions.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxConditions.Name="_groupBoxConditions";
			_groupBoxConditions.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxConditions.Size=new System.Drawing.Size(884, 488);
			_groupBoxConditions.TabIndex=0;
			_groupBoxConditions.TabStop=false;
			_groupBoxConditions.Text="Available Quality Conditions";
			// 
			// _panelConditions
			// 
			_panelConditions.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_panelConditions.Controls.Add(_conditionsLayerView);
			_panelConditions.Controls.Add(_conditionDatasetsControl);
			_panelConditions.Controls.Add(_conditionListControl);
			_panelConditions.Location=new System.Drawing.Point(13, 59);
			_panelConditions.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_panelConditions.Name="_panelConditions";
			_panelConditions.Size=new System.Drawing.Size(864, 422);
			_panelConditions.TabIndex=13;
			// 
			// _conditionsLayerView
			// 
			_conditionsLayerView.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_conditionsLayerView.CustomizeView=null;
			_conditionsLayerView.Location=new System.Drawing.Point(84, 61);
			_conditionsLayerView.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_conditionsLayerView.Name="_conditionsLayerView";
			_conditionsLayerView.Size=new System.Drawing.Size(620, 328);
			_conditionsLayerView.TabIndex=12;
			// 
			// _conditionDatasetsControl
			// 
			_conditionDatasetsControl.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_conditionDatasetsControl.CustomizeView=null;
			_conditionDatasetsControl.FilterRows=false;
			_conditionDatasetsControl.Location=new System.Drawing.Point(16, 8);
			_conditionDatasetsControl.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_conditionDatasetsControl.MatchCase=false;
			_conditionDatasetsControl.Name="_conditionDatasetsControl";
			_conditionDatasetsControl.Size=new System.Drawing.Size(814, 340);
			_conditionDatasetsControl.TabIndex=11;
			// 
			// _conditionListControl
			// 
			_conditionListControl.CustomizeView=null;
			_conditionListControl.FilterRows=false;
			_conditionListControl.Location=new System.Drawing.Point(4, 48);
			_conditionListControl.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_conditionListControl.MatchCase=false;
			_conditionListControl.Name="_conditionListControl";
			_conditionListControl.Size=new System.Drawing.Size(671, 186);
			_conditionListControl.TabIndex=15;
			// 
			// _toolStripConditionList
			// 
			_toolStripConditionList.ClickThrough=true;
			_toolStripConditionList.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripConditionList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripLabel1, _toolStripComboBoxView, _toolStripButtonWarningConditions, _toolStripButtonErrorConditions, _toolStripButtonStopConditions, toolStripSeparator1, _toolStripButtonEnableAll, _toolStripButtonEnableNone });
			_toolStripConditionList.Location=new System.Drawing.Point(4, 19);
			_toolStripConditionList.Name="_toolStripConditionList";
			_toolStripConditionList.RenderMode=System.Windows.Forms.ToolStripRenderMode.System;
			_toolStripConditionList.Size=new System.Drawing.Size(876, 25);
			_toolStripConditionList.TabIndex=15;
			_toolStripConditionList.Text="toolStrip1";
			// 
			// _toolStripLabel1
			// 
			_toolStripLabel1.Name="_toolStripLabel1";
			_toolStripLabel1.Size=new System.Drawing.Size(32, 22);
			_toolStripLabel1.Text="View";
			// 
			// _toolStripComboBoxView
			// 
			_toolStripComboBoxView.DropDownStyle=System.Windows.Forms.ComboBoxStyle.DropDownList;
			_toolStripComboBoxView.Font=new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			_toolStripComboBoxView.Name="_toolStripComboBoxView";
			_toolStripComboBoxView.Size=new System.Drawing.Size(256, 25);
			_toolStripComboBoxView.SelectedIndexChanged+=_toolStripComboBoxView_SelectedIndexChanged;
			// 
			// _toolStripButtonWarningConditions
			// 
			_toolStripButtonWarningConditions.CheckOnClick=true;
			_toolStripButtonWarningConditions.Image=TestTypeImages.TestTypeWarning;
			_toolStripButtonWarningConditions.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonWarningConditions.Name="_toolStripButtonWarningConditions";
			_toolStripButtonWarningConditions.Size=new System.Drawing.Size(77, 22);
			_toolStripButtonWarningConditions.Text="Warnings";
			_toolStripButtonWarningConditions.ToolTipText="Enable/disable all soft quality conditions (warnings)";
			_toolStripButtonWarningConditions.CheckedChanged+=_toolStripButtonWarningConditions_CheckedChanged;
			// 
			// _toolStripButtonErrorConditions
			// 
			_toolStripButtonErrorConditions.CheckOnClick=true;
			_toolStripButtonErrorConditions.Image=TestTypeImages.TestTypeError;
			_toolStripButtonErrorConditions.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonErrorConditions.Name="_toolStripButtonErrorConditions";
			_toolStripButtonErrorConditions.Size=new System.Drawing.Size(57, 22);
			_toolStripButtonErrorConditions.Text="Errors";
			_toolStripButtonErrorConditions.ToolTipText="Enable/disable all hard quality conditions (errors)";
			_toolStripButtonErrorConditions.CheckedChanged+=_toolStripButtonErrorConditions_CheckedChanged;
			// 
			// _toolStripButtonStopConditions
			// 
			_toolStripButtonStopConditions.CheckOnClick=true;
			_toolStripButtonStopConditions.Image=TestTypeImages.TestTypeStop;
			_toolStripButtonStopConditions.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonStopConditions.Name="_toolStripButtonStopConditions";
			_toolStripButtonStopConditions.Size=new System.Drawing.Size(112, 22);
			_toolStripButtonStopConditions.Text="Stop Conditions";
			_toolStripButtonStopConditions.ToolTipText="Enable/disable all stop conditions";
			_toolStripButtonStopConditions.CheckedChanged+=_toolStripButtonStopConditions_CheckedChanged;
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name="toolStripSeparator1";
			toolStripSeparator1.Size=new System.Drawing.Size(6, 25);
			// 
			// _toolStripButtonEnableAll
			// 
			_toolStripButtonEnableAll.Image=ProSuite.UI.Core.Properties.Resources.CheckAll;
			_toolStripButtonEnableAll.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonEnableAll.Name="_toolStripButtonEnableAll";
			_toolStripButtonEnableAll.Size=new System.Drawing.Size(41, 22);
			_toolStripButtonEnableAll.Text="All";
			_toolStripButtonEnableAll.Click+=_toolStripButtonEnableAll_Click;
			// 
			// _toolStripButtonEnableNone
			// 
			_toolStripButtonEnableNone.Image=ProSuite.UI.Core.Properties.Resources.UncheckAll;
			_toolStripButtonEnableNone.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonEnableNone.Name="_toolStripButtonEnableNone";
			_toolStripButtonEnableNone.Size=new System.Drawing.Size(56, 22);
			_toolStripButtonEnableNone.Text="None";
			_toolStripButtonEnableNone.Click+=_toolStripButtonEnableNone_Click;
			// 
			// _panelSelectedConditions
			// 
			_panelSelectedConditions.Controls.Add(_groupBoxSelected);
			_panelSelectedConditions.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelSelectedConditions.Location=new System.Drawing.Point(0, 0);
			_panelSelectedConditions.Name="_panelSelectedConditions";
			_panelSelectedConditions.Size=new System.Drawing.Size(259, 488);
			_panelSelectedConditions.TabIndex=4;
			// 
			// _groupBoxSelected
			// 
			_groupBoxSelected.Controls.Add(_dataGridViewEnabledConditions);
			_groupBoxSelected.Controls.Add(_labelEnabledConditions);
			_groupBoxSelected.Dock=System.Windows.Forms.DockStyle.Fill;
			_groupBoxSelected.Location=new System.Drawing.Point(0, 0);
			_groupBoxSelected.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxSelected.Name="_groupBoxSelected";
			_groupBoxSelected.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxSelected.Size=new System.Drawing.Size(259, 488);
			_groupBoxSelected.TabIndex=0;
			_groupBoxSelected.TabStop=false;
			_groupBoxSelected.Text="Enabled Quality Conditions";
			// 
			// _dataGridViewEnabledConditions
			// 
			_dataGridViewEnabledConditions.AllowUserToAddRows=false;
			_dataGridViewEnabledConditions.AllowUserToDeleteRows=false;
			_dataGridViewEnabledConditions.AllowUserToResizeRows=false;
			_dataGridViewEnabledConditions.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_dataGridViewEnabledConditions.AutoGenerateColumns=false;
			dataGridViewCellStyle4.Alignment=System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor=System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font=new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle4.ForeColor=System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor=System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor=System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode=System.Windows.Forms.DataGridViewTriState.True;
			_dataGridViewEnabledConditions.ColumnHeadersDefaultCellStyle=dataGridViewCellStyle4;
			_dataGridViewEnabledConditions.ColumnHeadersHeightSizeMode=System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewEnabledConditions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { dgcSelType, dgcSelTest });
			_dataGridViewEnabledConditions.DataSource=_bindingSourceEnabledConditions;
			dataGridViewCellStyle5.Alignment=System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle5.BackColor=System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font=new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle5.ForeColor=System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor=System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor=System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode=System.Windows.Forms.DataGridViewTriState.False;
			_dataGridViewEnabledConditions.DefaultCellStyle=dataGridViewCellStyle5;
			_dataGridViewEnabledConditions.Location=new System.Drawing.Point(7, 57);
			_dataGridViewEnabledConditions.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewEnabledConditions.MinimumSize=new System.Drawing.Size(70, 92);
			_dataGridViewEnabledConditions.Name="_dataGridViewEnabledConditions";
			_dataGridViewEnabledConditions.ReadOnly=true;
			dataGridViewCellStyle6.Alignment=System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor=System.Drawing.SystemColors.Control;
			dataGridViewCellStyle6.Font=new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			dataGridViewCellStyle6.ForeColor=System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle6.SelectionBackColor=System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor=System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode=System.Windows.Forms.DataGridViewTriState.True;
			_dataGridViewEnabledConditions.RowHeadersDefaultCellStyle=dataGridViewCellStyle6;
			_dataGridViewEnabledConditions.RowHeadersVisible=false;
			_dataGridViewEnabledConditions.RowHeadersWidth=20;
			_dataGridViewEnabledConditions.SelectionMode=System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewEnabledConditions.Size=new System.Drawing.Size(244, 424);
			_dataGridViewEnabledConditions.TabIndex=2;
			_dataGridViewEnabledConditions.CellFormatting+=_dataGridViewEnabledConditions_CellFormatting;
			_dataGridViewEnabledConditions.SelectionChanged+=_dataGridViewEnabledConditions_SelectionChanged;
			// 
			// dgcSelType
			// 
			dgcSelType.DataPropertyName="Type";
			dgcSelType.HeaderText="";
			dgcSelType.Name="dgcSelType";
			dgcSelType.ReadOnly=true;
			dgcSelType.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			dgcSelType.Width=20;
			// 
			// dgcSelTest
			// 
			dgcSelTest.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dgcSelTest.DataPropertyName="TestName";
			dgcSelTest.HeaderText="Quality Condition";
			dgcSelTest.Name="dgcSelTest";
			dgcSelTest.ReadOnly=true;
			dgcSelTest.ToolTipText="Quality Condition Name";
			// 
			// _bindingSourceEnabledConditions
			// 
			_bindingSourceEnabledConditions.DataSource=typeof(SpecificationDataset);
			// 
			// _labelEnabledConditions
			// 
			_labelEnabledConditions.AutoSize=true;
			_labelEnabledConditions.Location=new System.Drawing.Point(7, 30);
			_labelEnabledConditions.Margin=new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelEnabledConditions.Name="_labelEnabledConditions";
			_labelEnabledConditions.Size=new System.Drawing.Size(198, 15);
			_labelEnabledConditions.TabIndex=0;
			_labelEnabledConditions.Text="### of ### Tests selected to execute";
			// 
			// _groupBoxSelectedParameters
			// 
			_groupBoxSelectedParameters.Anchor=System.Windows.Forms.AnchorStyles.Top|System.Windows.Forms.AnchorStyles.Bottom|System.Windows.Forms.AnchorStyles.Left|System.Windows.Forms.AnchorStyles.Right;
			_groupBoxSelectedParameters.Controls.Add(_splitContainer);
			_groupBoxSelectedParameters.Location=new System.Drawing.Point(0, 3);
			_groupBoxSelectedParameters.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxSelectedParameters.Name="_groupBoxSelectedParameters";
			_groupBoxSelectedParameters.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_groupBoxSelectedParameters.Size=new System.Drawing.Size(1067, 251);
			_groupBoxSelectedParameters.TabIndex=0;
			_groupBoxSelectedParameters.TabStop=false;
			_groupBoxSelectedParameters.Text="Selected Quality Condition";
			_groupBoxSelectedParameters.EnabledChanged+=_groupBoxSelectedParameters_EnabledChanged;
			// 
			// _splitContainer
			// 
			_splitContainer.Dock=System.Windows.Forms.DockStyle.Fill;
			_splitContainer.FixedPanel=System.Windows.Forms.FixedPanel.Panel1;
			_splitContainer.Location=new System.Drawing.Point(4, 19);
			_splitContainer.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_splitContainer.Name="_splitContainer";
			// 
			// _splitContainer.Panel1
			// 
			_splitContainer.Panel1.Controls.Add(_qualityConditionControl);
			// 
			// _splitContainer.Panel2
			// 
			_splitContainer.Panel2.Controls.Add(_tabControl);
			_splitContainer.Size=new System.Drawing.Size(1059, 229);
			_splitContainer.SplitterDistance=352;
			_splitContainer.SplitterWidth=5;
			_splitContainer.TabIndex=2;
			// 
			// _qualityConditionControl
			// 
			_qualityConditionControl.Dock=System.Windows.Forms.DockStyle.Fill;
			_qualityConditionControl.Location=new System.Drawing.Point(0, 0);
			_qualityConditionControl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_qualityConditionControl.Name="_qualityConditionControl";
			_qualityConditionControl.QualityCondition=null;
			_qualityConditionControl.ReadOnly=true;
			_qualityConditionControl.Size=new System.Drawing.Size(352, 229);
			_qualityConditionControl.TabIndex=0;
			// 
			// _tabControl
			// 
			_tabControl.Controls.Add(_tabPageParameterValues);
			_tabControl.Controls.Add(_tabPageTestDescriptor);
			_tabControl.Dock=System.Windows.Forms.DockStyle.Fill;
			_tabControl.Location=new System.Drawing.Point(0, 0);
			_tabControl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabControl.Name="_tabControl";
			_tabControl.SelectedIndex=0;
			_tabControl.Size=new System.Drawing.Size(702, 229);
			_tabControl.TabIndex=0;
			// 
			// _tabPageParameterValues
			// 
			_tabPageParameterValues.Controls.Add(_qualityConditionTableViewControl);
			_tabPageParameterValues.Controls.Add(_toolStrip);
			_tabPageParameterValues.Location=new System.Drawing.Point(4, 24);
			_tabPageParameterValues.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageParameterValues.Name="_tabPageParameterValues";
			_tabPageParameterValues.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageParameterValues.Size=new System.Drawing.Size(694, 201);
			_tabPageParameterValues.TabIndex=0;
			_tabPageParameterValues.Text="Parameter Values";
			_tabPageParameterValues.UseVisualStyleBackColor=true;
			// 
			// _qualityConditionTableViewControl
			// 
			_qualityConditionTableViewControl.Dock=System.Windows.Forms.DockStyle.Fill;
			_qualityConditionTableViewControl.Location=new System.Drawing.Point(4, 28);
			_qualityConditionTableViewControl.Margin=new System.Windows.Forms.Padding(5, 3, 5, 3);
			_qualityConditionTableViewControl.Name="_qualityConditionTableViewControl";
			_qualityConditionTableViewControl.Size=new System.Drawing.Size(686, 170);
			_qualityConditionTableViewControl.TabIndex=2;
			// 
			// _toolStrip
			// 
			_toolStrip.ClickThrough=true;
			_toolStrip.GripStyle=System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripButtonCustomizeTestParameterValues, _toolStripButtonReset });
			_toolStrip.Location=new System.Drawing.Point(4, 3);
			_toolStrip.Name="_toolStrip";
			_toolStrip.RenderMode=System.Windows.Forms.ToolStripRenderMode.System;
			_toolStrip.Size=new System.Drawing.Size(686, 25);
			_toolStrip.TabIndex=4;
			_toolStrip.Text="Parameter Value Tools";
			// 
			// _toolStripButtonCustomizeTestParameterValues
			// 
			_toolStripButtonCustomizeTestParameterValues.Image=ProSuite.UI.Core.Properties.Resources.Edit;
			_toolStripButtonCustomizeTestParameterValues.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonCustomizeTestParameterValues.Name="_toolStripButtonCustomizeTestParameterValues";
			_toolStripButtonCustomizeTestParameterValues.Size=new System.Drawing.Size(92, 22);
			_toolStripButtonCustomizeTestParameterValues.Text="Customize...";
			_toolStripButtonCustomizeTestParameterValues.Click+=_toolStripButtonCustomizeTestParameterValues_Click;
			// 
			// _toolStripButtonReset
			// 
			_toolStripButtonReset.Image=ProSuite.UI.Core.Properties.Resources.Undo_16x;
			_toolStripButtonReset.ImageTransparentColor=System.Drawing.Color.Magenta;
			_toolStripButtonReset.Name="_toolStripButtonReset";
			_toolStripButtonReset.Size=new System.Drawing.Size(55, 22);
			_toolStripButtonReset.Text="Reset";
			_toolStripButtonReset.ToolTipText="Reset to original parameter values";
			_toolStripButtonReset.Click+=_toolStripButtonReset_Click;
			// 
			// _tabPageTestDescriptor
			// 
			_tabPageTestDescriptor.Controls.Add(_testDescriptorControl);
			_tabPageTestDescriptor.Location=new System.Drawing.Point(4, 24);
			_tabPageTestDescriptor.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageTestDescriptor.Name="_tabPageTestDescriptor";
			_tabPageTestDescriptor.Padding=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_tabPageTestDescriptor.Size=new System.Drawing.Size(698, 201);
			_tabPageTestDescriptor.TabIndex=1;
			_tabPageTestDescriptor.Text="Test";
			_tabPageTestDescriptor.UseVisualStyleBackColor=true;
			// 
			// _testDescriptorControl
			// 
			_testDescriptorControl.Dock=System.Windows.Forms.DockStyle.Fill;
			_testDescriptorControl.Location=new System.Drawing.Point(4, 3);
			_testDescriptorControl.Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			_testDescriptorControl.Name="_testDescriptorControl";
			_testDescriptorControl.Size=new System.Drawing.Size(690, 195);
			_testDescriptorControl.TabIndex=1;
			_testDescriptorControl.TestDescriptor=null;
			// 
			// _mainSplitterBottomPanel
			// 
			_mainSplitterBottomPanel.Controls.Add(_groupBoxSelectedParameters);
			_mainSplitterBottomPanel.Controls.Add(_buttonOK);
			_mainSplitterBottomPanel.Controls.Add(_buttonCancel);
			_mainSplitterBottomPanel.Dock=System.Windows.Forms.DockStyle.Fill;
			_mainSplitterBottomPanel.Location=new System.Drawing.Point(0, 0);
			_mainSplitterBottomPanel.Name="_mainSplitterBottomPanel";
			_mainSplitterBottomPanel.Size=new System.Drawing.Size(1148, 263);
			_mainSplitterBottomPanel.TabIndex=2;
			// 
			// dataGridViewCheckBoxColumn1
			// 
			dataGridViewCheckBoxColumn1.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
			dataGridViewCheckBoxColumn1.DataPropertyName="Enabled";
			dataGridViewCheckBoxColumn1.HeaderText="";
			dataGridViewCheckBoxColumn1.MinimumWidth=25;
			dataGridViewCheckBoxColumn1.Name="dataGridViewCheckBoxColumn1";
			dataGridViewCheckBoxColumn1.Resizable=System.Windows.Forms.DataGridViewTriState.False;
			dataGridViewCheckBoxColumn1.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			// 
			// dataGridViewImageColumn1
			// 
			dataGridViewImageColumn1.DataPropertyName="Type";
			dataGridViewImageColumn1.HeaderText="";
			dataGridViewImageColumn1.MinimumWidth=20;
			dataGridViewImageColumn1.Name="dataGridViewImageColumn1";
			dataGridViewImageColumn1.ReadOnly=true;
			dataGridViewImageColumn1.Resizable=System.Windows.Forms.DataGridViewTriState.False;
			dataGridViewImageColumn1.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			dataGridViewImageColumn1.Width=20;
			// 
			// dataGridViewTextBoxColumn1
			// 
			dataGridViewTextBoxColumn1.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			dataGridViewTextBoxColumn1.DataPropertyName="TestType";
			dataGridViewTextBoxColumn1.HeaderText="Test Type";
			dataGridViewTextBoxColumn1.Name="dataGridViewTextBoxColumn1";
			dataGridViewTextBoxColumn1.ReadOnly=true;
			// 
			// dataGridViewTextBoxColumn2
			// 
			dataGridViewTextBoxColumn2.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewTextBoxColumn2.DataPropertyName="DatasetName";
			dataGridViewTextBoxColumn2.HeaderText="Dataset";
			dataGridViewTextBoxColumn2.MinimumWidth=50;
			dataGridViewTextBoxColumn2.Name="dataGridViewTextBoxColumn2";
			dataGridViewTextBoxColumn2.ReadOnly=true;
			// 
			// dataGridViewImageColumn2
			// 
			dataGridViewImageColumn2.DataPropertyName="DatasetType";
			dataGridViewImageColumn2.HeaderText="";
			dataGridViewImageColumn2.MinimumWidth=20;
			dataGridViewImageColumn2.Name="dataGridViewImageColumn2";
			dataGridViewImageColumn2.ReadOnly=true;
			dataGridViewImageColumn2.Resizable=System.Windows.Forms.DataGridViewTriState.False;
			dataGridViewImageColumn2.SortMode=System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			dataGridViewImageColumn2.Width=20;
			// 
			// dataGridViewTextBoxColumn3
			// 
			dataGridViewTextBoxColumn3.AutoSizeMode=System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewTextBoxColumn3.DataPropertyName="DatasetName";
			dataGridViewTextBoxColumn3.HeaderText="Dataset";
			dataGridViewTextBoxColumn3.MinimumWidth=50;
			dataGridViewTextBoxColumn3.Name="dataGridViewTextBoxColumn3";
			dataGridViewTextBoxColumn3.ReadOnly=true;
			// 
			// _panelTop
			// 
			_panelTop.Controls.Add(_labelDescription);
			_panelTop.Controls.Add(_labelSpecification);
			_panelTop.Controls.Add(_textBoxSpecification);
			_panelTop.Controls.Add(_textBoxDescription);
			_panelTop.Dock=System.Windows.Forms.DockStyle.Top;
			_panelTop.Location=new System.Drawing.Point(0, 0);
			_panelTop.Name="_panelTop";
			_panelTop.Size=new System.Drawing.Size(1148, 100);
			_panelTop.TabIndex=8;
			// 
			// _panelConditionViews
			// 
			_panelConditionViews.Controls.Add(_groupBoxConditions);
			_panelConditionViews.Dock=System.Windows.Forms.DockStyle.Fill;
			_panelConditionViews.Location=new System.Drawing.Point(0, 0);
			_panelConditionViews.Name="_panelConditionViews";
			_panelConditionViews.Size=new System.Drawing.Size(884, 488);
			_panelConditionViews.TabIndex=1;
			// 
			// CustomizeQASpecForm
			// 
			AcceptButton=_buttonOK;
			AutoScaleDimensions=new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode=System.Windows.Forms.AutoScaleMode.Font;
			CancelButton=_buttonCancel;
			ClientSize=new System.Drawing.Size(1148, 878);
			Controls.Add(_splitContainerSpecification);
			Controls.Add(_statusStrip);
			Controls.Add(_panelTop);
			FormBorderStyle=System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			KeyPreview=true;
			Margin=new System.Windows.Forms.Padding(4, 3, 4, 3);
			MinimumSize=new System.Drawing.Size(814, 571);
			Name="CustomizeQASpecForm";
			ShowInTaskbar=false;
			StartPosition=System.Windows.Forms.FormStartPosition.CenterScreen;
			Text="Customize Quality Specification";
			FormClosed+=CustomizeQASpecForm_FormClosed;
			Load+=CustomizeQASpecForm_Load;
			KeyDown+=CustomizeQASpecForm_KeyDown;
			_splitContainerSpecification.Panel1.ResumeLayout(false);
			_splitContainerSpecification.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainerSpecification).EndInit();
			_splitContainerSpecification.ResumeLayout(false);
			_splitContainerConditions.Panel1.ResumeLayout(false);
			_splitContainerConditions.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainerConditions).EndInit();
			_splitContainerConditions.ResumeLayout(false);
			_groupBoxConditions.ResumeLayout(false);
			_groupBoxConditions.PerformLayout();
			_panelConditions.ResumeLayout(false);
			_toolStripConditionList.ResumeLayout(false);
			_toolStripConditionList.PerformLayout();
			_panelSelectedConditions.ResumeLayout(false);
			_groupBoxSelected.ResumeLayout(false);
			_groupBoxSelected.PerformLayout();
			((System.ComponentModel.ISupportInitialize)_dataGridViewEnabledConditions).EndInit();
			((System.ComponentModel.ISupportInitialize)_bindingSourceEnabledConditions).EndInit();
			_groupBoxSelectedParameters.ResumeLayout(false);
			_splitContainer.Panel1.ResumeLayout(false);
			_splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
			_splitContainer.ResumeLayout(false);
			_tabControl.ResumeLayout(false);
			_tabPageParameterValues.ResumeLayout(false);
			_tabPageParameterValues.PerformLayout();
			_toolStrip.ResumeLayout(false);
			_toolStrip.PerformLayout();
			_tabPageTestDescriptor.ResumeLayout(false);
			_mainSplitterBottomPanel.ResumeLayout(false);
			_panelTop.ResumeLayout(false);
			_panelTop.PerformLayout();
			_panelConditionViews.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();
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
		private System.Windows.Forms.Panel _panelTop;
		private System.Windows.Forms.Panel _mainSplitterBottomPanel;
		private System.Windows.Forms.Panel _panelSelectedConditions;
		private System.Windows.Forms.Panel _panelConditionViews;
	}
}
