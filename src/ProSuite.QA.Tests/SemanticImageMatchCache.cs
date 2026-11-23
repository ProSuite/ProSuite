using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Keeps the checked results of a feature class, rule and image in a file, so that
	/// a repeated verification sends only new or changed features, and pays only for
	/// those. A result is valid while the fingerprint of the feature's geometry is
	/// unchanged. Losing the file, or failing to write it, only costs money.
	/// </summary>
	public class SemanticImageMatchCache
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

		[NotNull] private readonly string _filePath;
		[CanBeNull] private Dictionary<long, CacheEntry> _entries;

		/// <param name="directory">The cache folder, created if missing.</param>
		/// <param name="scope">Everything a cached result depends on besides the
		/// feature itself: feature class, image, rule.</param>
		public SemanticImageMatchCache([NotNull] string directory, [NotNull] string scope)
		{
			Assert.ArgumentNotNullOrEmpty(directory, nameof(directory));
			Assert.ArgumentNotNullOrEmpty(scope, nameof(scope));

			Directory.CreateDirectory(directory);
			_filePath = Path.Combine(directory, GetFingerprint(scope).Substring(0, 32) + ".json");
		}

		[NotNull]
		public string FilePath => _filePath;

		/// <summary>
		/// A short hexadecimal value computed from the text. Any change of the text
		/// changes it.
		/// </summary>
		[NotNull]
		public static string GetFingerprint([NotNull] string text)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
				return string.Concat(hash.Select(b => b.ToString("x2")));
			}
		}

		public bool TryGet(long objectId,
		                   [NotNull] string geometryFingerprint,
		                   out FeatureCheckResult result)
		{
			if (_entries == null)
			{
				_entries = ReadEntries();
			}

			CacheEntry entry;
			if (_entries.TryGetValue(objectId, out entry) &&
			    string.Equals(entry.GeometryFingerprint, geometryFingerprint,
			                  StringComparison.Ordinal))
			{
				result = new FeatureCheckResult(objectId, FeatureCheckStatus.Checked,
				                                entry.ErrorProbability, entry.Category,
				                                entry.Reason);
				return true;
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Adds checked results to the file. The file is read again under an exclusive
		/// lock, so results written meanwhile by another verification are kept.
		/// </summary>
		public void Add([NotNull] IEnumerable<KeyValuePair<FeatureCheckResult, string>>
			                resultsWithGeometryFingerprint)
		{
			List<CacheEntry> added =
				resultsWithGeometryFingerprint
					.Where(pair => pair.Key.Status == FeatureCheckStatus.Checked)
					.Select(pair => new CacheEntry
					                {
						                ObjectId = pair.Key.ObjectId,
						                GeometryFingerprint = pair.Value,
						                ErrorProbability = pair.Key.ErrorProbability,
						                Category = pair.Key.Category,
						                Reason = pair.Key.Reason
					                })
					.ToList();

			if (added.Count == 0)
			{
				return;
			}

			try
			{
				using (FileStream stream = OpenLocked(FileMode.OpenOrCreate, FileAccess.ReadWrite,
				                                      FileShare.None))
				{
					Dictionary<long, CacheEntry> entries = Read(stream);
					foreach (CacheEntry entry in added)
					{
						entries[entry.ObjectId] = entry;
					}

					stream.SetLength(0);
					new DataContractJsonSerializer(typeof(CacheEntry[]))
						.WriteObject(stream, entries.Values.ToArray());

					_entries = entries;
				}
			}
			catch (Exception e)
			{
				_msg.WarnFormat("Unable to write the result cache {0}: {1}", _filePath,
				                e.Message);
			}
		}

		[NotNull]
		private Dictionary<long, CacheEntry> ReadEntries()
		{
			if (! File.Exists(_filePath))
			{
				return new Dictionary<long, CacheEntry>();
			}

			try
			{
				// Read access only: the folder can be a shared location that most users
				// may not write to.
				using (FileStream stream = OpenLocked(FileMode.Open, FileAccess.Read,
				                                      FileShare.Read))
				{
					return Read(stream);
				}
			}
			catch (Exception e)
			{
				_msg.WarnFormat("Unable to read the result cache {0}, all features are " +
				                "checked again: {1}", _filePath, e.Message);
				return new Dictionary<long, CacheEntry>();
			}
		}

		[NotNull]
		private static Dictionary<long, CacheEntry> Read([NotNull] FileStream stream)
		{
			var result = new Dictionary<long, CacheEntry>();
			if (stream.Length == 0)
			{
				return result;
			}

			var entries = (CacheEntry[]) new DataContractJsonSerializer(typeof(CacheEntry[]))
				.ReadObject(stream);

			foreach (CacheEntry entry in entries ?? new CacheEntry[0])
			{
				result[entry.ObjectId] = entry;
			}

			stream.Position = 0;
			return result;
		}

		[NotNull]
		private FileStream OpenLocked(FileMode mode, FileAccess access, FileShare share)
		{
			DateTime giveUpAt = DateTime.UtcNow + _lockTimeout;
			while (true)
			{
				try
				{
					return new FileStream(_filePath, mode, access, share);
				}
				catch (IOException) when (DateTime.UtcNow < giveUpAt)
				{
					// Another verification holds the file.
					Thread.Sleep(200);
				}
			}
		}

		[DataContract]
		private class CacheEntry
		{
			[DataMember(Name = "oid")]
			public long ObjectId { get; set; }

			[DataMember(Name = "geometry")]
			public string GeometryFingerprint { get; set; }

			[DataMember(Name = "probability")]
			public int? ErrorProbability { get; set; }

			[DataMember(Name = "category")]
			public string Category { get; set; }

			[DataMember(Name = "reason")]
			public string Reason { get; set; }
		}
	}
}
