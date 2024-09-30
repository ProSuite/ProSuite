using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geodatabase
{
	public class Subtype
	{
		public Subtype(int code, [NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Code = code;
			Name = name;
		}

		public int Code { get; }

		[NotNull]
		public string Name { get; }
	}
}
