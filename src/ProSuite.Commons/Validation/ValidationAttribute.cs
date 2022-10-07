using System;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class ValidationAttribute : Attribute
	{
		private ValidationAction _action = ValidationAction.Normal;
		private PropertyInfo _property;
		private Severity _severity = Severity.Error;

		public PropertyInfo Property
		{
			get { return _property; }
			set { _property = value; }
		}

		public string PropertyName
		{
			get { return _property.Name; }
		}

		public ValidationAction Action
		{
			get { return _action; }
			set { _action = value; }
		}

		public Severity Severity
		{
			get { return _severity; }
			set { _severity = value; }
		}

		public void Validate([NotNull] object target,
		                     [NotNull] Notification notification)
		{
			object rawValue = _property.GetValue(target, null);
			ValidateCore(target, rawValue, notification);
		}

		protected void LogMessage([NotNull] Notification notification,
		                          [NotNull] string message)
		{
			notification.RegisterMessage(Property.Name, message, _severity)
			            .Action = _action;
		}

		protected abstract void ValidateCore([NotNull] object target,
		                                     object rawValue,
		                                     [NotNull] Notification notification);
	}
}
