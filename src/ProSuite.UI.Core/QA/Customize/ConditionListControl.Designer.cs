namespace ProSuite.UI.Core.QA.Customize
{
	partial class ConditionListControl
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
			this.dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this._columnQualityConditionType = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnQualityCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnTestCategories = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDatasetNames = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this._columnCategory,
            this._columnTestType,
            this._columnTestCategories,
            this._columnDatasetNames});
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
			this._dataGridViewAllConditions.TabIndex = 1;
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
			this._dataGridViewFindToolStrip.TabIndex = 2;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
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
			this.dataGridViewTextBoxColumn1.DataPropertyName = "ConditionName";
			this.dataGridViewTextBoxColumn1.HeaderText = "Quality Condition";
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "Category";
			this.dataGridViewTextBoxColumn2.HeaderText = "Category";
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn3.DataPropertyName = "TestDescriptorName";
			this.dataGridViewTextBoxColumn3.HeaderText = "Test Type";
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			// 
			// dataGridViewTextBoxColumn4
			// 
			this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn4.DataPropertyName = "TestCategories";
			this.dataGridViewTextBoxColumn4.HeaderText = "Test Categories";
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			// 
			// dataGridViewTextBoxColumn5
			// 
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn5.DataPropertyName = "DatasetNames";
			this.dataGridViewTextBoxColumn5.HeaderText = "Datasets";
			this.dataGridViewTextBoxColumn5.MinimumWidth = 50;
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
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
			this._columnQualityCondition.DataPropertyName = "ConditionName";
			this._columnQualityCondition.HeaderText = "Quality Condition";
			this._columnQualityCondition.MinimumWidth = 100;
			this._columnQualityCondition.Name = "_columnQualityCondition";
			this._columnQualityCondition.ReadOnly = true;
			this._columnQualityCondition.Width = 400;
			// 
			// _columnCategory
			// 
			this._columnCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnCategory.DataPropertyName = "Category";
			this._columnCategory.HeaderText = "Category";
			this._columnCategory.Name = "_columnCategory";
			this._columnCategory.ReadOnly = true;
			this._columnCategory.Width = 74;
			// 
			// _columnTestType
			// 
			this._columnTestType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestType.DataPropertyName = "TestDescriptorName";
			this._columnTestType.HeaderText = "Test Type";
			this._columnTestType.Name = "_columnTestType";
			this._columnTestType.ReadOnly = true;
			this._columnTestType.Width = 80;
			// 
			// _columnTestCategories
			// 
			this._columnTestCategories.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnTestCategories.DataPropertyName = "TestCategories";
			this._columnTestCategories.HeaderText = "Test Categories";
			this._columnTestCategories.Name = "_columnTestCategories";
			this._columnTestCategories.ReadOnly = true;
			this._columnTestCategories.Width = 97;
			// 
			// _columnDatasetNames
			// 
			this._columnDatasetNames.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDatasetNames.DataPropertyName = "DatasetNames";
			this._columnDatasetNames.HeaderText = "Datasets";
			this._columnDatasetNames.MinimumWidth = 50;
			this._columnDatasetNames.Name = "_columnDatasetNames";
			this._columnDatasetNames.ReadOnly = true;
			// 
			// ConditionListControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridViewAllConditions);
			this.Controls.Add(this._dataGridViewFindToolStrip);
			this.Name = "ConditionListControl";
			this.Size = new System.Drawing.Size(678, 419);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewAllConditions)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewAllConditions;
		private global::ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnEnabled;
		private System.Windows.Forms.DataGridViewImageColumn _columnQualityConditionType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnQualityCondition;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestType;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnTestCategories;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDatasetNames;
	}
}
