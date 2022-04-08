using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
    partial class ObjectCategoryControl<E>
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
			this._labelName = new System.Windows.Forms.Label();
			this._labelMinimumSegmentLength = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this._textBoxSubtypeCode = new System.Windows.Forms.TextBox();
			this._labelSubtypeCode = new System.Windows.Forms.Label();
			this._textBoxDatasetMinimumSegmentLength = new System.Windows.Forms.TextBox();
			this._numericUpDownNullableMinimumSegmentLength = new global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable();
			this._labelAllowOrphanDeletion = new System.Windows.Forms.Label();
			this._comboBoxAllowOrphanDeletion = new global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(113, 6);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelMinimumSegmentLength
			// 
			this._labelMinimumSegmentLength.AutoSize = true;
			this._labelMinimumSegmentLength.Location = new System.Drawing.Point(19, 189);
			this._labelMinimumSegmentLength.Name = "_labelMinimumSegmentLength";
			this._labelMinimumSegmentLength.Size = new System.Drawing.Size(132, 13);
			this._labelMinimumSegmentLength.TabIndex = 2;
			this._labelMinimumSegmentLength.Text = "Minimum Segment Length:";
			this._labelMinimumSegmentLength.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(157, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(497, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(157, 55);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.Size = new System.Drawing.Size(497, 98);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(88, 58);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 0;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(333, 189);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(84, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Dataset Default:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxSubtypeCode
			// 
			this._textBoxSubtypeCode.Location = new System.Drawing.Point(157, 29);
			this._textBoxSubtypeCode.Name = "_textBoxSubtypeCode";
			this._textBoxSubtypeCode.ReadOnly = true;
			this._textBoxSubtypeCode.Size = new System.Drawing.Size(123, 20);
			this._textBoxSubtypeCode.TabIndex = 12;
			this._textBoxSubtypeCode.TabStop = false;
			// 
			// _labelSubtypeCode
			// 
			this._labelSubtypeCode.AutoSize = true;
			this._labelSubtypeCode.Location = new System.Drawing.Point(74, 32);
			this._labelSubtypeCode.Name = "_labelSubtypeCode";
			this._labelSubtypeCode.Size = new System.Drawing.Size(77, 13);
			this._labelSubtypeCode.TabIndex = 11;
			this._labelSubtypeCode.Text = "Subtype Code:";
			this._labelSubtypeCode.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDatasetMinimumSegmentLength
			// 
			this._textBoxDatasetMinimumSegmentLength.Location = new System.Drawing.Point(423, 186);
			this._textBoxDatasetMinimumSegmentLength.Name = "_textBoxDatasetMinimumSegmentLength";
			this._textBoxDatasetMinimumSegmentLength.ReadOnly = true;
			this._textBoxDatasetMinimumSegmentLength.Size = new System.Drawing.Size(71, 20);
			this._textBoxDatasetMinimumSegmentLength.TabIndex = 14;
			this._textBoxDatasetMinimumSegmentLength.TabStop = false;
			// 
			// _numericUpDownNullableMinimumSegmentLength
			// 
			this._numericUpDownNullableMinimumSegmentLength.DecimalPlaces = 2;
			this._numericUpDownNullableMinimumSegmentLength.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this._numericUpDownNullableMinimumSegmentLength.Location = new System.Drawing.Point(157, 186);
			this._numericUpDownNullableMinimumSegmentLength.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this._numericUpDownNullableMinimumSegmentLength.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
			this._numericUpDownNullableMinimumSegmentLength.Name = "_numericUpDownNullableMinimumSegmentLength";
			this._numericUpDownNullableMinimumSegmentLength.Size = new System.Drawing.Size(157, 20);
			this._numericUpDownNullableMinimumSegmentLength.TabIndex = 3;
			this._numericUpDownNullableMinimumSegmentLength.ThousandsSeparator = false;
			this._numericUpDownNullableMinimumSegmentLength.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
			// 
			// _labelAllowOrphanDeletion
			// 
			this._labelAllowOrphanDeletion.AutoSize = true;
			this._labelAllowOrphanDeletion.Location = new System.Drawing.Point(36, 162);
			this._labelAllowOrphanDeletion.Name = "_labelAllowOrphanDeletion";
			this._labelAllowOrphanDeletion.Size = new System.Drawing.Size(115, 13);
			this._labelAllowOrphanDeletion.TabIndex = 16;
			this._labelAllowOrphanDeletion.Text = "Allow Orphan Deletion:";
			this._labelAllowOrphanDeletion.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _comboBoxAllowOrphanDeletion
			// 
			this._comboBoxAllowOrphanDeletion.FalseText = "No";
			this._comboBoxAllowOrphanDeletion.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._comboBoxAllowOrphanDeletion.Location = new System.Drawing.Point(157, 159);
			this._comboBoxAllowOrphanDeletion.Name = "_comboBoxAllowOrphanDeletion";
			this._comboBoxAllowOrphanDeletion.Size = new System.Drawing.Size(90, 21);
			this._comboBoxAllowOrphanDeletion.TabIndex = 2;
			this._comboBoxAllowOrphanDeletion.TrueText = "Yes";
			this._comboBoxAllowOrphanDeletion.Value = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(253, 162);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(354, 13);
			this.label2.TabIndex = 18;
			this.label2.Text = "Orphan deletion currently implemented for unconnected network junctions";
			// 
			// ObjectCategoryControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label2);
			this.Controls.Add(this._comboBoxAllowOrphanDeletion);
			this.Controls.Add(this._labelAllowOrphanDeletion);
			this.Controls.Add(this._numericUpDownNullableMinimumSegmentLength);
			this.Controls.Add(this._textBoxDatasetMinimumSegmentLength);
			this.Controls.Add(this._textBoxSubtypeCode);
			this.Controls.Add(this._labelSubtypeCode);
			this.Controls.Add(this.label1);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelMinimumSegmentLength);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._labelName);
			this.Name = "ObjectCategoryControl";
			this.Size = new System.Drawing.Size(677, 215);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.Label _labelMinimumSegmentLength;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _textBoxSubtypeCode;
        private System.Windows.Forms.Label _labelSubtypeCode;
        private System.Windows.Forms.TextBox _textBoxDatasetMinimumSegmentLength;
        private global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable _numericUpDownNullableMinimumSegmentLength;
        private System.Windows.Forms.Label _labelAllowOrphanDeletion;
        private BooleanCombobox _comboBoxAllowOrphanDeletion;
        private System.Windows.Forms.Label label2;
    }
}
