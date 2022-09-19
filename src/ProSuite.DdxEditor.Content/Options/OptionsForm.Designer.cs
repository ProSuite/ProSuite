namespace ProSuite.DdxEditor.Content.Options
{
	partial class OptionsForm
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
			this._checkBoxIncludeDeletedModelElements = new System.Windows.Forms.CheckBox();
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets = new System.Windows.Forms.CheckBox();
			this._buttonOK = new System.Windows.Forms.Button();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._checkBoxListQualityConditionsWithDataset = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// _checkBoxIncludeDeletedModelElements
			// 
			this._checkBoxIncludeDeletedModelElements.AutoSize = true;
			this._checkBoxIncludeDeletedModelElements.Location = new System.Drawing.Point(12, 21);
			this._checkBoxIncludeDeletedModelElements.Name = "_checkBoxIncludeDeletedModelElements";
			this._checkBoxIncludeDeletedModelElements.Size = new System.Drawing.Size(167, 17);
			this._checkBoxIncludeDeletedModelElements.TabIndex = 0;
			this._checkBoxIncludeDeletedModelElements.Text = "Show deleted model elements";
			this._checkBoxIncludeDeletedModelElements.UseVisualStyleBackColor = true;
			// 
			// _checkBoxIncludeQualityConditionsBasedOnDeletedDatasets
			// 
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.AutoSize = true;
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Location = new System.Drawing.Point(12, 44);
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Name = "_checkBoxIncludeQualityConditionsBasedOnDeletedDatasets";
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Size = new System.Drawing.Size(304, 17);
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.TabIndex = 0;
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.Text = "Show quality conditions that are based on deleted datasets";
			this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets.UseVisualStyleBackColor = true;
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(184, 99);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 1;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(265, 99);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 2;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _checkBoxListQualityConditionsWithDataset
			// 
			this._checkBoxListQualityConditionsWithDataset.AutoSize = true;
			this._checkBoxListQualityConditionsWithDataset.Location = new System.Drawing.Point(12, 67);
			this._checkBoxListQualityConditionsWithDataset.Name = "_checkBoxListQualityConditionsWithDataset";
			this._checkBoxListQualityConditionsWithDataset.Size = new System.Drawing.Size(240, 17);
			this._checkBoxListQualityConditionsWithDataset.TabIndex = 0;
			this._checkBoxListQualityConditionsWithDataset.Text = "List quality conditions with dataset information";
			this._checkBoxListQualityConditionsWithDataset.UseVisualStyleBackColor = true;
			// 
			// OptionsForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(352, 134);
			this.Controls.Add(this._buttonCancel);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._checkBoxListQualityConditionsWithDataset);
			this.Controls.Add(this._checkBoxIncludeQualityConditionsBasedOnDeletedDatasets);
			this.Controls.Add(this._checkBoxIncludeDeletedModelElements);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OptionsForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox _checkBoxIncludeDeletedModelElements;
		private System.Windows.Forms.CheckBox _checkBoxIncludeQualityConditionsBasedOnDeletedDatasets;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.CheckBox _checkBoxListQualityConditionsWithDataset;
	}
}