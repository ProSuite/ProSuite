using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

/// <summary>
/// Read-only representation of a model from the data dictionary.
/// </summary>
public class BasicModel : DdxModel
{
	public BasicModel(int ddxId,
	                  [NotNull] string name) : base(name)
	{
		SetCloneId(ddxId);
	}

	protected override void DatasetAddedCore<T>(T dataset)
	{
		if (ModelElementNameUtils.IsQualifiedName(dataset.Name))
		{
			ElementNamesAreQualified = true;
		}
	}

	#region Overrides of DdxModel

	public override string QualifyModelElementName(string modelElementName)
	{
		throw new NotImplementedException();
	}

	public override string TranslateToModelElementName(string masterDatabaseDatasetName)
	{
		throw new NotImplementedException();
	}

	protected override void CheckAssignSpecialDatasetCore(Dataset dataset)
	{
		throw new NotImplementedException();
	}

	#endregion
}
