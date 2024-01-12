using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Framework.Help
{
	public partial class HtmlHelpForm : Form
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly FormStateManager<FormState> _formStateManager;
		[NotNull] private string _html;

		public HtmlHelpForm([NotNull] string html)
		{
			Assert.ArgumentNotNull(html, nameof(html)); //but can be string.empty

			_html = html;
			InitializeComponent();

			_formStateManager = new FormStateManager<FormState>(this);
			_formStateManager.RestoreState();
		}

		public void NavigateToString([NotNull] string html)
		{
			Assert.ArgumentNotNull(html, nameof(html)); //but can be string.empty

			_html = html;

			ViewUtils.Try(() => _webView.NavigateToString(_html), _msg);
		}

		private async void HtmlDocumentationForm_Load(object sender, EventArgs e)
		{
			string userDataFolder = Path.GetTempPath();

			_msg.Debug($"User temp path {userDataFolder}");

			CoreWebView2Environment env =
				await ViewUtils.TryAsync(
					CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder), _msg);

			await ViewUtils.TryAsync(_webView.EnsureCoreWebView2Async(env), _msg);

			ViewUtils.Try(() => _webView.NavigateToString(_html), _msg);
		}

		private void HtmlHelpForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_formStateManager.SaveState();
		}
	}
}
