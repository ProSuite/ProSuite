using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	public class Validator : IValidator
	{
		private static readonly Dictionary<Type, List<ValidationAttribute>>
			_attributeDictionary
				= new Dictionary<Type, List<ValidationAttribute>>();

		private static readonly object _locker = new object();

		#region IValidator Members

		public Notification Validate(object target)
		{
			return ValidateObject(target);
		}

		NotificationMessage[] IValidator.ValidateByField(object target,
		                                                 string propertyName)
		{
			return ValidateField(target, propertyName);
		}

		#endregion

		[NotNull]
		public static List<ValidationAttribute> FindAttributes([NotNull] Type type)
		{
			var result = new List<ValidationAttribute>();

			foreach (PropertyInfo property in type.GetProperties())
			{
				var attributes = Attribute.GetCustomAttributes(
					property, typeof(ValidationAttribute));

				foreach (var attribute in attributes.OfType<ValidationAttribute>())
				{
					attribute.Property = property;
					result.Add(attribute);
				}
			}

			return result;
		}

		[NotNull]
		public static Notification ValidateObject([CanBeNull] object target)
		{
			var notification = new Notification();

			if (target == null)
			{
				return notification;
			}

			if (target is IValidated validated)
			{
				validated.Validate(notification);
			}

			List<ValidationAttribute> atts = ScanType(target.GetType());
			foreach (ValidationAttribute att in atts)
			{
				att.Validate(target, notification);
			}

			return notification;
		}

		[NotNull]
		private static List<ValidationAttribute> ScanType([NotNull] Type type)
		{
			if (! _attributeDictionary.ContainsKey(type))
			{
				lock (_locker)
				{
					if (! _attributeDictionary.ContainsKey(type))
					{
						_attributeDictionary.Add(type, FindAttributes(type));
					}
				}
			}

			return _attributeDictionary[type];
		}

		public static void AssertValid([CanBeNull] object target)
		{
			Notification notification = ValidateObject(target);

			if (! notification.IsValid())
			{
				throw new ApplicationException(string.Format("{0} was not valid", target));
			}
		}

		[NotNull]
		public static NotificationMessage[] ValidateField([NotNull] object target,
		                                                  [NotNull] string propertyName)
		{
			List<ValidationAttribute> atts = ScanType(target.GetType());
			List<ValidationAttribute> list =
				atts.FindAll(att => att.PropertyName == propertyName);

			var notification = new Notification();
			foreach (ValidationAttribute attribute in list)
			{
				attribute.Validate(target, notification);
			}

			if (target is IValidated validated)
			{
				validated.Validate(notification);
			}

			return notification.GetMessages(propertyName);
		}
	}
}
