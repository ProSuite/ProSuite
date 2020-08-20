using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;

namespace Clients.AGP.ProSuiteSolution.ConfigUI
{
	public class ProSuiteConfigEventArgs : EventBase
	{
		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }
		public ProSuiteQASpecificationsConfiguration SpecificationsConfiguration { get; set; }

		// temporary only QA specific parameters
		public ProSuiteConfigEventArgs(IEnumerable<ProSuiteQAServerConfiguration> serverConfigs, ProSuiteQASpecificationsConfiguration specificationsConfig)
		{
			ServerConfigurations = serverConfigs;
			SpecificationsConfiguration = specificationsConfig;
		}
	}

	public class ProSuiteConfigChangedEvent : CompositePresentationEvent<ProSuiteConfigEventArgs>
	{
		public static SubscriptionToken Subscribe(Action<ProSuiteConfigEventArgs> action, bool keepSubscriberReferenceAlive = false)
		{
			return FrameworkApplication.EventAggregator.GetEvent<ProSuiteConfigChangedEvent>()
				.Register(action, keepSubscriberReferenceAlive);
		}

		public static void Unsubscribe(Action<ProSuiteConfigEventArgs> subscriber)
		{
			FrameworkApplication.EventAggregator.GetEvent<ProSuiteConfigChangedEvent>().Unregister(subscriber);
		}
		public static void Unsubscribe(SubscriptionToken token)
		{
			FrameworkApplication.EventAggregator.GetEvent<ProSuiteConfigChangedEvent>().Unregister(token);
		}

		internal static void Publish(ProSuiteConfigEventArgs payload)
		{
			FrameworkApplication.EventAggregator.GetEvent<ProSuiteConfigChangedEvent>().Broadcast(payload);
		}
	}
}
