using System;
using System.Collections;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public class ErrorProviderValidationMonitor : IValidationMonitor
	{
		[NotNull] private readonly ErrorProvider _errorProvider;
		[CanBeNull] private IScreenBinder _binder;

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorProviderValidationMonitor"/> class.
		/// </summary>
		/// <param name="errorProvider">The error provider.</param>
		public ErrorProviderValidationMonitor([NotNull] ErrorProvider errorProvider)
		{
			Assert.ArgumentNotNull(errorProvider, nameof(errorProvider));

			_errorProvider = errorProvider;
		}

		#region IValidationMonitor Members

		public IScreenBinder Binder
		{
			set { _binder = value; }
		}

		public void ClearErrors()
		{
			_errorProvider.Clear();
		}

		public void ShowErrorMessages(IScreenElement element, params string[] messages)
		{
			ShowMessagesCore(element, messages);
		}

		public void ShowErrorMessages(Notification notification)
		{
			Assert.ArgumentNotNull(notification, nameof(notification));
			Assert.NotNull(_binder, "binder not defined");

			foreach (IScreenElement element in _binder)
			{
				var boundElement = element as IBoundScreenElement;

				if (boundElement == null)
				{
					continue;
				}

				ShowMessagesCore(element, notification.GetMessages(boundElement.FieldName));
			}
		}

		public void ValidateField(IBoundScreenElement element, object model)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			NotificationMessage[] messages = element.Validate();

			ShowMessagesCore(element, messages);
		}

		#endregion

		private void ShowMessagesCore([NotNull] IScreenElement element,
		                              [NotNull] ICollection messages)
		{
			Assert.ArgumentNotNull(element, nameof(element));
			Assert.ArgumentNotNull(messages, nameof(messages));

			var control = element.Control as Control;
			if (control == null)
			{
				return;
			}

			if (messages.Count == 0)
			{
				_errorProvider.SetError(control, string.Empty);
			}
			else
			{
				string message = StringUtils.Concatenate(messages,
				                                         Environment.NewLine);

				_errorProvider.SetError(control, message);
			}
		}
	}
}
