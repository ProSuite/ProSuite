using System;
using System.Reflection;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property)]
	public class LessThanOrEqualAttribute : ValidationAttribute
	{
		private readonly double _maxValue;

		public LessThanOrEqualAttribute(double maxValue)
		{
			_maxValue = maxValue;
		}

		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				return;
			}

			double value = Convert.ToDouble(rawValue);

			if (value.CompareTo(_maxValue) > 0)
			{
				LogMessage(notification, string.Format("Must be less or equal to {0}", _maxValue));
			}
		}

		public static object GetMaximumValue(PropertyInfo property)
		{
			if (! (property.PropertyType is IComparable))
			{
				return 0;
			}

			var attribute =
				GetCustomAttribute(property, typeof(LessThanOrEqualAttribute)) as
					LessThanOrEqualAttribute;

			return attribute?._maxValue ?? double.MaxValue;
		}
	}
}
