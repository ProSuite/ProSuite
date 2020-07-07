namespace ProSuite.Commons.UI.WinForms.Controls
{
    partial class ObjectReferenceControl
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
			this._textBox = new System.Windows.Forms.TextBox();
			this._buttonFind = new System.Windows.Forms.Button();
			this._buttonClear = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _textBox
			// 
			this._textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBox.Location = new System.Drawing.Point(0, 0);
			this._textBox.Name = "_textBox";
			this._textBox.ReadOnly = true;
			this._textBox.Size = new System.Drawing.Size(352, 20);
			this._textBox.TabIndex = 0;
			// 
			// _buttonFind
			// 
			this._buttonFind.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonFind.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this._buttonFind.Image = global::ProSuite.Commons.UI.Properties.Resources.Find;
			this._buttonFind.Location = new System.Drawing.Point(358, 0);
			this._buttonFind.Name = "_buttonFind";
			this._buttonFind.Size = new System.Drawing.Size(28, 20);
			this._buttonFind.TabIndex = 1;
			this._buttonFind.UseVisualStyleBackColor = true;
			this._buttonFind.Click += new System.EventHandler(this._buttonFind_Click);
			// 
			// _buttonClear
			// 
			this._buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonClear.FlatAppearance.BorderSize = 0;
			this._buttonClear.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this._buttonClear.Image = global::ProSuite.Commons.UI.Properties.Resources.Remove;
			this._buttonClear.Location = new System.Drawing.Point(392, 0);
			this._buttonClear.Name = "_buttonClear";
			this._buttonClear.Size = new System.Drawing.Size(28, 20);
			this._buttonClear.TabIndex = 2;
			this._buttonClear.UseVisualStyleBackColor = true;
			this._buttonClear.Click += new System.EventHandler(this._buttonClear_Click);
			// 
			// ObjectReferenceControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._buttonClear);
			this.Controls.Add(this._buttonFind);
			this.Controls.Add(this._textBox);
			this.Name = "ObjectReferenceControl";
			this.Size = new System.Drawing.Size(420, 20);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBox;
        private System.Windows.Forms.Button _buttonFind;
        private System.Windows.Forms.Button _buttonClear;
    }
}