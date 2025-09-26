using System.Windows.Forms;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
    partial class ProductionModelControl<T>
        where T : ProductionModel
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
			this._labelTableIssueDataset = new System.Windows.Forms.Label();
			this._labelPolygonIssueDataset = new System.Windows.Forms.Label();
			this._labelLineIssueDataset = new System.Windows.Forms.Label();
			this._labelMultipointIssueDataset = new System.Windows.Forms.Label();
			this._textBoxLineIssueDataset = new System.Windows.Forms.TextBox();
			this._textBoxPolygonIssueDataset = new System.Windows.Forms.TextBox();
			this._textBoxTableIssueDataset = new System.Windows.Forms.TextBox();
			this._textBoxMultipointIssueDataset = new System.Windows.Forms.TextBox();
			this._groupBoxIssueDatasets = new System.Windows.Forms.GroupBox();
			this._textBoxMultiPatchIssueDataset = new System.Windows.Forms.TextBox();
			this._labelMultipatchIssueDataset = new System.Windows.Forms.Label();
			this._groupBoxIssueDatasets.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelTableIssueDataset
			// 
			this._labelTableIssueDataset.AutoSize = true;
			this._labelTableIssueDataset.Location = new System.Drawing.Point(12, 134);
			this._labelTableIssueDataset.Name = "_labelTableIssueDataset";
			this._labelTableIssueDataset.Size = new System.Drawing.Size(123, 13);
			this._labelTableIssueDataset.TabIndex = 1;
			this._labelTableIssueDataset.Text = "Issues without geometry:";
			this._labelTableIssueDataset.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelPolygonIssueDataset
			// 
			this._labelPolygonIssueDataset.AutoSize = true;
			this._labelPolygonIssueDataset.Location = new System.Drawing.Point(55, 82);
			this._labelPolygonIssueDataset.Name = "_labelPolygonIssueDataset";
			this._labelPolygonIssueDataset.Size = new System.Drawing.Size(80, 13);
			this._labelPolygonIssueDataset.TabIndex = 2;
			this._labelPolygonIssueDataset.Text = "Polygon issues:";
			this._labelPolygonIssueDataset.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelLineIssueDataset
			// 
			this._labelLineIssueDataset.AutoSize = true;
			this._labelLineIssueDataset.Location = new System.Drawing.Point(57, 55);
			this._labelLineIssueDataset.Name = "_labelLineIssueDataset";
			this._labelLineIssueDataset.Size = new System.Drawing.Size(78, 13);
			this._labelLineIssueDataset.TabIndex = 3;
			this._labelLineIssueDataset.Text = "Polyline issues:";
			this._labelLineIssueDataset.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _labelMultipointIssueDataset
			// 
			this._labelMultipointIssueDataset.AutoSize = true;
			this._labelMultipointIssueDataset.Location = new System.Drawing.Point(69, 28);
			this._labelMultipointIssueDataset.Name = "_labelMultipointIssueDataset";
			this._labelMultipointIssueDataset.Size = new System.Drawing.Size(66, 13);
			this._labelMultipointIssueDataset.TabIndex = 4;
			this._labelMultipointIssueDataset.Text = "Point issues:";
			this._labelMultipointIssueDataset.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxLineIssueDataset
			// 
			this._textBoxLineIssueDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxLineIssueDataset.Location = new System.Drawing.Point(141, 52);
			this._textBoxLineIssueDataset.Name = "_textBoxLineIssueDataset";
			this._textBoxLineIssueDataset.ReadOnly = true;
			this._textBoxLineIssueDataset.Size = new System.Drawing.Size(436, 20);
			this._textBoxLineIssueDataset.TabIndex = 2;
			this._textBoxLineIssueDataset.TabStop = false;
			// 
			// _textBoxPolygonIssueDataset
			// 
			this._textBoxPolygonIssueDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxPolygonIssueDataset.Location = new System.Drawing.Point(141, 79);
			this._textBoxPolygonIssueDataset.Name = "_textBoxPolygonIssueDataset";
			this._textBoxPolygonIssueDataset.ReadOnly = true;
			this._textBoxPolygonIssueDataset.Size = new System.Drawing.Size(436, 20);
			this._textBoxPolygonIssueDataset.TabIndex = 3;
			this._textBoxPolygonIssueDataset.TabStop = false;
			// 
			// _textBoxTableIssueDataset
			// 
			this._textBoxTableIssueDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxTableIssueDataset.Location = new System.Drawing.Point(141, 131);
			this._textBoxTableIssueDataset.Name = "_textBoxTableIssueDataset";
			this._textBoxTableIssueDataset.ReadOnly = true;
			this._textBoxTableIssueDataset.Size = new System.Drawing.Size(436, 20);
			this._textBoxTableIssueDataset.TabIndex = 4;
			this._textBoxTableIssueDataset.TabStop = false;
			// 
			// _textBoxMultipointIssueDataset
			// 
			this._textBoxMultipointIssueDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxMultipointIssueDataset.Location = new System.Drawing.Point(141, 25);
			this._textBoxMultipointIssueDataset.Name = "_textBoxMultipointIssueDataset";
			this._textBoxMultipointIssueDataset.ReadOnly = true;
			this._textBoxMultipointIssueDataset.Size = new System.Drawing.Size(436, 20);
			this._textBoxMultipointIssueDataset.TabIndex = 1;
			this._textBoxMultipointIssueDataset.TabStop = false;
			// 
			// _groupBoxIssueDatasets
			// 
			this._groupBoxIssueDatasets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxIssueDatasets.Controls.Add(this._labelMultipointIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._labelTableIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._labelMultipatchIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._labelPolygonIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._textBoxMultipointIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._labelLineIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._textBoxTableIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._textBoxLineIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._textBoxMultiPatchIssueDataset);
			this._groupBoxIssueDatasets.Controls.Add(this._textBoxPolygonIssueDataset);
			this._groupBoxIssueDatasets.Location = new System.Drawing.Point(3, 12);
			this._groupBoxIssueDatasets.Name = "_groupBoxIssueDatasets";
			this._groupBoxIssueDatasets.Size = new System.Drawing.Size(594, 159);
			this._groupBoxIssueDatasets.TabIndex = 5;
			this._groupBoxIssueDatasets.TabStop = false;
			this._groupBoxIssueDatasets.Text = "Datasets for storing quality verification issues";
			// 
			// _textBoxMultiPatchIssueDataset
			// 
			this._textBoxMultiPatchIssueDataset.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxMultiPatchIssueDataset.Location = new System.Drawing.Point(141, 105);
			this._textBoxMultiPatchIssueDataset.Name = "_textBoxMultiPatchIssueDataset";
			this._textBoxMultiPatchIssueDataset.ReadOnly = true;
			this._textBoxMultiPatchIssueDataset.Size = new System.Drawing.Size(436, 20);
			this._textBoxMultiPatchIssueDataset.TabIndex = 3;
			this._textBoxMultiPatchIssueDataset.TabStop = false;
			// 
			// _labelMultipatchIssueDataset
			// 
			this._labelMultipatchIssueDataset.AutoSize = true;
			this._labelMultipatchIssueDataset.Location = new System.Drawing.Point(44, 108);
			this._labelMultipatchIssueDataset.Name = "_labelMultipatchIssueDataset";
			this._labelMultipatchIssueDataset.Size = new System.Drawing.Size(91, 13);
			this._labelMultipatchIssueDataset.TabIndex = 2;
			this._labelMultipatchIssueDataset.Text = "Multipatch issues:";
			this._labelMultipatchIssueDataset.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// ProductionModelControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._groupBoxIssueDatasets);
			this.Name = "ProductionModelControl";
			this.Size = new System.Drawing.Size(600, 174);
			this._groupBoxIssueDatasets.ResumeLayout(false);
			this._groupBoxIssueDatasets.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private Label _labelTableIssueDataset;
        private Label _labelPolygonIssueDataset;
        private Label _labelLineIssueDataset;
        private Label _labelMultipointIssueDataset;
        private TextBox _textBoxLineIssueDataset;
        private TextBox _textBoxPolygonIssueDataset;
        private TextBox _textBoxTableIssueDataset;
        private TextBox _textBoxMultipointIssueDataset;
        private GroupBox _groupBoxIssueDatasets;
		private Label _labelMultipatchIssueDataset;
		private TextBox _textBoxMultiPatchIssueDataset;
    }
}
