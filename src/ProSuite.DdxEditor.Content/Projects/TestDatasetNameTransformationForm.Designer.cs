namespace ProSuite.DdxEditor.Content.Projects
{
	partial class TestDatasetNameTransformationForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestDatasetNameTransformationForm));
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._labelTransformationPatterns = new System.Windows.Forms.Label();
			this._textBoxTransformationPatterns = new System.Windows.Forms.TextBox();
			this._labelDatasetName = new System.Windows.Forms.Label();
			this._textBoxDatasetName = new System.Windows.Forms.TextBox();
			this._labelTransformedDatasetName = new System.Windows.Forms.Label();
			this._textBoxTransformedDatasetName = new System.Windows.Forms.TextBox();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._textBoxInfo = new System.Windows.Forms.TextBox();
			this._labelTransformationStatus = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(277, 275);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 3;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(196, 275);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 2;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _labelTransformationPatterns
			// 
			this._labelTransformationPatterns.AutoSize = true;
			this._labelTransformationPatterns.Location = new System.Drawing.Point(9, 112);
			this._labelTransformationPatterns.Name = "_labelTransformationPatterns";
			this._labelTransformationPatterns.Size = new System.Drawing.Size(121, 13);
			this._labelTransformationPatterns.TabIndex = 2;
			this._labelTransformationPatterns.Text = "Transformation patterns:";
			// 
			// _textBoxTransformationPatterns
			// 
			this._textBoxTransformationPatterns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTransformationPatterns.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._errorProvider.SetIconAlignment(this._textBoxTransformationPatterns, System.Windows.Forms.ErrorIconAlignment.TopRight);
			this._errorProvider.SetIconPadding(this._textBoxTransformationPatterns, 1);
			this._textBoxTransformationPatterns.Location = new System.Drawing.Point(12, 128);
			this._textBoxTransformationPatterns.Multiline = true;
			this._textBoxTransformationPatterns.Name = "_textBoxTransformationPatterns";
			this._textBoxTransformationPatterns.Size = new System.Drawing.Size(340, 53);
			this._textBoxTransformationPatterns.TabIndex = 0;
			this._textBoxTransformationPatterns.TextChanged += new System.EventHandler(this._textBoxTransformationPatterns_TextChanged);
			// 
			// _labelDatasetName
			// 
			this._labelDatasetName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._labelDatasetName.AutoSize = true;
			this._labelDatasetName.Location = new System.Drawing.Point(9, 190);
			this._labelDatasetName.Name = "_labelDatasetName";
			this._labelDatasetName.Size = new System.Drawing.Size(76, 13);
			this._labelDatasetName.TabIndex = 4;
			this._labelDatasetName.Text = "Dataset name:";
			// 
			// _textBoxDatasetName
			// 
			this._textBoxDatasetName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDatasetName.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxDatasetName.Location = new System.Drawing.Point(12, 206);
			this._textBoxDatasetName.Name = "_textBoxDatasetName";
			this._textBoxDatasetName.Size = new System.Drawing.Size(340, 20);
			this._textBoxDatasetName.TabIndex = 1;
			this._textBoxDatasetName.TextChanged += new System.EventHandler(this._textBoxDatasetName_TextChanged);
			// 
			// _labelTransformedDatasetName
			// 
			this._labelTransformedDatasetName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._labelTransformedDatasetName.AutoSize = true;
			this._labelTransformedDatasetName.Location = new System.Drawing.Point(9, 233);
			this._labelTransformedDatasetName.Name = "_labelTransformedDatasetName";
			this._labelTransformedDatasetName.Size = new System.Drawing.Size(136, 13);
			this._labelTransformedDatasetName.TabIndex = 6;
			this._labelTransformedDatasetName.Text = "Transformed dataset name:";
			// 
			// _textBoxTransformedDatasetName
			// 
			this._textBoxTransformedDatasetName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTransformedDatasetName.BackColor = System.Drawing.SystemColors.Control;
			this._textBoxTransformedDatasetName.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._textBoxTransformedDatasetName.Location = new System.Drawing.Point(12, 249);
			this._textBoxTransformedDatasetName.Name = "_textBoxTransformedDatasetName";
			this._textBoxTransformedDatasetName.ReadOnly = true;
			this._textBoxTransformedDatasetName.Size = new System.Drawing.Size(340, 20);
			this._textBoxTransformedDatasetName.TabIndex = 5;
			this._textBoxTransformedDatasetName.TabStop = false;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// _textBoxInfo
			// 
			this._textBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxInfo.BackColor = System.Drawing.SystemColors.Info;
			this._textBoxInfo.Location = new System.Drawing.Point(12, 6);
			this._textBoxInfo.Multiline = true;
			this._textBoxInfo.Name = "_textBoxInfo";
			this._textBoxInfo.ReadOnly = true;
			this._textBoxInfo.Size = new System.Drawing.Size(340, 103);
			this._textBoxInfo.TabIndex = 8;
			this._textBoxInfo.TabStop = false;
			this._textBoxInfo.Text = resources.GetString("_textBoxInfo.Text");
			// 
			// _labelTransformationStatus
			// 
			this._labelTransformationStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._labelTransformationStatus.AutoSize = true;
			this._labelTransformationStatus.Location = new System.Drawing.Point(12, 272);
			this._labelTransformationStatus.Name = "_labelTransformationStatus";
			this._labelTransformationStatus.Size = new System.Drawing.Size(66, 13);
			this._labelTransformationStatus.TabIndex = 9;
			this._labelTransformationStatus.Text = "<undefined>";
			// 
			// TestDatasetNameTransformationForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(370, 310);
			this.Controls.Add(this._labelTransformationStatus);
			this.Controls.Add(this._textBoxInfo);
			this.Controls.Add(this._labelTransformedDatasetName);
			this.Controls.Add(this._textBoxTransformedDatasetName);
			this.Controls.Add(this._textBoxDatasetName);
			this.Controls.Add(this._labelDatasetName);
			this.Controls.Add(this._textBoxTransformationPatterns);
			this.Controls.Add(this._labelTransformationPatterns);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(386, 344);
			this.Name = "TestDatasetNameTransformationForm";
			this.ShowInTaskbar = false;
			this.Text = "Dataset Name Transformation";
			this.Load += new System.EventHandler(this.TestDatasetNameTransformationForm_Load);
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Label _labelTransformationPatterns;
		private System.Windows.Forms.TextBox _textBoxTransformationPatterns;
		private System.Windows.Forms.Label _labelDatasetName;
		private System.Windows.Forms.TextBox _textBoxDatasetName;
		private System.Windows.Forms.Label _labelTransformedDatasetName;
		private System.Windows.Forms.TextBox _textBoxTransformedDatasetName;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private System.Windows.Forms.TextBox _textBoxInfo;
		private System.Windows.Forms.Label _labelTransformationStatus;
	}
}