using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

// TODO: Separate implementation of interface IDdxDataset to avoid having various Id values
public class BasicDataset : ObjectDataset
{
	public BasicDataset(int ddxId,
	                    [NotNull] string name,
	                    [CanBeNull] string abbreviation = null,
	                    [CanBeNull] string aliasName = null)
		: base(name, abbreviation, aliasName)
	{
		Id = ddxId;
	}

	public new int Id { get; }

	public override bool HasGeometry { get; }

	#region Overrides of Dataset

	public override DatasetType DatasetType { get; }

	#endregion
}
