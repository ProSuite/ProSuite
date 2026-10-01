using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// What a standalone condition list declares about one dataset: the same information as its
	/// <c>Dataset</c> element, in resolved form - role names looked up, no XML types.
	/// <para>
	/// It exists so that the model builders do not have to read the document format. They live in
	/// the service layer and must work the same whether the specification arrived as XML or, once
	/// the declarations are carried there too, as protobuf; this is the shape both produce.
	/// </para>
	/// </summary>
	public class DatasetDeclaration
	{
		public DatasetDeclaration([NotNull] string datasetName,
		                          SupportedDatasetType datasetType,
		                          [NotNull] IList<DatasetFieldRole> fieldRoles)
		{
			Assert.ArgumentNotNullOrEmpty(datasetName, nameof(datasetName));
			Assert.ArgumentCondition(datasetType != SupportedDatasetType.Null,
			                         "The dataset type must be declared", nameof(datasetType));
			Assert.ArgumentNotNull(fieldRoles, nameof(fieldRoles));

			DatasetName = datasetName;
			DatasetType = datasetType;
			FieldRoles = fieldRoles;
		}

		[NotNull]
		public string DatasetName { get; }

		public SupportedDatasetType DatasetType { get; }

		[NotNull]
		public IList<DatasetFieldRole> FieldRoles { get; }

		public override string ToString()
		{
			return $"{DatasetName} as {DatasetType} " +
			       $"[{StringUtils.Concatenate(FieldRoles, ", ")}]";
		}
	}
}
