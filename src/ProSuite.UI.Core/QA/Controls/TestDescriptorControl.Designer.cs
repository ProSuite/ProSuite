namespace ProSuite.UI.Core.QA.Controls
{
	partial class TestDescriptorControl
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
			this._textBoxTestDescription = new System.Windows.Forms.TextBox();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this.labelName = new System.Windows.Forms.Label();
			this.labelDescription = new System.Windows.Forms.Label();
			this._textBoxSignature = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxImplementation = new System.Windows.Forms.TextBox();
			this._labelImplementation = new System.Windows.Forms.Label();
			this._labelCategories = new System.Windows.Forms.Label();
			this._textBoxCategories = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// _textBoxTestDescription
			// 
			this._textBoxTestDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTestDescription.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxTestDescription.Location = new System.Drawing.Point(90, 107);
			this._textBoxTestDescription.Multiline = true;
			this._textBoxTestDescription.Name = "_textBoxTestDescription";
			this._textBoxTestDescription.ReadOnly = true;
			this._textBoxTestDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxTestDescription.Size = new System.Drawing.Size(345, 126);
			this._textBoxTestDescription.TabIndex = 6;
			this._textBoxTestDescription.TabStop = false;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxName.Location = new System.Drawing.Point(90, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.ReadOnly = true;
			this._textBoxName.Size = new System.Drawing.Size(345, 20);
			this._textBoxName.TabIndex = 5;
			this._textBoxName.TabStop = false;
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Location = new System.Drawing.Point(46, 6);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(38, 13);
			this.labelName.TabIndex = 4;
			this.labelName.Text = "Name:";
			this.labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// labelDescription
			// 
			this.labelDescription.AutoSize = true;
			this.labelDescription.Location = new System.Drawing.Point(21, 110);
			this.labelDescription.Name = "labelDescription";
			this.labelDescription.Size = new System.Drawing.Size(63, 13);
			this.labelDescription.TabIndex = 7;
			this.labelDescription.Text = "Description:";
			this.labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSignature
			// 
			this._textBoxSignature.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxSignature.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxSignature.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxSignature.Location = new System.Drawing.Point(90, 81);
			this._textBoxSignature.Name = "_textBoxSignature";
			this._textBoxSignature.ReadOnly = true;
			this._textBoxSignature.Size = new System.Drawing.Size(345, 20);
			this._textBoxSignature.TabIndex = 5;
			this._textBoxSignature.TabStop = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(29, 84);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Signature:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxImplementation
			// 
			this._textBoxImplementation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxImplementation.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxImplementation.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxImplementation.Location = new System.Drawing.Point(90, 55);
			this._textBoxImplementation.Name = "_textBoxImplementation";
			this._textBoxImplementation.ReadOnly = true;
			this._textBoxImplementation.Size = new System.Drawing.Size(345, 20);
			this._textBoxImplementation.TabIndex = 5;
			this._textBoxImplementation.TabStop = false;
			// 
			// _labelImplementation
			// 
			this._labelImplementation.AutoSize = true;
			this._labelImplementation.Location = new System.Drawing.Point(3, 58);
			this._labelImplementation.Name = "_labelImplementation";
			this._labelImplementation.Size = new System.Drawing.Size(81, 13);
			this._labelImplementation.TabIndex = 4;
			this._labelImplementation.Text = "Implementation:";
			this._labelImplementation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelCategories
			// 
			this._labelCategories.AutoSize = true;
			this._labelCategories.Location = new System.Drawing.Point(1, 32);
			this._labelCategories.Name = "_labelCategories";
			this._labelCategories.Size = new System.Drawing.Size(83, 13);
			this._labelCategories.TabIndex = 4;
			this._labelCategories.Text = "Test categories:";
			this._labelCategories.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxCategories
			// 
			this._textBoxCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxCategories.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxCategories.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxCategories.Location = new System.Drawing.Point(90, 29);
			this._textBoxCategories.Name = "_textBoxCategories";
			this._textBoxCategories.ReadOnly = true;
			this._textBoxCategories.Size = new System.Drawing.Size(345, 20);
			this._textBoxCategories.TabIndex = 5;
			this._textBoxCategories.TabStop = false;
			// 
			// TestDescriptorControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this._textBoxTestDescription);
			this.Controls.Add(this._textBoxCategories);
			this.Controls.Add(this._textBoxImplementation);
			this.Controls.Add(this._textBoxSignature);
			this.Controls.Add(this._labelCategories);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelImplementation);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.labelDescription);
			this.Name = "TestDescriptorControl";
			this.Size = new System.Drawing.Size(435, 233);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox _textBoxTestDescription;
		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.Label labelDescription;
		private System.Windows.Forms.TextBox _textBoxSignature;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox _textBoxImplementation;
		private System.Windows.Forms.Label _labelImplementation;
		private System.Windows.Forms.Label _labelCategories;
		private System.Windows.Forms.TextBox _textBoxCategories;
	}
}
