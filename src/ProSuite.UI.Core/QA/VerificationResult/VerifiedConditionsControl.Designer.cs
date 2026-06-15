namespace ProSuite.UI.Core.QA.VerificationResult
{
	partial class VerifiedConditionsControl
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
			this._dataGridView = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewIssueCountColumn1 = new VerifiedConditionsControl.VerifiedConditionItem.DataGridViewIssueCountColumn();
			this._columnStatus = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestDescriptor = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestCategories = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDatasetNames = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnIssueCountBar = new VerifiedConditionsControl.VerifiedConditionItem.DataGridViewIssueCountColumn();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnStatus,
            this._columnName,
            this._columnCategory,
            this._columnTestDescriptor,
            this._columnTestCategories,
            this._columnDatasetNames,
            this._columnIssueCount,
            this._columnIssueCountBar});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(0, 0);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(964, 407);
			this._dataGridView.TabIndex = 0;
			this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(0, 407);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(964, 25);
			this._dataGridViewFindToolStrip.TabIndex = 1;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			// 
			// dataGridViewIssueCountColumn1
			// 
			this.dataGridViewIssueCountColumn1.ErrorColor = System.Drawing.Color.Red;
			this.dataGridViewIssueCountColumn1.Margin = 5;
			this.dataGridViewIssueCountColumn1.MaximumIssueCount = 0;
			this.dataGridViewIssueCountColumn1.Name = "dataGridViewIssueCountColumn1";
			this.dataGridViewIssueCountColumn1.WarningColor = System.Drawing.Color.Yellow;
			// 
			// _columnStatus
			// 
			this._columnStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnStatus.DataPropertyName = "StatusImage";
			this._columnStatus.HeaderText = "";
			this._columnStatus.MinimumWidth = 20;
			this._columnStatus.Name = "_columnStatus";
			this._columnStatus.ReadOnly = true;
			this._columnStatus.Width = 20;
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnName.DataPropertyName = "Name";
			this._columnName.HeaderText = "Quality Condition";
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			// 
			// _columnCategory
			// 
			this._columnCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnCategory.DataPropertyName = "Category";
			this._columnCategory.FillWeight = 50F;
			this._columnCategory.HeaderText = "Category";
			this._columnCategory.MinimumWidth = 50;
			this._columnCategory.Name = "_columnCategory";
			this._columnCategory.ReadOnly = true;
			// 
			// _columnTestDescriptor
			// 
			this._columnTestDescriptor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestDescriptor.DataPropertyName = "TestDescriptorName";
			this._columnTestDescriptor.HeaderText = "Test Type";
			this._columnTestDescriptor.MinimumWidth = 100;
			this._columnTestDescriptor.Name = "_columnTestDescriptor";
			this._columnTestDescriptor.ReadOnly = true;
			// 
			// _columnTestCategories
			// 
			this._columnTestCategories.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnTestCategories.DataPropertyName = "TestCategories";
			this._columnTestCategories.FillWeight = 70F;
			this._columnTestCategories.HeaderText = "Test Categories";
			this._columnTestCategories.MinimumWidth = 150;
			this._columnTestCategories.Name = "_columnTestCategories";
			this._columnTestCategories.ReadOnly = true;
			// 
			// _columnDatasetNames
			// 
			this._columnDatasetNames.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDatasetNames.DataPropertyName = "DatasetNames";
			this._columnDatasetNames.FillWeight = 50F;
			this._columnDatasetNames.HeaderText = "Datasets";
			this._columnDatasetNames.Name = "_columnDatasetNames";
			this._columnDatasetNames.ReadOnly = true;
			// 
			// _columnIssueCount
			// 
			this._columnIssueCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnIssueCount.DataPropertyName = "IssueCount";
			this._columnIssueCount.HeaderText = "Issues";
			this._columnIssueCount.MinimumWidth = 40;
			this._columnIssueCount.Name = "_columnIssueCount";
			this._columnIssueCount.ReadOnly = true;
			this._columnIssueCount.Width = 62;
			// 
			// _columnIssueCountBar
			// 
			this._columnIssueCountBar.DataPropertyName = "IssueCount";
			this._columnIssueCountBar.ErrorColor = System.Drawing.Color.Red;
			this._columnIssueCountBar.HeaderText = "";
			this._columnIssueCountBar.Margin = 5;
			this._columnIssueCountBar.MaximumIssueCount = 0;
			this._columnIssueCountBar.MinimumWidth = 50;
			this._columnIssueCountBar.Name = "_columnIssueCountBar";
			this._columnIssueCountBar.ReadOnly = true;
			this._columnIssueCountBar.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnIssueCountBar.WarningColor = System.Drawing.Color.Yellow;
			this._columnIssueCountBar.Width = 155;
			// 
			// VerifiedConditionsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridView);
			this.Controls.Add(this._dataGridViewFindToolStrip);
			this.Name = "VerifiedConditionsControl";
			this.Size = new System.Drawing.Size(964, 432);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridView;
		private global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private VerifiedConditionsControl.VerifiedConditionItem.DataGridViewIssueCountColumn dataGridViewIssueCountColumn1;
		private System.Windows.Forms.DataGridViewImageColumn _columnStatus;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestDescriptor;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestCategories;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDatasetNames;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnIssueCount;
		private VerifiedConditionsControl.VerifiedConditionItem.DataGridViewIssueCountColumn _columnIssueCountBar;

	}
}
