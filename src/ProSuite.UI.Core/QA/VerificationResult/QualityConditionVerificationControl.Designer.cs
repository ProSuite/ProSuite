namespace ProSuite.UI.Core.QA.VerificationResult
{
    partial class QualityConditionVerificationControl
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
			this._splitContainer = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._qualityConditionControl = new ProSuite.UI.Core.QA.Controls.QualityConditionControl();
			this.tabControlResults = new System.Windows.Forms.TabControl();
			this.tabPageVerification = new System.Windows.Forms.TabPage();
			this._textBoxStopCondition = new System.Windows.Forms.TextBox();
			this.labelStopCondition = new System.Windows.Forms.Label();
			this._textBoxIssueType = new System.Windows.Forms.TextBox();
			this.labelErrorsAllowed = new System.Windows.Forms.Label();
			this._labelNumberOfErrors = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxIssueCount = new System.Windows.Forms.TextBox();
			this.tabPageParams = new System.Windows.Forms.TabPage();
			this._qualityConditionTableViewControl = new ProSuite.UI.Core.QA.Controls.QualityConditionTableViewControl();
			this._tabPageTestDescriptor = new System.Windows.Forms.TabPage();
			this._testDescriptorControl = new ProSuite.UI.Core.QA.Controls.TestDescriptorControl();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			this.tabControlResults.SuspendLayout();
			this.tabPageVerification.SuspendLayout();
			this.tabPageParams.SuspendLayout();
			this._tabPageTestDescriptor.SuspendLayout();
			this.SuspendLayout();
			// 
			// _splitContainer
			// 
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.Location = new System.Drawing.Point(0, 0);
			this._splitContainer.Name = "_splitContainer";
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._qualityConditionControl);
			this._splitContainer.Panel1MinSize = 50;
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.Controls.Add(this.tabControlResults);
			this._splitContainer.Panel2MinSize = 50;
			this._splitContainer.Size = new System.Drawing.Size(600, 204);
			this._splitContainer.SplitterDistance = 252;
			this._splitContainer.TabIndex = 2;
			// 
			// _qualityConditionControl
			// 
			this._qualityConditionControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionControl.Location = new System.Drawing.Point(0, 0);
			this._qualityConditionControl.Name = "_qualityConditionControl";
			this._qualityConditionControl.QualityCondition = null;
			this._qualityConditionControl.ReadOnly = true;
			this._qualityConditionControl.Size = new System.Drawing.Size(252, 204);
			this._qualityConditionControl.TabIndex = 0;
			// 
			// tabControlResults
			// 
			this.tabControlResults.Controls.Add(this.tabPageVerification);
			this.tabControlResults.Controls.Add(this.tabPageParams);
			this.tabControlResults.Controls.Add(this._tabPageTestDescriptor);
			this.tabControlResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControlResults.Location = new System.Drawing.Point(0, 0);
			this.tabControlResults.Name = "tabControlResults";
			this.tabControlResults.SelectedIndex = 0;
			this.tabControlResults.Size = new System.Drawing.Size(344, 204);
			this.tabControlResults.TabIndex = 1;
			// 
			// tabPageVerification
			// 
			this.tabPageVerification.Controls.Add(this._textBoxStopCondition);
			this.tabPageVerification.Controls.Add(this.labelStopCondition);
			this.tabPageVerification.Controls.Add(this._textBoxIssueType);
			this.tabPageVerification.Controls.Add(this.labelErrorsAllowed);
			this.tabPageVerification.Controls.Add(this._labelNumberOfErrors);
			this.tabPageVerification.Controls.Add(this.label1);
			this.tabPageVerification.Controls.Add(this._textBoxIssueCount);
			this.tabPageVerification.Location = new System.Drawing.Point(4, 22);
			this.tabPageVerification.Name = "tabPageVerification";
			this.tabPageVerification.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageVerification.Size = new System.Drawing.Size(336, 178);
			this.tabPageVerification.TabIndex = 0;
			this.tabPageVerification.Text = "Verification";
			this.tabPageVerification.UseVisualStyleBackColor = true;
			// 
			// _textBoxStopCondition
			// 
			this._textBoxStopCondition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxStopCondition.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxStopCondition.Location = new System.Drawing.Point(99, 88);
			this._textBoxStopCondition.Name = "_textBoxStopCondition";
			this._textBoxStopCondition.ReadOnly = true;
			this._textBoxStopCondition.Size = new System.Drawing.Size(218, 20);
			this._textBoxStopCondition.TabIndex = 6;
			// 
			// labelStopCondition
			// 
			this.labelStopCondition.AutoSize = true;
			this.labelStopCondition.Location = new System.Drawing.Point(15, 91);
			this.labelStopCondition.Name = "labelStopCondition";
			this.labelStopCondition.Size = new System.Drawing.Size(78, 13);
			this.labelStopCondition.TabIndex = 5;
			this.labelStopCondition.Text = "Stop condition:";
			// 
			// _textBoxIssueType
			// 
			this._textBoxIssueType.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxIssueType.Location = new System.Drawing.Point(99, 62);
			this._textBoxIssueType.Name = "_textBoxIssueType";
			this._textBoxIssueType.ReadOnly = true;
			this._textBoxIssueType.Size = new System.Drawing.Size(68, 20);
			this._textBoxIssueType.TabIndex = 4;
			// 
			// labelErrorsAllowed
			// 
			this.labelErrorsAllowed.AutoSize = true;
			this.labelErrorsAllowed.Location = new System.Drawing.Point(35, 65);
			this.labelErrorsAllowed.Name = "labelErrorsAllowed";
			this.labelErrorsAllowed.Size = new System.Drawing.Size(58, 13);
			this.labelErrorsAllowed.TabIndex = 3;
			this.labelErrorsAllowed.Text = "Issue type:";
			// 
			// _labelNumberOfErrors
			// 
			this._labelNumberOfErrors.AutoSize = true;
			this._labelNumberOfErrors.Location = new System.Drawing.Point(23, 39);
			this._labelNumberOfErrors.Name = "_labelNumberOfErrors";
			this._labelNumberOfErrors.Size = new System.Drawing.Size(70, 13);
			this._labelNumberOfErrors.TabIndex = 1;
			this._labelNumberOfErrors.Text = "Issues count:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(6, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(117, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Verification Results";
			// 
			// _textBoxIssueCount
			// 
			this._textBoxIssueCount.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxIssueCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxIssueCount.Location = new System.Drawing.Point(99, 36);
			this._textBoxIssueCount.Name = "_textBoxIssueCount";
			this._textBoxIssueCount.ReadOnly = true;
			this._textBoxIssueCount.Size = new System.Drawing.Size(68, 20);
			this._textBoxIssueCount.TabIndex = 2;
			// 
			// tabPageParams
			// 
			this.tabPageParams.Controls.Add(this._qualityConditionTableViewControl);
			this.tabPageParams.Location = new System.Drawing.Point(4, 22);
			this.tabPageParams.Name = "tabPageParams";
			this.tabPageParams.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageParams.Size = new System.Drawing.Size(336, 178);
			this.tabPageParams.TabIndex = 1;
			this.tabPageParams.Text = "Parameters";
			this.tabPageParams.UseVisualStyleBackColor = true;
			// 
			// _qualityConditionTableViewControl
			// 
			this._qualityConditionTableViewControl.AutoSize = true;
			this._qualityConditionTableViewControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionTableViewControl.Location = new System.Drawing.Point(3, 3);
			this._qualityConditionTableViewControl.Name = "_qualityConditionTableViewControl";
			this._qualityConditionTableViewControl.Size = new System.Drawing.Size(330, 172);
			this._qualityConditionTableViewControl.TabIndex = 1;
			// 
			// _tabPageTestDescriptor
			// 
			this._tabPageTestDescriptor.Controls.Add(this._testDescriptorControl);
			this._tabPageTestDescriptor.Location = new System.Drawing.Point(4, 22);
			this._tabPageTestDescriptor.Name = "_tabPageTestDescriptor";
			this._tabPageTestDescriptor.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageTestDescriptor.Size = new System.Drawing.Size(336, 178);
			this._tabPageTestDescriptor.TabIndex = 2;
			this._tabPageTestDescriptor.Text = "Test";
			this._tabPageTestDescriptor.UseVisualStyleBackColor = true;
			// 
			// _testDescriptorControl
			// 
			this._testDescriptorControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._testDescriptorControl.Location = new System.Drawing.Point(3, 3);
			this._testDescriptorControl.Name = "_testDescriptorControl";
			this._testDescriptorControl.Size = new System.Drawing.Size(330, 172);
			this._testDescriptorControl.TabIndex = 0;
			this._testDescriptorControl.TestDescriptor = null;
			// 
			// QualityConditionVerificationControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._splitContainer);
			this.MinimumSize = new System.Drawing.Size(520, 130);
			this.Name = "QualityConditionVerificationControl";
			this.Size = new System.Drawing.Size(600, 204);
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			this.tabControlResults.ResumeLayout(false);
			this.tabPageVerification.ResumeLayout(false);
			this.tabPageVerification.PerformLayout();
			this.tabPageParams.ResumeLayout(false);
			this.tabPageParams.PerformLayout();
			this._tabPageTestDescriptor.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlResults;
        private System.Windows.Forms.TabPage tabPageVerification;
        private System.Windows.Forms.TabPage tabPageParams;
        private System.Windows.Forms.TextBox _textBoxIssueCount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _textBoxIssueType;
        private System.Windows.Forms.Label labelErrorsAllowed;
        private System.Windows.Forms.Label _labelNumberOfErrors;
        private System.Windows.Forms.TextBox _textBoxStopCondition;
        private System.Windows.Forms.Label labelStopCondition;
        private ProSuite.UI.Core.QA.Controls.QualityConditionControl _qualityConditionControl;
    	private ProSuite.UI.Core.QA.Controls.TestDescriptorControl _testDescriptorControl;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private System.Windows.Forms.TabPage _tabPageTestDescriptor;
		private ProSuite.UI.Core.QA.Controls.QualityConditionTableViewControl _qualityConditionTableViewControl;
	}
}
