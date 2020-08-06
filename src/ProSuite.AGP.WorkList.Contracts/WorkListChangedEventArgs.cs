using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public class WorkListChangedEventArgs
	{
		[CanBeNull]
		public Envelope Extent { get; }
		[CanBeNull]
		public List<long> Items { get; }

		public WorkListChangedEventArgs([CanBeNull] Envelope extent, [CanBeNull] List<long> items)
		{
			Extent = extent;
			Items = items;
		}
	}
}
