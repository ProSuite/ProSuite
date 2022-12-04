using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
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

		public int Id { get; }

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
