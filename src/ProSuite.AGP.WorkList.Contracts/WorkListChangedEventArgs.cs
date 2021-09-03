using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public class WorkListChangedEventArgs
	{
		public WorkListChangedEventArgs([NotNull] object sender,
		                                [CanBeNull] Envelope extent,
		                                [CanBeNull] List<long> items)
		{
			Assert.ArgumentNotNull(sender, nameof(sender));

			Sender = sender;
			Extent = extent;
			Items = items;
		}

		[NotNull]
		public object Sender { get; }

		[CanBeNull]
		public Envelope Extent { get; }

		[CanBeNull]
		public List<long> Items { get; }
	}
}
