using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid.Test
{
	public class TestReport
	{
		private readonly List<VerifiedDataset> _verifiedDatasets =
			new List<VerifiedDataset>();

		[UsedImplicitly]
		public DateTime StartTime { get; set; }

		[NotNull]
		[UsedImplicitly]
		public List<VerifiedDataset> VerifiedDatasets
		{
			get { return _verifiedDatasets; }
		}

		public void AddVerifiedDataset([NotNull] VerifiedDataset verifiedDataset)
		{
			_verifiedDatasets.Add(verifiedDataset);
		}
	}
}
