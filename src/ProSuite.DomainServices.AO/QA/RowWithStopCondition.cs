using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public class RowWithStopCondition
	{
		private readonly StopInfo _stopInfo;

		public RowWithStopCondition([NotNull] string tableName,
		                            long oid,
		                            [NotNull] StopInfo stopInfo)
		{
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));
			Assert.ArgumentNotNull(stopInfo, nameof(tableName));

			OID = oid;
			TableName = tableName;
			_stopInfo = stopInfo;
		}

		[UsedImplicitly]
		public long OID { get; }

		[UsedImplicitly]
		[NotNull]
		public string TableName { get; }

		[NotNull]
		public QualityCondition StopCondition => _stopInfo.QualityCondition;
	}
}
