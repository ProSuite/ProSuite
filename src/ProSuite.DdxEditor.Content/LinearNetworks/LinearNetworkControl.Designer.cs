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
            this.components = new System.ComponentModel.Container();
            this._labelName = new System.Windows.Forms.Label();
            this._textBoxName = new System.Windows.Forms.TextBox();
            this._customTolerance = new System.Windows.Forms.Label();
            this.labelEnforceFd = new System.Windows.Forms.Label();
            this._updownCustomTolerance = new System.Windows.Forms.NumericUpDown();
            this._cbEnforceFd = new System.Windows.Forms.CheckBox();
            this._textBoxDescription = new System.Windows.Forms.TextBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this._dataGridViewLinearNetworkDatasets = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
            this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._columnDataset = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._columnEdgeSnap = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this._columnWhereClause = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._toolStripSnapTargets = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
            this._toolStripLabelSnapTargets = new System.Windows.Forms.ToolStripLabel();
            this._toolStripButtonRemoveSnapTargets = new System.Windows.Forms.ToolStripButton();
            this._buttonAddNetworkDatasets = new System.Windows.Forms.ToolStripButton();
            this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this._updownCustomTolerance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridViewLinearNetworkDatasets)).BeginInit();
            this._toolStripSnapTargets.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // _labelName
            // 
            this._labelName.AutoSize = true;
            this._labelName.Location = new System.Drawing.Point(15, 25);
            this._labelName.Name = "_labelName";
            this._labelName.Size = new System.Drawing.Size(35, 13);
            this._labelName.TabIndex = 3;
            this._labelName.Text = "Name";
            // 
            // _textBoxName
            // 
            this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxName.Location = new System.Drawing.Point(154, 22);
            this._textBoxName.Name = "_textBoxName";
            this._textBoxName.Size = new System.Drawing.Size(616, 20);
            this._textBoxName.TabIndex = 2;
            // 
            // _customTolerance
            // 
            this._customTolerance.AutoSize = true;
            this._customTolerance.Location = new System.Drawing.Point(15, 109);
            this._customTolerance.Name = "_customTolerance";
            this._customTolerance.Size = new System.Drawing.Size(93, 13);
            this._customTolerance.TabIndex = 28;
            this._customTolerance.Text = "Custom Tolerance";
            this._customTolerance.Click += new System.EventHandler(this._customTolerance_Click);
            // 
            // labelEnforceFd
            // 
            this.labelEnforceFd.AutoSize = true;
            this.labelEnforceFd.Location = new System.Drawing.Point(15, 133);
            this.labelEnforceFd.Name = "labelEnforceFd";
            this.labelEnforceFd.Size = new System.Drawing.Size(114, 13);
            this.labelEnforceFd.TabIndex = 30;
            this.labelEnforceFd.Text = "Enforce Flow Direction";
            this.labelEnforceFd.Click += new System.EventHandler(this.labelEnforceFd_Click);
            // 
            // _updownCustomTolerance
            // 
            this._updownCustomTolerance.DecimalPlaces = 2;
            this._updownCustomTolerance.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this._updownCustomTolerance.Location = new System.Drawing.Point(154, 107);
            this._updownCustomTolerance.Name = "_updownCustomTolerance";
            this._updownCustomTolerance.Size = new System.Drawing.Size(93, 20);
            this._updownCustomTolerance.TabIndex = 32;
            // 
            // _cbEnforceFd
            // 
            this._cbEnforceFd.AutoSize = true;
            this._cbEnforceFd.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this._cbEnforceFd.Location = new System.Drawing.Point(154, 133);
            this._cbEnforceFd.Name = "_cbEnforceFd";
            this._cbEnforceFd.Size = new System.Drawing.Size(15, 14);
            this._cbEnforceFd.TabIndex = 33;
            this._cbEnforceFd.UseVisualStyleBackColor = true;
            // 
            // _textBoxDescription
            // 
            this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxDescription.Location = new System.Drawing.Point(154, 48);
            this._textBoxDescription.Multiline = true;
            this._textBoxDescription.Name = "_textBoxDescription";
            this._textBoxDescription.Size = new System.Drawing.Size(616, 53);
            this._textBoxDescription.TabIndex = 34;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(15, 48);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(60, 13);
            this.lblDescription.TabIndex = 35;
            this.lblDescription.Text = "Description";
            // 
            // _dataGridViewLinearNetworkDatasets
            // 
            this._dataGridViewLinearNetworkDatasets.AllowUserToAddRows = false;
            this._dataGridViewLinearNetworkDatasets.AllowUserToDeleteRows = false;
            this._dataGridViewLinearNetworkDatasets.AllowUserToResizeRows = false;
            this._dataGridViewLinearNetworkDatasets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._dataGridViewLinearNetworkDatasets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this._dataGridViewLinearNetworkDatasets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGridViewLinearNetworkDatasets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnImage,
            this.Column1,
            this._columnDataset,
            this._columnEdgeSnap,
            this._columnWhereClause});
            this._dataGridViewLinearNetworkDatasets.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this._dataGridViewLinearNetworkDatasets.Location = new System.Drawing.Point(18, 197);
            this._dataGridViewLinearNetworkDatasets.Name = "_dataGridViewLinearNetworkDatasets";
            this._dataGridViewLinearNetworkDatasets.RowHeadersVisible = false;
            this._dataGridViewLinearNetworkDatasets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dataGridViewLinearNetworkDatasets.Size = new System.Drawing.Size(752, 188);
            this._dataGridViewLinearNetworkDatasets.TabIndex = 25;
            this._dataGridViewLinearNetworkDatasets.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this._dataGridViewLinearNetworkDatasets_CellValidating);
            this._dataGridViewLinearNetworkDatasets.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewLinearNetworkDatasets_CellValueChanged);
            this._dataGridViewLinearNetworkDatasets.SelectionChanged += new System.EventHandler(this._dataGridViewLinearNetworkDatasets_SelectionChanged);
            // 
            // _columnImage
            // 
            this._columnImage.DataPropertyName = "Image";
            this._columnImage.HeaderText = "";
            this._columnImage.MinimumWidth = 20;
            this._columnImage.Name = "_columnImage";
            this._columnImage.ReadOnly = true;
            this._columnImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this._columnImage.Width = 20;
            // 
            // Column1
            // 
            this.Column1.DataPropertyName = "ModelName";
            this.Column1.HeaderText = "Model";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Column1.Width = 42;
            // 
            // _columnDataset
            // 
            this._columnDataset.DataPropertyName = "DatasetAliasName";
            this._columnDataset.HeaderText = "Dataset";
            this._columnDataset.Name = "_columnDataset";
            this._columnDataset.ReadOnly = true;
            this._columnDataset.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this._columnDataset.Width = 50;
            // 
            // _columnEdgeSnap
            // 
            this._columnEdgeSnap.DataPropertyName = "IsDefaultJunction";
            this._columnEdgeSnap.HeaderText = "Is Default Junction";
            this._columnEdgeSnap.Name = "_columnEdgeSnap";
            this._columnEdgeSnap.Width = 91;
            // 
            // _columnWhereClause
            // 
            this._columnWhereClause.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this._columnWhereClause.DataPropertyName = "WhereClause";
            this._columnWhereClause.HeaderText = "Where Clause";
            this._columnWhereClause.Name = "_columnWhereClause";
            // 
            // _toolStripSnapTargets
            // 
            this._toolStripSnapTargets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._toolStripSnapTargets.AutoSize = false;
            this._toolStripSnapTargets.ClickThrough = true;
            this._toolStripSnapTargets.Dock = System.Windows.Forms.DockStyle.None;
            this._toolStripSnapTargets.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._toolStripSnapTargets.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripLabelSnapTargets,
            this._toolStripButtonRemoveSnapTargets,
            this._buttonAddNetworkDatasets});
            this._toolStripSnapTargets.Location = new System.Drawing.Point(17, 173);
            this._toolStripSnapTargets.Name = "_toolStripSnapTargets";
            this._toolStripSnapTargets.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._toolStripSnapTargets.Size = new System.Drawing.Size(753, 25);
            this._toolStripSnapTargets.TabIndex = 26;
            this._toolStripSnapTargets.Text = "toolStrip1";
            // 
            // _toolStripLabelSnapTargets
            // 
            this._toolStripLabelSnapTargets.Name = "_toolStripLabelSnapTargets";
            this._toolStripLabelSnapTargets.Size = new System.Drawing.Size(99, 22);
            this._toolStripLabelSnapTargets.Text = "Network Datasets";
            // 
            // _toolStripButtonRemoveSnapTargets
            // 
            this._toolStripButtonRemoveSnapTargets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this._toolStripButtonRemoveSnapTargets.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
            this._toolStripButtonRemoveSnapTargets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripButtonRemoveSnapTargets.Name = "_toolStripButtonRemoveSnapTargets";
            this._toolStripButtonRemoveSnapTargets.Size = new System.Drawing.Size(70, 22);
            this._toolStripButtonRemoveSnapTargets.Text = "Remove";
            this._toolStripButtonRemoveSnapTargets.Click += new System.EventHandler(this._buttonRemoveNetworkDatasets_Click);
            // 
            // _buttonAddNetworkDatasets
            // 
            this._buttonAddNetworkDatasets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this._buttonAddNetworkDatasets.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
            this._buttonAddNetworkDatasets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._buttonAddNetworkDatasets.ImageTransparentColor = System.Drawing.Color.Lime;
            this._buttonAddNetworkDatasets.Name = "_buttonAddNetworkDatasets";
            this._buttonAddNetworkDatasets.Size = new System.Drawing.Size(58, 22);
            this._buttonAddNetworkDatasets.Text = "Add...";
            this._buttonAddNetworkDatasets.Click += new System.EventHandler(this._buttonAddNetworkDatasets_Click);
            // 
            // _errorProvider
            // 
            this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this._errorProvider.ContainerControl = this;
            // 
            // LinearNetworkControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this._textBoxDescription);
            this.Controls.Add(this._cbEnforceFd);
            this.Controls.Add(this._updownCustomTolerance);
            this.Controls.Add(this.labelEnforceFd);
            this.Controls.Add(this._customTolerance);
            this.Controls.Add(this._dataGridViewLinearNetworkDatasets);
            this.Controls.Add(this._toolStripSnapTargets);
            this.Controls.Add(this._labelName);
            this.Controls.Add(this._textBoxName);
            this.Name = "LinearNetworkControl";
            this.Size = new System.Drawing.Size(785, 524);
            ((System.ComponentModel.ISupportInitialize)(this._updownCustomTolerance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridViewLinearNetworkDatasets)).EndInit();
            this._toolStripSnapTargets.ResumeLayout(false);
            this._toolStripSnapTargets.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.TextBox _textBoxName;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewLinearNetworkDatasets;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripSnapTargets;
		private System.Windows.Forms.ToolStripLabel _toolStripLabelSnapTargets;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveSnapTargets;
		private System.Windows.Forms.ToolStripButton _buttonAddNetworkDatasets;
		private System.Windows.Forms.Label _customTolerance;
		private System.Windows.Forms.Label labelEnforceFd;
		private System.Windows.Forms.NumericUpDown _updownCustomTolerance;
		private System.Windows.Forms.CheckBox _cbEnforceFd;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDataset;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnEdgeSnap;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnWhereClause;
		private System.Windows.Forms.ErrorProvider _errorProvider;
	}
}