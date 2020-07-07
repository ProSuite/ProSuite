namespace ProSuite.Commons.UI.WinForms.Controls
{
    partial class BooleanCombobox
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
            this._comboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // _comboBox
            // 
            this._comboBox.Dock = System.Windows.Forms.DockStyle.Top;
            this._comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBox.FormattingEnabled = true;
            this._comboBox.Location = new System.Drawing.Point(0, 0);
            this._comboBox.Name = "_comboBox";
            this._comboBox.Size = new System.Drawing.Size(299, 21);
            this._comboBox.TabIndex = 0;
            this._comboBox.SelectedIndexChanged += new System.EventHandler(this._comboBox_SelectedIndexChanged);
            // 
            // BooleanCombobox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._comboBox);
            this.Name = "BooleanCombobox";
            this.Size = new System.Drawing.Size(299, 21);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox _comboBox;
    }
}
