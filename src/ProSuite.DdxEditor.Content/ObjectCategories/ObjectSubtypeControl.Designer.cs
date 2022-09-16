namespace ProSuite.DdxEditor.Content.ObjectCategories
{
    partial class ObjectSubtypeControl<T>
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
			this._toolStripCriteria = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripLabelCriteria = new System.Windows.Forms.ToolStripLabel();
			this._toolStripButtonRemoveCriteria = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonAddCriterium = new System.Windows.Forms.ToolStripButton();
			this._dataGridViewCriteria = new global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this.attributeNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.attributeValueDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.AttributeType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._bindingSourceCriteriaListItem = new System.Windows.Forms.BindingSource(this.components);
			this._toolStripCriteria.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewCriteria)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceCriteriaListItem)).BeginInit();
			this.SuspendLayout();
			// 
			// _toolStripCriteria
			// 
			this._toolStripCriteria.AutoSize = false;
			this._toolStripCriteria.ClickThrough = true;
			this._toolStripCriteria.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripCriteria.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripLabelCriteria,
            this._toolStripButtonRemoveCriteria,
            this._toolStripButtonAddCriterium});
			this._toolStripCriteria.Location = new System.Drawing.Point(0, 0);
			this._toolStripCriteria.Name = "_toolStripCriteria";
			this._toolStripCriteria.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripCriteria.Size = new System.Drawing.Size(681, 25);
			this._toolStripCriteria.TabIndex = 26;
			this._toolStripCriteria.Text = "toolStrip1";
			// 
			// _toolStripLabelCriteria
			// 
			this._toolStripLabelCriteria.Name = "_toolStripLabelCriteria";
			this._toolStripLabelCriteria.Size = new System.Drawing.Size(45, 22);
			this._toolStripLabelCriteria.Text = "Criteria";
			// 
			// _toolStripButtonRemoveCriteria
			// 
			this._toolStripButtonRemoveCriteria.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonRemoveCriteria.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Remove;
			this._toolStripButtonRemoveCriteria.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonRemoveCriteria.Name = "_toolStripButtonRemoveCriteria";
			this._toolStripButtonRemoveCriteria.Size = new System.Drawing.Size(70, 22);
			this._toolStripButtonRemoveCriteria.Text = "Remove";
			this._toolStripButtonRemoveCriteria.Click += new System.EventHandler(this._toolStripButtonRemoveCriterium_Click);
			// 
			// _toolStripButtonAddCriterium
			// 
			this._toolStripButtonAddCriterium.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonAddCriterium.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Assign;
			this._toolStripButtonAddCriterium.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonAddCriterium.Name = "_toolStripButtonAddCriterium";
			this._toolStripButtonAddCriterium.Size = new System.Drawing.Size(58, 22);
			this._toolStripButtonAddCriterium.Text = "Add...";
			this._toolStripButtonAddCriterium.Click += new System.EventHandler(this._toolStripButtonAddCriterium_Click);
			// 
			// _dataGridViewCriteria
			// 
			this._dataGridViewCriteria.AllowUserToAddRows = false;
			this._dataGridViewCriteria.AllowUserToDeleteRows = false;
			this._dataGridViewCriteria.AllowUserToResizeRows = false;
			this._dataGridViewCriteria.AutoGenerateColumns = false;
			this._dataGridViewCriteria.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridViewCriteria.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridViewCriteria.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.attributeNameDataGridViewTextBoxColumn,
            this.attributeValueDataGridViewTextBoxColumn,
            this.AttributeType});
			this._dataGridViewCriteria.DataSource = this._bindingSourceCriteriaListItem;
			this._dataGridViewCriteria.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridViewCriteria.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridViewCriteria.Location = new System.Drawing.Point(0, 25);
			this._dataGridViewCriteria.Name = "_dataGridViewCriteria";
			this._dataGridViewCriteria.RowHeadersVisible = false;
			this._dataGridViewCriteria.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridViewCriteria.Size = new System.Drawing.Size(681, 209);
			this._dataGridViewCriteria.TabIndex = 25;
			this._dataGridViewCriteria.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridViewCriteria_CellValueChanged);
			this._dataGridViewCriteria.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this._dataGridViewCriteria_DataError);
			this._dataGridViewCriteria.SelectionChanged += new System.EventHandler(this._dataGridViewCriteria_SelectionChanged);
			// 
			// attributeNameDataGridViewTextBoxColumn
			// 
			this.attributeNameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.attributeNameDataGridViewTextBoxColumn.DataPropertyName = "AttributeName";
			this.attributeNameDataGridViewTextBoxColumn.HeaderText = "Attribute Name";
			this.attributeNameDataGridViewTextBoxColumn.MinimumWidth = 150;
			this.attributeNameDataGridViewTextBoxColumn.Name = "attributeNameDataGridViewTextBoxColumn";
			this.attributeNameDataGridViewTextBoxColumn.ReadOnly = true;
			this.attributeNameDataGridViewTextBoxColumn.Width = 150;
			// 
			// attributeValueDataGridViewTextBoxColumn
			// 
			this.attributeValueDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.attributeValueDataGridViewTextBoxColumn.DataPropertyName = "AttributeValue";
			this.attributeValueDataGridViewTextBoxColumn.HeaderText = "Attribute Value";
			this.attributeValueDataGridViewTextBoxColumn.Name = "attributeValueDataGridViewTextBoxColumn";
			// 
			// AttributeType
			// 
			this.AttributeType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.AttributeType.DataPropertyName = "AttributeType";
			this.AttributeType.HeaderText = "Type";
			this.AttributeType.MinimumWidth = 100;
			this.AttributeType.Name = "AttributeType";
			this.AttributeType.ReadOnly = true;
			// 
			// _bindingSourceCriteriaListItem
			// 
			this._bindingSourceCriteriaListItem.DataSource = typeof(ObjectSubtypeCriterionTableRow);
			// 
			// ObjectSubtypeControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridViewCriteria);
			this.Controls.Add(this._toolStripCriteria);
			this.Name = "ObjectSubtypeControl";
			this.Size = new System.Drawing.Size(681, 234);
			this._toolStripCriteria.ResumeLayout(false);
			this._toolStripCriteria.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridViewCriteria)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSourceCriteriaListItem)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.ToolStripLabel _toolStripLabelCriteria;
        private System.Windows.Forms.ToolStripButton _toolStripButtonRemoveCriteria;
		private System.Windows.Forms.ToolStripButton _toolStripButtonAddCriterium;
        private System.Windows.Forms.BindingSource _bindingSourceCriteriaListItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn attributeNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn attributeValueDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn AttributeType;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStripCriteria;
		private global::ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView _dataGridViewCriteria;
    }
}
