using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.UI.Core.QA.VerificationResult
{
    partial class QAVerificationForm
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
			this._labelSpecification = new System.Windows.Forms.Label();
			this._textBoxSpecification = new System.Windows.Forms.TextBox();
			this._groupBoxCondition = new System.Windows.Forms.GroupBox();
			this._qualityConditionVerificationControl = new VerificationResult.QualityConditionVerificationControl();
			this._groupBoxConditions = new System.Windows.Forms.GroupBox();
			this._panel1 = new System.Windows.Forms.Panel();
			this._verifiedConditionsHierarchyControl = new VerifiedConditionsHierarchyControl();
			this._verifiedConditionsControl = new VerifiedConditionsControl();
			this._verifiedDatasetsControl = new VerifiedDatasetsControl();
			this._toolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this._toolStripComboBoxView = new System.Windows.Forms.ToolStripComboBox();
			this._toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this._toolStripButtonNoIssues = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonWarnings = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonErrors = new System.Windows.Forms.ToolStripButton();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelContext = new System.Windows.Forms.Label();
			this._textBoxContext = new System.Windows.Forms.TextBox();
			this._textBoxUser = new System.Windows.Forms.TextBox();
			this._labelOperator = new System.Windows.Forms.Label();
			this._labelStartDate = new System.Windows.Forms.Label();
			this._textBoxStartDate = new System.Windows.Forms.TextBox();
			this._textBoxEndDate = new System.Windows.Forms.TextBox();
			this._labelEnded = new System.Windows.Forms.Label();
			this._labelProcessorTime = new System.Windows.Forms.Label();
			this._textBoxCPUTime = new System.Windows.Forms.TextBox();
			this._textBoxTotalTime = new System.Windows.Forms.TextBox();
			this._labelTotalTime = new System.Windows.Forms.Label();
			this._textBoxIssueCount = new System.Windows.Forms.TextBox();
			this._labelTotalErrors = new System.Windows.Forms.Label();
			this._splitContainerDetail = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._textBoxVerificationStatus = new System.Windows.Forms.TextBox();
			this._textBoxErrorCount = new System.Windows.Forms.TextBox();
			this._labelHardErrors = new System.Windows.Forms.Label();
			this._textBoxWarningCount = new System.Windows.Forms.TextBox();
			this._labelSoftErrors = new System.Windows.Forms.Label();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._toolStripStatusLabelFiller = new System.Windows.Forms.ToolStripStatusLabel();
			this._toolStripSplitButtonClose = new System.Windows.Forms.ToolStripSplitButton();
			this._groupBoxCondition.SuspendLayout();
			this._groupBoxConditions.SuspendLayout();
			this._panel1.SuspendLayout();
			this._toolStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDetail)).BeginInit();
			this._splitContainerDetail.Panel1.SuspendLayout();
			this._splitContainerDetail.Panel2.SuspendLayout();
			this._splitContainerDetail.SuspendLayout();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelSpecification
			// 
			this._labelSpecification.AutoSize = true;
			this._labelSpecification.Location = new System.Drawing.Point(12, 15);
			this._labelSpecification.Name = "_labelSpecification";
			this._labelSpecification.Size = new System.Drawing.Size(71, 13);
			this._labelSpecification.TabIndex = 0;
			this._labelSpecification.Text = "Specification:";
			// 
			// _textBoxSpecification
			// 
			this._textBoxSpecification.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxSpecification.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxSpecification.Location = new System.Drawing.Point(89, 12);
			this._textBoxSpecification.Name = "_textBoxSpecification";
			this._textBoxSpecification.ReadOnly = true;
			this._textBoxSpecification.Size = new System.Drawing.Size(706, 20);
			this._textBoxSpecification.TabIndex = 1;
			this._textBoxSpecification.TabStop = false;
			// 
			// _groupBoxCondition
			// 
			this._groupBoxCondition.Controls.Add(this._qualityConditionVerificationControl);
			this._groupBoxCondition.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxCondition.Location = new System.Drawing.Point(2, 2);
			this._groupBoxCondition.MinimumSize = new System.Drawing.Size(0, 150);
			this._groupBoxCondition.Name = "_groupBoxCondition";
			this._groupBoxCondition.Padding = new System.Windows.Forms.Padding(3, 8, 3, 3);
			this._groupBoxCondition.Size = new System.Drawing.Size(883, 169);
			this._groupBoxCondition.TabIndex = 0;
			this._groupBoxCondition.TabStop = false;
			this._groupBoxCondition.Text = "Selected quality condition";
			// 
			// _qualityConditionVerificationControl
			// 
			this._qualityConditionVerificationControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionVerificationControl.Location = new System.Drawing.Point(3, 21);
			this._qualityConditionVerificationControl.MinimumSize = new System.Drawing.Size(520, 130);
			this._qualityConditionVerificationControl.Name = "_qualityConditionVerificationControl";
			this._qualityConditionVerificationControl.Size = new System.Drawing.Size(877, 145);
			this._qualityConditionVerificationControl.TabIndex = 0;
			this._qualityConditionVerificationControl.EnabledChanged += new System.EventHandler(this._qualityConditionVerificationControl_EnabledChanged);
			// 
			// _groupBoxConditions
			// 
			this._groupBoxConditions.Controls.Add(this._panel1);
			this._groupBoxConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupBoxConditions.Location = new System.Drawing.Point(2, 2);
			this._groupBoxConditions.Name = "_groupBoxConditions";
			this._groupBoxConditions.Size = new System.Drawing.Size(883, 308);
			this._groupBoxConditions.TabIndex = 0;
			this._groupBoxConditions.TabStop = false;
			this._groupBoxConditions.Text = "Quality Conditions";
			// 
			// _panel1
			// 
			this._panel1.Controls.Add(this._verifiedConditionsHierarchyControl);
			this._panel1.Controls.Add(this._verifiedConditionsControl);
			this._panel1.Controls.Add(this._verifiedDatasetsControl);
			this._panel1.Controls.Add(this._toolStrip);
			this._panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panel1.Location = new System.Drawing.Point(3, 16);
			this._panel1.Name = "_panel1";
			this._panel1.Size = new System.Drawing.Size(877, 289);
			this._panel1.TabIndex = 11;
			// 
			// _verifiedConditionsHierarchyControl
			// 
			this._verifiedConditionsHierarchyControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._verifiedConditionsHierarchyControl.Location = new System.Drawing.Point(0, 25);
			this._verifiedConditionsHierarchyControl.Name = "_verifiedConditionsHierarchyControl";
			this._verifiedConditionsHierarchyControl.Size = new System.Drawing.Size(877, 264);
			this._verifiedConditionsHierarchyControl.SplitterDistance = 399;
			this._verifiedConditionsHierarchyControl.TabIndex = 13;
			this._verifiedConditionsHierarchyControl.SelectionChanged += new System.EventHandler(this._verifiedConditionsHierarchyControl_SelectionChanged);
			// 
			// _verifiedConditionsControl
			// 
			this._verifiedConditionsControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._verifiedConditionsControl.FilterRows = false;
			this._verifiedConditionsControl.Location = new System.Drawing.Point(0, 25);
			this._verifiedConditionsControl.MatchCase = false;
			this._verifiedConditionsControl.Name = "_verifiedConditionsControl";
			this._verifiedConditionsControl.Size = new System.Drawing.Size(877, 264);
			this._verifiedConditionsControl.TabIndex = 11;
			this._verifiedConditionsControl.Visible = false;
			this._verifiedConditionsControl.SelectionChanged += new System.EventHandler(this._verifiedConditionsControl_SelectionChanged);
			// 
			// _verifiedDatasetsControl
			// 
			this._verifiedDatasetsControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._verifiedDatasetsControl.FilterRows = false;
			this._verifiedDatasetsControl.Location = new System.Drawing.Point(0, 25);
			this._verifiedDatasetsControl.MatchCase = false;
			this._verifiedDatasetsControl.Name = "_verifiedDatasetsControl";
			this._verifiedDatasetsControl.Size = new System.Drawing.Size(877, 264);
			this._verifiedDatasetsControl.TabIndex = 15;
			this._verifiedDatasetsControl.SelectionChanged += new System.EventHandler(this._verifiedDatasetsControl_SelectionChanged);
			// 
			// _toolStrip
			// 
			this._toolStrip.ClickThrough = true;
			this._toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripLabel1,
            this._toolStripComboBoxView,
            this._toolStripSeparator1,
            this._toolStripButtonNoIssues,
            this._toolStripButtonWarnings,
            this._toolStripButtonErrors});
			this._toolStrip.Location = new System.Drawing.Point(0, 0);
			this._toolStrip.Name = "_toolStrip";
			this._toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStrip.Size = new System.Drawing.Size(877, 25);
			this._toolStrip.TabIndex = 14;
			this._toolStrip.Text = "toolStrip1";
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
			this._toolStripComboBoxView.Size = new System.Drawing.Size(260, 25);
			this._toolStripComboBoxView.SelectedIndexChanged += new System.EventHandler(this._toolStripComboBoxView_SelectedIndexChanged);
			// 
			// _toolStripSeparator1
			// 
			this._toolStripSeparator1.Name = "_toolStripSeparator1";
			this._toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// _toolStripButtonNoIssues
			// 
			this._toolStripButtonNoIssues.CheckOnClick = true;
			this._toolStripButtonNoIssues.Image = global::ProSuite.UI.Core.Properties.VerificationResultImages.OK;
			this._toolStripButtonNoIssues.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonNoIssues.Name = "_toolStripButtonNoIssues";
			this._toolStripButtonNoIssues.Size = new System.Drawing.Size(77, 22);
			this._toolStripButtonNoIssues.Text = "No Issues";
			this._toolStripButtonNoIssues.CheckedChanged += new System.EventHandler(this._toolStripButtonNoIssues_CheckedChanged);
			// 
			// _toolStripButtonWarnings
			// 
			this._toolStripButtonWarnings.Checked = true;
			this._toolStripButtonWarnings.CheckOnClick = true;
			this._toolStripButtonWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
			this._toolStripButtonWarnings.Image = global::ProSuite.UI.Core.Properties.VerificationResultImages.Warning;
			this._toolStripButtonWarnings.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonWarnings.Name = "_toolStripButtonWarnings";
			this._toolStripButtonWarnings.Size = new System.Drawing.Size(77, 22);
			this._toolStripButtonWarnings.Text = "Warnings";
			this._toolStripButtonWarnings.Click += new System.EventHandler(this._toolStripButtonWarnings_Click);
			// 
			// _toolStripButtonErrors
			// 
			this._toolStripButtonErrors.Checked = true;
			this._toolStripButtonErrors.CheckOnClick = true;
			this._toolStripButtonErrors.CheckState = System.Windows.Forms.CheckState.Checked;
			this._toolStripButtonErrors.Image = global::ProSuite.UI.Core.Properties.VerificationResultImages.Error;
			this._toolStripButtonErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonErrors.Name = "_toolStripButtonErrors";
			this._toolStripButtonErrors.Size = new System.Drawing.Size(57, 22);
			this._toolStripButtonErrors.Text = "Errors";
			this._toolStripButtonErrors.Click += new System.EventHandler(this._toolStripButtonErrors_Click);
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(20, 41);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 2;
			this._labelDescription.Text = "Description:";
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxDescription.Location = new System.Drawing.Point(89, 38);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ReadOnly = true;
			this._textBoxDescription.Size = new System.Drawing.Size(810, 35);
			this._textBoxDescription.TabIndex = 3;
			this._textBoxDescription.TabStop = false;
			// 
			// _labelContext
			// 
			this._labelContext.AutoSize = true;
			this._labelContext.Location = new System.Drawing.Point(37, 82);
			this._labelContext.Name = "_labelContext";
			this._labelContext.Size = new System.Drawing.Size(46, 13);
			this._labelContext.TabIndex = 4;
			this._labelContext.Text = "Context:";
			this._labelContext.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxContext
			// 
			this._textBoxContext.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxContext.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxContext.Location = new System.Drawing.Point(89, 79);
			this._textBoxContext.Name = "_textBoxContext";
			this._textBoxContext.ReadOnly = true;
			this._textBoxContext.Size = new System.Drawing.Size(810, 20);
			this._textBoxContext.TabIndex = 5;
			this._textBoxContext.TabStop = false;
			// 
			// _textBoxUser
			// 
			this._textBoxUser.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxUser.Location = new System.Drawing.Point(594, 131);
			this._textBoxUser.Name = "_textBoxUser";
			this._textBoxUser.ReadOnly = true;
			this._textBoxUser.Size = new System.Drawing.Size(98, 20);
			this._textBoxUser.TabIndex = 13;
			this._textBoxUser.TabStop = false;
			// 
			// _labelOperator
			// 
			this._labelOperator.AutoSize = true;
			this._labelOperator.Location = new System.Drawing.Point(556, 134);
			this._labelOperator.Name = "_labelOperator";
			this._labelOperator.Size = new System.Drawing.Size(32, 13);
			this._labelOperator.TabIndex = 12;
			this._labelOperator.Text = "User:";
			// 
			// _labelStartDate
			// 
			this._labelStartDate.AutoSize = true;
			this._labelStartDate.Location = new System.Drawing.Point(37, 108);
			this._labelStartDate.Name = "_labelStartDate";
			this._labelStartDate.Size = new System.Drawing.Size(44, 13);
			this._labelStartDate.TabIndex = 6;
			this._labelStartDate.Text = "Started:";
			// 
			// _textBoxStartDate
			// 
			this._textBoxStartDate.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxStartDate.Location = new System.Drawing.Point(87, 105);
			this._textBoxStartDate.Name = "_textBoxStartDate";
			this._textBoxStartDate.ReadOnly = true;
			this._textBoxStartDate.Size = new System.Drawing.Size(99, 20);
			this._textBoxStartDate.TabIndex = 7;
			this._textBoxStartDate.TabStop = false;
			this._textBoxStartDate.Text = "30.12.2000 23:59";
			this._textBoxStartDate.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// _textBoxEndDate
			// 
			this._textBoxEndDate.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxEndDate.Location = new System.Drawing.Point(256, 105);
			this._textBoxEndDate.Name = "_textBoxEndDate";
			this._textBoxEndDate.ReadOnly = true;
			this._textBoxEndDate.Size = new System.Drawing.Size(98, 20);
			this._textBoxEndDate.TabIndex = 15;
			this._textBoxEndDate.TabStop = false;
			this._textBoxEndDate.Text = "31.12.2000 20:20";
			this._textBoxEndDate.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// _labelEnded
			// 
			this._labelEnded.AutoSize = true;
			this._labelEnded.Location = new System.Drawing.Point(201, 108);
			this._labelEnded.Name = "_labelEnded";
			this._labelEnded.Size = new System.Drawing.Size(49, 13);
			this._labelEnded.TabIndex = 14;
			this._labelEnded.Text = "Finished:";
			// 
			// _labelProcessorTime
			// 
			this._labelProcessorTime.AutoSize = true;
			this._labelProcessorTime.Location = new System.Drawing.Point(534, 108);
			this._labelProcessorTime.Name = "_labelProcessorTime";
			this._labelProcessorTime.Size = new System.Drawing.Size(54, 13);
			this._labelProcessorTime.TabIndex = 16;
			this._labelProcessorTime.Text = "CPU time:";
			// 
			// _textBoxCPUTime
			// 
			this._textBoxCPUTime.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxCPUTime.Location = new System.Drawing.Point(594, 105);
			this._textBoxCPUTime.Name = "_textBoxCPUTime";
			this._textBoxCPUTime.ReadOnly = true;
			this._textBoxCPUTime.Size = new System.Drawing.Size(98, 20);
			this._textBoxCPUTime.TabIndex = 17;
			this._textBoxCPUTime.TabStop = false;
			this._textBoxCPUTime.Text = "1800 sec";
			this._textBoxCPUTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// _textBoxTotalTime
			// 
			this._textBoxTotalTime.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxTotalTime.Location = new System.Drawing.Point(425, 105);
			this._textBoxTotalTime.Name = "_textBoxTotalTime";
			this._textBoxTotalTime.ReadOnly = true;
			this._textBoxTotalTime.Size = new System.Drawing.Size(99, 20);
			this._textBoxTotalTime.TabIndex = 9;
			this._textBoxTotalTime.TabStop = false;
			this._textBoxTotalTime.Text = "3600 sec";
			this._textBoxTotalTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// _labelTotalTime
			// 
			this._labelTotalTime.AutoSize = true;
			this._labelTotalTime.Location = new System.Drawing.Point(363, 108);
			this._labelTotalTime.Name = "_labelTotalTime";
			this._labelTotalTime.Size = new System.Drawing.Size(56, 13);
			this._labelTotalTime.TabIndex = 8;
			this._labelTotalTime.Text = "Total time:";
			// 
			// _textBoxIssueCount
			// 
			this._textBoxIssueCount.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxIssueCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxIssueCount.Location = new System.Drawing.Point(87, 131);
			this._textBoxIssueCount.Name = "_textBoxIssueCount";
			this._textBoxIssueCount.ReadOnly = true;
			this._textBoxIssueCount.Size = new System.Drawing.Size(99, 20);
			this._textBoxIssueCount.TabIndex = 11;
			this._textBoxIssueCount.TabStop = false;
			// 
			// _labelTotalErrors
			// 
			this._labelTotalErrors.AutoSize = true;
			this._labelTotalErrors.Location = new System.Drawing.Point(15, 134);
			this._labelTotalErrors.Name = "_labelTotalErrors";
			this._labelTotalErrors.Size = new System.Drawing.Size(66, 13);
			this._labelTotalErrors.TabIndex = 10;
			this._labelTotalErrors.Text = "Total issues:";
			// 
			// _splitContainerDetail
			// 
			this._splitContainerDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._splitContainerDetail.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this._splitContainerDetail.Location = new System.Drawing.Point(12, 157);
			this._splitContainerDetail.MinimumSize = new System.Drawing.Size(500, 200);
			this._splitContainerDetail.Name = "_splitContainerDetail";
			this._splitContainerDetail.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainerDetail.Panel1
			// 
			this._splitContainerDetail.Panel1.Controls.Add(this._groupBoxConditions);
			this._splitContainerDetail.Panel1.Padding = new System.Windows.Forms.Padding(2);
			// 
			// _splitContainerDetail.Panel2
			// 
			this._splitContainerDetail.Panel2.Controls.Add(this._groupBoxCondition);
			this._splitContainerDetail.Panel2.Padding = new System.Windows.Forms.Padding(2);
			this._splitContainerDetail.Size = new System.Drawing.Size(887, 489);
			this._splitContainerDetail.SplitterDistance = 312;
			this._splitContainerDetail.TabIndex = 26;
			// 
			// _textBoxVerificationStatus
			// 
			this._textBoxVerificationStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxVerificationStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._textBoxVerificationStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxVerificationStatus.Location = new System.Drawing.Point(801, 13);
			this._textBoxVerificationStatus.Multiline = true;
			this._textBoxVerificationStatus.Name = "_textBoxVerificationStatus";
			this._textBoxVerificationStatus.ReadOnly = true;
			this._textBoxVerificationStatus.Size = new System.Drawing.Size(98, 18);
			this._textBoxVerificationStatus.TabIndex = 21;
			this._textBoxVerificationStatus.TabStop = false;
			this._textBoxVerificationStatus.Text = "Fulfilled / Not Fulfilled / Cancelled ";
			this._textBoxVerificationStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// _textBoxErrorCount
			// 
			this._textBoxErrorCount.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxErrorCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxErrorCount.Location = new System.Drawing.Point(256, 131);
			this._textBoxErrorCount.Name = "_textBoxErrorCount";
			this._textBoxErrorCount.ReadOnly = true;
			this._textBoxErrorCount.Size = new System.Drawing.Size(98, 20);
			this._textBoxErrorCount.TabIndex = 19;
			this._textBoxErrorCount.TabStop = false;
			// 
			// _labelHardErrors
			// 
			this._labelHardErrors.AutoSize = true;
			this._labelHardErrors.Location = new System.Drawing.Point(213, 134);
			this._labelHardErrors.Name = "_labelHardErrors";
			this._labelHardErrors.Size = new System.Drawing.Size(37, 13);
			this._labelHardErrors.TabIndex = 18;
			this._labelHardErrors.Text = "Errors:";
			// 
			// _textBoxWarningCount
			// 
			this._textBoxWarningCount.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxWarningCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxWarningCount.Location = new System.Drawing.Point(425, 131);
			this._textBoxWarningCount.Name = "_textBoxWarningCount";
			this._textBoxWarningCount.ReadOnly = true;
			this._textBoxWarningCount.Size = new System.Drawing.Size(98, 20);
			this._textBoxWarningCount.TabIndex = 25;
			this._textBoxWarningCount.TabStop = false;
			// 
			// _labelSoftErrors
			// 
			this._labelSoftErrors.AutoSize = true;
			this._labelSoftErrors.Location = new System.Drawing.Point(364, 134);
			this._labelSoftErrors.Name = "_labelSoftErrors";
			this._labelSoftErrors.Size = new System.Drawing.Size(55, 13);
			this._labelSoftErrors.TabIndex = 24;
			this._labelSoftErrors.Text = "Warnings:";
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabelFiller,
            this._toolStripSplitButtonClose});
			this._statusStrip.Location = new System.Drawing.Point(0, 653);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(911, 22);
			this._statusStrip.TabIndex = 27;
			this._statusStrip.Text = "_statusStrip";
			// 
			// _toolStripStatusLabelFiller
			// 
			this._toolStripStatusLabelFiller.Name = "_toolStripStatusLabelFiller";
			this._toolStripStatusLabelFiller.Size = new System.Drawing.Size(808, 17);
			this._toolStripStatusLabelFiller.Spring = true;
			// 
			// _toolStripSplitButtonClose
			// 
			this._toolStripSplitButtonClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripSplitButtonClose.DropDownButtonWidth = 0;
			this._toolStripSplitButtonClose.Image = global::ProSuite.UI.Core.Properties.Resources.Exit;
			this._toolStripSplitButtonClose.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripSplitButtonClose.Name = "_toolStripSplitButtonClose";
			this._toolStripSplitButtonClose.Size = new System.Drawing.Size(57, 20);
			this._toolStripSplitButtonClose.Text = "Close";
			this._toolStripSplitButtonClose.ButtonClick += new System.EventHandler(this._toolStripSplitButtonClose_ButtonClick);
			// 
			// QAVerificationForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(911, 675);
			this.Controls.Add(this._textBoxWarningCount);
			this.Controls.Add(this._labelSoftErrors);
			this.Controls.Add(this._textBoxErrorCount);
			this.Controls.Add(this._labelHardErrors);
			this.Controls.Add(this._textBoxVerificationStatus);
			this.Controls.Add(this._splitContainerDetail);
			this.Controls.Add(this._textBoxIssueCount);
			this.Controls.Add(this._labelTotalErrors);
			this.Controls.Add(this._labelTotalTime);
			this.Controls.Add(this._textBoxTotalTime);
			this.Controls.Add(this._textBoxCPUTime);
			this.Controls.Add(this._labelProcessorTime);
			this.Controls.Add(this._labelEnded);
			this.Controls.Add(this._textBoxEndDate);
			this.Controls.Add(this._textBoxStartDate);
			this.Controls.Add(this._labelStartDate);
			this.Controls.Add(this._labelOperator);
			this.Controls.Add(this._textBoxUser);
			this.Controls.Add(this._textBoxContext);
			this.Controls.Add(this._labelContext);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxSpecification);
			this.Controls.Add(this._labelSpecification);
			this.Controls.Add(this._statusStrip);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(650, 472);
			this.Name = "QAVerificationForm";
			this.ShowInTaskbar = false;
			this.Text = "Quality Verification Results";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.QAVerificationForm_FormClosed);
			this.Load += new System.EventHandler(this.QAVerificationForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.QAVerificationForm_KeyDown);
			this._groupBoxCondition.ResumeLayout(false);
			this._groupBoxConditions.ResumeLayout(false);
			this._panel1.ResumeLayout(false);
			this._panel1.PerformLayout();
			this._toolStrip.ResumeLayout(false);
			this._toolStrip.PerformLayout();
			this._splitContainerDetail.Panel1.ResumeLayout(false);
			this._splitContainerDetail.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerDetail)).EndInit();
			this._splitContainerDetail.ResumeLayout(false);
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelSpecification;
        private System.Windows.Forms.TextBox _textBoxSpecification;
        private System.Windows.Forms.GroupBox _groupBoxCondition;
		private System.Windows.Forms.GroupBox _groupBoxConditions;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelContext;
        private System.Windows.Forms.TextBox _textBoxContext;
        private System.Windows.Forms.TextBox _textBoxUser;
        private System.Windows.Forms.Label _labelOperator;
        private System.Windows.Forms.Label _labelStartDate;
        private System.Windows.Forms.TextBox _textBoxStartDate;
        private System.Windows.Forms.TextBox _textBoxEndDate;
        private System.Windows.Forms.Label _labelEnded;
        private System.Windows.Forms.Label _labelProcessorTime;
        private System.Windows.Forms.TextBox _textBoxCPUTime;
        private System.Windows.Forms.TextBox _textBoxTotalTime;
		private System.Windows.Forms.Label _labelTotalTime;
        private System.Windows.Forms.TextBox _textBoxIssueCount;
        private System.Windows.Forms.Label _labelTotalErrors;
        private QualityConditionVerificationControl _qualityConditionVerificationControl;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerDetail;
		private System.Windows.Forms.TextBox _textBoxVerificationStatus;
        private System.Windows.Forms.TextBox _textBoxErrorCount;
        private System.Windows.Forms.Label _labelHardErrors;
        private System.Windows.Forms.TextBox _textBoxWarningCount;
        private System.Windows.Forms.Label _labelSoftErrors;
		private System.Windows.Forms.ToolTip _toolTip;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.Panel _panel1;
		private VerifiedConditionsControl _verifiedConditionsControl;
		private VerifiedConditionsHierarchyControl _verifiedConditionsHierarchyControl;
		private ToolStripEx _toolStrip;
		private System.Windows.Forms.ToolStripLabel _toolStripLabel1;
		private System.Windows.Forms.ToolStripComboBox _toolStripComboBoxView;
		private System.Windows.Forms.ToolStripButton _toolStripButtonNoIssues;
		private System.Windows.Forms.ToolStripButton _toolStripButtonWarnings;
		private System.Windows.Forms.ToolStripButton _toolStripButtonErrors;
		private System.Windows.Forms.ToolStripSeparator _toolStripSeparator1;
		private VerifiedDatasetsControl _verifiedDatasetsControl;
		private System.Windows.Forms.ToolStripSplitButton _toolStripSplitButtonClose;
		private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabelFiller;
    }
}
