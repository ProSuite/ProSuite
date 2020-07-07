using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Env;

namespace ProSuite.Commons.UI.Dialogs
{
	internal partial class MessageListForm : Form
	{
		private MessageBoxButtons _buttons;

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageListForm"/> class.
		/// </summary>
		public MessageListForm()
		{
			InitializeComponent();
		}

		public DialogResult ShowDialog([CanBeNull] IWin32Window owner,
		                               [NotNull] string title,
		                               [NotNull] string headerText,
		                               [NotNull] IList<string> listInfos,
		                               [CanBeNull] string footerText,
		                               MessageBoxIcon icon,
		                               MessageBoxButtons buttons)
		{
			Assert.ArgumentNotNullOrEmpty(title, nameof(title));
			Assert.ArgumentNotNullOrEmpty(headerText, nameof(headerText));
			Assert.ArgumentNotNull(listInfos, nameof(listInfos));

			_buttons = buttons;

			//SuspendLayout();

			SetIcon(icon);
			SetButtons(buttons);
			SetTexts(title, headerText, footerText);
			SetListText(listInfos);

			Refresh();

			//ResumeLayout(true);

			return UIEnvironment.ShowDialog(this, owner);
		}

		#region Non-public methods

		private void SetTexts([NotNull] string title,
		                      [CanBeNull] string preListText,
		                      [CanBeNull] string postListText)
		{
			Text = title;
			_labelListPreInfo.Text = preListText;
			_labelListPostInfo.Text = postListText;
		}

		private void SetListText([NotNull] IEnumerable<string> listInfos)
		{
			_flowLayoutPanelList.Controls.Clear();

			foreach (string line in listInfos)
			{
				var label = new Label {Text = line};

				_flowLayoutPanelList.Controls.Add(label);
				_flowLayoutPanelList.SetFlowBreak(label, true);
			}

			_flowLayoutPanelList.WrapContents = true;
		}

		private void SetIcon(MessageBoxIcon iconType)
		{
			switch (iconType)
			{
				case MessageBoxIcon.Asterisk:
					_pictureBoxIcon.Image = SystemIcons.Asterisk.ToBitmap();
					break;
				case MessageBoxIcon.Error:
					_pictureBoxIcon.Image = SystemIcons.Error.ToBitmap();
					break;
				case MessageBoxIcon.None:
					_pictureBoxIcon.Image = null;
					break;
				case MessageBoxIcon.Question:
					_pictureBoxIcon.Image = SystemIcons.Question.ToBitmap();
					break;
				case MessageBoxIcon.Warning:
					_pictureBoxIcon.Image = SystemIcons.Warning.ToBitmap();
					break;
			}
		}

		private void SetButtons(MessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
					SetButtonTexts("Abort", "Retry", "Ignore", true, true, true);
					break;
				case MessageBoxButtons.OK:
					SetButtonTexts("", "OK", "", false, true, false);
					break;
				case MessageBoxButtons.OKCancel:
					SetButtonTexts("", "OK", "Cancel", false, true, true);
					break;
				case MessageBoxButtons.RetryCancel:
					SetButtonTexts("", "Retry", "Cancel", false, true, true);
					break;
				case MessageBoxButtons.YesNo:
					SetButtonTexts("", "Yes", "No", false, true, true);
					break;
				case MessageBoxButtons.YesNoCancel:
					SetButtonTexts("Yes", "No", "Cancel", true, true, true);
					break;
			}
		}

		private void SetButtonTexts(string leftText, string middleText, string rightText,
		                            bool leftVisible, bool middleVisible,
		                            bool rightVisible)
		{
			_buttonLeft.Text = leftText;
			_buttonLeft.Visible = leftVisible;
			_buttonMiddle.Text = middleText;
			_buttonMiddle.Visible = middleVisible;
			_buttonRight.Text = rightText;
			_buttonRight.Visible = rightVisible;
		}

		private static DialogResult GetLeftDialogResult(MessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
					return DialogResult.Abort;

				case MessageBoxButtons.YesNoCancel:
					return DialogResult.Yes;
			}

			Assert.CantReach(
				"Left Button is only used in AbortRetryIgnore and YesNoCancel" +
				" -> Event should not be raised.");
			return DialogResult.Ignore;
		}

		private static DialogResult GetMiddleDialogResult(MessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
				case MessageBoxButtons.RetryCancel:
					return DialogResult.Retry;

				case MessageBoxButtons.OK:
				case MessageBoxButtons.OKCancel:
					return DialogResult.OK;

				case MessageBoxButtons.YesNo:
					return DialogResult.Yes;

				case MessageBoxButtons.YesNoCancel:
					return DialogResult.No;
			}

			Assert.CantReach(
				"Middle Button is used in all MessageBoxButton states and should be return" +
				" something reasonalbe in any case.<br>Check switch statement.");
			return DialogResult.Ignore;
		}

		private static DialogResult GetRightDialogResult(MessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case MessageBoxButtons.AbortRetryIgnore:
					return DialogResult.Ignore;

				case MessageBoxButtons.OKCancel:
				case MessageBoxButtons.RetryCancel:
				case MessageBoxButtons.YesNoCancel:
					return DialogResult.Cancel;

				case MessageBoxButtons.YesNo:
					return DialogResult.No;
			}

			Assert.CantReach(
				"Event raised for Right Button for OK where it is not used." +
				" -> Check switch statement.");
			return DialogResult.Ignore;
		}

		#endregion Non-public methods

		#region Events

		private void _buttonLeft_Click(object sender, EventArgs e)
		{
			DialogResult = GetLeftDialogResult(_buttons);
			Close();
		}

		private void _buttonMiddle_Click(object sender, EventArgs e)
		{
			DialogResult = GetMiddleDialogResult(_buttons);
			Close();
		}

		private void _buttonRight_Click(object sender, EventArgs e)
		{
			DialogResult = GetRightDialogResult(_buttons);
		}

		#endregion Events
	}
}
