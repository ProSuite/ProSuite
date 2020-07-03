using System.Reflection;

namespace ProSuite.Commons.Validation
{
	public class MaximumStringLengthAttribute : ValidationAttribute
	{
		private readonly int _length;

		public MaximumStringLengthAttribute(int length)
		{
			_length = length;
		}

		protected override void ValidateCore(object target, object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null)
			{
				return;
			}

			if (rawValue.ToString().Length > _length)
			{
				string message =
					string.Format("[{0}] cannot be longer than {1} characters",
					              PropertyName, _length);
				LogMessage(notification, message);
			}
		}

		public static int? GetLength(PropertyInfo property)
		{
			if (property.PropertyType != typeof(string))
			{
				return null;
			}

			var attribute =
				GetCustomAttribute(property, typeof(MaximumStringLengthAttribute)) as
					MaximumStringLengthAttribute;

			return attribute == null
				       ? (int?) null
				       : attribute._length;
		}
	}
}