namespace ProSuite.DdxEditor.Framework.ContentPanel
{
    partial class ContentControl
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
            this._panelContent = new System.Windows.Forms.Panel();
            this._panelHeader = new System.Windows.Forms.Panel();
            this._labelHeaderImage = new System.Windows.Forms.Label();
            this._labelHeader = new System.Windows.Forms.Label();
            this._panelHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // _panelContent
            // 
            this._panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panelContent.Location = new System.Drawing.Point(0, 25);
            this._panelContent.Name = "_panelContent";
            this._panelContent.Size = new System.Drawing.Size(389, 374);
            this._panelContent.TabIndex = 0;
            // 
            // _panelHeader
            // 
            this._panelHeader.BackColor = System.Drawing.SystemColors.Info;
            this._panelHeader.Controls.Add(this._labelHeaderImage);
            this._panelHeader.Controls.Add(this._labelHeader);
            this._panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this._panelHeader.Location = new System.Drawing.Point(0, 0);
            this._panelHeader.Name = "_panelHeader";
            this._panelHeader.Size = new System.Drawing.Size(389, 25);
            this._panelHeader.TabIndex = 1;
            // 
            // _labelHeaderImage
            // 
            this._labelHeaderImage.AutoEllipsis = true;
            this._labelHeaderImage.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._labelHeaderImage.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.DefaultItemImage;
            this._labelHeaderImage.Location = new System.Drawing.Point(6, 3);
            this._labelHeaderImage.Name = "_labelHeaderImage";
            this._labelHeaderImage.Size = new System.Drawing.Size(18, 18);
            this._labelHeaderImage.TabIndex = 0;
            // 
            // _labelHeader
            // 
            this._labelHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._labelHeader.AutoEllipsis = true;
            this._labelHeader.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._labelHeader.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._labelHeader.Location = new System.Drawing.Point(27, 4);
            this._labelHeader.Name = "_labelHeader";
            this._labelHeader.Size = new System.Drawing.Size(359, 18);
            this._labelHeader.TabIndex = 0;
            this._labelHeader.Text = "<header>";
            // 
            // ContentControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._panelContent);
            this.Controls.Add(this._panelHeader);
            this.Name = "ContentControl";
            this.Size = new System.Drawing.Size(389, 399);
            this._panelHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel _panelContent;
        private System.Windows.Forms.Panel _panelHeader;
        private System.Windows.Forms.Label _labelHeader;
        private System.Windows.Forms.Label _labelHeaderImage;
    }
}
