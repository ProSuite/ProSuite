using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.Customize
{
	partial class TestParameterValuesEditorForm
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
			global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
			this._qualityConditionParams = new QualityConditionParametersControl();
			this._qualityConditionParams.TestConfigurationCreator = TestConfigurationCreator;
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._tabControl = new System.Windows.Forms.TabControl();
			this._tabPageQualityCondition = new System.Windows.Forms.TabPage();
			this._tabPageTest = new System.Windows.Forms.TabPage();
			this._qualityConditionControl = new QualityConditionControl();
			this._testDescriptorControl = new TestDescriptorControl();
			_splitContainer = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			((System.ComponentModel.ISupportInitialize)(_splitContainer)).BeginInit();
			_splitContainer.Panel1.SuspendLayout();
			_splitContainer.Panel2.SuspendLayout();
			_splitContainer.SuspendLayout();
			this._tabControl.SuspendLayout();
			this._tabPageQualityCondition.SuspendLayout();
			this._tabPageTest.SuspendLayout();
			this.SuspendLayout();
			// 
			// _splitContainer
			// 
			_splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			_splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			_splitContainer.IsSplitterFixed = true;
			_splitContainer.Location = new System.Drawing.Point(12, 12);
			_splitContainer.Name = "_splitContainer";
			_splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			_splitContainer.Panel1.Controls.Add(this._tabControl);
			// 
			// _splitContainer.Panel2
			// 
			_splitContainer.Panel2.Controls.Add(this._qualityConditionParams);
			_splitContainer.Size = new System.Drawing.Size(632, 486);
			_splitContainer.SplitterDistance = 200;
			_splitContainer.TabIndex = 5;
			// 
			// _qualityConditionParams
			// 
			this._qualityConditionParams.AutoSyncQualityCondition = false;
			this._qualityConditionParams.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionParams.Location = new System.Drawing.Point(0, 0);
			this._qualityConditionParams.Name = "_qualityConditionParams";
			this._qualityConditionParams.QualityCondition = null;
			this._qualityConditionParams.ReadOnly = false;
			this._qualityConditionParams.Size = new System.Drawing.Size(632, 282);
			this._qualityConditionParams.TabIndex = 3;
			this._qualityConditionParams.TestParameterDatasetProvider = null;
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(569, 504);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 4;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(488, 504);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 3;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _tabControl
			// 
			this._tabControl.Controls.Add(this._tabPageQualityCondition);
			this._tabControl.Controls.Add(this._tabPageTest);
			this._tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tabControl.Location = new System.Drawing.Point(0, 0);
			this._tabControl.Name = "_tabControl";
			this._tabControl.SelectedIndex = 0;
			this._tabControl.Size = new System.Drawing.Size(632, 200);
			this._tabControl.TabIndex = 2;
			// 
			// _tabPageQualityCondition
			// 
			this._tabPageQualityCondition.Controls.Add(this._qualityConditionControl);
			this._tabPageQualityCondition.Location = new System.Drawing.Point(4, 22);
			this._tabPageQualityCondition.Name = "_tabPageQualityCondition";
			this._tabPageQualityCondition.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageQualityCondition.Size = new System.Drawing.Size(624, 174);
			this._tabPageQualityCondition.TabIndex = 0;
			this._tabPageQualityCondition.Text = "Quality condition";
			this._tabPageQualityCondition.UseVisualStyleBackColor = true;
			// 
			// _tabPageTest
			// 
			this._tabPageTest.Controls.Add(this._testDescriptorControl);
			this._tabPageTest.Location = new System.Drawing.Point(4, 22);
			this._tabPageTest.Name = "_tabPageTest";
			this._tabPageTest.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageTest.Size = new System.Drawing.Size(580, 174);
			this._tabPageTest.TabIndex = 1;
			this._tabPageTest.Text = "Test";
			this._tabPageTest.UseVisualStyleBackColor = true;
			// 
			// _qualityConditionControl
			// 
			this._qualityConditionControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._qualityConditionControl.Location = new System.Drawing.Point(3, 3);
			this._qualityConditionControl.Name = "_qualityConditionControl";
			this._qualityConditionControl.QualityCondition = null;
			this._qualityConditionControl.ReadOnly = true;
			this._qualityConditionControl.Size = new System.Drawing.Size(618, 168);
			this._qualityConditionControl.TabIndex = 3;
			// 
			// _testDescriptorControl
			// 
			this._testDescriptorControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._testDescriptorControl.Location = new System.Drawing.Point(3, 3);
			this._testDescriptorControl.Name = "_testDescriptorControl";
			this._testDescriptorControl.Size = new System.Drawing.Size(574, 168);
			this._testDescriptorControl.TabIndex = 2;
			this._testDescriptorControl.TestDescriptor = null;
			// 
			// TestParameterValuesEditorForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(656, 539);
			this.Controls.Add(_splitContainer);
			this.Controls.Add(this._buttonCancel);
			this.Controls.Add(this._buttonOK);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(240, 420);
			this.Name = "TestParameterValuesEditorForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Customize Parameter Values";
			_splitContainer.Panel1.ResumeLayout(false);
			_splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(_splitContainer)).EndInit();
			_splitContainer.ResumeLayout(false);
			this._tabControl.ResumeLayout(false);
			this._tabPageQualityCondition.ResumeLayout(false);
			this._tabPageTest.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private QualityConditionParametersControl _qualityConditionParams;
		private System.Windows.Forms.TabControl _tabControl;
		private System.Windows.Forms.TabPage _tabPageQualityCondition;
		private QualityConditionControl _qualityConditionControl;
		private System.Windows.Forms.TabPage _tabPageTest;
		private TestDescriptorControl _testDescriptorControl;
	}
}
