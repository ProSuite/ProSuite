namespace ProSuite.DdxEditor.Content.QA.Categories
{
	partial class DataQualityCategoryControl
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
			this._textBoxAbbreviation = new System.Windows.Forms.TextBox();
			this._labelAbbreviation = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._labelListOrder = new System.Windows.Forms.Label();
			this._numericUpDownListOrder = new System.Windows.Forms.NumericUpDown();
			this._objectReferenceControlDefaultDataModel = new ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this._labelDefaultDataModel = new System.Windows.Forms.Label();
			this._groupBoxAllowedContent = new System.Windows.Forms.GroupBox();
			this._checkBoxCanContainSubCategories = new System.Windows.Forms.CheckBox();
			this._checkBoxCanContainQualitySpecifications = new System.Windows.Forms.CheckBox();
			this._checkBoxCanContainQualityConditions = new System.Windows.Forms.CheckBox();
			this._textBoxUuid = new System.Windows.Forms.TextBox();
			this._labelUuid = new System.Windows.Forms.Label();
			this._labelParentCategory = new System.Windows.Forms.Label();
			this._textBoxParentCategory = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownListOrder)).BeginInit();
			this._groupBoxAllowedContent.SuspendLayout();
			this.SuspendLayout();
			// 
			// _textBoxAbbreviation
			// 
			this._textBoxAbbreviation.Location = new System.Drawing.Point(148, 39);
			this._textBoxAbbreviation.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxAbbreviation.Name = "_textBoxAbbreviation";
			this._textBoxAbbreviation.Size = new System.Drawing.Size(139, 23);
			this._textBoxAbbreviation.TabIndex = 2;
			// 
			// _labelAbbreviation
			// 
			this._labelAbbreviation.AutoSize = true;
			this._labelAbbreviation.Location = new System.Drawing.Point(61, 43);
			this._labelAbbreviation.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelAbbreviation.Name = "_labelAbbreviation";
			this._labelAbbreviation.Size = new System.Drawing.Size(78, 15);
			this._labelAbbreviation.TabIndex = 66;
			this._labelAbbreviation.Text = "Abbreviation:";
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(148, 69);
			this._textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(667, 82);
			this._textBoxDescription.TabIndex = 4;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(68, 73);
			this._labelDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(70, 15);
			this._labelDescription.TabIndex = 65;
			this._labelDescription.Text = "Description:";
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(148, 9);
			this._textBoxName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(461, 23);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(97, 13);
			this._labelName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(42, 15);
			this._labelName.TabIndex = 64;
			this._labelName.Text = "Name:";
			// 
			// _labelListOrder
			// 
			this._labelListOrder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelListOrder.AutoSize = true;
			this._labelListOrder.Location = new System.Drawing.Point(629, 13);
			this._labelListOrder.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelListOrder.Name = "_labelListOrder";
			this._labelListOrder.Size = new System.Drawing.Size(102, 15);
			this._labelListOrder.TabIndex = 68;
			this._labelListOrder.Text = "Display List Order:";
			this._labelListOrder.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _numericUpDownListOrder
			// 
			this._numericUpDownListOrder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._numericUpDownListOrder.Location = new System.Drawing.Point(743, 10);
			this._numericUpDownListOrder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._numericUpDownListOrder.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this._numericUpDownListOrder.Name = "_numericUpDownListOrder";
			this._numericUpDownListOrder.Size = new System.Drawing.Size(72, 23);
			this._numericUpDownListOrder.TabIndex = 1;
			// 
			// _objectReferenceControlDefaultDataModel
			// 
			this._objectReferenceControlDefaultDataModel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlDefaultDataModel.DataSource = null;
			this._objectReferenceControlDefaultDataModel.DisplayMember = null;
			this._objectReferenceControlDefaultDataModel.FindObjectDelegate = null;
			this._objectReferenceControlDefaultDataModel.FormatTextDelegate = null;
			this._objectReferenceControlDefaultDataModel.Location = new System.Drawing.Point(148, 189);
			this._objectReferenceControlDefaultDataModel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._objectReferenceControlDefaultDataModel.Name = "_objectReferenceControlDefaultDataModel";
			this._objectReferenceControlDefaultDataModel.ReadOnly = false;
			this._objectReferenceControlDefaultDataModel.Size = new System.Drawing.Size(667, 23);
			this._objectReferenceControlDefaultDataModel.TabIndex = 6;
			// 
			// _labelDefaultDataModel
			// 
			this._labelDefaultDataModel.AutoSize = true;
			this._labelDefaultDataModel.Location = new System.Drawing.Point(22, 194);
			this._labelDefaultDataModel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelDefaultDataModel.Name = "_labelDefaultDataModel";
			this._labelDefaultDataModel.Size = new System.Drawing.Size(111, 15);
			this._labelDefaultDataModel.TabIndex = 70;
			this._labelDefaultDataModel.Text = "Default data model:";
			this._labelDefaultDataModel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _groupBoxAllowedContent
			// 
			this._groupBoxAllowedContent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxAllowedContent.Controls.Add(this._checkBoxCanContainSubCategories);
			this._groupBoxAllowedContent.Controls.Add(this._checkBoxCanContainQualitySpecifications);
			this._groupBoxAllowedContent.Controls.Add(this._checkBoxCanContainQualityConditions);
			this._groupBoxAllowedContent.Location = new System.Drawing.Point(148, 219);
			this._groupBoxAllowedContent.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxAllowedContent.Name = "_groupBoxAllowedContent";
			this._groupBoxAllowedContent.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxAllowedContent.Size = new System.Drawing.Size(667, 126);
			this._groupBoxAllowedContent.TabIndex = 7;
			this._groupBoxAllowedContent.TabStop = false;
			this._groupBoxAllowedContent.Text = "Allowed content for category";
			// 
			// _checkBoxCanContainSubCategories
			// 
			this._checkBoxCanContainSubCategories.AutoSize = true;
			this._checkBoxCanContainSubCategories.Location = new System.Drawing.Point(27, 87);
			this._checkBoxCanContainSubCategories.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxCanContainSubCategories.Name = "_checkBoxCanContainSubCategories";
			this._checkBoxCanContainSubCategories.Size = new System.Drawing.Size(105, 19);
			this._checkBoxCanContainSubCategories.TabIndex = 2;
			this._checkBoxCanContainSubCategories.Text = "Sub-categories";
			this._checkBoxCanContainSubCategories.UseVisualStyleBackColor = true;
			// 
			// _checkBoxCanContainQualitySpecifications
			// 
			this._checkBoxCanContainQualitySpecifications.AutoSize = true;
			this._checkBoxCanContainQualitySpecifications.Location = new System.Drawing.Point(27, 33);
			this._checkBoxCanContainQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxCanContainQualitySpecifications.Name = "_checkBoxCanContainQualitySpecifications";
			this._checkBoxCanContainQualitySpecifications.Size = new System.Drawing.Size(140, 19);
			this._checkBoxCanContainQualitySpecifications.TabIndex = 1;
			this._checkBoxCanContainQualitySpecifications.Text = "Quality Specifications";
			this._checkBoxCanContainQualitySpecifications.UseVisualStyleBackColor = true;
			// 
			// _checkBoxCanContainQualityConditions
			// 
			this._checkBoxCanContainQualityConditions.AutoSize = true;
			this._checkBoxCanContainQualityConditions.Location = new System.Drawing.Point(27, 60);
			this._checkBoxCanContainQualityConditions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxCanContainQualityConditions.Name = "_checkBoxCanContainQualityConditions";
			this._checkBoxCanContainQualityConditions.Size = new System.Drawing.Size(439, 19);
			this._checkBoxCanContainQualityConditions.TabIndex = 0;
			this._checkBoxCanContainQualityConditions.Text = "Quality Conditions, Transformer Configurations and Issue Filter Configurations";
			this._checkBoxCanContainQualityConditions.UseVisualStyleBackColor = true;
			// 
			// _textBoxUuid
			// 
			this._textBoxUuid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxUuid.Location = new System.Drawing.Point(340, 39);
			this._textBoxUuid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxUuid.Name = "_textBoxUuid";
			this._textBoxUuid.ReadOnly = true;
			this._textBoxUuid.Size = new System.Drawing.Size(475, 23);
			this._textBoxUuid.TabIndex = 3;
			// 
			// _labelUuid
			// 
			this._labelUuid.AutoSize = true;
			this._labelUuid.Location = new System.Drawing.Point(295, 43);
			this._labelUuid.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelUuid.Name = "_labelUuid";
			this._labelUuid.Size = new System.Drawing.Size(35, 15);
			this._labelUuid.TabIndex = 66;
			this._labelUuid.Text = "Uuid:";
			// 
			// _labelParentCategory
			// 
			this._labelParentCategory.AutoSize = true;
			this._labelParentCategory.Location = new System.Drawing.Point(42, 163);
			this._labelParentCategory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelParentCategory.Name = "_labelParentCategory";
			this._labelParentCategory.Size = new System.Drawing.Size(93, 15);
			this._labelParentCategory.TabIndex = 73;
			this._labelParentCategory.Text = "Parent category:";
			this._labelParentCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxParentCategory
			// 
			this._textBoxParentCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxParentCategory.Location = new System.Drawing.Point(148, 159);
			this._textBoxParentCategory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._textBoxParentCategory.Name = "_textBoxParentCategory";
			this._textBoxParentCategory.ReadOnly = true;
			this._textBoxParentCategory.Size = new System.Drawing.Size(667, 23);
			this._textBoxParentCategory.TabIndex = 5;
			// 
			// DataQualityCategoryControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelParentCategory);
			this.Controls.Add(this._textBoxParentCategory);
			this.Controls.Add(this._groupBoxAllowedContent);
			this.Controls.Add(this._objectReferenceControlDefaultDataModel);
			this.Controls.Add(this._labelDefaultDataModel);
			this.Controls.Add(this._labelListOrder);
			this.Controls.Add(this._numericUpDownListOrder);
			this.Controls.Add(this._textBoxUuid);
			this.Controls.Add(this._textBoxAbbreviation);
			this.Controls.Add(this._labelUuid);
			this.Controls.Add(this._labelAbbreviation);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelName);
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "DataQualityCategoryControl";
			this.Size = new System.Drawing.Size(844, 353);
			((System.ComponentModel.ISupportInitialize)(this._numericUpDownListOrder)).EndInit();
			this._groupBoxAllowedContent.ResumeLayout(false);
			this._groupBoxAllowedContent.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox _textBoxAbbreviation;
		private System.Windows.Forms.Label _labelAbbreviation;
		private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
		private System.Windows.Forms.TextBox _textBoxName;
		private System.Windows.Forms.Label _labelName;
		private System.Windows.Forms.Label _labelListOrder;
		private System.Windows.Forms.NumericUpDown _numericUpDownListOrder;
		private global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl _objectReferenceControlDefaultDataModel;
		private System.Windows.Forms.Label _labelDefaultDataModel;
		private System.Windows.Forms.GroupBox _groupBoxAllowedContent;
		private System.Windows.Forms.CheckBox _checkBoxCanContainQualityConditions;
		private System.Windows.Forms.CheckBox _checkBoxCanContainSubCategories;
		private System.Windows.Forms.CheckBox _checkBoxCanContainQualitySpecifications;
		private System.Windows.Forms.TextBox _textBoxUuid;
		private System.Windows.Forms.Label _labelUuid;
		private System.Windows.Forms.Label _labelParentCategory;
		private System.Windows.Forms.TextBox _textBoxParentCategory;
	}
}
