namespace ProSuite.DdxEditor.Content.Projects
{
	partial class DeleteVerificationsBeforeDateForm
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
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._monthCalendar = new System.Windows.Forms.MonthCalendar();
			this._radioButtonOlderThanMonths = new System.Windows.Forms.RadioButton();
			this._radioButtonOlderThanSpecificDate = new System.Windows.Forms.RadioButton();
			this._labelMonths = new System.Windows.Forms.Label();
			this._groupBox = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this._numericUpDownMonths = new System.Windows.Forms.NumericUpDown();
			this._groupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownMonths)).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(180, 262);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 0;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(99, 262);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 1;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _monthCalendar
			// 
			this._monthCalendar.Location = new System.Drawing.Point(11, 25);
			this._monthCalendar.MaxSelectionCount = 1;
			this._monthCalendar.Name = "_monthCalendar";
			this._monthCalendar.ScrollChange = 1;
			this._monthCalendar.ShowWeekNumbers = true;
			this._monthCalendar.TabIndex = 2;
			this._monthCalendar.DateChanged += new System.Windows.Forms.DateRangeEventHandler(this._monthCalendar_DateChanged);
			// 
			// _radioButtonOlderThanMonths
			// 
			this._radioButtonOlderThanMonths.AutoSize = true;
			this._radioButtonOlderThanMonths.Location = new System.Drawing.Point(23, 31);
			this._radioButtonOlderThanMonths.Name = "_radioButtonOlderThanMonths";
			this._radioButtonOlderThanMonths.Size = new System.Drawing.Size(72, 17);
			this._radioButtonOlderThanMonths.TabIndex = 4;
			this._radioButtonOlderThanMonths.TabStop = true;
			this._radioButtonOlderThanMonths.Text = "older than";
			this._radioButtonOlderThanMonths.UseVisualStyleBackColor = true;
			this._radioButtonOlderThanMonths.CheckedChanged += new System.EventHandler(this._radioButtonOlderThanMonths_CheckedChanged);
			// 
			// _radioButtonOlderThanSpecificDate
			// 
			this._radioButtonOlderThanSpecificDate.AutoSize = true;
			this._radioButtonOlderThanSpecificDate.Location = new System.Drawing.Point(23, 54);
			this._radioButtonOlderThanSpecificDate.Name = "_radioButtonOlderThanSpecificDate";
			this._radioButtonOlderThanSpecificDate.Size = new System.Drawing.Size(138, 17);
			this._radioButtonOlderThanSpecificDate.TabIndex = 5;
			this._radioButtonOlderThanSpecificDate.TabStop = true;
			this._radioButtonOlderThanSpecificDate.Text = "older than specific date:";
			this._radioButtonOlderThanSpecificDate.UseVisualStyleBackColor = true;
			this._radioButtonOlderThanSpecificDate.CheckedChanged += new System.EventHandler(this._radioButtonOlderThanSpecificDate_CheckedChanged);
			// 
			// _labelMonths
			// 
			this._labelMonths.AutoSize = true;
			this._labelMonths.Location = new System.Drawing.Point(164, 33);
			this._labelMonths.Name = "_labelMonths";
			this._labelMonths.Size = new System.Drawing.Size(42, 13);
			this._labelMonths.TabIndex = 7;
			this._labelMonths.Text = "Months";
			// 
			// _groupBox
			// 
			this._groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBox.Controls.Add(this._monthCalendar);
			this._groupBox.Location = new System.Drawing.Point(12, 58);
			this._groupBox.Name = "_groupBox";
			this._groupBox.Size = new System.Drawing.Size(243, 198);
			this._groupBox.TabIndex = 8;
			this._groupBox.TabStop = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(130, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Delete quality verifications";
			// 
			// _numericUpDownMonths
			// 
			this._numericUpDownMonths.Location = new System.Drawing.Point(97, 31);
			this._numericUpDownMonths.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this._numericUpDownMonths.Name = "_numericUpDownMonths";
			this._numericUpDownMonths.Size = new System.Drawing.Size(61, 20);
			this._numericUpDownMonths.TabIndex = 10;
			this._numericUpDownMonths.ValueChanged += new System.EventHandler(this._numericUpDownMonths_ValueChanged);
			// 
			// DeleteVerificationsBeforeDateForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(267, 297);
			this.Controls.Add(this._numericUpDownMonths);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._labelMonths);
			this.Controls.Add(this._radioButtonOlderThanSpecificDate);
			this.Controls.Add(this._radioButtonOlderThanMonths);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.Controls.Add(this._groupBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MinimizeBox = false;
			this.Name = "DeleteVerificationsBeforeDateForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Delete Quality Verifications by Date";
			this._groupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownMonths)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.MonthCalendar _monthCalendar;
		private System.Windows.Forms.RadioButton _radioButtonOlderThanMonths;
		private System.Windows.Forms.RadioButton _radioButtonOlderThanSpecificDate;
		private System.Windows.Forms.Label _labelMonths;
		private System.Windows.Forms.GroupBox _groupBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown _numericUpDownMonths;
	}
}