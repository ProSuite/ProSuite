using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public class InvolvedTable
{
	public InvolvedTable([NotNull] string tableName,
	                     [NotNull] IEnumerable<RowReference> rowReferences,
	                     [CanBeNull] string keyField = null)
	{
		Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));
		Assert.ArgumentNotNull(rowReferences, nameof(rowReferences));

		TableName = tableName;
		KeyField = string.IsNullOrEmpty(keyField)
			           ? null
			           : keyField.Trim();
		RowReferences = rowReferences.ToList();

		if (string.IsNullOrEmpty(KeyField))
		{
			Assert.True(RowReferences.All(r => r.UsesOID), "OID row references expected");
		}
		else
		{
			Assert.True(RowReferences.All(r => r.Key != null),
			            "Alternate key references expected");
		}
	}

	[NotNull]
	public string TableName { get; }

	[CanBeNull]
	public string KeyField { get; }

	[NotNull]
	public IList<RowReference> RowReferences { get; }

	public override string ToString()
	{
		return KeyField == null
			       ? $"Table: {TableName}, Rows: {RowReferences.Count}"
			       : $"Table: {TableName}, KeyField: {KeyField}, Rows: {RowReferences.Count}";
	}
}
