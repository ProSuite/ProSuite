using System;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI;

public enum LogMessageAction
{
	Details,

	// for later use - "links" in LogMassages allows to perform actions on message content with payload
	LaunchFileExplorer,
	AddLayer
}

public class LogMessageActionEventArgs : EventArgs
{
	public LoggingEventItem LogMessage { get; }

	public LogMessageAction MessageAction { get; }

	[CanBeNull]
	public string MessageActionPayload { get; }

	public LogMessageActionEventArgs(LoggingEventItem message,
	                                 LogMessageAction messageActionType,
	                                 string messageActionPayload = null)
	{
		LogMessage = message;
		MessageAction = messageActionType;
		MessageActionPayload = messageActionPayload;
	}
}

/// <summary>
/// Occurs when user clicks on a log message
/// </summary>
public sealed class LogMessageActionEvent : CompositePresentationEvent<LogMessageActionEventArgs>
{
	public static SubscriptionToken Subscribe(Action<LogMessageActionEventArgs> action,
	                                          bool keepSubscriberReferenceAlive = false)
	{
		return GetEvent().Register(action, keepSubscriberReferenceAlive);
	}

	public static void Unsubscribe(Action<LogMessageActionEventArgs> subscriber)
	{
		GetEvent().Unregister(subscriber);
	}

	public static void Unsubscribe(SubscriptionToken token)
	{
		GetEvent().Unregister(token);
	}

	internal static void Publish(LogMessageActionEventArgs payload)
	{
		GetEvent().Broadcast(payload);
	}

	private static LogMessageActionEvent GetEvent()
	{
		return FrameworkApplication.EventAggregator.GetEvent<LogMessageActionEvent>();
	}
}
