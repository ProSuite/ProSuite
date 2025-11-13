using System;
using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IFields : IEnumerable<IField>
	{
		int FieldCount { get; }

		[Obsolete("Use indexer")]
		IList<IField> Field { get; }

		IField this[int index] { get; }

		IField get_Field(int index);

		int FindField(string name);

		int FindFieldByAliasName(string name);
	}
}
