namespace ProSuite.DdxEditor.Content.QA.QCon
{
	partial class FindQualityConditionByNameForm
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
			this._textBoxName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this._textBoxName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this._textBoxName.Location = new System.Drawing.Point(135, 12);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(298, 20);
			this._textBoxName.TabIndex = 0;
			this._textBoxName.TextChanged += new System.EventHandler(this._textBoxName_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(117, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Quality condition name:";
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(358, 38);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 2;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(277, 38);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 3;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// FindQualityConditionByNameForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(445, 68);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._textBoxName);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximumSize = new System.Drawing.Size(2000, 125);
			this.MinimumSize = new System.Drawing.Size(300, 125);
			this.Name = "FindQualityConditionByNameForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Find Quality Condition by Name";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
	}
}
