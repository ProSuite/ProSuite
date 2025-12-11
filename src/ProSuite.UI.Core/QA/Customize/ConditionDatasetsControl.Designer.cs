namespace ProSuite.UI.Core.QA.Customize
{
	partial class ConditionDatasetsControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			this._dataGridViewAllConditions = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._dataGridViewFindToolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this._columnEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this._columnQualityConditionType = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnQualityCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDatasetType = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnDataset = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewAllConditions)).BeginInit();
			this.SuspendLayout();
			// 
			// _dataGridViewAllConditions
			// 
			this._dataGridViewAllConditions.AllowUserToAddRows = false;
			this._dataGridViewAllConditions.AllowUserToDeleteRows = false;
			this._dataGridViewAllConditions.AllowUserToResizeRows = false;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridViewAllConditions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridViewAllConditions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewAllConditions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnEnabled,
            this._columnQualityConditionType,
            this._columnQualityCondition,
            this._columnTestType,
            this._columnDatasetType,
            this._columnDataset});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewAllConditions.DefaultCellStyle = dataGridViewCellStyle2;
			this._dataGridViewAllConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewAllConditions.Location = new System.Drawing.Point(0, 0);
			this._dataGridViewAllConditions.Name = "_dataGridViewAllConditions";
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridViewAllConditions.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this._dataGridViewAllConditions.RowHeadersVisible = false;
			this._dataGridViewAllConditions.RowHeadersWidth = 20;
			this._dataGridViewAllConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewAllConditions.Size = new System.Drawing.Size(678, 394);
			this._dataGridViewAllConditions.MinimumSize=new System.Drawing.Size(20, 60);
			this._dataGridViewAllConditions.TabIndex = 9;
			this._dataGridViewAllConditions.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridViewAllConditions_CellFormatting);
			this._dataGridViewAllConditions.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this._dataGridViewAllConditions_CellMouseUp);
			this._dataGridViewAllConditions.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewAllConditions_CellValueChanged);
			this._dataGridViewAllConditions.SelectionChanged += new System.EventHandler(this._dataGridViewAllConditions_SelectionChanged);
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(0, 394);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(678, 25);
			this._dataGridViewFindToolStrip.TabIndex = 10;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
			// 
			// _columnEnabled
			// 
			this._columnEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
			this._columnEnabled.DataPropertyName = "Enabled";
			this._columnEnabled.HeaderText = "";
			this._columnEnabled.MinimumWidth = 25;
			this._columnEnabled.Name = "_columnEnabled";
			this._columnEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnEnabled.Width = 25;
			// 
			// _columnQualityConditionType
			// 
			this._columnQualityConditionType.DataPropertyName = "Type";
			this._columnQualityConditionType.HeaderText = "";
			this._columnQualityConditionType.MinimumWidth = 20;
			this._columnQualityConditionType.Name = "_columnQualityConditionType";
			this._columnQualityConditionType.ReadOnly = true;
			this._columnQualityConditionType.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnQualityConditionType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnQualityConditionType.Width = 20;
			// 
			// _columnQualityCondition
			// 
			this._columnQualityCondition.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this._columnQualityCondition.DataPropertyName = "TestName";
			this._columnQualityCondition.HeaderText = "Quality Condition";
			this._columnQualityCondition.MinimumWidth = 100;
			this._columnQualityCondition.Name = "_columnQualityCondition";
			this._columnQualityCondition.ReadOnly = true;
			this._columnQualityCondition.Width = 400;
			// 
			// _columnTestType
			// 
			this._columnTestType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestType.DataPropertyName = "TestType";
			this._columnTestType.HeaderText = "Test Type";
			this._columnTestType.Name = "_columnTestType";
			this._columnTestType.ReadOnly = true;
			this._columnTestType.Width = 80;
			// 
			// _columnDatasetType
			// 
			this._columnDatasetType.DataPropertyName = "DatasetType";
			this._columnDatasetType.HeaderText = "";
			this._columnDatasetType.MinimumWidth = 20;
			this._columnDatasetType.Name = "_columnDatasetType";
			this._columnDatasetType.ReadOnly = true;
			this._columnDatasetType.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnDatasetType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnDatasetType.Width = 20;
			// 
			// _columnDataset
			// 
			this._columnDataset.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDataset.DataPropertyName = "DatasetName";
			this._columnDataset.HeaderText = "Dataset";
			this._columnDataset.MinimumWidth = 50;
			this._columnDataset.Name = "_columnDataset";
			this._columnDataset.ReadOnly = true;
			// 
			// ConditionDatasetsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridViewAllConditions);
			this.Controls.Add(this._dataGridViewFindToolStrip);
			this.Name = "ConditionDatasetsControl";
			this.Size = new System.Drawing.Size(678, 419);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewAllConditions)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewAllConditions;
		private global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnEnabled;
		private System.Windows.Forms.DataGridViewImageColumn _columnQualityConditionType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnQualityCondition;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestType;
		private System.Windows.Forms.DataGridViewImageColumn _columnDatasetType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDataset;
	}
}
