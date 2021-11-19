using Castle.Core.Resource;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.IoC
{
	internal class RootedStaticContentResource : StaticContentResource
	{
		public RootedStaticContentResource([NotNull] string contents,
		                                   [CanBeNull] string rootPath = null)
			: base(contents)
		{
			FileBasePath = string.IsNullOrEmpty(rootPath)
				               ? DefaultBasePath
				               : rootPath;
		}

		[NotNull]
		public override string FileBasePath { get; }
	}
}
