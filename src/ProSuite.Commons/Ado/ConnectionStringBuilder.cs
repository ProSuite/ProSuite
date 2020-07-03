using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Ado
{
	public class ConnectionStringBuilder
	{
		private readonly DbConnectionStringBuilder _builder;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionStringBuilder"/> class.
		/// </summary>
		public ConnectionStringBuilder() : this(null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionStringBuilder"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public ConnectionStringBuilder([CanBeNull] string connectionString)
		{
			_builder = new DbConnectionStringBuilder {ConnectionString = connectionString};
		}

		#endregion

		[NotNull]
		public IList<KeyValuePair<string, string>> GetEntries()
		{
			var result = new List<KeyValuePair<string, string>>();

			ICollection keywords = _builder.Keys;

			foreach (string keyword in keywords)
			{
				object value = _builder[keyword];

				if (value == null)
				{
					continue;
				}

				var stringValue = value as string;

				if (stringValue == null)
				{
					Assert.Fail("connection string value is not a string: {0}", value);
				}

				result.Add(new KeyValuePair<string, string>(keyword, stringValue));
			}

			return result;
		}

		public void Add([NotNull] string keyword, [NotNull] string newValue)
		{
			Assert.ArgumentNotNullOrEmpty(keyword, nameof(keyword));
			Assert.ArgumentNotNullOrEmpty(newValue, nameof(newValue));

			_builder.Add(keyword, newValue);
		}

		public void Update([NotNull] string keyword, [NotNull] string newValue)
		{
			Assert.ArgumentNotNullOrEmpty(keyword, nameof(keyword));
			Assert.ArgumentNotNullOrEmpty(newValue, nameof(newValue));

			_builder[keyword] = newValue;
		}

		public bool Remove([NotNull] string keyword)
		{
			Assert.ArgumentNotNullOrEmpty(keyword, nameof(keyword));

			return _builder.Remove(keyword);
		}

		[NotNull]
		public string ConnectionString => _builder.ConnectionString ?? string.Empty;
	}
}