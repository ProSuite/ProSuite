namespace ProSuite.Commons.UI.Dialogs
{
    partial class MessageListForm
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
            this._flowLayoutPanelMain = new System.Windows.Forms.FlowLayoutPanel();
            this._flowLayoutPanelIconMsg = new System.Windows.Forms.FlowLayoutPanel();
            this._pictureBoxIcon = new System.Windows.Forms.PictureBox();
            this._panelMessage = new System.Windows.Forms.Panel();
            this._flowLayoutPanelMessageText = new System.Windows.Forms.FlowLayoutPanel();
            this._tableLayoutPanelMessage = new System.Windows.Forms.TableLayoutPanel();
            this._panelPreInfo = new System.Windows.Forms.Panel();
            this._labelListPreInfo = new System.Windows.Forms.Label();
            this._panelPostInfo = new System.Windows.Forms.Panel();
            this._labelListPostInfo = new System.Windows.Forms.Label();
            this._buttonRight = new System.Windows.Forms.Button();
            this._buttonLeft = new System.Windows.Forms.Button();
            this._buttonMiddle = new System.Windows.Forms.Button();
            this._tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this._flowLayoutPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this._flowLayoutPanelList = new System.Windows.Forms.FlowLayoutPanel();
            this._flowLayoutPanelMain.SuspendLayout();
            this._flowLayoutPanelIconMsg.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pictureBoxIcon)).BeginInit();
            this._panelMessage.SuspendLayout();
            this._flowLayoutPanelMessageText.SuspendLayout();
            this._tableLayoutPanelMessage.SuspendLayout();
            this._panelPreInfo.SuspendLayout();
            this._panelPostInfo.SuspendLayout();
            this._tableLayoutPanelMain.SuspendLayout();
            this._flowLayoutPanelButtons.SuspendLayout();
            this._flowLayoutPanelList.SuspendLayout();
            this.SuspendLayout();
            // 
            // _flowLayoutPanelMain
            // 
            this._flowLayoutPanelMain.AutoSize = true;
            this._flowLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._tableLayoutPanelMain.SetColumnSpan(this._flowLayoutPanelMain, 3);
            this._flowLayoutPanelMain.Controls.Add(this._flowLayoutPanelIconMsg);
            this._flowLayoutPanelMain.Location = new System.Drawing.Point(3, 3);
            this._flowLayoutPanelMain.Name = "_flowLayoutPanelMain";
            this._flowLayoutPanelMain.Size = new System.Drawing.Size(314, 83);
            this._flowLayoutPanelMain.TabIndex = 0;
            // 
            // _flowLayoutPanelIconMsg
            // 
            this._flowLayoutPanelIconMsg.AutoSize = true;
            this._flowLayoutPanelIconMsg.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowLayoutPanelIconMsg.Controls.Add(this._pictureBoxIcon);
            this._flowLayoutPanelIconMsg.Controls.Add(this._panelMessage);
            this._flowLayoutPanelMain.SetFlowBreak(this._flowLayoutPanelIconMsg, true);
            this._flowLayoutPanelIconMsg.Location = new System.Drawing.Point(3, 3);
            this._flowLayoutPanelIconMsg.Name = "_flowLayoutPanelIconMsg";
            this._flowLayoutPanelIconMsg.Size = new System.Drawing.Size(308, 77);
            this._flowLayoutPanelIconMsg.TabIndex = 1;
            this._flowLayoutPanelIconMsg.WrapContents = false;
            // 
            // _pictureBoxIcon
            // 
			this._pictureBoxIcon.Image = global::ProSuite.Commons.UI.Properties.Resources.WarnMessage;
            this._pictureBoxIcon.Location = new System.Drawing.Point(3, 3);
            this._pictureBoxIcon.Name = "_pictureBoxIcon";
            this._pictureBoxIcon.Size = new System.Drawing.Size(14, 14);
            this._pictureBoxIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this._pictureBoxIcon.TabIndex = 5;
            this._pictureBoxIcon.TabStop = false;
            // 
            // _panelMessage
            // 
            this._panelMessage.AutoSize = true;
            this._panelMessage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._panelMessage.Controls.Add(this._flowLayoutPanelMessageText);
            this._flowLayoutPanelIconMsg.SetFlowBreak(this._panelMessage, true);
            this._panelMessage.Location = new System.Drawing.Point(23, 3);
            this._panelMessage.Name = "_panelMessage";
            this._panelMessage.Size = new System.Drawing.Size(282, 71);
            this._panelMessage.TabIndex = 4;
            // 
            // _flowLayoutPanelMessageText
            // 
            this._flowLayoutPanelMessageText.AutoSize = true;
            this._flowLayoutPanelMessageText.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowLayoutPanelMessageText.Controls.Add(this._tableLayoutPanelMessage);
            this._flowLayoutPanelMessageText.Location = new System.Drawing.Point(7, 3);
            this._flowLayoutPanelMessageText.Name = "_flowLayoutPanelMessageText";
            this._flowLayoutPanelMessageText.Size = new System.Drawing.Size(272, 65);
            this._flowLayoutPanelMessageText.TabIndex = 4;
            this._flowLayoutPanelMessageText.WrapContents = false;
            // 
            // _tableLayoutPanelMessage
            // 
            this._tableLayoutPanelMessage.AutoSize = true;
            this._tableLayoutPanelMessage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._tableLayoutPanelMessage.ColumnCount = 1;
            this._tableLayoutPanelMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._tableLayoutPanelMessage.Controls.Add(this._panelPreInfo, 0, 0);
            this._tableLayoutPanelMessage.Controls.Add(this._panelPostInfo, 0, 2);
            this._tableLayoutPanelMessage.Controls.Add(this._flowLayoutPanelList, 0, 1);
            this._tableLayoutPanelMessage.Location = new System.Drawing.Point(3, 3);
            this._tableLayoutPanelMessage.Name = "_tableLayoutPanelMessage";
            this._tableLayoutPanelMessage.RowCount = 3;
            this._tableLayoutPanelMessage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._tableLayoutPanelMessage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._tableLayoutPanelMessage.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._tableLayoutPanelMessage.Size = new System.Drawing.Size(266, 59);
            this._tableLayoutPanelMessage.TabIndex = 6;
            // 
            // _panelPreInfo
            // 
            this._panelPreInfo.AutoSize = true;
            this._panelPreInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._panelPreInfo.Controls.Add(this._labelListPreInfo);
            this._panelPreInfo.Location = new System.Drawing.Point(3, 3);
            this._panelPreInfo.Name = "_panelPreInfo";
            this._panelPreInfo.Size = new System.Drawing.Size(41, 13);
            this._panelPreInfo.TabIndex = 3;
            // 
            // _labelListPreInfo
            // 
            this._labelListPreInfo.AutoSize = true;
            this._labelListPreInfo.Location = new System.Drawing.Point(0, 0);
            this._labelListPreInfo.Margin = new System.Windows.Forms.Padding(0);
            this._labelListPreInfo.Name = "_labelListPreInfo";
            this._labelListPreInfo.Size = new System.Drawing.Size(41, 13);
            this._labelListPreInfo.TabIndex = 4;
            this._labelListPreInfo.Text = "PreInfo";
            // 
            // _panelPostInfo
            // 
            this._panelPostInfo.AutoSize = true;
            this._panelPostInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._panelPostInfo.Controls.Add(this._labelListPostInfo);
            this._panelPostInfo.Location = new System.Drawing.Point(3, 43);
            this._panelPostInfo.Name = "_panelPostInfo";
            this._panelPostInfo.Size = new System.Drawing.Size(46, 13);
            this._panelPostInfo.TabIndex = 3;
            // 
            // _labelListPostInfo
            // 
            this._labelListPostInfo.AutoSize = true;
            this._labelListPostInfo.Location = new System.Drawing.Point(0, 0);
            this._labelListPostInfo.Margin = new System.Windows.Forms.Padding(0);
            this._labelListPostInfo.Name = "_labelListPostInfo";
            this._labelListPostInfo.Size = new System.Drawing.Size(46, 13);
            this._labelListPostInfo.TabIndex = 2;
            this._labelListPostInfo.Text = "PostInfo";
            // 
            // _buttonRight
            // 
            this._buttonRight.Location = new System.Drawing.Point(165, 3);
            this._buttonRight.Name = "_buttonRight";
            this._buttonRight.Size = new System.Drawing.Size(75, 23);
            this._buttonRight.TabIndex = 0;
            this._buttonRight.Text = "Right";
            this._buttonRight.UseVisualStyleBackColor = true;
            this._buttonRight.Click += new System.EventHandler(this._buttonRight_Click);
            // 
            // _buttonLeft
            // 
            this._buttonLeft.Location = new System.Drawing.Point(3, 3);
            this._buttonLeft.Name = "_buttonLeft";
            this._buttonLeft.Size = new System.Drawing.Size(75, 23);
            this._buttonLeft.TabIndex = 1;
            this._buttonLeft.Text = "Left";
            this._buttonLeft.UseVisualStyleBackColor = true;
            this._buttonLeft.Click += new System.EventHandler(this._buttonLeft_Click);
            // 
            // _buttonMiddle
            // 
            this._buttonMiddle.Location = new System.Drawing.Point(84, 3);
            this._buttonMiddle.Name = "_buttonMiddle";
            this._buttonMiddle.Size = new System.Drawing.Size(75, 23);
            this._buttonMiddle.TabIndex = 2;
            this._buttonMiddle.Text = "Middle";
            this._buttonMiddle.UseVisualStyleBackColor = true;
            this._buttonMiddle.Click += new System.EventHandler(this._buttonMiddle_Click);
            // 
            // _tableLayoutPanelMain
            // 
            this._tableLayoutPanelMain.AutoSize = true;
            this._tableLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._tableLayoutPanelMain.ColumnCount = 3;
            this._tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayoutPanelMain.Controls.Add(this._flowLayoutPanelButtons, 1, 1);
            this._tableLayoutPanelMain.Controls.Add(this._flowLayoutPanelMain, 0, 0);
            this._tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Top;
            this._tableLayoutPanelMain.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this._tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this._tableLayoutPanelMain.Name = "_tableLayoutPanelMain";
            this._tableLayoutPanelMain.RowCount = 2;
            this._tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this._tableLayoutPanelMain.Size = new System.Drawing.Size(320, 124);
            this._tableLayoutPanelMain.TabIndex = 1;
            // 
            // _flowLayoutPanelButtons
            // 
            this._flowLayoutPanelButtons.AutoSize = true;
            this._flowLayoutPanelButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowLayoutPanelButtons.Controls.Add(this._buttonLeft);
            this._flowLayoutPanelButtons.Controls.Add(this._buttonMiddle);
            this._flowLayoutPanelButtons.Controls.Add(this._buttonRight);
            this._flowLayoutPanelButtons.Location = new System.Drawing.Point(38, 92);
            this._flowLayoutPanelButtons.Name = "_flowLayoutPanelButtons";
            this._flowLayoutPanelButtons.Size = new System.Drawing.Size(243, 29);
            this._flowLayoutPanelButtons.TabIndex = 2;
            this._flowLayoutPanelButtons.WrapContents = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this._flowLayoutPanelList.SetFlowBreak(this.label1, true);
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "label1";
            // 
            // _flowLayoutPanelList
            // 
            this._flowLayoutPanelList.AutoScroll = true;
            this._flowLayoutPanelList.AutoSize = true;
            this._flowLayoutPanelList.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowLayoutPanelList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._flowLayoutPanelList.Controls.Add(this.label1);
            this._flowLayoutPanelList.Location = new System.Drawing.Point(3, 22);
            this._flowLayoutPanelList.MaximumSize = new System.Drawing.Size(600, 300);
            this._flowLayoutPanelList.MinimumSize = new System.Drawing.Size(260, 14);
            this._flowLayoutPanelList.Name = "_flowLayoutPanelList";
            this._flowLayoutPanelList.Size = new System.Drawing.Size(260, 15);
            this._flowLayoutPanelList.TabIndex = 3;
            // 
            // MessageListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(320, 124);
            this.ControlBox = false;
            this.Controls.Add(this._tableLayoutPanelMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MessageListForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "MessageListForm";
            this._flowLayoutPanelMain.ResumeLayout(false);
            this._flowLayoutPanelMain.PerformLayout();
            this._flowLayoutPanelIconMsg.ResumeLayout(false);
            this._flowLayoutPanelIconMsg.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pictureBoxIcon)).EndInit();
            this._panelMessage.ResumeLayout(false);
            this._panelMessage.PerformLayout();
            this._flowLayoutPanelMessageText.ResumeLayout(false);
            this._flowLayoutPanelMessageText.PerformLayout();
            this._tableLayoutPanelMessage.ResumeLayout(false);
            this._tableLayoutPanelMessage.PerformLayout();
            this._panelPreInfo.ResumeLayout(false);
            this._panelPreInfo.PerformLayout();
            this._panelPostInfo.ResumeLayout(false);
            this._panelPostInfo.PerformLayout();
            this._tableLayoutPanelMain.ResumeLayout(false);
            this._tableLayoutPanelMain.PerformLayout();
            this._flowLayoutPanelButtons.ResumeLayout(false);
            this._flowLayoutPanelList.ResumeLayout(false);
            this._flowLayoutPanelList.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanelMain;
        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanelIconMsg;
        private System.Windows.Forms.Button _buttonRight;
        private System.Windows.Forms.Button _buttonLeft;
        private System.Windows.Forms.Button _buttonMiddle;
        private System.Windows.Forms.PictureBox _pictureBoxIcon;
        private System.Windows.Forms.Panel _panelMessage;
        private System.Windows.Forms.Label _labelListPostInfo;
        private System.Windows.Forms.TableLayoutPanel _tableLayoutPanelMain;
        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanelButtons;
        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanelMessageText;
        private System.Windows.Forms.TableLayoutPanel _tableLayoutPanelMessage;
        private System.Windows.Forms.Label _labelListPreInfo;
        private System.Windows.Forms.Panel _panelPreInfo;
        private System.Windows.Forms.Panel _panelPostInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel _flowLayoutPanelList;
    }
}