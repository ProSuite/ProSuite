namespace ProSuite.UI.Core.QA.Controls
{
	partial class QualityConditionTableViewControl
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
			this._dataGridViewTestParameters = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._splitContainer = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._parameterDescriptionTextBox = new System.Windows.Forms.TextBox();
			this._bindingSourceParametrValueList = new System.Windows.Forms.BindingSource(this.components);
			this.parameterNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.datasetTypeDataGridViewImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.valueDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnParameterValueModelName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnParameterValueFilterExpression = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnParameterUsedAsReferenceData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewTestParameters)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
			this._splitContainer.Panel1.SuspendLayout();
			this._splitContainer.Panel2.SuspendLayout();
			this._splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceParametrValueList)).BeginInit();
			this.SuspendLayout();
			// 
			// _dataGridViewTestParameters
			// 
			this._dataGridViewTestParameters.AllowUserToAddRows = false;
			this._dataGridViewTestParameters.AllowUserToDeleteRows = false;
			this._dataGridViewTestParameters.AllowUserToResizeRows = false;
			this._dataGridViewTestParameters.AutoGenerateColumns = false;
			this._dataGridViewTestParameters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewTestParameters.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridViewTestParameters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewTestParameters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.parameterNameDataGridViewTextBoxColumn,
            this.datasetTypeDataGridViewImageColumn,
            this.valueDataGridViewTextBoxColumn,
            this._columnParameterValueModelName,
            this._columnParameterValueFilterExpression,
            this._columnParameterUsedAsReferenceData});
			this._dataGridViewTestParameters.DataSource = this._bindingSourceParametrValueList;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(2);
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridViewTestParameters.DefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridViewTestParameters.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewTestParameters.Location = new System.Drawing.Point(0, 0);
			this._dataGridViewTestParameters.Name = "_dataGridViewTestParameters";
			this._dataGridViewTestParameters.ReadOnly = true;
			this._dataGridViewTestParameters.RowHeadersVisible = false;
			this._dataGridViewTestParameters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewTestParameters.Size = new System.Drawing.Size(468, 86);
			this._dataGridViewTestParameters.TabIndex = 6;
			this._dataGridViewTestParameters.CurrentCellChanged += new System.EventHandler(this._dataGridViewTestParameters_CurrentCellChanged);
			this._dataGridViewTestParameters.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this._dataGridViewTestParameters_DataBindingComplete);
			// 
			// _splitContainer
			// 
			this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this._splitContainer.Location = new System.Drawing.Point(0, 0);
			this._splitContainer.Name = "_splitContainer";
			this._splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// _splitContainer.Panel1
			// 
			this._splitContainer.Panel1.Controls.Add(this._dataGridViewTestParameters);
			this._splitContainer.Panel1MinSize = 80;
			// 
			// _splitContainer.Panel2
			// 
			this._splitContainer.Panel2.Controls.Add(this._parameterDescriptionTextBox);
			this._splitContainer.Size = new System.Drawing.Size(468, 157);
			this._splitContainer.SplitterDistance = 86;
			this._splitContainer.TabIndex = 7;
			// 
			// _parameterDescriptionTextBox
			// 
			this._parameterDescriptionTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this._parameterDescriptionTextBox.Location = new System.Drawing.Point(0, 0);
			this._parameterDescriptionTextBox.Multiline = true;
			this._parameterDescriptionTextBox.Name = "_parameterDescriptionTextBox";
			this._parameterDescriptionTextBox.ReadOnly = true;
			this._parameterDescriptionTextBox.Size = new System.Drawing.Size(468, 67);
			this._parameterDescriptionTextBox.TabIndex = 5;
			// 
			// _bindingSourceParametrValueList
			// 
			this._bindingSourceParametrValueList.AllowNew = false;
			this._bindingSourceParametrValueList.DataSource = typeof(ParameterValueListItem);
			// 
			// parameterNameDataGridViewTextBoxColumn
			// 
			this.parameterNameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.parameterNameDataGridViewTextBoxColumn.DataPropertyName = "ParameterName";
			this.parameterNameDataGridViewTextBoxColumn.HeaderText = "Parameter";
			this.parameterNameDataGridViewTextBoxColumn.MinimumWidth = 50;
			this.parameterNameDataGridViewTextBoxColumn.Name = "parameterNameDataGridViewTextBoxColumn";
			this.parameterNameDataGridViewTextBoxColumn.ReadOnly = true;
			this.parameterNameDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.parameterNameDataGridViewTextBoxColumn.Width = 65;
			// 
			// datasetTypeDataGridViewImageColumn
			// 
			this.datasetTypeDataGridViewImageColumn.DataPropertyName = "DatasetType";
			this.datasetTypeDataGridViewImageColumn.HeaderText = "";
			this.datasetTypeDataGridViewImageColumn.MinimumWidth = 24;
			this.datasetTypeDataGridViewImageColumn.Name = "datasetTypeDataGridViewImageColumn";
			this.datasetTypeDataGridViewImageColumn.ReadOnly = true;
			this.datasetTypeDataGridViewImageColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.datasetTypeDataGridViewImageColumn.Width = 24;
			// 
			// valueDataGridViewTextBoxColumn
			// 
			this.valueDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.valueDataGridViewTextBoxColumn.DataPropertyName = "Value";
			this.valueDataGridViewTextBoxColumn.HeaderText = "Value";
			this.valueDataGridViewTextBoxColumn.MinimumWidth = 80;
			this.valueDataGridViewTextBoxColumn.Name = "valueDataGridViewTextBoxColumn";
			this.valueDataGridViewTextBoxColumn.ReadOnly = true;
			this.valueDataGridViewTextBoxColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.valueDataGridViewTextBoxColumn.Width = 80;
			// 
			// _columnParameterValueModelName
			// 
			this._columnParameterValueModelName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnParameterValueModelName.DataPropertyName = "ModelName";
			this._columnParameterValueModelName.HeaderText = "Data Model";
			this._columnParameterValueModelName.MinimumWidth = 80;
			this._columnParameterValueModelName.Name = "_columnParameterValueModelName";
			this._columnParameterValueModelName.ReadOnly = true;
			this._columnParameterValueModelName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this._columnParameterValueModelName.Width = 80;
			// 
			// _columnParameterValueFilterExpression
			// 
			this._columnParameterValueFilterExpression.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnParameterValueFilterExpression.DataPropertyName = "FilterExpression";
			this._columnParameterValueFilterExpression.HeaderText = "Filter Expression";
			this._columnParameterValueFilterExpression.MinimumWidth = 146;
			this._columnParameterValueFilterExpression.Name = "_columnParameterValueFilterExpression";
			this._columnParameterValueFilterExpression.ReadOnly = true;
			this._columnParameterValueFilterExpression.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// _columnParameterUsedAsReferenceData
			// 
			this._columnParameterUsedAsReferenceData.DataPropertyName = "UsedAsReferenceData";
			this._columnParameterUsedAsReferenceData.HeaderText = "Reference";
			this._columnParameterUsedAsReferenceData.MinimumWidth = 70;
			this._columnParameterUsedAsReferenceData.Name = "_columnParameterUsedAsReferenceData";
			this._columnParameterUsedAsReferenceData.ReadOnly = true;
			this._columnParameterUsedAsReferenceData.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this._columnParameterUsedAsReferenceData.ToolTipText = "Used as reference data";
			this._columnParameterUsedAsReferenceData.Width = 70;
			// 
			// QualityConditionTableViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._splitContainer);
			this.Name = "QualityConditionTableViewControl";
			this.Size = new System.Drawing.Size(468, 157);
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewTestParameters)).EndInit();
			this._splitContainer.Panel1.ResumeLayout(false);
			this._splitContainer.Panel2.ResumeLayout(false);
			this._splitContainer.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
			this._splitContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceParametrValueList)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewTestParameters;
		private System.Windows.Forms.BindingSource _bindingSourceParametrValueList;
		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainer;
		private System.Windows.Forms.TextBox _parameterDescriptionTextBox;
		private System.Windows.Forms.DataGridViewTextBoxColumn parameterNameDataGridViewTextBoxColumn;
		private System.Windows.Forms.DataGridViewImageColumn datasetTypeDataGridViewImageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn valueDataGridViewTextBoxColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnParameterValueModelName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnParameterValueFilterExpression;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnParameterUsedAsReferenceData;
	}
}
