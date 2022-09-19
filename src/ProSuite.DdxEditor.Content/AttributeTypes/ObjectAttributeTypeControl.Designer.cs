namespace ProSuite.DdxEditor.Content.AttributeTypes
{
    partial class ObjectAttributeTypeControl<T>
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
			this._labelReadOnly = new System.Windows.Forms.Label();
			this._labelIsObjectDefining = new System.Windows.Forms.Label();
			this._booleanComboboxReadOnly = new global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this._booleanComboboxIsObjectDefining = new global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox();
			this.SuspendLayout();
			// 
			// _textBoxRole
			// 
			this._textBoxRole.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxRole.Location = new System.Drawing.Point(132, 57);
			this._textBoxRole.Name = "_textBoxRole";
			this._textBoxRole.ReadOnly = true;
			this._textBoxRole.Size = new System.Drawing.Size(448, 20);
			this._textBoxRole.TabIndex = 2;
			// 
			// _labelRole
			// 
			this._labelRole.AutoSize = true;
			this._labelRole.Location = new System.Drawing.Point(94, 60);
			this._labelRole.Name = "_labelRole";
			this._labelRole.Size = new System.Drawing.Size(32, 13);
			this._labelRole.TabIndex = 3;
			this._labelRole.Text = "Role:";
			this._labelRole.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelReadOnly
			// 
			this._labelReadOnly.AutoSize = true;
			this._labelReadOnly.Location = new System.Drawing.Point(66, 6);
			this._labelReadOnly.Name = "_labelReadOnly";
			this._labelReadOnly.Size = new System.Drawing.Size(60, 13);
			this._labelReadOnly.TabIndex = 5;
			this._labelReadOnly.Text = "Read Only:";
			this._labelReadOnly.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelIsObjectDefining
			// 
			this._labelIsObjectDefining.AutoSize = true;
			this._labelIsObjectDefining.Location = new System.Drawing.Point(32, 33);
			this._labelIsObjectDefining.Name = "_labelIsObjectDefining";
			this._labelIsObjectDefining.Size = new System.Drawing.Size(94, 13);
			this._labelIsObjectDefining.TabIndex = 4;
			this._labelIsObjectDefining.Text = "Is Object Defining:";
			this._labelIsObjectDefining.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _booleanComboboxReadOnly
			// 
			this._booleanComboboxReadOnly.FalseText = "No";
			this._booleanComboboxReadOnly.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._booleanComboboxReadOnly.Location = new System.Drawing.Point(132, 3);
			this._booleanComboboxReadOnly.Name = "_booleanComboboxReadOnly";
			this._booleanComboboxReadOnly.Size = new System.Drawing.Size(50, 21);
			this._booleanComboboxReadOnly.TabIndex = 0;
			this._booleanComboboxReadOnly.TrueText = "Yes";
			this._booleanComboboxReadOnly.Value = false;
			// 
			// _booleanComboboxIsObjectDefining
			// 
			this._booleanComboboxIsObjectDefining.FalseText = "No";
			this._booleanComboboxIsObjectDefining.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this._booleanComboboxIsObjectDefining.Location = new System.Drawing.Point(132, 30);
			this._booleanComboboxIsObjectDefining.Name = "_booleanComboboxIsObjectDefining";
			this._booleanComboboxIsObjectDefining.Size = new System.Drawing.Size(50, 21);
			this._booleanComboboxIsObjectDefining.TabIndex = 1;
			this._booleanComboboxIsObjectDefining.TrueText = "Yes";
			this._booleanComboboxIsObjectDefining.Value = false;
			// 
			// ObjectAttributeTypeControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._booleanComboboxIsObjectDefining);
			this.Controls.Add(this._booleanComboboxReadOnly);
			this.Controls.Add(this._labelReadOnly);
			this.Controls.Add(this._labelIsObjectDefining);
			this.Controls.Add(this._labelRole);
			this.Controls.Add(this._textBoxRole);
			this.Name = "ObjectAttributeTypeControl";
			this.Size = new System.Drawing.Size(600, 82);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxRole;
        private System.Windows.Forms.Label _labelRole;
        private System.Windows.Forms.Label _labelReadOnly;
        private System.Windows.Forms.Label _labelIsObjectDefining;
        private global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox _booleanComboboxReadOnly;
        private global::ProSuite.Commons.UI.WinForms.Controls.BooleanCombobox _booleanComboboxIsObjectDefining;
    }
}