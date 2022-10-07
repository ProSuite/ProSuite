using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.Validation
{
	public class Notification
	{
		#region statics

		[NotNull]
		public static Notification Valid()
		{
			return new Notification();
		}

		#endregion

		[NotNull] private readonly Dictionary<string, MessageBag> _bags =
			new Dictionary<string, MessageBag>();

		[NotNull] private readonly Dictionary<string, Notification> _children =
			new Dictionary<string, Notification>();

		[NotNull] private readonly List<NotificationMessage> _list =
			new List<NotificationMessage>();

		[NotNull]
		public NotificationMessage[] AllMessages
		{
			get
			{
				_list.Sort();
				return _list.ToArray();
			}
		}

		[NotNull]
		public ICollection<KeyValuePair<string, Notification>> Children => _children;

		public void Include([NotNull] Notification peer)
		{
			Assert.ArgumentNotNull(peer, nameof(peer));

			_list.AddRange(peer.AllMessages);
		}

		public bool IsValid()
		{
			if (_children.Any(pair => ! pair.Value.IsValid()))
			{
				return false;
			}

			return _list.Count == 0;
		}

		[NotNull]
		public NotificationMessage RegisterMessage([NotNull] string message,
		                                           Severity severity)
		{
			return RegisterMessage(null, message, severity);
		}

		[NotNull]
		public NotificationMessage RegisterMessage<M>(
			[NotNull] Expression<Func<M, object>> expression,
			[NotNull] string message,
			Severity severity)
		{
			Assert.ArgumentNotNull(expression, nameof(expression));
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			PropertyInfo propertyInfo = ReflectionUtils.GetProperty(expression);

			return RegisterMessage(propertyInfo.Name, message, severity);
		}

		[NotNull]
		public NotificationMessage RegisterMessage([CanBeNull] string fieldName,
		                                           [NotNull] string message,
		                                           Severity severity)
		{
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			var result = new NotificationMessage(fieldName, message) {Severity = severity};

			if (! _list.Contains(result))
			{
				_list.Add(result);

				if (result.FieldName != null)
				{
					MessagesFor(result.FieldName).Add(result);
				}
			}

			return result;
		}

		[NotNull]
		public NotificationMessage[] GetMessages([CanBeNull] string fieldName)
		{
			List<NotificationMessage> messages =
				_list.FindAll(m => Equals(m.FieldName, fieldName));

			return messages.ToArray();
		}

		public void AddChild([NotNull] string propertyName,
		                     [NotNull] Notification notification)
		{
			if (_children.ContainsKey(propertyName))
			{
				_children[propertyName] = notification;
			}
			else
			{
				_children.Add(propertyName, notification);
			}
		}

		public Notification GetChild([NotNull] string propertyName)
		{
			return _children.ContainsKey(propertyName)
				       ? _children[propertyName]
				       : Valid();
		}

		public bool HasMessage([CanBeNull] string fieldName,
		                       [NotNull] string messageText)
		{
			var message = new NotificationMessage(fieldName, messageText);
			return _list.Contains(message);
		}

		public void AliasFieldInMessages([NotNull] string fieldName,
		                                 [NotNull] string alias)
		{
			string substitution = string.Format("[{0}]", fieldName);

			foreach (NotificationMessage message in _list)
			{
				message.Substitute(substitution, alias);
			}
		}

		public bool IsValid([NotNull] string fieldName)
		{
			if (_children.Any(pair => ! pair.Value.IsValid(fieldName)))
			{
				return false;
			}

			return GetMessages(fieldName).Length == 0;
		}

		public void AssertValid()
		{
			if (IsValid())
			{
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine("Validation Failures");

			AddMessages(sb);

			throw new ApplicationException(sb.ToString());
		}

		public MessageBag MessagesFor([NotNull] string fieldName)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			MessageBag messageBag;
			if (! _bags.TryGetValue(fieldName, out messageBag))
			{
				messageBag = new MessageBag(fieldName);
				_bags.Add(fieldName, messageBag);
			}

			return messageBag;
		}

		public void ForEachField([NotNull] Action<MessageBag> action)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			foreach (MessageBag bag in _bags.Values)
			{
				action(bag);
			}
		}

		[NotNull]
		public Notification Flatten()
		{
			var list = new List<NotificationMessage>();
			Gather(list);

			var returnValue = new Notification();

			returnValue._list.AddRange(list);

			return returnValue;
		}

		public bool HasImmediateErrors(bool shallow)
		{
			if (shallow)
			{
				// match if immediate
				return _list.Find(m => m.Action == ValidationAction.Immediate) != null;
			}

			return Flatten().HasImmediateErrors(true);
		}

		private void AddMessages([NotNull] StringBuilder sb)
		{
			foreach (NotificationMessage message in _list)
			{
				sb.AppendLine(message.ToString());
			}

			foreach (KeyValuePair<string, Notification> pair in _children)
			{
				sb.AppendLine("Properties from " + pair.Key);
				pair.Value.AddMessages(sb);
			}
		}

		private void Gather([NotNull] List<NotificationMessage> list)
		{
			Assert.ArgumentNotNull(list, nameof(list));

			list.AddRange(_list);

			foreach (KeyValuePair<string, Notification> pair in _children)
			{
				pair.Value.Gather(list);
			}
		}
	}
}
