using System;

namespace ProSuite.Commons.Validation
{
	/// <summary>
	/// Property validation attribute:
	/// Require a property to be a valid Entity name,
	/// that is, a non-empty string without fancy characters.
	/// By default, "\0\a\b\f\n\r\t\v" are considered fancy
	/// characters, but this can be redefined with an overloaded
	/// constructor. Implies that a property is required.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidNameAttribute : ValidationAttribute
	{
		private readonly string _invalidChars = "\0\a\b\f\n\r\t\v";
		private readonly string _message = "must be a valid name";

		#region Constructors

		public ValidNameAttribute() { }

		public ValidNameAttribute(string invalidChars)
			: this(invalidChars, null) { }

		public ValidNameAttribute(string invalidChars, string message)
		{
			if (! string.IsNullOrEmpty(message))
			{
				_message = message;
			}

			_invalidChars = invalidChars;
		}

		#endregion

		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				LogMessage(notification, GetMessage("is missing"));
				return;
			}

			string stringValue = rawValue.ToString();

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (stringValue == null) // ignore R# (ToString() implementation might be incorrect)
			{
				LogMessage(notification, GetMessage("is missing"));
				return;
			}

			stringValue = stringValue.Trim();

			if (stringValue.Length < 1)
			{
				LogMessage(notification, GetMessage("is empty"));
			}
			else if (stringValue.IndexOfAny(_invalidChars.ToCharArray()) >= 0)
			{
				LogMessage(notification, GetMessage("contains invalid characters"));
			}
		}

		protected virtual string GetMessage(string errorString)
		{
			return string.Format("{0} (now it {1})", _message, errorString);
		}
	}
}
