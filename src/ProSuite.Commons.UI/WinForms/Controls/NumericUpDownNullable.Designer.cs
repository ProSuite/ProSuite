namespace ProSuite.Commons.UI.WinForms.Controls
{
    partial class NumericUpDownNullable
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
			this._numericUpDown = new System.Windows.Forms.NumericUpDown();
			this._checkBoxNull = new System.Windows.Forms.CheckBox();
			this._textBoxNull = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// _numericUpDown
			// 
			this._numericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._numericUpDown.Location = new System.Drawing.Point(0, 0);
			this._numericUpDown.Name = "_numericUpDown";
			this._numericUpDown.Size = new System.Drawing.Size(495, 20);
			this._numericUpDown.TabIndex = 0;
			this._numericUpDown.ValueChanged += new System.EventHandler(this._numericUpDown_ValueChanged);
			this._numericUpDown.KeyDown += new System.Windows.Forms.KeyEventHandler(this._numericUpDown_KeyDown);
			// 
			// _checkBoxNull
			// 
			this._checkBoxNull.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._checkBoxNull.AutoSize = true;
			this._checkBoxNull.Location = new System.Drawing.Point(505, 3);
			this._checkBoxNull.Margin = new System.Windows.Forms.Padding(0);
			this._checkBoxNull.Name = "_checkBoxNull";
			this._checkBoxNull.Size = new System.Drawing.Size(60, 17);
			this._checkBoxNull.TabIndex = 1;
			this._checkBoxNull.Text = "Not set";
			this._checkBoxNull.UseVisualStyleBackColor = true;
			this._checkBoxNull.CheckedChanged += new System.EventHandler(this._checkBoxNull_CheckedChanged);
			// 
			// _textBoxNull
			// 
			this._textBoxNull.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxNull.Location = new System.Drawing.Point(0, 0);
			this._textBoxNull.Name = "_textBoxNull";
			this._textBoxNull.ReadOnly = true;
			this._textBoxNull.Size = new System.Drawing.Size(495, 20);
			this._textBoxNull.TabIndex = 2;
			// 
			// NumericUpDownNullable
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._checkBoxNull);
			this.Controls.Add(this._numericUpDown);
			this.Controls.Add(this._textBoxNull);
			this.Name = "NumericUpDownNullable";
			this.Size = new System.Drawing.Size(565, 20);
			((System.ComponentModel.ISupportInitialize)(this._numericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown _numericUpDown;
        private System.Windows.Forms.CheckBox _checkBoxNull;
        private System.Windows.Forms.TextBox _textBoxNull;
    }
}
