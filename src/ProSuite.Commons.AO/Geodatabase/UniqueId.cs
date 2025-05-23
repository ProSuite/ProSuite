using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class UniqueId
	{
		[NotNull] private readonly IUniqueIdProvider<IReadOnlyFeature> _uniqueIdProvider;

		public UniqueId([NotNull] IReadOnlyFeature feature,
		                [NotNull] IUniqueIdProvider<IReadOnlyFeature> uniqueIdProvider)
		{
			_uniqueIdProvider = uniqueIdProvider;
			Id = uniqueIdProvider.GetUniqueId(feature);
		}

		public long Id { get; }

		public void Drop()
		{
			_uniqueIdProvider.Remove(Id);
		}

		public override string ToString()
		{
			return $"{Id}";
		}
	}
}
