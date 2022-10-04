namespace ProSuite.DdxEditor.Framework.EntityProperties
{
	partial class EntityPropertiesForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this._groupBoxProperties = new System.Windows.Forms.GroupBox();
			this._buttonClose = new System.Windows.Forms.Button();
			this._dataGridView = new System.Windows.Forms.DataGridView();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._buttonCopy = new System.Windows.Forms.Button();
			this._columnPropertyName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._groupBoxProperties.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _groupBoxProperties
			// 
			this._groupBoxProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxProperties.Controls.Add(this._dataGridView);
			this._groupBoxProperties.Location = new System.Drawing.Point(12, 12);
			this._groupBoxProperties.Name = "_groupBoxProperties";
			this._groupBoxProperties.Padding = new System.Windows.Forms.Padding(6);
			this._groupBoxProperties.Size = new System.Drawing.Size(351, 296);
			this._groupBoxProperties.TabIndex = 1;
			this._groupBoxProperties.TabStop = false;
			this._groupBoxProperties.Text = "Properties";
			// 
			// _buttonClose
			// 
			this._buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonClose.Location = new System.Drawing.Point(288, 314);
			this._buttonClose.Name = "_buttonClose";
			this._buttonClose.Size = new System.Drawing.Size(75, 23);
			this._buttonClose.TabIndex = 2;
			this._buttonClose.Text = "Close";
			this._buttonClose.UseVisualStyleBackColor = true;
			this._buttonClose.Click += new System.EventHandler(this._buttonClose_Click);
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnPropertyName,
            this._columnValue});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(6, 19);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(339, 271);
			this._dataGridView.TabIndex = 0;
			this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusLabel});
			this._statusStrip.Location = new System.Drawing.Point(0, 346);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(375, 22);
			this._statusStrip.SizingGrip = false;
			this._statusStrip.TabIndex = 3;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _statusLabel
			// 
			this._statusLabel.Name = "_statusLabel";
			this._statusLabel.Size = new System.Drawing.Size(0, 17);
			// 
			// _buttonCopy
			// 
			this._buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCopy.Location = new System.Drawing.Point(207, 314);
			this._buttonCopy.Name = "_buttonCopy";
			this._buttonCopy.Size = new System.Drawing.Size(75, 23);
			this._buttonCopy.TabIndex = 4;
			this._buttonCopy.Text = "Copy";
			this._buttonCopy.UseVisualStyleBackColor = true;
			this._buttonCopy.Click += new System.EventHandler(this._buttonCopy_Click);
			// 
			// _columnPropertyName
			// 
			this._columnPropertyName.DataPropertyName = "Name";
			this._columnPropertyName.HeaderText = "Property";
			this._columnPropertyName.Name = "_columnPropertyName";
			this._columnPropertyName.ReadOnly = true;
			// 
			// _columnValue
			// 
			this._columnValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnValue.DataPropertyName = "Value";
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._columnValue.DefaultCellStyle = dataGridViewCellStyle1;
			this._columnValue.HeaderText = "Value";
			this._columnValue.Name = "_columnValue";
			this._columnValue.ReadOnly = true;
			// 
			// EntityPropertiesForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonClose;
			this.ClientSize = new System.Drawing.Size(375, 368);
			this.Controls.Add(this._buttonCopy);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._buttonClose);
			this.Controls.Add(this._groupBoxProperties);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(240, 270);
			this.Name = "EntityPropertiesForm";
			this.ShowInTaskbar = false;
			this.Text = "Properties";
			this.Load += new System.EventHandler(this.EntityPropertiesForm_Load);
			this._groupBoxProperties.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox _groupBoxProperties;
		private System.Windows.Forms.Button _buttonClose;
		private System.Windows.Forms.DataGridView _dataGridView;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel _statusLabel;
		private System.Windows.Forms.Button _buttonCopy;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnPropertyName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnValue;
	}
}