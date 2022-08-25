using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Framework.Help
{
	public partial class HtmlHelpForm : Form
	{
		[NotNull] private readonly FormStateManager<FormState> _formStateManager;
		private string _html;

		public HtmlHelpForm(string html)
		{
			_html = html;
			InitializeComponent();

			_formStateManager = new FormStateManager<FormState>(this);
			_formStateManager.RestoreState();
		}

		public void NavigateToString(string html)
		{
			_html = html;
			_webView.NavigateToString(_html);
		}

		private async void HtmlDocumentationForm_Load(object sender, EventArgs e)
		{
			await _webView.EnsureCoreWebView2Async();
			_webView.NavigateToString(_html);
		}

		private void HtmlHelpForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager.SaveState();
		}
	}
}
