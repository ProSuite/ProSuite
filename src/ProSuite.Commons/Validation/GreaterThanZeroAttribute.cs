using System;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property)]
	public class GreaterThanZeroAttribute : ValidationAttribute
	{
		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				return;
			}

			double doubleValue = Convert.ToDouble(rawValue);
			// double doubleValue = (double) rawValue;
			if (doubleValue <= 0)
			{
				LogMessage(notification, "Must be a positive number");
			}
		}
	}
}