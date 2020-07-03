using System;

namespace ProSuite.Commons.Validation
{
	/// <summary>
	/// Property validation attribute:
	/// Require a property to be a valid token, that is,
	/// precisely one sequence of non-white-space characters.
	/// Optionally, the property value can be trimmed before
	/// validation and/or additional "invalid" characters can
	/// be specified. Implies that a property is required.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidTokenAttribute : ValidationAttribute
	{
		private const string _whiteSpaceChars = " \0\a\b\f\n\r\t\v";
		private readonly string _invalidChars = string.Empty;
		private readonly string _message = "must be a single token";
		private bool _trimBeforeValidation;
		private bool _optional;

		#region Constructors

		public ValidTokenAttribute() { }

		public ValidTokenAttribute(string message)
			: this(false, message) { }

		public ValidTokenAttribute(bool trimBeforeValidation)
			: this(trimBeforeValidation, null) { }

		public ValidTokenAttribute(bool trimBeforeValidation, string message)
		{
			if (! string.IsNullOrEmpty(message))
			{
				_message = message;
			}

			_trimBeforeValidation = trimBeforeValidation;
		}

		// TODO: user-provided set of invalid chars

		#endregion

		public bool TrimBeforeValidation
		{
			get { return _trimBeforeValidation; }
			set { _trimBeforeValidation = value; }
		}

		public bool Optional
		{
			get { return _optional; }
			set { _optional = value; }
		}

		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				if (! _optional)
				{
					LogMessage(notification, GetMessage("is missing"));
				}

				return;
			}

			string stringValue = rawValue.ToString();

			if (stringValue == null) // ignore R# (ToString() implementation might be incorrect)
			{
				if (! _optional)
				{
					LogMessage(notification, GetMessage("is missing"));
				}

				return;
			}

			if (_trimBeforeValidation)
			{
				stringValue = stringValue.Trim();
			}

			if (stringValue.Length < 1)
			{
				if (! _optional)
				{
					LogMessage(notification, GetMessage("is empty"));
				}
			}
			else if (stringValue.IndexOfAny(_whiteSpaceChars.ToCharArray()) >= 0)
			{
				LogMessage(notification, GetMessage("contains white space"));
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