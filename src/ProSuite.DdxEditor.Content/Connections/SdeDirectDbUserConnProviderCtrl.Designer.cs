namespace ProSuite.DdxEditor.Content.Connections
{
    partial class SdeDirectDbUserConnProviderCtrl<T>
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
			this._textBoxPlainTextPassword = new System.Windows.Forms.TextBox();
			this._labelPlainTextPassword = new System.Windows.Forms.Label();
			this._textBoxUserName = new System.Windows.Forms.TextBox();
			this._labelUserName = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _textBoxPlainTextPassword
			// 
			this._textBoxPlainTextPassword.Location = new System.Drawing.Point(100, 33);
			this._textBoxPlainTextPassword.Name = "_textBoxPlainTextPassword";
			this._textBoxPlainTextPassword.Size = new System.Drawing.Size(298, 20);
			this._textBoxPlainTextPassword.TabIndex = 1;
			this._textBoxPlainTextPassword.UseSystemPasswordChar = true;
			// 
			// _labelPlainTextPassword
			// 
			this._labelPlainTextPassword.AutoSize = true;
			this._labelPlainTextPassword.Location = new System.Drawing.Point(38, 36);
			this._labelPlainTextPassword.Name = "_labelPlainTextPassword";
			this._labelPlainTextPassword.Size = new System.Drawing.Size(56, 13);
			this._labelPlainTextPassword.TabIndex = 8;
			this._labelPlainTextPassword.Text = "Password:";
			this._labelPlainTextPassword.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxUserName
			// 
			this._textBoxUserName.Location = new System.Drawing.Point(100, 6);
			this._textBoxUserName.Name = "_textBoxUserName";
			this._textBoxUserName.Size = new System.Drawing.Size(298, 20);
			this._textBoxUserName.TabIndex = 0;
			// 
			// _labelUserName
			// 
			this._labelUserName.AutoSize = true;
			this._labelUserName.Location = new System.Drawing.Point(33, 9);
			this._labelUserName.Name = "_labelUserName";
			this._labelUserName.Size = new System.Drawing.Size(61, 13);
			this._labelUserName.TabIndex = 6;
			this._labelUserName.Text = "User name:";
			this._labelUserName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// SdeDirectDbUserConnProviderCtrl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxPlainTextPassword);
			this.Controls.Add(this._labelPlainTextPassword);
			this.Controls.Add(this._textBoxUserName);
			this.Controls.Add(this._labelUserName);
			this.Name = "SdeDirectDbUserConnProviderCtrl";
			this.Size = new System.Drawing.Size(600, 58);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxPlainTextPassword;
        private System.Windows.Forms.Label _labelPlainTextPassword;
        private System.Windows.Forms.TextBox _textBoxUserName;
        private System.Windows.Forms.Label _labelUserName;
    }
}
