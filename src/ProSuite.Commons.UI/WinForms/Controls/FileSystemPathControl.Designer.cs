namespace ProSuite.Commons.UI.WinForms.Controls
{
    partial class FileSystemPathControl
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
			this.TextBox = new System.Windows.Forms.TextBox();
			this._buttonBrowse = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _textBox
			// 
			this.TextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TextBox.Location = new System.Drawing.Point(0, 3);
			this.TextBox.Name = "TextBox";
			this.TextBox.Size = new System.Drawing.Size(332, 20);
			this.TextBox.TabIndex = 2;
			this.TextBox.TextChanged += new System.EventHandler(this._textBox_TextChanged);
			this.TextBox.Leave += new System.EventHandler(this._textBox_Leave);
			// 
			// _buttonBrowse
			// 
			this._buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this._buttonBrowse.Image = global::ProSuite.Commons.UI.Properties.Resources.Browse;
			this._buttonBrowse.Location = new System.Drawing.Point(338, 3);
			this._buttonBrowse.Name = "_buttonBrowse";
			this._buttonBrowse.Size = new System.Drawing.Size(28, 20);
			this._buttonBrowse.TabIndex = 3;
			this._buttonBrowse.UseVisualStyleBackColor = true;
			this._buttonBrowse.Click += new System.EventHandler(this._buttonBrowse_Click);
			// 
			// FileSystemPathControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._buttonBrowse);
			this.Controls.Add(this.TextBox);
			this.Name = "FileSystemPathControl";
			this.Size = new System.Drawing.Size(367, 26);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _buttonBrowse;
    }
}