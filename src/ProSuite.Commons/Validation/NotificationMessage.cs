using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Validation
{
	public class NotificationMessage : IComparable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationMessage"/> class.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="message">The message.</param>
		public NotificationMessage([CanBeNull] string fieldName,
		                           [NotNull] string message)
		{
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			FieldName = fieldName;
			Message = message;
		}

		public Severity Severity { get; set; } = Severity.Error;

		public ValidationAction Action { get; set; } = ValidationAction.Normal;

		public string FieldName { get; set; }

		public string Message { get; set; }

		#region IComparable Members

		public int CompareTo(object obj)
		{
			var message = (NotificationMessage) obj;

			return Equals(FieldName, message.FieldName)
				       ? Message.CompareTo(message.Message)
				       : string.Compare(FieldName, message.FieldName);
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var notificationMessage = obj as NotificationMessage;
			if (notificationMessage == null)
			{
				return false;
			}

			return Equals(FieldName, notificationMessage.FieldName) &&
			       Equals(Message, notificationMessage.Message) &&
			       Equals(Severity, notificationMessage.Severity);
		}

		public override int GetHashCode()
		{
			return
				(FieldName != null
					 ? FieldName.GetHashCode()
					 : 0) +
				29 * (Message != null
					      ? Message.GetHashCode()
					      : 0);
		}

		public override string ToString()
		{
			return FieldName != null
				       ? $"Field '{FieldName}': {Message}"
				       : Message;
		}

		public void Substitute([NotNull] string substitution,
		                       [NotNull] string alias)
		{
			Message = Message.Replace(substitution, alias);
		}
	}
}