namespace ProSuite.DdxEditor.Content.DatasetCategories
{
    partial class DatasetCategoryControl
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
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxAbbreviation = new System.Windows.Forms.TextBox();
			this._labelAbbreviation = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(37, 8);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(81, 5);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(499, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(81, 31);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.Size = new System.Drawing.Size(499, 95);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(12, 34);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 2;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxAbbreviation
			// 
			this._textBoxAbbreviation.Location = new System.Drawing.Point(81, 132);
			this._textBoxAbbreviation.Name = "_textBoxAbbreviation";
			this._textBoxAbbreviation.Size = new System.Drawing.Size(141, 20);
			this._textBoxAbbreviation.TabIndex = 2;
			// 
			// _labelAbbreviation
			// 
			this._labelAbbreviation.AutoSize = true;
			this._labelAbbreviation.Location = new System.Drawing.Point(6, 135);
			this._labelAbbreviation.Name = "_labelAbbreviation";
			this._labelAbbreviation.Size = new System.Drawing.Size(69, 13);
			this._labelAbbreviation.TabIndex = 4;
			this._labelAbbreviation.Text = "Abbreviation:";
			this._labelAbbreviation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// DatasetCategoryControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxAbbreviation);
			this.Controls.Add(this._labelAbbreviation);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelName);
			this.Name = "DatasetCategoryControl";
			this.Size = new System.Drawing.Size(600, 157);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxAbbreviation;
        private System.Windows.Forms.Label _labelAbbreviation;
    }
}
