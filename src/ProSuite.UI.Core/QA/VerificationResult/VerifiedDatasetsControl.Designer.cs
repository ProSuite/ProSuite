namespace ProSuite.UI.Core.QA.VerificationResult
{
	partial class VerifiedDatasetsControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this._dataGridView = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewImageColumn2 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewImageColumn3 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTimeColumn1 = new VerifiedDatasetsControl.DataGridViewTimeColumn();
			this._columnStatusImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnTestName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDatasetType = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnDatasetName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDataLoadTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnExecuteTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnFullTime = new VerifiedDatasetsControl.DataGridViewTimeColumn();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnStatusImage,
            this._columnImage,
            this._columnTestName,
            this._columnTestType,
            this._columnDatasetType,
            this._columnDatasetName,
            this._columnDataLoadTime,
            this._columnExecuteTime,
            this._columnFullTime});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(0, 0);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(867, 478);
			this._dataGridView.TabIndex = 13;
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
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(0, 478);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(867, 25);
			this._dataGridViewFindToolStrip.TabIndex = 14;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewImageColumn1.DataPropertyName = "Status";
			this.dataGridViewImageColumn1.HeaderText = "";
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			this.dataGridViewImageColumn1.ReadOnly = true;
			this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn1.ToolTipText = "Verification SelectedDateTime";
			this.dataGridViewImageColumn1.Width = 20;
			// 
			// dataGridViewImageColumn2
			// 
			this.dataGridViewImageColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewImageColumn2.DataPropertyName = "Type";
			this.dataGridViewImageColumn2.HeaderText = "";
			this.dataGridViewImageColumn2.Name = "dataGridViewImageColumn2";
			this.dataGridViewImageColumn2.ReadOnly = true;
			this.dataGridViewImageColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewImageColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn2.ToolTipText = "Quality Condition Criticality";
			this.dataGridViewImageColumn2.Width = 20;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn1.DataPropertyName = "TestName";
			this.dataGridViewTextBoxColumn1.HeaderText = "Quality Condition";
			this.dataGridViewTextBoxColumn1.MinimumWidth = 110;
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			this.dataGridViewTextBoxColumn1.ToolTipText = "Quality Condition";
			this.dataGridViewTextBoxColumn1.Width = 110;
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "TestType";
			this.dataGridViewTextBoxColumn2.HeaderText = "Test Type";
			this.dataGridViewTextBoxColumn2.MinimumWidth = 90;
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			this.dataGridViewTextBoxColumn2.ToolTipText = "Test Type";
			this.dataGridViewTextBoxColumn2.Width = 90;
			// 
			// dataGridViewImageColumn3
			// 
			this.dataGridViewImageColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewImageColumn3.DataPropertyName = "DatasetType";
			this.dataGridViewImageColumn3.HeaderText = "";
			this.dataGridViewImageColumn3.Name = "dataGridViewImageColumn3";
			this.dataGridViewImageColumn3.ReadOnly = true;
			this.dataGridViewImageColumn3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridViewImageColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn3.Width = 20;
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.dataGridViewTextBoxColumn3.DataPropertyName = "DatasetName";
			this.dataGridViewTextBoxColumn3.HeaderText = "Dataset ";
			this.dataGridViewTextBoxColumn3.MinimumWidth = 50;
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			this.dataGridViewTextBoxColumn3.ToolTipText = "Involved Datasets";
			this.dataGridViewTextBoxColumn3.Width = 170;
			// 
			// dataGridViewTextBoxColumn4
			// 
			this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn4.DataPropertyName = "DataLoadTime";
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle3.Format = "N2";
			this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle3;
			this.dataGridViewTextBoxColumn4.HeaderText = "Load [s]";
			this.dataGridViewTextBoxColumn4.MinimumWidth = 70;
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			this.dataGridViewTextBoxColumn4.ToolTipText = "Load Time [s]";
			this.dataGridViewTextBoxColumn4.Width = 70;
			// 
			// dataGridViewTextBoxColumn5
			// 
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn5.DataPropertyName = "ExecuteTime";
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle4.Format = "N2";
			this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle4;
			this.dataGridViewTextBoxColumn5.HeaderText = "Exec [s]";
			this.dataGridViewTextBoxColumn5.MinimumWidth = 70;
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
			this.dataGridViewTextBoxColumn5.ToolTipText = "Execution Time [s]";
			this.dataGridViewTextBoxColumn5.Width = 70;
			// 
			// dataGridViewTimeColumn1
			// 
			this.dataGridViewTimeColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTimeColumn1.DataPropertyName = "RowExecuteTime";
			this.dataGridViewTimeColumn1.ExecColor = System.Drawing.Color.Empty;
			this.dataGridViewTimeColumn1.HeaderText = "Total Time";
			this.dataGridViewTimeColumn1.LoadColor = System.Drawing.Color.Empty;
			this.dataGridViewTimeColumn1.Margin = 5;
			this.dataGridViewTimeColumn1.MaximumTime = 0D;
			this.dataGridViewTimeColumn1.Name = "dataGridViewTimeColumn1";
			this.dataGridViewTimeColumn1.ReadOnly = true;
			this.dataGridViewTimeColumn1.ToolTipText = "Load + Execution Time";
			// 
			// _columnStatusImage
			// 
			this._columnStatusImage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnStatusImage.DataPropertyName = "Status";
			this._columnStatusImage.HeaderText = "";
			this._columnStatusImage.Name = "_columnStatusImage";
			this._columnStatusImage.ReadOnly = true;
			this._columnStatusImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnStatusImage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnStatusImage.ToolTipText = "Verification SelectedDateTime";
			this._columnStatusImage.Width = 20;
			// 
			// _columnImage
			// 
			this._columnImage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnImage.DataPropertyName = "Type";
			this._columnImage.HeaderText = "";
			this._columnImage.Name = "_columnImage";
			this._columnImage.ReadOnly = true;
			this._columnImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnImage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnImage.ToolTipText = "Quality Condition Criticality";
			this._columnImage.Visible = false;
			this._columnImage.Width = 20;
			// 
			// _columnTestName
			// 
			this._columnTestName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestName.DataPropertyName = "TestName";
			this._columnTestName.HeaderText = "Quality Condition";
			this._columnTestName.MinimumWidth = 110;
			this._columnTestName.Name = "_columnTestName";
			this._columnTestName.ReadOnly = true;
			this._columnTestName.ToolTipText = "Quality Condition";
			this._columnTestName.Width = 110;
			// 
			// _columnTestType
			// 
			this._columnTestType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestType.DataPropertyName = "TestType";
			this._columnTestType.HeaderText = "Test Type";
			this._columnTestType.MinimumWidth = 90;
			this._columnTestType.Name = "_columnTestType";
			this._columnTestType.ReadOnly = true;
			this._columnTestType.ToolTipText = "Test Type";
			this._columnTestType.Width = 90;
			// 
			// _columnDatasetType
			// 
			this._columnDatasetType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnDatasetType.DataPropertyName = "DatasetType";
			this._columnDatasetType.HeaderText = "";
			this._columnDatasetType.Name = "_columnDatasetType";
			this._columnDatasetType.ReadOnly = true;
			this._columnDatasetType.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnDatasetType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnDatasetType.Width = 20;
			// 
			// _columnDatasetName
			// 
			this._columnDatasetName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnDatasetName.DataPropertyName = "DatasetName";
			this._columnDatasetName.HeaderText = "Dataset ";
			this._columnDatasetName.MinimumWidth = 50;
			this._columnDatasetName.Name = "_columnDatasetName";
			this._columnDatasetName.ReadOnly = true;
			this._columnDatasetName.ToolTipText = "Involved Datasets";
			this._columnDatasetName.Width = 170;
			// 
			// _columnDataLoadTime
			// 
			this._columnDataLoadTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnDataLoadTime.DataPropertyName = "DataLoadTime";
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle1.Format = "N2";
			this._columnDataLoadTime.DefaultCellStyle = dataGridViewCellStyle1;
			this._columnDataLoadTime.HeaderText = "Load [s]";
			this._columnDataLoadTime.MinimumWidth = 70;
			this._columnDataLoadTime.Name = "_columnDataLoadTime";
			this._columnDataLoadTime.ReadOnly = true;
			this._columnDataLoadTime.ToolTipText = "Load Time [s]";
			this._columnDataLoadTime.Width = 70;
			// 
			// _columnExecuteTime
			// 
			this._columnExecuteTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnExecuteTime.DataPropertyName = "ExecuteTime";
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle2.Format = "N2";
			this._columnExecuteTime.DefaultCellStyle = dataGridViewCellStyle2;
			this._columnExecuteTime.HeaderText = "Exec [s]";
			this._columnExecuteTime.MinimumWidth = 70;
			this._columnExecuteTime.Name = "_columnExecuteTime";
			this._columnExecuteTime.ReadOnly = true;
			this._columnExecuteTime.ToolTipText = "Execution Time [s]";
			this._columnExecuteTime.Width = 70;
			// 
			// _columnFullTime
			// 
			this._columnFullTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnFullTime.DataPropertyName = "RowExecuteTime";
			this._columnFullTime.ExecColor = System.Drawing.Color.Empty;
			this._columnFullTime.HeaderText = "Total Time";
			this._columnFullTime.LoadColor = System.Drawing.Color.Empty;
			this._columnFullTime.Margin = 5;
			this._columnFullTime.MaximumTime = 0D;
			this._columnFullTime.Name = "_columnFullTime";
			this._columnFullTime.ReadOnly = true;
			this._columnFullTime.ToolTipText = "Load + Execution Time";
			// 
			// VerifiedDatasetsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridView);
			this.Controls.Add(this._dataGridViewFindToolStrip);
			this.Name = "VerifiedDatasetsControl";
			this.Size = new System.Drawing.Size(867, 503);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridView;
		private global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.DataGridViewImageColumn _columnStatusImage;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestType;
		private System.Windows.Forms.DataGridViewImageColumn _columnDatasetType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDatasetName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDataLoadTime;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnExecuteTime;
		private VerifiedDatasetsControl.DataGridViewTimeColumn _columnFullTime;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private VerifiedDatasetsControl.DataGridViewTimeColumn dataGridViewTimeColumn1;
	}
}
