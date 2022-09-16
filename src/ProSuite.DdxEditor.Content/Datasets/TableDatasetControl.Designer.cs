namespace ProSuite.DdxEditor.Content.Datasets
{
    partial class TableDatasetControl<T>
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
			this._textBoxTable = new System.Windows.Forms.TextBox();
			this._labelTable = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _textBoxTable
			// 
			this._textBoxTable.Location = new System.Drawing.Point(200, 4);
			this._textBoxTable.Name = "_textBoxTable";
			this._textBoxTable.Size = new System.Drawing.Size(380, 20);
			this._textBoxTable.TabIndex = 0;
			// 
			// _labelTable
			// 
			this._labelTable.AutoSize = true;
			this._labelTable.Location = new System.Drawing.Point(157, 7);
			this._labelTable.Name = "_labelTable";
			this._labelTable.Size = new System.Drawing.Size(37, 13);
			this._labelTable.TabIndex = 1;
			this._labelTable.Text = "Table:";
			this._labelTable.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// TableDatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelTable);
			this.Controls.Add(this._textBoxTable);
			this.Name = "TableDatasetControl";
			this.Size = new System.Drawing.Size(600, 30);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxTable;
        private System.Windows.Forms.Label _labelTable;
    }
}
