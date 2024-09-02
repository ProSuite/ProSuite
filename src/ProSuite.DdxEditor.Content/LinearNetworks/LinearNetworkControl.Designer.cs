namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	partial class LinearNetworkControl
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
			components = new System.ComponentModel.Container();
			_labelName = new System.Windows.Forms.Label();
			_textBoxName = new System.Windows.Forms.TextBox();
			_labelDescription = new System.Windows.Forms.Label();
			_textBoxDescription = new System.Windows.Forms.TextBox();
			_labelCustomTolerance = new System.Windows.Forms.Label();
			_updownCustomTolerance = new System.Windows.Forms.NumericUpDown();
			_labelEnforceFlowDirection = new System.Windows.Forms.Label();
			_checkBoxEnforceFlowDirection = new System.Windows.Forms.CheckBox();
			_dataGridViewLinearNetworkDatasets = new Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			_toolStripSnapTargets = new Commons.UI.WinForms.Controls.ToolStripEx();
			_toolStripLabelSnapTargets = new System.Windows.Forms.ToolStripLabel();
			_toolStripButtonRemoveSnapTargets = new System.Windows.Forms.ToolStripButton();
			_buttonAddNetworkDatasets = new System.Windows.Forms.ToolStripButton();
			_errorProvider = new System.Windows.Forms.ErrorProvider(components);
			linearNetworkDatasetTableRowBindingSource = new System.Windows.Forms.BindingSource(components);
			_columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			_columnModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnDataset = new System.Windows.Forms.DataGridViewTextBoxColumn();
			_columnIsDefaultJunction = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			_columnSplitting = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			_columnWhereClause = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)_updownCustomTolerance).BeginInit();
			((System.ComponentModel.ISupportInitialize)_dataGridViewLinearNetworkDatasets).BeginInit();
			_toolStripSnapTargets.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_errorProvider).BeginInit();
			((System.ComponentModel.ISupportInitialize)linearNetworkDatasetTableRowBindingSource).BeginInit();
			SuspendLayout();
			// 
			// _labelName
			// 
			_labelName.AutoSize = true;
			_labelName.Location = new System.Drawing.Point(18, 29);
			_labelName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelName.Name = "_labelName";
			_labelName.Size = new System.Drawing.Size(42, 15);
			_labelName.TabIndex = 3;
			_labelName.Text = "Name:";
			// 
			// _textBoxName
			// 
			_textBoxName.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_textBoxName.Location = new System.Drawing.Point(180, 25);
			_textBoxName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxName.Name = "_textBoxName";
			_textBoxName.Size = new System.Drawing.Size(718, 23);
			_textBoxName.TabIndex = 2;
			// 
			// _labelDescription
			// 
			_labelDescription.AutoSize = true;
			_labelDescription.Location = new System.Drawing.Point(18, 55);
			_labelDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelDescription.Name = "_labelDescription";
			_labelDescription.Size = new System.Drawing.Size(70, 15);
			_labelDescription.TabIndex = 35;
			_labelDescription.Text = "Description:";
			// 
			// _textBoxDescription
			// 
			_textBoxDescription.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_textBoxDescription.Location = new System.Drawing.Point(180, 55);
			_textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_textBoxDescription.Multiline = true;
			_textBoxDescription.Name = "_textBoxDescription";
			_textBoxDescription.Size = new System.Drawing.Size(718, 61);
			_textBoxDescription.TabIndex = 34;
			// 
			// _labelCustomTolerance
			// 
			_labelCustomTolerance.AutoSize = true;
			_labelCustomTolerance.Location = new System.Drawing.Point(18, 126);
			_labelCustomTolerance.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelCustomTolerance.Name = "_labelCustomTolerance";
			_labelCustomTolerance.Size = new System.Drawing.Size(105, 15);
			_labelCustomTolerance.TabIndex = 28;
			_labelCustomTolerance.Text = "Custom Tolerance:";
			// 
			// _updownCustomTolerance
			// 
			_updownCustomTolerance.DecimalPlaces = 2;
			_updownCustomTolerance.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
			_updownCustomTolerance.Location = new System.Drawing.Point(180, 123);
			_updownCustomTolerance.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_updownCustomTolerance.Name = "_updownCustomTolerance";
			_updownCustomTolerance.Size = new System.Drawing.Size(108, 23);
			_updownCustomTolerance.TabIndex = 32;
			// 
			// _labelEnforceFlowDirection
			// 
			_labelEnforceFlowDirection.AutoSize = true;
			_labelEnforceFlowDirection.Location = new System.Drawing.Point(18, 153);
			_labelEnforceFlowDirection.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			_labelEnforceFlowDirection.Name = "_labelEnforceFlowDirection";
			_labelEnforceFlowDirection.Size = new System.Drawing.Size(129, 15);
			_labelEnforceFlowDirection.TabIndex = 30;
			_labelEnforceFlowDirection.Text = "Enforce Flow Direction:";
			// 
			// _checkBoxEnforceFlowDirection
			// 
			_checkBoxEnforceFlowDirection.AutoSize = true;
			_checkBoxEnforceFlowDirection.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			_checkBoxEnforceFlowDirection.Location = new System.Drawing.Point(180, 153);
			_checkBoxEnforceFlowDirection.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_checkBoxEnforceFlowDirection.Name = "_checkBoxEnforceFlowDirection";
			_checkBoxEnforceFlowDirection.Size = new System.Drawing.Size(15, 14);
			_checkBoxEnforceFlowDirection.TabIndex = 33;
			_checkBoxEnforceFlowDirection.UseVisualStyleBackColor = true;
			// 
			// _dataGridViewLinearNetworkDatasets
			// 
			_dataGridViewLinearNetworkDatasets.AllowUserToAddRows = false;
			_dataGridViewLinearNetworkDatasets.AllowUserToDeleteRows = false;
			_dataGridViewLinearNetworkDatasets.AllowUserToResizeRows = false;
			_dataGridViewLinearNetworkDatasets.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_dataGridViewLinearNetworkDatasets.AutoGenerateColumns = false;
			_dataGridViewLinearNetworkDatasets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			_dataGridViewLinearNetworkDatasets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_dataGridViewLinearNetworkDatasets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _columnImage, _columnModel, _columnDataset, _columnIsDefaultJunction, _columnSplitting, _columnWhereClause });
			_dataGridViewLinearNetworkDatasets.DataSource = linearNetworkDatasetTableRowBindingSource;
			_dataGridViewLinearNetworkDatasets.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			_dataGridViewLinearNetworkDatasets.Location = new System.Drawing.Point(21, 227);
			_dataGridViewLinearNetworkDatasets.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_dataGridViewLinearNetworkDatasets.Name = "_dataGridViewLinearNetworkDatasets";
			_dataGridViewLinearNetworkDatasets.RowHeadersVisible = false;
			_dataGridViewLinearNetworkDatasets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			_dataGridViewLinearNetworkDatasets.Size = new System.Drawing.Size(877, 222);
			_dataGridViewLinearNetworkDatasets.TabIndex = 25;
			_dataGridViewLinearNetworkDatasets.CellValidating += _dataGridViewLinearNetworkDatasets_CellValidating;
			_dataGridViewLinearNetworkDatasets.CellValueChanged += _dataGridViewLinearNetworkDatasets_CellValueChanged;
			_dataGridViewLinearNetworkDatasets.SelectionChanged += _dataGridViewLinearNetworkDatasets_SelectionChanged;
			// 
			// _toolStripSnapTargets
			// 
			_toolStripSnapTargets.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_toolStripSnapTargets.AutoSize = false;
			_toolStripSnapTargets.ClickThrough = true;
			_toolStripSnapTargets.Dock = System.Windows.Forms.DockStyle.None;
			_toolStripSnapTargets.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			_toolStripSnapTargets.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _toolStripLabelSnapTargets, _toolStripButtonRemoveSnapTargets, _buttonAddNetworkDatasets });
			_toolStripSnapTargets.Location = new System.Drawing.Point(20, 200);
			_toolStripSnapTargets.Name = "_toolStripSnapTargets";
			_toolStripSnapTargets.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			_toolStripSnapTargets.Size = new System.Drawing.Size(878, 29);
			_toolStripSnapTargets.TabIndex = 26;
			_toolStripSnapTargets.Text = "toolStrip1";
			// 
			// _toolStripLabelSnapTargets
			// 
			_toolStripLabelSnapTargets.Name = "_toolStripLabelSnapTargets";
			_toolStripLabelSnapTargets.Size = new System.Drawing.Size(99, 26);
			_toolStripLabelSnapTargets.Text = "Network Datasets";
			// 
			// _toolStripButtonRemoveSnapTargets
			// 
			_toolStripButtonRemoveSnapTargets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			_toolStripButtonRemoveSnapTargets.Image = Properties.Resources.Remove;
			_toolStripButtonRemoveSnapTargets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			_toolStripButtonRemoveSnapTargets.Name = "_toolStripButtonRemoveSnapTargets";
			_toolStripButtonRemoveSnapTargets.Size = new System.Drawing.Size(70, 26);
			_toolStripButtonRemoveSnapTargets.Text = "Remove";
			_toolStripButtonRemoveSnapTargets.Click += _buttonRemoveNetworkDatasets_Click;
			// 
			// _buttonAddNetworkDatasets
			// 
			_buttonAddNetworkDatasets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			_buttonAddNetworkDatasets.Image = Properties.Resources.Assign;
			_buttonAddNetworkDatasets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			_buttonAddNetworkDatasets.ImageTransparentColor = System.Drawing.Color.Lime;
			_buttonAddNetworkDatasets.Name = "_buttonAddNetworkDatasets";
			_buttonAddNetworkDatasets.Size = new System.Drawing.Size(58, 26);
			_buttonAddNetworkDatasets.Text = "Add...";
			_buttonAddNetworkDatasets.Click += _buttonAddNetworkDatasets_Click;
			// 
			// _errorProvider
			// 
			_errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			_errorProvider.ContainerControl = this;
			// 
			// linearNetworkDatasetTableRowBindingSource
			// 
			linearNetworkDatasetTableRowBindingSource.DataSource = typeof(LinearNetworkDatasetTableRow);
			// 
			// _columnImage
			// 
			_columnImage.DataPropertyName = "Image";
			_columnImage.HeaderText = "";
			_columnImage.MinimumWidth = 20;
			_columnImage.Name = "_columnImage";
			_columnImage.ReadOnly = true;
			_columnImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			_columnImage.Width = 20;
			// 
			// _columnModel
			// 
			_columnModel.DataPropertyName = "ModelName";
			_columnModel.HeaderText = "Model";
			_columnModel.Name = "_columnModel";
			_columnModel.ReadOnly = true;
			_columnModel.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			_columnModel.Width = 47;
			// 
			// _columnDataset
			// 
			_columnDataset.DataPropertyName = "DatasetAliasName";
			_columnDataset.HeaderText = "Dataset";
			_columnDataset.Name = "_columnDataset";
			_columnDataset.ReadOnly = true;
			_columnDataset.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			_columnDataset.Width = 52;
			// 
			// _columnIsDefaultJunction
			// 
			_columnIsDefaultJunction.DataPropertyName = "IsDefaultJunction";
			_columnIsDefaultJunction.HeaderText = "Is Default Junction";
			_columnIsDefaultJunction.Name = "_columnIsDefaultJunction";
			_columnIsDefaultJunction.Width = 99;
			// 
			// _columnSplitting
			// 
			_columnSplitting.DataPropertyName = "Splitting";
			_columnSplitting.HeaderText = "Splitting";
			_columnSplitting.Name = "_columnSplitting";
			_columnSplitting.Width = 57;
			// 
			// _columnWhereClause
			// 
			_columnWhereClause.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			_columnWhereClause.DataPropertyName = "WhereClause";
			_columnWhereClause.HeaderText = "Where Clause";
			_columnWhereClause.Name = "_columnWhereClause";
			// 
			// LinearNetworkControl
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_labelDescription);
			Controls.Add(_textBoxDescription);
			Controls.Add(_checkBoxEnforceFlowDirection);
			Controls.Add(_updownCustomTolerance);
			Controls.Add(_labelEnforceFlowDirection);
			Controls.Add(_labelCustomTolerance);
			Controls.Add(_dataGridViewLinearNetworkDatasets);
			Controls.Add(_toolStripSnapTargets);
			Controls.Add(_labelName);
			Controls.Add(_textBoxName);
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "LinearNetworkControl";
			Size = new System.Drawing.Size(916, 467);
			((System.ComponentModel.ISupportInitialize)_updownCustomTolerance).EndInit();
			((System.ComponentModel.ISupportInitialize)_dataGridViewLinearNetworkDatasets).EndInit();
			_toolStripSnapTargets.ResumeLayout(false);
			_toolStripSnapTargets.PerformLayout();
			((System.ComponentModel.ISupportInitialize)_errorProvider).EndInit();
			((System.ComponentModel.ISupportInitialize)linearNetworkDatasetTableRowBindingSource).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewLinearNetworkDatasets;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripSnapTargets;
		private System.Windows.Forms.ToolStripLabel _toolStripLabelSnapTargets;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveSnapTargets;
		private System.Windows.Forms.ToolStripButton _buttonAddNetworkDatasets;
		private System.Windows.Forms.Label _labelCustomTolerance;
		private System.Windows.Forms.NumericUpDown _updownCustomTolerance;
		private System.Windows.Forms.Label _labelEnforceFlowDirection;
		private System.Windows.Forms.CheckBox _checkBoxEnforceFlowDirection;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnModel;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDataset;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnIsDefaultJunction;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnSplitting;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnWhereClause;
		private System.Windows.Forms.BindingSource linearNetworkDatasetTableRowBindingSource;
	}
}
