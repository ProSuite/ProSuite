using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.Geodatabase;
using Cursor = System.Windows.Forms.Cursor;

namespace ProSuite.DdxEditor.Content.Connections
{
	public partial class ConnectionProviderControl<T> : UserControl,
	                                                    IEntityPanel<T>
		where T : ConnectionProvider
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ConnectionProviderItem<T> _item;

		public ConnectionProviderControl(ConnectionProviderItem<T> item)
		{
			_item = item;
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Connection Provider Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.TypeDescription)
			      .To(_textBoxConnectionType)
			      .AsReadOnly()
			      .WithLabel(_labelType);
		}

		public void OnBoundTo(T entity) { }

		#endregion

		private void _buttonTest_Click(object sender, EventArgs e)
		{
			Cursor normalCursor = Cursor;

			try
			{
				Cursor = Cursors.WaitCursor;

				T entity = Assert.NotNull(_item.GetEntity());

				Assert.NotNull(entity.OpenWorkspace(Handle.ToInt32()), "null workspace returned");

				const bool passed = true;
				ReportTestResult(passed);
			}
			catch (Exception exception)
			{
				if (exception is COMException comException &&
				    comException.ErrorCode == (decimal) fdoError.FDO_E_CONNECTION_CANCELLED)
				{
					// cancelled by the user
					_msg.WarnFormat("Connection cancelled");
				}
				else
				{
					const bool passed = false;
					ReportTestResult(passed, exception);
				}
			}
			finally
			{
				Cursor = normalCursor;
			}
		}

		private static void ReportTestResult(bool passed)
		{
			ReportTestResult(passed, null);
		}

		private static void ReportTestResult(bool passed, [CanBeNull] Exception e)
		{
			if (passed)
			{
				MessageBox.Show(@"Connection succeeded.",
				                @"Test Connection",
				                MessageBoxButtons.OK,
				                MessageBoxIcon.Information);
			}
			else
			{
				if (e == null)
				{
					MessageBox.Show(@"Failed to connect.",
					                @"Test Connection",
					                MessageBoxButtons.OK,
					                MessageBoxIcon.Warning);
				}
				else
				{
					string msg = string.Format("Failed to connect.{0}Error: {1}",
					                           Environment.NewLine, e.Message);

					_msg.Debug("Failed to connect", e);

					MessageBox.Show(msg,
					                @"Test Connection",
					                MessageBoxButtons.OK,
					                MessageBoxIcon.Warning);
				}
			}
		}
	}
}
