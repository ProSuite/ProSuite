using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using System;

namespace ProSuite.AGP.Solution.LoggerUI
{
	public enum LogMessageAction
	{
		Details,
		// for later use - "links" in LogMassages allows to perform actions on message content with payload
		LaunchFileExplorer, 
		AddLayer
	}

	public class LogMessageActionEventArgs : EventBase
	{
		public LoggingEventItem LogMessage { get; }

		public LogMessageAction MessageAction { get; }

		[CanBeNull] public string MessageActionPayload { get; }

		public LogMessageActionEventArgs(LoggingEventItem message, LogMessageAction messageActionType, string messageActionPayload = null)
		{
			LogMessage = message;
			MessageAction = messageActionType;
			MessageActionPayload = messageActionPayload;
		}
	}

	public class LogMessageActionEvent : CompositePresentationEvent<LogMessageActionEventArgs>
	{
		public static SubscriptionToken Subscribe(Action<LogMessageActionEventArgs> action, bool keepSubscriberReferenceAlive = false)
		{
			return FrameworkApplication.EventAggregator.GetEvent<LogMessageActionEvent>()
			                           .Register(action, keepSubscriberReferenceAlive);
		}

		public static void Unsubscribe(Action<LogMessageActionEventArgs> subscriber)
		{
			FrameworkApplication.EventAggregator.GetEvent<LogMessageActionEvent>().Unregister(subscriber);
		}
		public static void Unsubscribe(SubscriptionToken token)
		{
			FrameworkApplication.EventAggregator.GetEvent<LogMessageActionEvent>().Unregister(token);
		}

		internal static void Publish(LogMessageActionEventArgs payload)
		{
			FrameworkApplication.EventAggregator.GetEvent<LogMessageActionEvent>().Broadcast(payload);
		}
	}
}
