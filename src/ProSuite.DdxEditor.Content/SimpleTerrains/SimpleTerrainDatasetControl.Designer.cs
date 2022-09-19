using System.Windows.Forms;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	partial class SimpleTerrainDatasetControl
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
			this._pointDensity = new System.Windows.Forms.Label();
			this._updownPointDensity = new System.Windows.Forms.NumericUpDown();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._dataGridViewSourceDatasets = new DoubleBufferedDataGridView();
			this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDataset = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnSurfaceType = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this._columnWhereClause = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._toolStripSnapTargets = new ToolStripEx();
			this._toolStripLabelSnapTargets = new System.Windows.Forms.ToolStripLabel();
			this._toolStripButtonRemoveSnapTargets = new System.Windows.Forms.ToolStripButton();
			this._buttonAddSourceDatasets = new System.Windows.Forms.ToolStripButton();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._objectReferenceControlDatasetCategory = new ObjectReferenceControl();
			this._textBoxGeometryType = new System.Windows.Forms.TextBox();
			this._textBoxAliasName = new System.Windows.Forms.TextBox();
			this._textBoxAbbreviation = new System.Windows.Forms.TextBox();
			this._labelGeometryType = new System.Windows.Forms.Label();
			this._labelDatasetCategory = new System.Windows.Forms.Label();
			this._labelAliasName = new System.Windows.Forms.Label();
			this._labelAbbreviation = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this._updownPointDensity)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewSourceDatasets)).BeginInit();
			this._toolStripSnapTargets.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(78, 25);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 3;
			this._labelName.Text = "Name:";
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(122, 22);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(648, 20);
			this._textBoxName.TabIndex = 2;
			// 
			// _pointDensity
			// 
			this._pointDensity.AutoSize = true;
			this._pointDensity.Location = new System.Drawing.Point(44, 217);
			this._pointDensity.Name = "_pointDensity";
			this._pointDensity.Size = new System.Drawing.Size(72, 13);
			this._pointDensity.TabIndex = 28;
			this._pointDensity.Text = "Point Density:";
			this._pointDensity.Click += new System.EventHandler(this._customTolerance_Click);
			// 
			// _updownPointDensity
			// 
			this._updownPointDensity.DecimalPlaces = 2;
			this._updownPointDensity.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
			this._updownPointDensity.Location = new System.Drawing.Point(122, 215);
			this._updownPointDensity.Name = "_updownPointDensity";
			this._updownPointDensity.Size = new System.Drawing.Size(125, 20);
			this._updownPointDensity.TabIndex = 32;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(122, 48);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.Size = new System.Drawing.Size(648, 53);
			this._textBoxDescription.TabIndex = 34;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(53, 48);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 35;
			this._labelDescription.Text = "Description:";
			// 
			// _dataGridViewSourceDatasets
			// 
			this._dataGridViewSourceDatasets.AllowUserToAddRows = false;
			this._dataGridViewSourceDatasets.AllowUserToDeleteRows = false;
			this._dataGridViewSourceDatasets.AllowUserToResizeRows = false;
			this._dataGridViewSourceDatasets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._dataGridViewSourceDatasets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewSourceDatasets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewSourceDatasets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnImage,
            this.Column1,
            this._columnDataset,
            this._columnSurfaceType,
            this._columnWhereClause});
			this._dataGridViewSourceDatasets.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridViewSourceDatasets.Location = new System.Drawing.Point(17, 270);
			this._dataGridViewSourceDatasets.Name = "_dataGridViewSourceDatasets";
			this._dataGridViewSourceDatasets.RowHeadersVisible = false;
			this._dataGridViewSourceDatasets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewSourceDatasets.Size = new System.Drawing.Size(752, 188);
			this._dataGridViewSourceDatasets.TabIndex = 25;
			this._dataGridViewSourceDatasets.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this._dataGridViewSourceDatasets_CellValidating);
			this._dataGridViewSourceDatasets.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewSourceDatasets_CellValueChanged);
			this._dataGridViewSourceDatasets.SelectionChanged += new System.EventHandler(this._dataGridViewSourceDatasets_SelectionChanged);
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
			// _columnSurfaceType
			// 
			this._columnSurfaceType.DataPropertyName = "SurfaceType";
			this._columnSurfaceType.DataSource = new TinSurfaceType[] {
			TinSurfaceType.HardLine,
			TinSurfaceType.HardClip,
			TinSurfaceType.HardErase,
			TinSurfaceType.HardReplace,
			TinSurfaceType.SoftLine,
			TinSurfaceType.SoftClip,
			TinSurfaceType.SoftErase,
			TinSurfaceType.SoftReplace,
			TinSurfaceType.MassPoint};
			this._columnSurfaceType.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.Nothing;
			this._columnSurfaceType.HeaderText = "Surface Type";
			this._columnSurfaceType.Name = "_columnSurfaceType";
			this._columnSurfaceType.Width = 77;
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
            this._buttonAddSourceDatasets});
			this._toolStripSnapTargets.Location = new System.Drawing.Point(17, 245);
			this._toolStripSnapTargets.Name = "_toolStripSnapTargets";
			this._toolStripSnapTargets.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripSnapTargets.Size = new System.Drawing.Size(753, 25);
			this._toolStripSnapTargets.TabIndex = 26;
			this._toolStripSnapTargets.Text = "toolStrip1";
			// 
			// _toolStripLabelSnapTargets
			// 
			this._toolStripLabelSnapTargets.Name = "_toolStripLabelSnapTargets";
			this._toolStripLabelSnapTargets.Size = new System.Drawing.Size(128, 22);
			this._toolStripLabelSnapTargets.Text = "Terrain Source Datasets";
			// 
			// _toolStripButtonRemoveSnapTargets
			// 
			this._toolStripButtonRemoveSnapTargets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemoveSnapTargets.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemoveSnapTargets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonRemoveSnapTargets.Name = "_toolStripButtonRemoveSnapTargets";
			this._toolStripButtonRemoveSnapTargets.Size = new System.Drawing.Size(70, 22);
			this._toolStripButtonRemoveSnapTargets.Text = "Remove";
			this._toolStripButtonRemoveSnapTargets.Click += new System.EventHandler(this._buttonRemoveSourceDatasets_Click);
			// 
			// _buttonAddSourceDatasets
			// 
			this._buttonAddSourceDatasets.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._buttonAddSourceDatasets.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._buttonAddSourceDatasets.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._buttonAddSourceDatasets.ImageTransparentColor = System.Drawing.Color.Lime;
			this._buttonAddSourceDatasets.Name = "_buttonAddSourceDatasets";
			this._buttonAddSourceDatasets.Size = new System.Drawing.Size(58, 22);
			this._buttonAddSourceDatasets.Text = "Add...";
			this._buttonAddSourceDatasets.Click += new System.EventHandler(this._buttonAddSourceDatasets_Click);
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _objectReferenceControlDatasetCategory
			// 
			this._objectReferenceControlDatasetCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlDatasetCategory.DataSource = null;
			this._objectReferenceControlDatasetCategory.DisplayMember = null;
			this._objectReferenceControlDatasetCategory.FindObjectDelegate = null;
			this._objectReferenceControlDatasetCategory.FormatTextDelegate = null;
			this._objectReferenceControlDatasetCategory.Location = new System.Drawing.Point(122, 160);
			this._objectReferenceControlDatasetCategory.Name = "_objectReferenceControlDatasetCategory";
			this._objectReferenceControlDatasetCategory.ReadOnly = false;
			this._objectReferenceControlDatasetCategory.Size = new System.Drawing.Size(648, 20);
			this._objectReferenceControlDatasetCategory.TabIndex = 38;
			// 
			// _textBoxGeometryType
			// 
			this._textBoxGeometryType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxGeometryType.Location = new System.Drawing.Point(122, 187);
			this._textBoxGeometryType.Name = "_textBoxGeometryType";
			this._textBoxGeometryType.ReadOnly = true;
			this._textBoxGeometryType.Size = new System.Drawing.Size(648, 20);
			this._textBoxGeometryType.TabIndex = 39;
			// 
			// _textBoxAliasName
			// 
			this._textBoxAliasName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxAliasName.Location = new System.Drawing.Point(122, 107);
			this._textBoxAliasName.Name = "_textBoxAliasName";
			this._textBoxAliasName.Size = new System.Drawing.Size(648, 20);
			this._textBoxAliasName.TabIndex = 36;
			// 
			// _textBoxAbbreviation
			// 
			this._textBoxAbbreviation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxAbbreviation.Location = new System.Drawing.Point(122, 133);
			this._textBoxAbbreviation.Name = "_textBoxAbbreviation";
			this._textBoxAbbreviation.Size = new System.Drawing.Size(648, 20);
			this._textBoxAbbreviation.TabIndex = 37;
			// 
			// _labelGeometryType
			// 
			this._labelGeometryType.AutoSize = true;
			this._labelGeometryType.Location = new System.Drawing.Point(34, 190);
			this._labelGeometryType.Name = "_labelGeometryType";
			this._labelGeometryType.Size = new System.Drawing.Size(82, 13);
			this._labelGeometryType.TabIndex = 43;
			this._labelGeometryType.Text = "Geometry Type:";
			this._labelGeometryType.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelDatasetCategory
			// 
			this._labelDatasetCategory.AutoSize = true;
			this._labelDatasetCategory.Location = new System.Drawing.Point(24, 163);
			this._labelDatasetCategory.Name = "_labelDatasetCategory";
			this._labelDatasetCategory.Size = new System.Drawing.Size(92, 13);
			this._labelDatasetCategory.TabIndex = 42;
			this._labelDatasetCategory.Text = "Dataset Category:";
			this._labelDatasetCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAliasName
			// 
			this._labelAliasName.AutoSize = true;
			this._labelAliasName.Location = new System.Drawing.Point(53, 110);
			this._labelAliasName.Name = "_labelAliasName";
			this._labelAliasName.Size = new System.Drawing.Size(63, 13);
			this._labelAliasName.TabIndex = 41;
			this._labelAliasName.Text = "Alias Name:";
			this._labelAliasName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAbbreviation
			// 
			this._labelAbbreviation.AutoSize = true;
			this._labelAbbreviation.Location = new System.Drawing.Point(47, 136);
			this._labelAbbreviation.Name = "_labelAbbreviation";
			this._labelAbbreviation.Size = new System.Drawing.Size(69, 13);
			this._labelAbbreviation.TabIndex = 40;
			this._labelAbbreviation.Text = "Abbreviation:";
			this._labelAbbreviation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// SimpleTerrainDatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelGeometryType);
			this.Controls.Add(this._labelDatasetCategory);
			this.Controls.Add(this._labelAliasName);
			this.Controls.Add(this._labelAbbreviation);
			this.Controls.Add(this._objectReferenceControlDatasetCategory);
			this.Controls.Add(this._textBoxGeometryType);
			this.Controls.Add(this._textBoxAliasName);
			this.Controls.Add(this._textBoxAbbreviation);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._updownPointDensity);
			this.Controls.Add(this._pointDensity);
			this.Controls.Add(this._dataGridViewSourceDatasets);
			this.Controls.Add(this._toolStripSnapTargets);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxName);
			this.Name = "SimpleTerrainDatasetControl";
			this.Size = new System.Drawing.Size(785, 524);
			((System.ComponentModel.ISupportInitialize)(this._updownPointDensity)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewSourceDatasets)).EndInit();
			this._toolStripSnapTargets.ResumeLayout(false);
			this._toolStripSnapTargets.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.TextBox _textBoxName;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewSourceDatasets;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripSnapTargets;
		private System.Windows.Forms.ToolStripLabel _toolStripLabelSnapTargets;
		private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveSnapTargets;
		private System.Windows.Forms.ToolStripButton _buttonAddSourceDatasets;
		private System.Windows.Forms.Label _pointDensity;
		private System.Windows.Forms.NumericUpDown _updownPointDensity;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDataset;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnWhereClause;
		private System.Windows.Forms.DataGridViewComboBoxColumn _columnSurfaceType;
		private TextBox _textBoxGeometryType;
		private TextBox _textBoxAliasName;
		private TextBox _textBoxAbbreviation;
		private Label _labelGeometryType;
		private Label _labelDatasetCategory;
		private Label _labelAliasName;
		private Label _labelAbbreviation;
		private ObjectReferenceControl _objectReferenceControlDatasetCategory;
	}
}
