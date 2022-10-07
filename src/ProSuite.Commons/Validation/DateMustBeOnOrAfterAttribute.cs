using System;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class DateMustBeOnOrAfterAttribute : ValidationAttribute
	{
		private readonly string _comparisonField;
		private PropertyInfo _comparisonProperty;
		private string _format = "[{0}] must be on or after [{1}]";
		private string _message;

		/// <summary>
		/// Initializes a new instance of the <see cref="DateMustBeOnOrAfterAttribute"/> class.
		/// </summary>
		/// <param name="comparisonField">The comparison field.</param>
		public DateMustBeOnOrAfterAttribute(string comparisonField)
		{
			_comparisonField = comparisonField;
		}

		public string Message
		{
			get { return _message; }
			set { _message = value; }
		}

		public string Format
		{
			get { return _format; }
			set { _format = value; }
		}

		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (_comparisonProperty == null)
			{
				_comparisonProperty = target.GetType().GetProperty(_comparisonField);
			}

			var baselineDate =
				(DateTime?) Assert.NotNull(_comparisonProperty).GetValue(target, null);

			var dateBeingValidated = (DateTime?) rawValue;

			if (IsDate1BeforeDate2(dateBeingValidated, baselineDate))
			{
				string message = getMessage();
				LogMessage(notification, message);
			}
		}

		private string getMessage()
		{
			if (! string.IsNullOrEmpty(_message))
			{
				return _message;
			}

			return string.Format(_format, PropertyName, _comparisonField);
		}

		public static bool IsDate1BeforeDate2(DateTime? date1, DateTime? date2)
		{
			if (date1.HasValue && date2.HasValue)
			{
				return date1.Value < date2.Value;
			}

			return false;
		}
	}
}
