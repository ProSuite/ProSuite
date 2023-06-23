using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel
{
	/// <summary>
	/// Read-only representation of a model from the data dictionary.
	/// </summary>
	public class BasicModel : DdxModel
	{
		public BasicModel(int ddxId,
		                  [NotNull] string name,
		                  bool elementNamesAreQualified = true) : base(name)
		{
			Id = ddxId;

			// TODO: Determine when datasets are added
			ElementNamesAreQualified = elementNamesAreQualified;
		}

		public new int Id { get; }

		#region Overrides of DdxModel

		public override string QualifyModelElementName(string modelElementName)
		{
			throw new NotImplementedException();
		}

		protected override void CheckAssignSpecialDatasetCore(Dataset dataset)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
