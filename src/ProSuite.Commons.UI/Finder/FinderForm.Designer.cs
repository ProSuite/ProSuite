using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
    partial class FinderForm<T>
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
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this._toolStripStatusLabelMessage = new System.Windows.Forms.ToolStripStatusLabel();
			this.panel1 = new System.Windows.Forms.Panel();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._dataGridView = new DoubleBufferedDataGridView();
			this._dataGridViewFindToolStrip = new DataGridViewFindToolStrip();
			this.statusStrip1.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabelMessage});
			this.statusStrip1.Location = new System.Drawing.Point(0, 427);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(907, 22);
			this.statusStrip1.TabIndex = 0;
			this.statusStrip1.Text = "_statusStrip";
			// 
			// _toolStripStatusLabelMessage
			// 
			this._toolStripStatusLabelMessage.Name = "_toolStripStatusLabelMessage";
			this._toolStripStatusLabelMessage.Size = new System.Drawing.Size(54, 17);
			this._toolStripStatusLabelMessage.Text = "<status>";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this._buttonCancel);
			this.panel1.Controls.Add(this._buttonOK);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 390);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(907, 37);
			this.panel1.TabIndex = 2;
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(820, 6);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 1;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(739, 6);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 0;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.Location = new System.Drawing.Point(0, 25);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.ReadOnly = true;
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(907, 365);
			this._dataGridView.TabIndex = 1;
			this._dataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellDoubleClick);
			this._dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this._dataGridView_CellFormatting);
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 150;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(0, 0);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(907, 25);
			this._dataGridViewFindToolStrip.TabIndex = 0;
			this._dataGridViewFindToolStrip.TabStop = true;
			this._dataGridViewFindToolStrip.Text = "_dataGridViewFindToolStrip";
			// 
			// FinderForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(907, 449);
			this.Controls.Add(this._dataGridView);
			this.Controls.Add(this._dataGridViewFindToolStrip);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.statusStrip1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimizeBox = false;
			this.Name = "FinderForm";
			this.ShowInTaskbar = false;
			this.Text = "Finder";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FinderForm_FormClosed);
			this.Load += new System.EventHandler(this.FinderForm_Load);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.Button _buttonCancel;
        private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
        private DoubleBufferedDataGridView _dataGridView;
		private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabelMessage;
    }
}