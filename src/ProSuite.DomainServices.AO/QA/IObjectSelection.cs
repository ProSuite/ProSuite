using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	[CLSCompliant(false)]
	public interface IObjectSelection
	{
		bool Contains([NotNull] InvolvedRow involvedRow,
		              [NotNull] QualityCondition qualityCondition);

		bool Contains([NotNull] InvolvedRow involvedRow,
		              [NotNull] QualityCondition qualityCondition,
		              out bool unknownTable);

		[NotNull]
		IEnumerable<int> GetSelectedOIDs([NotNull] IObjectDataset dataset);
	}
}
