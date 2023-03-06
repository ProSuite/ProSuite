using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
					t => t.FullName?.EndsWith(candidate, StringComparison.Ordinal) ?? false);

				if (found != null)
				{
					return found;
				}

				int index = candidate.IndexOf('.', 1);
				candidate = index < 0 ? string.Empty : candidate.Substring(index);
			}

			return null;
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
