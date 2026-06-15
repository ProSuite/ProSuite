using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.UI.Core.DataModel
{
    partial class DatasetCatalogControl
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
			this._groupedListView = new global::ProSuite.Commons.UI.WinForms.Controls.GroupedListView();
			this.SuspendLayout();
			// 
			// _groupedListView
			// 
			this._groupedListView.AutoScroll = true;
			this._groupedListView.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this._groupedListView.BackColor = System.Drawing.SystemColors.Window;
			this._groupedListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._groupedListView.GroupHeadingColor = System.Drawing.SystemColors.ControlText;
			this._groupedListView.GroupHeadingFont = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
			this._groupedListView.ListNullGroupHeadingFirst = false;
			this._groupedListView.Location = new System.Drawing.Point(0, 0);
			this._groupedListView.MinimumSize = new System.Drawing.Size(256, 128);
			this._groupedListView.Name = "_groupedListView";
			this._groupedListView.NullGroupHeadingText = "Others";
			this._groupedListView.ShowCheckBoxes = false;
			this._groupedListView.Size = new System.Drawing.Size(256, 128);
			this._groupedListView.StatusTextColor = System.Drawing.Color.Red;
			this._groupedListView.TabIndex = 1;
			// 
			// DatasetCatalogControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._groupedListView);
			this.Name = "DatasetCatalogControl";
			this.Size = new System.Drawing.Size(256, 128);
			this.ResumeLayout(false);

        }

        #endregion

        private GroupedListView _groupedListView;
    }
}
