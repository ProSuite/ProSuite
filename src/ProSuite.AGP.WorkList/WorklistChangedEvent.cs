using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	[UsedImplicitly]
	public class WorklistChangedEvent : RegisteredPresentationEvent<object, WorkListChangedEventArgs>
	{
		private readonly HashSet<SubscriptionToken> _tokens = new HashSet<SubscriptionToken>();

		// todo daro Interface for receiver?
		public static SubscriptionToken Subscribe(Func<WorkListChangedEventArgs, Task> action,
		                                          object receiver, bool keepSubscriberAlive = false)
		{
			return FrameworkApplication.EventAggregator.GetEvent<WorklistChangedEvent>()
			                           .Register(receiver, action, keepSubscriberAlive);
		}

		public static void Unsubscribe(Func<WorkListChangedEventArgs, Task> action)
		{
			FrameworkApplication.EventAggregator.GetEvent<WorklistChangedEvent>()
			                    .Unregister(action);
		}

		public static void Unsubscribe(SubscriptionToken token)
		{
			FrameworkApplication.EventAggregator.GetEvent<WorklistChangedEvent>().Unregister(token);
		}

		public static async Task PublishAsync(WorkListChangedEventArgs e)
		{
			await FrameworkApplication.EventAggregator.GetEvent<WorklistChangedEvent>()
			                          .BroadcastAsync(e);
		}

		protected override void OnSubscribe(object receiver, SubscriptionToken token)
		{
			if (_tokens.Contains(token))
			{
				return;
			}

			_tokens.Add(token);
		}

		protected override void OnUnsubscribe(object param, SubscriptionToken token)
		{
			if (_tokens.Contains(token))
			{
				Assert.True(_tokens.Remove(token), "{0} was not subscribed",
				            nameof(WorklistChangedEvent));
			}
		}
	}
}
