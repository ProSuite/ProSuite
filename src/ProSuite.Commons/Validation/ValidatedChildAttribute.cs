using System;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidatedChildAttribute : ValidationAttribute
	{
		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			Notification child = Validator.ValidateObject(rawValue);
			notification.AddChild(PropertyName, child);
		}
	}
}