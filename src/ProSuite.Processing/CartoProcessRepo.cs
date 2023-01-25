using System;
using System.Collections.Generic;
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

		public int ProcessCount => _list.Count;

		public IReadOnlyList<CartoProcessDefinition> ProcessDefinitions => _list;

		public void Clear()
		{
			lock (_syncLock)
			{
				_definitions.Clear();
			}
		}

		// TODO Consider overloads to load repo from DDX, from .aprx, from ...

		public void LoadFile(string xmlFilePath, IReadOnlyList<Type> knownTypes)
		{
			var doc = XDocument.Load(xmlFilePath, LoadOptions.SetLineInfo);
			var root = doc.Root ?? new XElement("CartoProcesses");

			try
			{
				Reload(root, knownTypes);
			}
			catch (FormatException ex)
			{
				throw new CartoConfigException(
					$"Invalid carto process configuration file {xmlFilePath}: {ex.Message}");
			}
		}

		private void Reload(XElement root, IReadOnlyList<Type> knownTypes)
		{
			root = root ?? throw new ArgumentNullException(nameof(root));
			knownTypes = knownTypes ?? Array.Empty<Type>();

			var declaredTypes = root.Element("Types")?.Elements("ProcessType").ToList();

			int seqNo = 0; // so we could restore original ordering

			var groups = root.Element("Groups")?.Elements("ProcessGroup")
			                 .Select(e => CreateGroupItem(++seqNo, e, declaredTypes, knownTypes))
			                 .ToArray() ?? Array.Empty<CartoProcessDefinition>();

			var processes = root.Element("Processes")?.Elements("Process")
			                    .Select(e => CreateProcessItem(++seqNo, e, declaredTypes, knownTypes))
			                    .ToArray() ?? Array.Empty<CartoProcessDefinition>();

			lock (_syncLock)
			{
				_definitions.Clear();
				_definitions.AddRange(groups.Concat(processes));
			}
		}

		private static CartoProcessDefinition CreateProcessItem(
			int seqNo, XElement processElement,
			IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
		{
			var name = Canonical((string) processElement.Attribute("name"));

			var typeElement = processElement.Element("TypeReference");
			var typeAlias = (string) typeElement?.Attribute("name") ??
			                throw new FormatException(AppendLineInfo("Process has no type reference", processElement));

			var type = ResolveType(typeAlias, declaredTypes, knownTypes);
			var configText = processElement.ToString();
			var description = Canonical((string) processElement.Attribute("description"));

			return new CartoProcessDefinition(seqNo, name, type, configText, description);
		}

		private static CartoProcessDefinition CreateGroupItem(
			int seqNo, XElement groupElement,
			IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
		{
			var name = Canonical((string) groupElement.Attribute("name"));

			var typeElement = groupElement.Element("AssociatedGroupProcessTypeReference") ??
			                  groupElement.Element("GroupProcessTypeReference") ??
			                  groupElement.Element("TypeReference");
			var typeAlias = (string) typeElement?.Attribute("name") ??
			                throw new FormatException(AppendLineInfo("Process has no type reference", groupElement));

			var type = ResolveType(typeAlias, declaredTypes, knownTypes);
			var configText = groupElement.ToString();
			var description = Canonical((string) groupElement.Attribute("description"));

			return new CartoProcessDefinition(seqNo, name, type, configText, description);
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

		[NotNull]
		private static CartoProcessType ResolveType(string typeAlias, IReadOnlyList<XElement> declaredTypes, IReadOnlyList<Type> knownTypes)
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
				return new CartoProcessType(typeAlias, null);
			}

			string typeName = (string) cd.Attribute("type");
			string assemblyName = (string) cd.Attribute("assembly");

			for (string candidate = typeName; candidate.Length > 0; )
			{
				var found = knownTypes.FirstOrDefault(
					t => t.FullName?.EndsWith(candidate, StringComparison.Ordinal) ?? false);

				if (found != null)
				{
					return new CartoProcessType(typeAlias, found, typeName, assemblyName);
				}

				int index = candidate.IndexOf('.', 1);
				candidate = index < 0 ? string.Empty : candidate.Substring(index);
			}

			return new CartoProcessType(typeAlias, null);
		}

		private static string Canonical(string text)
		{
			if (text is null) return null;
			text = text.Trim();
			return text.Length > 0 ? text : null;
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
		public int SeqNo { get; }
		public string Name { get; }
		public string Description { get; }
		public string ConfigText { get; }

		private CartoProcessType Type { get; }
		public string TypeAlias => Type.AliasName;
		[CanBeNull] public Type ResolvedType => Type.ResolvedType;

		private ICollection<string> _tags;
		public ICollection<string> Tags => _tags ?? (_tags = GetTags());

		public CartoProcessDefinition(int seqNo, string name, CartoProcessType type, string configText, string description = null)
		{
			SeqNo = seqNo;
			Name = name ?? "(no name)";
			Type = type ?? throw new ArgumentNullException(nameof(type));
			ConfigText = configText;
			Description = description ?? "(no description)";
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

	public class CartoProcessType
	{
		public string AliasName { get; }
		[CanBeNull] public Type ResolvedType { get; }
		public string RequestedType { get; }
		public string RequestedAssembly { get; }

		public CartoProcessType(string aliasName, Type resolvedType,
		                        string requestedType = null, string requestedAssembly = null)
		{
			AliasName = aliasName ?? throw new ArgumentNullException(nameof(aliasName));
			ResolvedType = resolvedType; // may be null!
			RequestedType = requestedType;
			RequestedAssembly = requestedAssembly;
		}

		public override string ToString()
		{
			return ResolvedType == null
				       ? $"{AliasName} (cannot resolve)"
				       : $"{AliasName} (resolved as {ResolvedType.Name}";
		}
	}


}
