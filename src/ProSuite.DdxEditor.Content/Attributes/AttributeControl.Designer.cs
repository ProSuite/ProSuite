namespace ProSuite.DdxEditor.Content.Attributes
{
    partial class AttributeControl<T>
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
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxFieldType = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this._pictureBoxFieldType = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this._pictureBoxFieldType)).BeginInit();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(108, 6);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.ReadOnly = true;
			this._textBoxName.Size = new System.Drawing.Size(470, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(64, 9);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 1;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(108, 58);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.Size = new System.Drawing.Size(470, 65);
			this._textBoxDescription.TabIndex = 2;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(39, 61);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 41;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxFieldType
			// 
			this._textBoxFieldType.Location = new System.Drawing.Point(108, 32);
			this._textBoxFieldType.Name = "_textBoxFieldType";
			this._textBoxFieldType.ReadOnly = true;
			this._textBoxFieldType.Size = new System.Drawing.Size(118, 20);
			this._textBoxFieldType.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(43, 35);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(59, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Field Type:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _pictureBoxFieldType
			// 
			this._pictureBoxFieldType.Location = new System.Drawing.Point(232, 34);
			this._pictureBoxFieldType.Name = "_pictureBoxFieldType";
			this._pictureBoxFieldType.Size = new System.Drawing.Size(44, 20);
			this._pictureBoxFieldType.TabIndex = 42;
			this._pictureBoxFieldType.TabStop = false;
			// 
			// AttributeControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._pictureBoxFieldType);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxFieldType);
			this.Controls.Add(this._textBoxName);
			this.Name = "AttributeControl";
			this.Size = new System.Drawing.Size(600, 133);
			((System.ComponentModel.ISupportInitialize)(this._pictureBoxFieldType)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.TextBox _textBoxFieldType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox _pictureBoxFieldType;
    }
}