using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA.Xml;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased
{
	public class DataSource
	{
		private string _workspaceAsText;
		private string _connectionString;
		private string _factoryProgId;
		private string _catalogPath;
		private bool? _referencesValidWorkspace;

		public static readonly string AnonymousId = string.Empty;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DataSource"/> class.
		/// </summary>
		/// <param name="displayName">The name.</param>
		/// <param name="id">The id.</param>
		public DataSource([NotNull] string displayName, [NotNull] string id)
		{
			Assert.ArgumentNotNullOrEmpty(displayName, nameof(displayName));
			Assert.ArgumentNotNull(id, nameof(id)); // may be empty

			DisplayName = displayName;
			ID = id;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataSource"/> class.
		/// </summary>
		/// <param name="displayName">The name.</param>
		/// <param name="id">The id.</param>
		/// <param name="databaseName"></param>
		/// <param name="schemaOwner"></param>
		/// <param name="catalogPath">The catalog path.</param>
		public DataSource([NotNull] string displayName, [NotNull] string id,
		                  [NotNull] string catalogPath,
		                  [CanBeNull] string databaseName = null,
		                  [CanBeNull] string schemaOwner = null)
			: this(displayName, id)
		{
			Assert.ArgumentNotNullOrEmpty(displayName, nameof(displayName));
			Assert.ArgumentNotNull(id, nameof(id)); // may be empty

			DisplayName = displayName;
			ID = id;

			DatabaseName = databaseName;
			SchemaOwner = schemaOwner;

			if (StringUtils.IsNotEmpty(catalogPath))
			{
				_catalogPath = catalogPath;
				_workspaceAsText = _catalogPath;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataSource"/> class.
		/// </summary>
		/// <param name="xmlWorkspace">The XML workspace.</param>
		public DataSource([NotNull] XmlWorkspace xmlWorkspace)
			: this(xmlWorkspace.ModelName, xmlWorkspace.ID)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));

			DatabaseName = xmlWorkspace.Database;
			SchemaOwner = xmlWorkspace.SchemaOwner;

			if (StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath))
			{
				_catalogPath = xmlWorkspace.CatalogPath.Trim();
				_workspaceAsText = _catalogPath;
			}
			else if (StringUtils.IsNotEmpty(xmlWorkspace.ConnectionString))
			{
				_connectionString = xmlWorkspace.ConnectionString.Trim();
				_workspaceAsText = _connectionString;

				if (StringUtils.IsNotEmpty(xmlWorkspace.FactoryProgId))
				{
					_factoryProgId = xmlWorkspace.FactoryProgId.Trim();

					try
					{
						string catalogPath = WorkspaceUtils.TryGetCatalogPath(OpenWorkspace());

						if (! string.IsNullOrEmpty(catalogPath))
						{
							_catalogPath = catalogPath;
							_workspaceAsText = _catalogPath;
						}

						_referencesValidWorkspace = true;
					}
					catch (Exception)
					{
						_referencesValidWorkspace = false;
					}
				}
			}
			else
			{
				_workspaceAsText = string.Empty;
			}
		}

		#endregion

		[NotNull]
		public IWorkspace OpenWorkspace()
		{
			if (! string.IsNullOrEmpty(_catalogPath))
			{
				Assert.True(File.Exists(_catalogPath) || Directory.Exists(_catalogPath),
				            $"The catalog path {_catalogPath} does not exist.");

				return WorkspaceUtils.OpenWorkspace(_catalogPath);
			}

			if (! string.IsNullOrEmpty(_connectionString) &&
			    ! string.IsNullOrEmpty(_factoryProgId))
			{
				return WorkspaceUtils.OpenWorkspace(_connectionString, _factoryProgId);
			}

			throw new InvalidOperationException(
				$"Workspace connection parameters not defined for data source {ID}");
		}

		#region Overrides of Object

		public override string ToString()
		{
			return
				$"{DisplayName} (Id: {ID}, Database Name: {DatabaseName}, Catalog Path: {_catalogPath})";
		}

		#endregion

		[NotNull]
		public string ID { get; }

		[NotNull]
		public string DisplayName { get; }

		[CanBeNull]
		public string DatabaseName { get; }

		[CanBeNull]
		public string SchemaOwner { get; }

		public string WorkspaceAsText
		{
			get { return _workspaceAsText; }
			set
			{
				if (string.Equals(_workspaceAsText, value))
				{
					return;
				}

				_catalogPath = null;
				_connectionString = null;
				_factoryProgId = null;

				_workspaceAsText = value;

				if (IsConnectionString(_workspaceAsText, out _factoryProgId))
				{
					_connectionString = _workspaceAsText;
				}
				else
				{
					_catalogPath = _workspaceAsText;
				}

				_referencesValidWorkspace = null;
			}
		}

		private static bool IsConnectionString([NotNull] string text,
		                                       [NotNull] out string factoryProgId)
		{
			Assert.ArgumentNotNull(text, nameof(text));

			if (text.Length == 0)
			{
				factoryProgId = string.Empty;
				return false;
			}

			if (File.Exists(text) || Directory.Exists(text))
			{
				factoryProgId = string.Empty;
				return false;
			}

			var propertySeparators = new[] {';'};
			var nameValueSeparators = new[] {'='};

			if (text.IndexOfAny(nameValueSeparators) < 0)
			{
				factoryProgId = string.Empty;
				return false;
			}

			string[] properties = text.Split(propertySeparators,
			                                 StringSplitOptions.RemoveEmptyEntries);

			var propertyValuesByName =
				new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (string property in properties)
			{
				string[] tokens = property.Split(nameValueSeparators,
				                                 StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 2)
				{
					factoryProgId = string.Empty;
					return false;
				}

				string propertyName = tokens[0];

				if (! propertyValuesByName.ContainsKey(propertyName))
				{
					int separatorIndex = property.IndexOfAny(nameValueSeparators);

					string propertyValue = property.Substring(separatorIndex + 1);

					propertyValuesByName.Add(propertyName, propertyValue);
				}
			}

			// for each property, there is at least on nameValueSeparator
			if (propertyValuesByName.ContainsKey("INSTANCE"))
			{
				factoryProgId = "esriDataSourcesGDB.SdeWorkspaceFactory.1";
				return true;
			}

			string path;

			if (propertyValuesByName.TryGetValue("DATABASE", out path))
			{
				if (path.EndsWith(".gdb"))
				{
					factoryProgId = "esriDataSourcesGDB.FileGDBWorkspaceFactory.1";
					return true;
				}

				if (path.EndsWith(".mdb"))
				{
					factoryProgId = "esriDataSourcesGDB.AccessWorkspaceFactory.1";
					return true;
				}
			}

			factoryProgId = string.Empty;
			return false;
		}

		public bool HasWorkspaceInformation => ! string.IsNullOrEmpty(_catalogPath) ||
		                                       ! string.IsNullOrEmpty(_connectionString) &&
		                                       ! string.IsNullOrEmpty(_factoryProgId);

		public bool ReferencesValidWorkspace
		{
			get
			{
				if (! _referencesValidWorkspace.HasValue)
				{
					_referencesValidWorkspace = HasWorkspaceInformation && TryOpenWorkspace();
				}

				return _referencesValidWorkspace.Value;
			}
		}

		private bool TryOpenWorkspace()
		{
			try
			{
				OpenWorkspace();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
