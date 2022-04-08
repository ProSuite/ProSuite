namespace ProSuite.DdxEditor.Content.Datasets
{
    partial class ObjectDatasetControl<T>
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
			this._labelDisplayFormat = new System.Windows.Forms.Label();
			this._textBoxDisplayFormat = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// _labelDisplayFormat
			// 
			this._labelDisplayFormat.AutoSize = true;
			this._labelDisplayFormat.Location = new System.Drawing.Point(58, 8);
			this._labelDisplayFormat.Name = "_labelDisplayFormat";
			this._labelDisplayFormat.Size = new System.Drawing.Size(136, 13);
			this._labelDisplayFormat.TabIndex = 1;
			this._labelDisplayFormat.Text = "Display Format For Objects:";
			this._labelDisplayFormat.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDisplayFormat
			// 
			this._textBoxDisplayFormat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDisplayFormat.Location = new System.Drawing.Point(200, 5);
			this._textBoxDisplayFormat.Name = "_textBoxDisplayFormat";
			this._textBoxDisplayFormat.Size = new System.Drawing.Size(380, 20);
			this._textBoxDisplayFormat.TabIndex = 3;
			// 
			// ObjectDatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxDisplayFormat);
			this.Controls.Add(this._labelDisplayFormat);
			this.Name = "ObjectDatasetControl";
			this.Size = new System.Drawing.Size(600, 30);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelDisplayFormat;
        private System.Windows.Forms.TextBox _textBoxDisplayFormat;
    }
}
