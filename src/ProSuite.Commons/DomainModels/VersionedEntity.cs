using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public abstract class VersionedEntity : Entity, IVersionedEntity
	{
		[UsedImplicitly] private int _version = -1;
		// must not be readonly, updated after construction via reflection

		public int Version
		{
			get { return _version; }
			protected set { _version = value; }
		}
	}
}