using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class UniqueId
	{
		[NotNull] private readonly UniqueIdProvider _uniqueIdProvider;

		public UniqueId([NotNull] IFeature feature,
		                [NotNull] UniqueIdProvider uniqueIdProvider)
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
