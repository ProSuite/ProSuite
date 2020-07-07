using System;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public partial class ObjectReferenceControl : UserControl
	{
		private string _displayMember;
		private Func<object, string> _formatTextDelegate;
		private object _dataSource;
		private bool _readOnly;
		private Func<object> _findObjectDelegate;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectReferenceControl"/> class.
		/// </summary>
		public ObjectReferenceControl()
		{
			InitializeComponent();
		}

		#endregion

		public event EventHandler Changed;

		public bool ReadOnly
		{
			get { return _readOnly; }
			set
			{
				if (_readOnly != value)
				{
					_readOnly = value;

					UpdateAppearance();
				}
			}
		}

		public Func<object> FindObjectDelegate
		{
			get { return _findObjectDelegate; }
			set
			{
				if (_findObjectDelegate != value)
				{
					_findObjectDelegate = value;

					UpdateAppearance();
				}
			}
		}

		public string DisplayMember
		{
			get { return _displayMember; }
			set
			{
				if (_displayMember != value)
				{
					_displayMember = value;
					if (_dataSource != null)
					{
						BindToSource();
					}
				}
			}
		}

		public Func<object, string> FormatTextDelegate
		{
			get { return _formatTextDelegate; }
			set
			{
				if (_formatTextDelegate != value)
				{
					_formatTextDelegate = value;
					if (_dataSource != null)
					{
						BindToSource();
					}
				}
			}
		}

		public object DataSource
		{
			get { return _dataSource; }
			set { SetDataSource(value); }
		}

		#region Non-public members

		protected virtual void OnChanged(EventArgs e)
		{
			if (Changed != null)
			{
				Changed(this, e);
			}
		}

		private void BindToSource()
		{
			string text;
			if (_dataSource == null)
			{
				text = string.Empty;
			}
			else
			{
				text = null;
				if (_formatTextDelegate != null)
				{
					text = _formatTextDelegate(_dataSource);
				}

				if (text == null)
				{
					if (! string.IsNullOrEmpty(_displayMember))
					{
						Type type = _dataSource.GetType();
						PropertyInfo property = type.GetProperty(_displayMember);

						if (property == null)
						{
							throw new InvalidOperationException(
								string.Format("Property {0} not found on type {1}",
								              _displayMember,
								              type.FullName));
						}

						text = property.GetValue(_dataSource, null).ToString();
					}
					else
					{
						text = _dataSource.ToString();
					}
				}
			}

			_textBox.Text = text;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			UpdateAppearance();
		}

		private void SetDataSource(object value)
		{
			if (_dataSource != value)
			{
				_dataSource = value;

				BindToSource();

				OnChanged(EventArgs.Empty);
			}

			UpdateAppearance();
		}

		private void UpdateAppearance()
		{
			_buttonClear.Enabled = ! _readOnly && _dataSource != null;
			_buttonFind.Enabled = ! _readOnly && _findObjectDelegate != null;
		}

		private void _buttonClear_Click(object sender, EventArgs e)
		{
			SetDataSource(null);
		}

		private void _buttonFind_Click(object sender, EventArgs e)
		{
			Assert.NotNull(_findObjectDelegate, "Delegate not defined");

			object result = _findObjectDelegate();

			if (result != null)
			{
				SetDataSource(result);
			}
		}

		#endregion
	}
}
