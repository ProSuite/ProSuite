using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public interface IObjectSelection
	{
		bool Contains([NotNull] InvolvedRow involvedRow,
		              [NotNull] QualityCondition qualityCondition);

		bool Contains([NotNull] InvolvedRow involvedRow,
		              [NotNull] QualityCondition qualityCondition,
		              out bool unknownTable);

		[NotNull]
		IEnumerable<long> GetSelectedOIDs([NotNull] IObjectDataset dataset);
	}
}
