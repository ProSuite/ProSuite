using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel
{
	// TODO: Separate implementation of interface IDdxDataset to avoid having various Id values
	public class BasicDataset : ObjectDataset
	{
		public BasicDataset(int ddxId,
		                    [NotNull] string name,
		                    [CanBeNull] string abbreviation,
		                    [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName)
		{
			Id = ddxId;
		}

		public new int Id { get; }

		public override bool HasGeometry { get; }
	}
}
