using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.Datasets
{
    partial class DatasetControl<T>
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
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxAbbreviation = new System.Windows.Forms.TextBox();
			this._labelAbbreviation = new System.Windows.Forms.Label();
			this._labelDescriprion = new System.Windows.Forms.Label();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelAliasName = new System.Windows.Forms.Label();
			this._textBoxAliasName = new System.Windows.Forms.TextBox();
			this._labelDatasetCategory = new System.Windows.Forms.Label();
			this._labelGeometryType = new System.Windows.Forms.Label();
			this._textBoxGeometryType = new System.Windows.Forms.TextBox();
			this._objectReferenceControlDatasetCategory = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.SuspendLayout();
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(200, 3);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.ReadOnly = true;
			this._textBoxName.Size = new System.Drawing.Size(380, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(156, 6);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 1;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxAbbreviation
			// 
			this._textBoxAbbreviation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxAbbreviation.Location = new System.Drawing.Point(200, 142);
			this._textBoxAbbreviation.Name = "_textBoxAbbreviation";
			this._textBoxAbbreviation.Size = new System.Drawing.Size(380, 20);
			this._textBoxAbbreviation.TabIndex = 3;
			// 
			// _labelAbbreviation
			// 
			this._labelAbbreviation.AutoSize = true;
			this._labelAbbreviation.Location = new System.Drawing.Point(125, 145);
			this._labelAbbreviation.Name = "_labelAbbreviation";
			this._labelAbbreviation.Size = new System.Drawing.Size(69, 13);
			this._labelAbbreviation.TabIndex = 3;
			this._labelAbbreviation.Text = "Abbreviation:";
			this._labelAbbreviation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelDescriprion
			// 
			this._labelDescriprion.AutoSize = true;
			this._labelDescriprion.Location = new System.Drawing.Point(131, 32);
			this._labelDescriprion.Name = "_labelDescriprion";
			this._labelDescriprion.Size = new System.Drawing.Size(63, 13);
			this._labelDescriprion.TabIndex = 5;
			this._labelDescriprion.Text = "Description:";
			this._labelDescriprion.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(200, 29);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(380, 81);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelAliasName
			// 
			this._labelAliasName.AutoSize = true;
			this._labelAliasName.Location = new System.Drawing.Point(131, 119);
			this._labelAliasName.Name = "_labelAliasName";
			this._labelAliasName.Size = new System.Drawing.Size(63, 13);
			this._labelAliasName.TabIndex = 7;
			this._labelAliasName.Text = "Alias Name:";
			this._labelAliasName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxAliasName
			// 
			this._textBoxAliasName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxAliasName.Location = new System.Drawing.Point(200, 116);
			this._textBoxAliasName.Name = "_textBoxAliasName";
			this._textBoxAliasName.Size = new System.Drawing.Size(380, 20);
			this._textBoxAliasName.TabIndex = 2;
			// 
			// _labelDatasetCategory
			// 
			this._labelDatasetCategory.AutoSize = true;
			this._labelDatasetCategory.Location = new System.Drawing.Point(102, 172);
			this._labelDatasetCategory.Name = "_labelDatasetCategory";
			this._labelDatasetCategory.Size = new System.Drawing.Size(92, 13);
			this._labelDatasetCategory.TabIndex = 9;
			this._labelDatasetCategory.Text = "Dataset Category:";
			this._labelDatasetCategory.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelGeometryType
			// 
			this._labelGeometryType.AutoSize = true;
			this._labelGeometryType.Location = new System.Drawing.Point(112, 199);
			this._labelGeometryType.Name = "_labelGeometryType";
			this._labelGeometryType.Size = new System.Drawing.Size(82, 13);
			this._labelGeometryType.TabIndex = 11;
			this._labelGeometryType.Text = "Geometry Type:";
			this._labelGeometryType.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxGeometryType
			// 
			this._textBoxGeometryType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxGeometryType.Location = new System.Drawing.Point(200, 196);
			this._textBoxGeometryType.Name = "_textBoxGeometryType";
			this._textBoxGeometryType.ReadOnly = true;
			this._textBoxGeometryType.Size = new System.Drawing.Size(380, 20);
			this._textBoxGeometryType.TabIndex = 5;
			// 
			// _objectReferenceControlDatasetCategory
			// 
			this._objectReferenceControlDatasetCategory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlDatasetCategory.DataSource = null;
			this._objectReferenceControlDatasetCategory.DisplayMember = null;
			this._objectReferenceControlDatasetCategory.FindObjectDelegate = null;
			this._objectReferenceControlDatasetCategory.FormatTextDelegate = null;
			this._objectReferenceControlDatasetCategory.Location = new System.Drawing.Point(200, 169);
			this._objectReferenceControlDatasetCategory.Name = "_objectReferenceControlDatasetCategory";
			this._objectReferenceControlDatasetCategory.ReadOnly = false;
			this._objectReferenceControlDatasetCategory.Size = new System.Drawing.Size(380, 20);
			this._objectReferenceControlDatasetCategory.TabIndex = 4;
			// 
			// DatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._objectReferenceControlDatasetCategory);
			this.Controls.Add(this._labelGeometryType);
			this.Controls.Add(this._textBoxGeometryType);
			this.Controls.Add(this._labelDatasetCategory);
			this.Controls.Add(this._labelAliasName);
			this.Controls.Add(this._textBoxAliasName);
			this.Controls.Add(this._labelDescriprion);
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelAbbreviation);
			this.Controls.Add(this._textBoxAbbreviation);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this._textBoxName);
			this.MinimumSize = new System.Drawing.Size(400, 0);
			this.Name = "DatasetControl";
			this.Size = new System.Drawing.Size(600, 222);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxAbbreviation;
        private System.Windows.Forms.Label _labelAbbreviation;
        private System.Windows.Forms.Label _labelDescriprion;
        private System.Windows.Forms.TextBox _textBoxDescription;
        private System.Windows.Forms.Label _labelAliasName;
        private System.Windows.Forms.TextBox _textBoxAliasName;
        private System.Windows.Forms.Label _labelDatasetCategory;
        private System.Windows.Forms.Label _labelGeometryType;
        private System.Windows.Forms.TextBox _textBoxGeometryType;
        private ObjectReferenceControl _objectReferenceControlDatasetCategory;
    }
}
