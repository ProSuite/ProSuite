namespace ProSuite.DdxEditor.Content.Models
{
	partial class SpatialReferenceComparisonForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			this._dataGridView = new System.Windows.Forms.DataGridView();
			this._groupBoxFeatureClasses = new System.Windows.Forms.GroupBox();
			this._buttonClose = new System.Windows.Forms.Button();
			this._buttonCopy = new System.Windows.Forms.Button();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this._groupBoxFeatureClasses.SuspendLayout();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeColumns = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this._dataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(5, 18);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this._dataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(410, 403);
			this._dataGridView.TabIndex = 0;
			this._dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
			// 
			// _groupBoxFeatureClasses
			// 
			this._groupBoxFeatureClasses.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxFeatureClasses.Controls.Add(this._dataGridView);
			this._groupBoxFeatureClasses.Location = new System.Drawing.Point(12, 12);
			this._groupBoxFeatureClasses.Name = "_groupBoxFeatureClasses";
			this._groupBoxFeatureClasses.Padding = new System.Windows.Forms.Padding(5);
			this._groupBoxFeatureClasses.Size = new System.Drawing.Size(420, 426);
			this._groupBoxFeatureClasses.TabIndex = 1;
			this._groupBoxFeatureClasses.TabStop = false;
			this._groupBoxFeatureClasses.Text = "Feature class spatial references";
			// 
			// _buttonClose
			// 
			this._buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonClose.Location = new System.Drawing.Point(357, 444);
			this._buttonClose.Name = "_buttonClose";
			this._buttonClose.Size = new System.Drawing.Size(75, 23);
			this._buttonClose.TabIndex = 2;
			this._buttonClose.Text = "Close";
			this._buttonClose.UseVisualStyleBackColor = true;
			this._buttonClose.Click += new System.EventHandler(this._buttonClose_Click);
			// 
			// _buttonCopy
			// 
			this._buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCopy.Location = new System.Drawing.Point(276, 444);
			this._buttonCopy.Name = "_buttonCopy";
			this._buttonCopy.Size = new System.Drawing.Size(75, 23);
			this._buttonCopy.TabIndex = 3;
			this._buttonCopy.Text = "Copy";
			this._buttonCopy.UseVisualStyleBackColor = true;
			this._buttonCopy.Click += new System.EventHandler(this._buttonCopy_Click);
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusLabel});
			this._statusStrip.Location = new System.Drawing.Point(0, 474);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(444, 22);
			this._statusStrip.TabIndex = 4;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _statusLabel
			// 
			this._statusLabel.Name = "_statusLabel";
			this._statusLabel.Size = new System.Drawing.Size(0, 17);
			// 
			// SpatialReferenceComparisonForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonClose;
			this.ClientSize = new System.Drawing.Size(444, 496);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._buttonCopy);
			this.Controls.Add(this._buttonClose);
			this.Controls.Add(this._groupBoxFeatureClasses);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimizeBox = false;
			this.Name = "SpatialReferenceComparisonForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Spatial Reference Comparison";
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this._groupBoxFeatureClasses.ResumeLayout(false);
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView _dataGridView;
		private System.Windows.Forms.GroupBox _groupBoxFeatureClasses;
		private System.Windows.Forms.Button _buttonClose;
		private System.Windows.Forms.Button _buttonCopy;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel _statusLabel;
	}
}
