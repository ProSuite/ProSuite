using System;

namespace ProSuite.Commons.Validation
{
	public class GreaterOrEqualToZeroAttribute : ValidationAttribute
	{
		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				return;
			}

			double doubleValue = Convert.ToDouble(rawValue);
			if (doubleValue < 0)
			{
				LogMessage(notification, "Must be greater or equal to zero");
			}
		}
	}
}