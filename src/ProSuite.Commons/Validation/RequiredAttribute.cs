using System;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property)]
	public class RequiredAttribute : ValidationAttribute
	{
		protected override void ValidateCore(object target,
		                                     [CanBeNull] object rawValue,
		                                     Notification notification)
		{
			if (rawValue == null || string.Empty.Equals(rawValue))
			{
				LogMessage(notification, GetMessage());
			}
		}

		protected virtual string GetMessage()
		{
			return "Required Field";
		}

		public static bool IsRequired([NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(property, nameof(property));

			var attribute =
				GetCustomAttribute(property, typeof(RequiredAttribute)) as
					RequiredAttribute;

			return attribute != null &&
			       attribute.GetType().Equals(typeof(RequiredAttribute));
		}
	}
}
