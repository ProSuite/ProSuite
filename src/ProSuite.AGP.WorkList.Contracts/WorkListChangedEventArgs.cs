using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public class WorkListChangedEventArgs
	{
		// todo daro order parameters, make items optional
		// todo daro rename to WorklistChangedEventArgs
		public WorkListChangedEventArgs([CanBeNull] Envelope extent,
		                                [CanBeNull] List<long> items = null)
		{
			Extent = extent;
			Items = items;
		}

		[CanBeNull]
		public Envelope Extent { get; }

		[CanBeNull]
		public List<long> Items { get; }
	}
}
