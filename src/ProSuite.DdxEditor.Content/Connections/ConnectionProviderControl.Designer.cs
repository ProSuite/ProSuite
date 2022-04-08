namespace ProSuite.DdxEditor.Content.Connections
{
    partial class ConnectionProviderControl<T>
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
			this._buttonTest = new System.Windows.Forms.Button();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxConnectionType = new System.Windows.Forms.TextBox();
			this._labelType = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(100, 38);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(480, 20);
			this._textBoxName.TabIndex = 1;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(56, 41);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 2;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _buttonTest
			// 
			this._buttonTest.Location = new System.Drawing.Point(100, 140);
			this._buttonTest.Name = "_buttonTest";
			this._buttonTest.Size = new System.Drawing.Size(128, 23);
			this._buttonTest.TabIndex = 3;
			this._buttonTest.Text = "Test Connection";
			this._buttonTest.UseVisualStyleBackColor = true;
			this._buttonTest.Click += new System.EventHandler(this._buttonTest_Click);
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(100, 64);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.Size = new System.Drawing.Size(480, 70);
			this._textBoxDescription.TabIndex = 2;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(31, 67);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 5;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxConnectionType
			// 
			this._textBoxConnectionType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxConnectionType.Location = new System.Drawing.Point(100, 12);
			this._textBoxConnectionType.Name = "_textBoxConnectionType";
			this._textBoxConnectionType.ReadOnly = true;
			this._textBoxConnectionType.Size = new System.Drawing.Size(480, 20);
			this._textBoxConnectionType.TabIndex = 0;
			this._textBoxConnectionType.TabStop = false;
			// 
			// _labelType
			// 
			this._labelType.AutoSize = true;
			this._labelType.Location = new System.Drawing.Point(7, 15);
			this._labelType.Name = "_labelType";
			this._labelType.Size = new System.Drawing.Size(87, 13);
			this._labelType.TabIndex = 2;
			this._labelType.Text = "Connection type:";
			this._labelType.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// ConnectionProviderControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._buttonTest);
			this.Controls.Add(this._textBoxConnectionType);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelType);
			this.Controls.Add(this._labelName);
			this.Name = "ConnectionProviderControl";
			this.Size = new System.Drawing.Size(600, 172);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.Button _buttonTest;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.TextBox _textBoxConnectionType;
		private System.Windows.Forms.Label _labelType;
    }
}
