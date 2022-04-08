using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.Attributes
{
    partial class ObjectAttributeControl<T>
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
			this._textBoxRole = new System.Windows.Forms.TextBox();
			this._labelRole = new System.Windows.Forms.Label();
			this._labelAttributeType = new System.Windows.Forms.Label();
			this._labelObjectAttTypeRequireEqualValues = new System.Windows.Forms.Label();
			this._textBoxObjectAttTypeRequireEqualValues = new System.Windows.Forms.TextBox();
			this._labelObjectAttTypeReadOnly = new System.Windows.Forms.Label();
			this._textBoxObjectAttTypeReadOnly = new System.Windows.Forms.TextBox();
			this._labelReadOnly = new System.Windows.Forms.Label();
			this._labelIsObjectDefining = new System.Windows.Forms.Label();
			this._nullableBooleanComboboxIsObjectDefiningOverride = new global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox();
			this._nullableBooleanComboboxReadOnly = new global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox();
			this._objectReferenceControlAttributeType = new global::ProSuite.Commons.UI.WinForms.Controls.ObjectReferenceControl();
			this.SuspendLayout();
			// 
			// _textBoxRole
			// 
			this._textBoxRole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxRole.Location = new System.Drawing.Point(355, 62);
			this._textBoxRole.Name = "_textBoxRole";
			this._textBoxRole.ReadOnly = true;
			this._textBoxRole.Size = new System.Drawing.Size(232, 20);
			this._textBoxRole.TabIndex = 2;
			// 
			// _labelRole
			// 
			this._labelRole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._labelRole.AutoSize = true;
			this._labelRole.Location = new System.Drawing.Point(320, 65);
			this._labelRole.Name = "_labelRole";
			this._labelRole.Size = new System.Drawing.Size(32, 13);
			this._labelRole.TabIndex = 3;
			this._labelRole.Text = "Role:";
			this._labelRole.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelAttributeType
			// 
			this._labelAttributeType.AutoSize = true;
			this._labelAttributeType.Location = new System.Drawing.Point(25, 65);
			this._labelAttributeType.Name = "_labelAttributeType";
			this._labelAttributeType.Size = new System.Drawing.Size(76, 13);
			this._labelAttributeType.TabIndex = 3;
			this._labelAttributeType.Text = "Attribute Type:";
			this._labelAttributeType.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelObjectAttTypeRequireEqualValues
			// 
			this._labelObjectAttTypeRequireEqualValues.AutoSize = true;
			this._labelObjectAttTypeRequireEqualValues.Location = new System.Drawing.Point(207, 36);
			this._labelObjectAttTypeRequireEqualValues.Name = "_labelObjectAttTypeRequireEqualValues";
			this._labelObjectAttTypeRequireEqualValues.Size = new System.Drawing.Size(113, 13);
			this._labelObjectAttTypeRequireEqualValues.TabIndex = 20;
			this._labelObjectAttTypeRequireEqualValues.Text = "Attribute Type Default:";
			this._labelObjectAttTypeRequireEqualValues.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxObjectAttTypeRequireEqualValues
			// 
			this._textBoxObjectAttTypeRequireEqualValues.Location = new System.Drawing.Point(323, 32);
			this._textBoxObjectAttTypeRequireEqualValues.Name = "_textBoxObjectAttTypeRequireEqualValues";
			this._textBoxObjectAttTypeRequireEqualValues.ReadOnly = true;
			this._textBoxObjectAttTypeRequireEqualValues.Size = new System.Drawing.Size(71, 20);
			this._textBoxObjectAttTypeRequireEqualValues.TabIndex = 19;
			// 
			// _labelObjectAttTypeReadOnly
			// 
			this._labelObjectAttTypeReadOnly.AutoSize = true;
			this._labelObjectAttTypeReadOnly.Location = new System.Drawing.Point(207, 9);
			this._labelObjectAttTypeReadOnly.Name = "_labelObjectAttTypeReadOnly";
			this._labelObjectAttTypeReadOnly.Size = new System.Drawing.Size(113, 13);
			this._labelObjectAttTypeReadOnly.TabIndex = 18;
			this._labelObjectAttTypeReadOnly.Text = "Attribute Type Default:";
			this._labelObjectAttTypeReadOnly.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxObjectAttTypeReadOnly
			// 
			this._textBoxObjectAttTypeReadOnly.Location = new System.Drawing.Point(323, 6);
			this._textBoxObjectAttTypeReadOnly.Name = "_textBoxObjectAttTypeReadOnly";
			this._textBoxObjectAttTypeReadOnly.ReadOnly = true;
			this._textBoxObjectAttTypeReadOnly.Size = new System.Drawing.Size(71, 20);
			this._textBoxObjectAttTypeReadOnly.TabIndex = 17;
			// 
			// _labelReadOnly
			// 
			this._labelReadOnly.AutoSize = true;
			this._labelReadOnly.Location = new System.Drawing.Point(42, 9);
			this._labelReadOnly.Name = "_labelReadOnly";
			this._labelReadOnly.Size = new System.Drawing.Size(60, 13);
			this._labelReadOnly.TabIndex = 14;
			this._labelReadOnly.Text = "Read Only:";
			this._labelReadOnly.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelIsObjectDefining
			// 
			this._labelIsObjectDefining.AutoSize = true;
			this._labelIsObjectDefining.Location = new System.Drawing.Point(8, 35);
			this._labelIsObjectDefining.Name = "_labelIsObjectDefining";
			this._labelIsObjectDefining.Size = new System.Drawing.Size(94, 13);
			this._labelIsObjectDefining.TabIndex = 13;
			this._labelIsObjectDefining.Text = "Is Object Defining:";
			this._labelIsObjectDefining.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _nullableBooleanComboboxIsObjectDefiningOverride
			// 
			this._nullableBooleanComboboxIsObjectDefiningOverride.DefaultText = "Use Default";
			this._nullableBooleanComboboxIsObjectDefiningOverride.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._nullableBooleanComboboxIsObjectDefiningOverride.Location = new System.Drawing.Point(108, 32);
			this._nullableBooleanComboboxIsObjectDefiningOverride.Name = "_nullableBooleanComboboxIsObjectDefiningOverride";
			this._nullableBooleanComboboxIsObjectDefiningOverride.Size = new System.Drawing.Size(93, 21);
			this._nullableBooleanComboboxIsObjectDefiningOverride.TabIndex = 15;
			this._nullableBooleanComboboxIsObjectDefiningOverride.Value = null;
			// 
			// _nullableBooleanComboboxReadOnly
			// 
			this._nullableBooleanComboboxReadOnly.DefaultText = "Use Default";
			this._nullableBooleanComboboxReadOnly.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._nullableBooleanComboboxReadOnly.Location = new System.Drawing.Point(108, 4);
			this._nullableBooleanComboboxReadOnly.Name = "_nullableBooleanComboboxReadOnly";
			this._nullableBooleanComboboxReadOnly.Size = new System.Drawing.Size(93, 21);
			this._nullableBooleanComboboxReadOnly.TabIndex = 16;
			this._nullableBooleanComboboxReadOnly.Value = null;
			// 
			// _objectReferenceControlAttributeType
			// 
			this._objectReferenceControlAttributeType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._objectReferenceControlAttributeType.DataSource = null;
			this._objectReferenceControlAttributeType.DisplayMember = null;
			this._objectReferenceControlAttributeType.FindObjectDelegate = null;
			this._objectReferenceControlAttributeType.FormatTextDelegate = null;
			this._objectReferenceControlAttributeType.Location = new System.Drawing.Point(107, 61);
			this._objectReferenceControlAttributeType.Name = "_objectReferenceControlAttributeType";
			this._objectReferenceControlAttributeType.ReadOnly = false;
			this._objectReferenceControlAttributeType.Size = new System.Drawing.Size(197, 20);
			this._objectReferenceControlAttributeType.TabIndex = 4;
			// 
			// ObjectAttributeControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelObjectAttTypeRequireEqualValues);
			this.Controls.Add(this._textBoxObjectAttTypeRequireEqualValues);
			this.Controls.Add(this._labelObjectAttTypeReadOnly);
			this.Controls.Add(this._textBoxObjectAttTypeReadOnly);
			this.Controls.Add(this._nullableBooleanComboboxIsObjectDefiningOverride);
			this.Controls.Add(this._nullableBooleanComboboxReadOnly);
			this.Controls.Add(this._labelReadOnly);
			this.Controls.Add(this._labelIsObjectDefining);
			this.Controls.Add(this._objectReferenceControlAttributeType);
			this.Controls.Add(this._labelAttributeType);
			this.Controls.Add(this._labelRole);
			this.Controls.Add(this._textBoxRole);
			this.Name = "ObjectAttributeControl";
			this.Size = new System.Drawing.Size(590, 91);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxRole;
        private System.Windows.Forms.Label _labelRole;
        private ObjectReferenceControl _objectReferenceControlAttributeType;
        private System.Windows.Forms.Label _labelAttributeType;
        private System.Windows.Forms.Label _labelObjectAttTypeRequireEqualValues;
        private System.Windows.Forms.TextBox _textBoxObjectAttTypeRequireEqualValues;
        private System.Windows.Forms.Label _labelObjectAttTypeReadOnly;
        private System.Windows.Forms.TextBox _textBoxObjectAttTypeReadOnly;
        private global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox _nullableBooleanComboboxIsObjectDefiningOverride;
        private global::ProSuite.Commons.UI.WinForms.Controls.NullableBooleanCombobox _nullableBooleanComboboxReadOnly;
        private System.Windows.Forms.Label _labelReadOnly;
        private System.Windows.Forms.Label _labelIsObjectDefining;
    }
}