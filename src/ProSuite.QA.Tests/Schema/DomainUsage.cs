using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Schema
{
	/// <summary>
	/// Represents the usage of a given domain in a table.
	/// </summary>
	internal class DomainUsage
	{
		private readonly HashSet<IField> _fields = new HashSet<IField>();

		/// <summary>
		/// Initializes a new instance of the <see cref="DomainUsage"/> class.
		/// </summary>
		/// <param name="domain">The domain.</param>
		public DomainUsage([NotNull] IDomain domain)
		{
			Assert.ArgumentNotNull(domain, nameof(domain));

			Domain = domain;
		}

		[NotNull]
		public IDomain Domain { get; }

		[NotNull]
		public string DomainName => Domain.Name ?? string.Empty;

		internal void AddReferenceFrom([NotNull] IField field)
		{
			_fields.Add(field);
		}

		[NotNull]
		public IEnumerable<IField> ReferencingFields => _fields;
	}
}
