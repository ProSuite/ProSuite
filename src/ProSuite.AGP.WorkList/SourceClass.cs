using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public abstract class SourceClass : ISourceClass
	{
		private GdbTableIdentity _identity;

		protected SourceClass(GdbTableIdentity identity, IAttributeReader attributeReader)
		{
			_identity = identity;
			AttributeReader = attributeReader;
		}

		public bool HasGeometry => _identity.HasGeometry;

		public long Id => _identity.Id;

		[NotNull]
		public string Name => _identity.Name;

		public IAttributeReader AttributeReader { get; set; }

		public bool Uses(GdbTableIdentity table)
		{
			return _identity.Equals(table);
		}
	}
}
