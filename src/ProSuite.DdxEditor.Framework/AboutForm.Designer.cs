namespace ProSuite.DdxEditor.Framework
{
    partial class AboutForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._buttonClose = new System.Windows.Forms.Button();
            this._labelHeader = new System.Windows.Forms.Label();
            this._textBoxInfo = new System.Windows.Forms.TextBox();
            this._buttonCopy = new System.Windows.Forms.Button();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonClose
            // 
            this._buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonClose.Location = new System.Drawing.Point(472, 285);
            this._buttonClose.Name = "_buttonClose";
            this._buttonClose.Size = new System.Drawing.Size(75, 23);
            this._buttonClose.TabIndex = 0;
            this._buttonClose.Text = "Close";
            this._buttonClose.UseVisualStyleBackColor = true;
            this._buttonClose.Click += new System.EventHandler(this._buttonClose_Click);
            // 
            // _labelHeader
            // 
            this._labelHeader.AutoSize = true;
            this._labelHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._labelHeader.Location = new System.Drawing.Point(7, 9);
            this._labelHeader.Name = "_labelHeader";
            this._labelHeader.Size = new System.Drawing.Size(75, 20);
            this._labelHeader.TabIndex = 2;
            this._labelHeader.Text = "<Client>";
            // 
            // textBoxInfo
            // 
            this._textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxInfo.BackColor = System.Drawing.SystemColors.Info;
            this._textBoxInfo.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._textBoxInfo.Location = new System.Drawing.Point(12, 38);
            this._textBoxInfo.Multiline = true;
            this._textBoxInfo.Name = "_textBoxInfo";
            this._textBoxInfo.ReadOnly = true;
            this._textBoxInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._textBoxInfo.Size = new System.Drawing.Size(535, 241);
            this._textBoxInfo.TabIndex = 3;
            // 
            // buttonCopy
            // 
            this._buttonCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCopy.Location = new System.Drawing.Point(400, 285);
            this._buttonCopy.Name = "_buttonCopy";
            this._buttonCopy.Size = new System.Drawing.Size(66, 23);
            this._buttonCopy.TabIndex = 4;
            this._buttonCopy.Text = "Copy";
            this._buttonCopy.UseVisualStyleBackColor = true;
            this._buttonCopy.Click += new System.EventHandler(this._buttonCopy_Click);
            // 
            // statusStrip
            // 
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
            this._statusStrip.Location = new System.Drawing.Point(0, 317);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(559, 22);
            this._statusStrip.TabIndex = 5;
            this._statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this._toolStripStatusLabel.Name = "toolStripStatusLabel";
            this._toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // AboutForm
            // 
            this.AcceptButton = this._buttonClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._buttonClose;
            this.ClientSize = new System.Drawing.Size(559, 339);
            this.Controls.Add(this._statusStrip);
            this.Controls.Add(this._buttonCopy);
            this.Controls.Add(this._textBoxInfo);
            this.Controls.Add(this._labelHeader);
            this.Controls.Add(this._buttonClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(550, 284);
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About <Client>";
            this.Load += new System.EventHandler(this.AboutBoxView_Load);
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _buttonClose;
        private System.Windows.Forms.Label _labelHeader;
        private System.Windows.Forms.TextBox _textBoxInfo;
        private System.Windows.Forms.Button _buttonCopy;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
    }
}
