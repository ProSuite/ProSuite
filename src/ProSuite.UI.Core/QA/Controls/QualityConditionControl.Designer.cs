namespace ProSuite.UI.Core.QA.Controls
{
    partial class QualityConditionControl
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
			this.labelName = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._textBoxTestDescription = new System.Windows.Forms.TextBox();
			this.labelDescription = new System.Windows.Forms.Label();
			this._linkLabelUrl = new System.Windows.Forms.LinkLabel();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxTest = new System.Windows.Forms.TextBox();
			this._labelTest = new System.Windows.Forms.Label();
			this._textBoxCategory = new System.Windows.Forms.TextBox();
			this._labelCategory = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Location = new System.Drawing.Point(28, 6);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(38, 13);
			this.labelName.TabIndex = 0;
			this.labelName.Text = "Name:";
			this.labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxName.Location = new System.Drawing.Point(72, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(368, 20);
			this._textBoxName.TabIndex = 0;
			this._textBoxName.TabStop = false;
			// 
			// _textBoxTestDescription
			// 
			this._textBoxTestDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTestDescription.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxTestDescription.Location = new System.Drawing.Point(72, 81);
			this._textBoxTestDescription.Multiline = true;
			this._textBoxTestDescription.Name = "_textBoxTestDescription";
			this._textBoxTestDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxTestDescription.Size = new System.Drawing.Size(368, 140);
			this._textBoxTestDescription.TabIndex = 2;
			this._textBoxTestDescription.TabStop = false;
			// 
			// labelDescription
			// 
			this.labelDescription.AutoSize = true;
			this.labelDescription.Location = new System.Drawing.Point(3, 84);
			this.labelDescription.Name = "labelDescription";
			this.labelDescription.Size = new System.Drawing.Size(63, 13);
			this.labelDescription.TabIndex = 3;
			this.labelDescription.Text = "Description:";
			this.labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _linkLabelUrl
			// 
			this._linkLabelUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._linkLabelUrl.AutoSize = true;
			this._linkLabelUrl.LinkArea = new System.Windows.Forms.LinkArea(0, 0);
			this._linkLabelUrl.Location = new System.Drawing.Point(69, 224);
			this._linkLabelUrl.Name = "_linkLabelUrl";
			this._linkLabelUrl.Size = new System.Drawing.Size(56, 13);
			this._linkLabelUrl.TabIndex = 3;
			this._linkLabelUrl.Text = "<no URL>";
			this._linkLabelUrl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._linkLabelUrl_LinkClicked);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(34, 224);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "URL:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxTest
			// 
			this._textBoxTest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTest.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxTest.Location = new System.Drawing.Point(72, 55);
			this._textBoxTest.Name = "_textBoxTest";
			this._textBoxTest.ReadOnly = true;
			this._textBoxTest.Size = new System.Drawing.Size(368, 20);
			this._textBoxTest.TabIndex = 1;
			this._textBoxTest.TabStop = false;
			// 
			// _labelTest
			// 
			this._labelTest.AutoSize = true;
			this._labelTest.Location = new System.Drawing.Point(35, 58);
			this._labelTest.Name = "_labelTest";
			this._labelTest.Size = new System.Drawing.Size(31, 13);
			this._labelTest.TabIndex = 3;
			this._labelTest.Text = "Test:";
			this._labelTest.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxCategory
			// 
			this._textBoxCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategory.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxCategory.Location = new System.Drawing.Point(72, 29);
			this._textBoxCategory.Name = "_textBoxCategory";
			this._textBoxCategory.ReadOnly = true;
			this._textBoxCategory.Size = new System.Drawing.Size(368, 20);
			this._textBoxCategory.TabIndex = 1;
			this._textBoxCategory.TabStop = false;
			// 
			// _labelCategory
			// 
			this._labelCategory.AutoSize = true;
			this._labelCategory.Location = new System.Drawing.Point(14, 32);
			this._labelCategory.Name = "_labelCategory";
			this._labelCategory.Size = new System.Drawing.Size(52, 13);
			this._labelCategory.TabIndex = 3;
			this._labelCategory.Text = "Category:";
			this._labelCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// QualityConditionControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._linkLabelUrl);
			this.Controls.Add(this._textBoxTestDescription);
			this.Controls.Add(this._textBoxCategory);
			this.Controls.Add(this._textBoxTest);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._labelCategory);
			this.Controls.Add(this._labelTest);
			this.Controls.Add(this.labelDescription);
			this.Name = "QualityConditionControl";
			this.Size = new System.Drawing.Size(440, 246);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.TextBox _textBoxTestDescription;
        private System.Windows.Forms.Label labelDescription;
		private System.Windows.Forms.LinkLabel _linkLabelUrl;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox _textBoxTest;
		private System.Windows.Forms.Label _labelTest;
		private System.Windows.Forms.TextBox _textBoxCategory;
		private System.Windows.Forms.Label _labelCategory;
    }
}
