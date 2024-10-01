using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing
{
	public class CartoProcessRepo
	{
		private readonly List<CartoProcessDefinition> _definitions;
		private readonly IReadOnlyList<CartoProcessDefinition> _list;
		private readonly object _syncLock = new object();

		public CartoProcessRepo(/*known types*/)
		{
			_definitions = new List<CartoProcessDefinition>();
			_list = new ReadOnlyList<CartoProcessDefinition>(_definitions);
		}

		public IReadOnlyList<CartoProcessDefinition> ProcessDefinitions => _list;

		public void Clear()
		{
			lock (_syncLock)
			{
				_definitions.Clear();
			}
		}

		// TODO Consider overloads to load repo from DDX, from .aprx, from ...

		public void Load(string xmlFilePath, IReadOnlyList<Type> knownTypes)
		{
			using (var reader = File.OpenText(xmlFilePath))
			{
				Load(reader, knownTypes);
			}
		}

		public void Load(TextReader reader, IReadOnlyList<Type> knownTypes)
		{
			var doc = XDocument.Load(reader, LoadOptions.SetLineInfo);
			var root = doc.Root ?? new XElement("CartoProcesses");

			try
			{
				Reload(root, knownTypes);
			}
			catch (FormatException ex)
			{
				throw new CartoConfigException(
					$"Invalid carto process configuration file: {ex.Message}");
			}
		}

		public void Save(string xmlFilePath)
		{
			using (var stream = File.Create(xmlFilePath))
			using (var writer = new StreamWriter(stream))
			{
				Save(writer);
			}
		}

		public void Save(TextWriter writer)
		{
			var doc = ToXml();

			doc.Save(writer);
		}

		public void UpdateProcess(CartoProcessConfig config, Type resolvedType = null)
		{
			if (config is null)
				throw new ArgumentNullException(nameof(config));

			var name = config.Name;
			if (string.IsNullOrEmpty(name))
				throw new FormatException("Name is missing");

			if (resolvedType != null)
			{
				// avoid TypeAlias pointing to old type:
				config.TypeAlias = resolvedType.Name;
			}

			lock (_syncLock)
			{
				int index = FindIndex(_definitions, name);
				if (index < 0)
				{
					_definitions.Add(new CartoProcessDefinition(config, resolvedType));
				}
				else
				{
					_definitions[index] = new CartoProcessDefinition(config, resolvedType);
				}
			}
		}

		public bool RemoveProcess(string name)
		{
			lock (_syncLock)
			{
				int count = _definitions.RemoveAll(
					d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
				return count > 0;
			}
		}

		private static int FindIndex(IList<CartoProcessDefinition> list, string name)
		{
			for (int i = 0; i < list.Count; i++)
			{
				var d = list[i];
				if (string.Equals(d.Name, name, StringComparison.Ordinal))
				{
					return i;
				}
			}

			for (int i = 0; i < list.Count; i++)
			{
				var d = list[i];
				if (string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		private void Reload(XElement root, IReadOnlyList<Type> knownTypes)
		{
			root = root ?? throw new ArgumentNullException(nameof(root));
			knownTypes = knownTypes ?? Array.Empty<Type>();

			var declaredTypes = root.Element("Types")?.Elements("ProcessType").ToList();

			var groups = root.Element("Groups")?.Elements("ProcessGroup")
			                 .Select(e => CreateGroupItem(e, declaredTypes, knownTypes))
			                 .ToArray() ?? Array.Empty<CartoProcessDefinition>();

			var processes = root.Element("Processes")?.Elements("Process")
			                    .Select(e => CreateProcessItem(e, declaredTypes, knownTypes))
			                    .ToArray() ?? Array.Empty<CartoProcessDefinition>();

			lock (_syncLock)
			{
				_definitions.Clear();
				_definitions.AddRange(groups.Concat(processes));
			}
		}

		private XDocument ToXml()
		{
			lock (_syncLock)
			{
				return ToXml(_definitions);
			}
		}

		private static XDocument ToXml(IList<CartoProcessDefinition> definitions)
		{
			var groupType = typeof(IGroupCartoProcess);

			var groups = definitions
			             .Where(d => d.ResolvedType != null)
			             .Where(d => groupType.IsAssignableFrom(d.ResolvedType))
			             .Select(d => new XElement("ProcessGroup",
			                                       new XAttribute("name", d.Name ?? string.Empty),
			                                       MakeAttribute("description", d.Description),
			                                       new XElement(
				                                       "GroupProcessTypeReference",
				                                       new XAttribute("name", d.TypeAlias)),
			                                       GetProcesses(d.Config)))
			             .ToList();

			var types = definitions
			            .Where(d => d.ResolvedType != null)
			            .GroupBy(d => d.TypeAlias)
			            .OrderBy(g => g.Key)
			            .Select(g => new XElement("ProcessType",
			                                      MakeAttribute("name", g.Key), // i.e., TypeAlias
			                                      new XElement("ClassDescriptor",
															   MakeAttribute("type", g.First().ResolvedType?.FullName),
				                                      MakeAttribute("assembly", g.First().ResolvedType?.Assembly.GetName().Name))))
			            .ToList();

			var procs = definitions
			            .Where(d => d.TypeAlias != null)
			            .Where(d => ! groupType.IsAssignableFrom(d.ResolvedType))
			            .Select(d => new XElement(
				                    "Process",
				                    new XAttribute("name", d.Name ?? string.Empty),
									MakeAttribute("description", d.Description),
				                    new XElement("TypeReference",
				                                 MakeAttribute("name", d.TypeAlias)),
				                    GetParameters(d.Config)))
			            .ToList();

			return new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				new XElement("CartoProcesses",
				             new XElement("Groups", groups),
				             new XElement("Processes", procs),
				             new XElement("Types", types)));
		}

		private static XAttribute MakeAttribute(string name, string value = null)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			return value is null ? null : new XAttribute(name, value);
		}

		private static XElement GetProcesses(CartoProcessConfig config)
		{
			var processes = config
			                .Where(p => string.Equals(p.Key, "Processes",
			                                          StringComparison.OrdinalIgnoreCase))
			                .Select(p => new XElement("Process", MakeAttribute("name", p.Value)));

			return new XElement("Processes", processes);
		}

		private static XElement GetParameters(CartoProcessConfig config)
		{
			var parameters = config.Select(p => new XElement("Parameter",
			                                                 new XAttribute("name", p.Key ?? string.Empty),
			                                                 new XAttribute("value", p.Value ?? string.Empty)));

			return new XElement("Parameters", parameters);
		}

		private static CartoProcessDefinition CreateProcessItem(
			XElement processElement,
			IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
		{
			var typeElement = processElement.Element("TypeReference");
			var typeAlias = (string) typeElement?.Attribute("name") ??
			                throw new FormatException(AppendLineInfo("Process has no type reference", processElement));

			var type = ResolveType(typeAlias, declaredTypes, knownTypes);
			var config = CartoProcessConfig.FromProcess(processElement);
			return new CartoProcessDefinition(config, type);
		}

		private static CartoProcessDefinition CreateGroupItem(
			XElement groupElement,
			IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
		{
			var typeElement = groupElement.Element("AssociatedGroupProcessTypeReference") ??
			                  groupElement.Element("GroupProcessTypeReference") ??
			                  groupElement.Element("TypeReference");
			var typeAlias = (string) typeElement?.Attribute("name") ??
			                throw new FormatException(AppendLineInfo("Process has no type reference", groupElement));

			var resolvedType = ResolveType(typeAlias, declaredTypes, knownTypes);
			var config = CartoProcessConfig.FromProcessGroup(groupElement);
			return new CartoProcessDefinition(config, resolvedType);
		}

		private static string AppendLineInfo(string message, XObject xml, bool includePosition = false)
		{
			if (xml is IXmlLineInfo info && info.HasLineInfo())
			{
				return includePosition
					       ? $"{message} (line {info.LineNumber} position {info.LinePosition})"
					       : $"{message} (line {info.LineNumber})";
			}

			return message;
		}

		[CanBeNull]
		private static Type ResolveType(string typeAlias, IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
		{
			if (typeAlias is null)
				throw new ArgumentNullException(nameof(typeAlias));

			// 1. look up typeAlias in declaredTypes to find the actual typeName
			// 2. look up typeName in knownTypes to find the actual Type

			var declaredType = declaredTypes.FirstOrDefault(e => MatchOrdinal(e, typeAlias)) ??
			                   declaredTypes.FirstOrDefault(e => MatchIgnoreCase(e, typeAlias));

			var cd = declaredType?.Element("ClassDescriptor");
			if (cd == null)
			{
				return null;
			}

			string typeName = (string) cd.Attribute("type");

			for (string candidate = typeName; candidate.Length > 0; )
			{
				var found = knownTypes.FirstOrDefault(
					t => t.FullName != null && FullNameEndsWith(t.FullName, candidate));

				if (found != null)
				{
					return found;
				}

				int index = candidate.IndexOf('.', 1);
				if (index < 0) index = candidate.IndexOf('+', 1); // nested class
				candidate = index < 0 ? string.Empty : candidate.Substring(index + 1);
			}

			return null;
		}

		private static bool FullNameEndsWith(string fullName, string candidate)
		{
			if (! fullName.EndsWith(candidate, StringComparison.Ordinal)) return false;
			if (candidate.Length >= fullName.Length) return true;
			char c = fullName[fullName.Length - candidate.Length - 1];
			return c == '.' || c == '+'; // plus is for nested class: Name.Space.Outer+Nested
		}

		private static bool MatchOrdinal(XElement declaredType, string aliasName)
		{
			var name = (string) declaredType.Attribute("name");
			return string.Equals(name, aliasName, StringComparison.Ordinal);
		}

		private static bool MatchIgnoreCase(XElement declaredType, string aliasName)
		{
			var name = (string) declaredType.Attribute("name");
			return string.Equals(name, aliasName, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class CartoProcessDefinition : ITagged
	{
		public string Name => Config.Name;
		public string TypeAlias => Config.TypeAlias;
		public string Description => Config.Description;
		public CartoProcessConfig Config { get; }

		[CanBeNull] public Type ResolvedType { get; }

		private ICollection<string> _tags; // cache
		public ICollection<string> Tags => _tags ?? (_tags = GetTags());

		public CartoProcessDefinition(CartoProcessConfig config, Type resolvedType = null)
		{
			Config = config ?? throw new ArgumentNullException(nameof(config));

			ResolvedType = resolvedType;
		}

		private ICollection<string> GetTags()
		{
			return FilterUtils.MakeTags(Name)
			                  .Concat(FilterUtils.MakeTags(TypeAlias))
			                  .Concat(FilterUtils.MakeTags(TypeAlias, "type"))
			                  .Concat(FilterUtils.MakeTags(ResolvedType?.Name, "type"))
			                  .Distinct().ToList();
			// TODO add Description? look for datasets in ConfigText?
		}

		public override string ToString()
		{
			return $"{Name} (of type {ResolvedType?.Name ?? "(null)"})";
		}
	}
}
