using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Ado
{
	public class ConnectionStringBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly DbConnectionStringBuilder _builder;

		/// <summary>
		/// </summary>
		/// <param name="keyword">Must be lower case</param>
		/// <returns></returns>
		public string this[string keyword]
		{
			get
			{
				if (_builder.TryGetValue(keyword, out object value))
				{
					return ConvertToString(value);
				}

				_msg.DebugFormat("no keyword {0} in {1}", keyword, _builder.ConnectionString);
				return string.Empty;
			}
		}

		public bool TryGetValue(string keyword, out string value)
		{
			if (_builder.TryGetValue(keyword, out object obj))
			{
				value = ConvertToString(obj);
				return true;
			}

			value = string.Empty;

			return false;
		}

		[NotNull]
		public string ConnectionString => _builder.ConnectionString;

		[NotNull]
		public Dictionary<string, string> GetEntries()
		{
			var result = new Dictionary<string, string>();

			ICollection keywords = _builder.Keys;
			Assert.NotNull(keywords);

			foreach (string keyword in keywords)
			{
				object value = _builder[keyword];

				if (value == null)
				{
					continue;
				}

				result.Add(keyword, ConvertToString(value));
			}

			return result;
		}

		private static string ConvertToString(object value)
		{
			var stringValue = value as string;

			if (stringValue == null)
			{
				Assert.Fail("connection string value is not a string: {0}", value);
			}

			return stringValue;
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

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionStringBuilder" /> class.
		/// </summary>
		public ConnectionStringBuilder() : this(string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionStringBuilder" /> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public ConnectionStringBuilder([NotNull] string connectionString)
		{
			_builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
		}

		#endregion
	}
}
