namespace ProSuite.DdxEditor.Content.ObjectCategoryAttributeConstraints
{
	partial class ObjectCategoryAttributeConstraintsControl
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
			this._dataGridView = new RotatedHeadersDataGridView();
			this._bindingSource = new System.Windows.Forms.BindingSource(this.components);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSource)).BeginInit();
			this.SuspendLayout();
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeColumns = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoGenerateColumns = false;
			this._dataGridView.ColumnHeadersHeight = 80;
			this._dataGridView.DataSource = this._bindingSource;
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(0, 0);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RotationAngle = 0;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this._dataGridView.ShowCellErrors = false;
			this._dataGridView.Size = new System.Drawing.Size(597, 370);
			this._dataGridView.TabIndex = 0;
			this._dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
			// 
			// ObjectCategoryAttributeConstraintsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._dataGridView);
			this.Name = "ObjectCategoryAttributeConstraintsControl";
			this.Size = new System.Drawing.Size(597, 370);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._bindingSource)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private RotatedHeadersDataGridView _dataGridView;
		private System.Windows.Forms.BindingSource _bindingSource;
	}
}
