using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Core
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class SqlExpressionAttribute : Attribute
	{
		public SqlExpressionAttribute([NotNull] string tableParameter,
		                              [CanBeNull] string tablePrefixes = null,
		                              [CanBeNull] string extraFields = null)
		{
			TableParameter = tableParameter;
			TablePrefixes = tablePrefixes;
		}

		[NotNull]
		public string TableParameter { get; }

		[CanBeNull]
		public string TablePrefixes { get; }
	}
}
