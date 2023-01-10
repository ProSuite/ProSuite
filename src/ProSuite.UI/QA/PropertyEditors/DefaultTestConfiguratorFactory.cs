using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.UI.PropertyEditors;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.PropertyEditors
{
	public static class DefaultTestConfiguratorFactory
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static ITestConfigurator Create(
			[NotNull] TestFactory testFactory,
			[CanBeNull] string testAssemblyName,
			bool readOnly)
		{
			Assert.ArgumentNotNull(testFactory, nameof(testFactory));

			return CreateTestConfigurator(testFactory, testAssemblyName, readOnly);
		}

		#region Non-public

		private static void AddReferencedAssembly<T>(
			[NotNull] CompilerParameters cp,
			[NotNull] ICollection<string> assemblies)
		{
			AddReferencedAssembly(cp, typeof(T).Assembly, assemblies);
		}

		private static void AddReferencedAssembly(
			[NotNull] CompilerParameters cp,
			[NotNull] Assembly assembly,
			[NotNull] ICollection<string> assemblies)
		{
			string location = assembly.Location;

			if (assemblies.Contains(location))
			{
				return;
			}

			cp.ReferencedAssemblies.Add(location);
			assemblies.Add(location);
		}

		[NotNull]
		private static ITestConfigurator CreateTestConfigurator(
			[NotNull] TestFactory testFactory,
			[CanBeNull] string testAssemblyName,
			bool readOnly)
		{
			Assert.ArgumentNotNull(testFactory, nameof(testFactory));

			return CreateTestConfigurator(testFactory.GetTestTypeDescription(),
			                              testFactory.Parameters,
			                              testAssemblyName, readOnly);
		}

		[NotNull]
		public static ITestConfigurator CreateTestConfigurator(
			[NotNull] string testType,
			[NotNull] IEnumerable<TestParameter> parameters,
			[CanBeNull] string testAssemblyName,
			bool readOnly)
		{
			Assert.ArgumentNotNull(testType, nameof(testType));
			Assert.ArgumentNotNull(parameters, nameof(parameters));

			const string namespaceName = "TestConfigurator";
			string className = string.Format("{0}Configurator", testType);
			string fullClassName = string.Format("{0}.{1}", namespaceName, className);

			string code = GenerateCode(namespaceName, className, parameters, readOnly);

			Assembly assembly = CompileAssembly(code, testAssemblyName);

			AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				var compiled = (ITestConfigurator) assembly.CreateInstance(fullClassName);

				return Assert.NotNull(compiled, "compiled instance is null");
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		[NotNull]
		private static Assembly CompileAssembly([NotNull] string code,
		                                        [CanBeNull] string assemblyName)
		{
			Assert.ArgumentNotNullOrEmpty(code, nameof(code));

			var compilerParameters = new CompilerParameters();

			AddReferencedAssemblies(compilerParameters, assemblyName);

			compilerParameters.GenerateExecutable = false;
			compilerParameters.GenerateInMemory = true;

			CodeDomProvider comp = new CSharpCodeProvider();
			CompilerResults results = comp.CompileAssemblyFromSource(compilerParameters, code);

			if (results.Errors.HasErrors)
			{
				_msg.Debug("Generated code failing to compile:");
				_msg.Debug(code);

				throw new InvalidOperationException(GetCompilerErrorMessage(results));
			}

			return results.CompiledAssembly;
		}

		[NotNull]
		private static string GetCompilerErrorMessage(
			[NotNull] CompilerResults compilerResults)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Error compiling generated code:");

			foreach (CompilerError error in compilerResults.Errors)
			{
				sb.AppendFormat("- {0}", error.ErrorText);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		[NotNull]
		private static string GenerateCode([NotNull] string namespaceName,
		                                   [NotNull] string className,
		                                   [NotNull] IEnumerable<TestParameter> parameters,
		                                   bool readOnly)
		{
			Assert.ArgumentNotNullOrEmpty(namespaceName, nameof(namespaceName));
			Assert.ArgumentNotNullOrEmpty(className, nameof(className));
			Assert.ArgumentNotNull(parameters, nameof(parameters));

			var code = new CodeBuilder();

			code.AppendLine("using System;");
			code.AppendLine("using System.Collections.Generic;");
			code.AppendLine("using System.ComponentModel;");
			code.AppendFormat("using {0};", typeof(Assert).Namespace);

			code.AppendLine("namespace " + namespaceName);
			code.AppendLine("{");
			code.AppendFormat("  public class {0} : {1}", className,
			                  ReflectionUtils.GetFullName(typeof(DefaultTestConfigurator)));
			code.AppendLine();
			code.AppendLine("  {");

			foreach (TestParameter parameter in parameters)
			{
				AppendProperty(code, parameter, readOnly);
			}

			code.AppendLine("  }");
			code.AppendLine("}");

			return code.ToString();
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender,
		                                                      ResolveEventArgs args)
		{
			return AssemblyResolveUtils.TryLoadAssembly(
				args.Name, Assembly.GetExecutingAssembly().CodeBase, _msg.Debug);
		}

		private static void AddReferencedAssemblies([NotNull] CompilerParameters cp,
		                                            [CanBeNull] string testAssemblyName)
		{
			Assert.ArgumentNotNull(cp, nameof(cp));

			var assemblies = new SimpleSet<string>();

			// System:
			AddReferencedAssembly<string>(cp, assemblies);

			// netstandard (necessary probably due to upstream assemblies have changed to .netstandard)
			Assembly netstandard =
				Assembly.Load(
					"netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
			AddReferencedAssembly(cp, netstandard, assemblies);

			// ComponentModel
			AddReferencedAssembly<DisplayNameAttribute>(cp, assemblies);

			// System.Drawing:
			AddReferencedAssembly<Point>(cp, assemblies);

			// ProSuite.UI:
			AddReferencedAssembly<DatasetConfig>(cp, assemblies);

			// Commons.UI:
			AddReferencedAssembly<ListEditor>(cp, assemblies);

			// ProSuite.DomainModel:
			AddReferencedAssembly<ITestConfigurator>(cp, assemblies);

			// ProSuite.DomainModel.AO:
			AddReferencedAssembly<TestFactory>(cp, assemblies);

			// ProSuite.DomainModel.Core:
			AddReferencedAssembly<DatasetTestParameterValue>(cp, assemblies);

			// Commons:
			AddReferencedAssembly<IContextAware>(cp, assemblies);

			// Commons.Essentials:
			AddReferencedAssembly<NotNullAttribute>(cp, assemblies);

			// Commons.AO:
			AddReferencedAssembly<CodedValue>(cp, assemblies);

			// ProSuite.QA.Container:
			AddReferencedAssembly<ContainerTest>(cp, assemblies);

			// Test assembly, plus references of it
			if (! string.IsNullOrEmpty(testAssemblyName))
			{
				Assembly testAssembly = PrivateAssemblyUtils.LoadAssembly(testAssemblyName);

				AddReferencedAssembly(cp, testAssembly, assemblies);

				foreach (
					AssemblyName assemblyName in testAssembly.GetReferencedAssemblies())
				{
					AddReferencedAssembly(cp, Assembly.Load(assemblyName), assemblies);
				}
			}

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug("Referenced assemblies:");
				foreach (string referencedAssemblyLocation in cp.ReferencedAssemblies)
				{
					_msg.DebugFormat("- {0}", referencedAssemblyLocation);
				}
			}
		}

		private static void AppendProperty([NotNull] CodeBuilder code,
		                                   [NotNull] TestParameter parameter,
		                                   bool readOnly)
		{
			TestParameterProperties props = GetProperties(parameter, readOnly);

			bool isDataset = TestParameterTypeUtils.IsDatasetType(parameter.Type);

			string name = parameter.Name;
			string propSheetMember = "_" + name.ToLower()[0] + name.Substring(1);

			string testParamType = GetTestParameterType(isDataset);

			string propSheetType;
			string propSheetElemType;
			GetPropertySheetTypes(parameter, readOnly, isDataset, props,
			                      out propSheetType,
			                      out propSheetElemType);

			// Write Description for (get, set) Method
			code.AppendFormat("     private {0} {1}", propSheetType, propSheetMember);

			if (props.CanCreateInstance)
			{
				code.AppendLine();
				code.AppendFormat("       = new {0}()", propSheetType);
			}

			code.AppendLine(";");

			string parameterDescription = parameter.Description;
			if (! string.IsNullOrEmpty(parameterDescription))
			{
				AppendDescriptionAttribute(code, parameterDescription);
			}

			if (props.Editor != null)
			{
				// TODO: handle different types
				code.AppendFormat("     {0}", props.Editor);
				code.AppendLine();
			}

			if (! parameter.IsConstructorParameter)
			{
				code.AppendFormat("     [DisplayName(\"[{0}]\")]", name);
				code.AppendLine();
			}

			if (! isDataset && parameter.ArrayDimension == 0)
			{
				AppendSimpleScalarProperty(name, parameter, readOnly, props, testParamType, code);
			}
			else
			{
				// DatasetTestParameterValue or Array
				code.AppendFormat("     public {0} {1}", propSheetType, name);
				code.AppendLine();
				code.AppendLine("     {");
				code.AppendLine("       get { return " + propSheetMember + "; } ");
				//code.AppendLine("       set { " + var + " = value; } ");
				code.AppendLine("       set");
				code.AppendLine("       {");
				code.AppendLine("         " + propSheetMember + " = value;");
				code.AppendLine("         TrySetContext(" + propSheetMember + ");");
				code.AppendLine("       }");
				code.AppendLine("     }");
				code.AppendLine();

				if (parameter.ArrayDimension > 0)
				{
					string lt = string.Format("List<{0}>", testParamType);
					code.AppendLine();
					code.AppendFormat("    public {0} {1}()", lt,
					                  ReflectionUtils.GetPropertyGetMethodName(name));
					code.AppendLine();
					code.AppendLine("    {");
					code.AppendFormat("      {0} list = new {0}();", lt);
					code.AppendLine();
					code.AppendFormat("      foreach ({0} param in {1})",
					                  propSheetElemType,
					                  propSheetMember);
					code.AppendLine();
					code.AppendLine("      {");
					code.AppendFormat("        {0} c = param.GetParameterConfig();",
					                  ReflectionUtils.GetFullName(typeof(ParameterConfig)));
					code.AppendLine();
					code.AppendFormat(
						"        list.Add(({0})c.GetTestParameterValue());", testParamType);
					code.AppendLine();
					code.AppendLine("      }");
					code.AppendFormat("      return list;");
					code.AppendLine();
					code.AppendLine("    }");
					code.AppendFormat("    public void {0}({1} value)",
					                  ReflectionUtils.GetPropertySetMethodName(name),
					                  testParamType);
					code.AppendLine();
					code.AppendLine("    {");
					code.AppendFormat("      {0} item = new {0}();", propSheetElemType);
					code.AppendLine();
					code.AppendFormat("      {0} c = item.GetParameterConfig();",
					                  ReflectionUtils.GetFullName(typeof(ParameterConfig)));
					code.AppendLine();
					code.AppendFormat("      c.SetTestParameterValue(value);");
					code.AppendLine();
					code.AppendFormat("      {0}.Add(item);", propSheetMember);
					code.AppendLine();
					code.AppendLine("    }");
					code.AppendLine();
				}
				else if (isDataset)
				{
					code.AppendLine();
					code.AppendFormat("    public {0} {1}()", testParamType,
					                  ReflectionUtils.GetPropertyGetMethodName(name));
					code.AppendLine();
					code.AppendLine("    {");
					code.AppendLine($"       if ({propSheetMember} == null) return null;");
					code.AppendFormat("      return ({0}){1}.GetTestParameterValue();",
					                  testParamType,
					                  propSheetMember);
					code.AppendLine();
					code.AppendLine("    }");
					code.AppendFormat("    public void {0}({1} value)",
					                  ReflectionUtils.GetPropertySetMethodName(name),
					                  testParamType);
					code.AppendLine();
					code.AppendLine("    {");
					code.AppendFormat("      {0} = new {1}();", propSheetMember,
					                  props.TypeName);
					code.AppendLine();
					code.AppendFormat("      {0}.SetTestParameterValue(value);",
					                  propSheetMember);
					code.AppendLine();
					code.AppendLine("    }");
					code.AppendLine();
				}
			}
		}

		private static void AppendDescriptionAttribute([NotNull] CodeBuilder code,
		                                               [NotNull] string description)
		{
			// https://issuetracker02.eggits.net/browse/TGS-35
			// NOTE: replacement changed due to more robust use of verbatim strings
			description = description.Replace("\"", "\"\"");

			description = ReplaceNewLine(description, "                  ");
			code.AppendFormat("     [Description(@\"{0}\")]", description);
			code.AppendLine();
		}

		private static void GetPropertySheetTypes([NotNull] TestParameter parameter,
		                                          bool readOnly,
		                                          bool isDataset,
		                                          [NotNull] TestParameterProperties props,
		                                          [NotNull] out string propSheetType,
		                                          [CanBeNull] out string propSheetElemType)
		{
			if (parameter.ArrayDimension <= 0 || isDataset)
			{
				// datasets and simple scalars
				propSheetType = props.TypeName;
				propSheetElemType = ReflectionUtils.GetFullName(props.ListType);

				return;
			}

			// array of scalar values
			Type scalarType = ! readOnly
				                  ? typeof(ScalarProperty<>)
				                  : typeof(ReadOnlyScalarProperty<>);
			string gen = ReflectionUtils.GetFullName(scalarType);

			propSheetElemType =
				string.Format(
					"{0}<{1}>", gen, ReflectionUtils.GetFullName(props.ListType));

			propSheetType = string.Format("List<{0}>", propSheetElemType);
		}

		[NotNull]
		private static string GetTestParameterType(bool isDataset)
		{
			string result =
				isDataset
					? ReflectionUtils.GetFullName(typeof(DatasetTestParameterValue))
					: ReflectionUtils.GetFullName(typeof(ScalarTestParameterValue));
			Assert.NotNullOrEmpty(result);

			return result;
		}

		private static void AppendSimpleScalarProperty(
			[NotNull] string name,
			[NotNull] TestParameter parameter, bool readOnly,
			[NotNull] TestParameterProperties props,
			[NotNull] string testParamType,
			[NotNull] CodeBuilder code)
		{
			if (readOnly)
			{
				code.AppendFormat("     [ReadOnly(true)]");
			}

			string testParameterValueMemberName = string.Format("_tp{0}", name);

			string create;
			Type simpleType = props.SimpleType;
			string simpleTypeName = ReflectionUtils.GetFullName(simpleType);

			if (simpleType == typeof(string))
			{
				create = "\"\"";
			}
			else if (simpleType.GetConstructors().Length == 0 ||
			         simpleType.GetConstructor(new Type[] { }) != null ||
			         simpleType == typeof(DateTime))
			{
				// Note: the GetConstructor(new Type[] {}) call returns null for DateTime, nor sure why.
				// DateTime does have a parameterless constructor.
				create = string.Format("({0})Activator.CreateInstance(typeof({0}));",
				                       simpleTypeName);
			}
			else
			{
				throw new NotImplementedException(
					string.Format(
						"Cannot handle type without parameterless constructor (parameter {0})",
						parameter.Name));
			}

			string defaultValueConst = null;
			if (parameter.DefaultValue != null)
			{
				defaultValueConst = string.Format("__{0}Const", name);
				code.AppendFormat("     [DefaultValue({0})] ", defaultValueConst);
				code.AppendLine();
				create = defaultValueConst;
			}

			// Write (Get,Set)-Method
			code.AppendFormat("     public {0} {1}", props.TypeName, name);
			code.AppendLine();
			code.AppendLine("     {");
			code.AppendLine("       get");
			code.AppendLine("       {");
			AppendTestParameterValueAssertion(code, name, testParameterValueMemberName);
			code.AppendFormat("         {0} o = {1}.GetValue(typeof({2}));",
			                  ReflectionUtils.GetFullName(typeof(object)),
			                  testParameterValueMemberName,
			                  simpleTypeName);
			code.AppendLine();
			code.AppendFormat("         if (! (o is {0}))", simpleTypeName);
			code.AppendLine();
			code.AppendLine("         {");
			code.AppendLine("           // init value");
			code.AppendFormat("           o = {0};", create);
			code.AppendLine();
			code.AppendFormat("           {0}.StringValue = o.ToString();",
			                  testParameterValueMemberName);
			code.AppendLine();
			code.AppendLine("         }");
			code.AppendFormat("         return ({0})o;", simpleTypeName);
			code.AppendLine();
			code.AppendLine("       }");
			code.AppendLine("       set");
			code.AppendLine("       {");
			AppendTestParameterValueAssertion(code, name, testParameterValueMemberName);
			code.AppendFormat("         {0}.StringValue = value.ToString();",
			                  testParameterValueMemberName);
			code.AppendLine();
			code.AppendLine("       }");
			code.AppendLine("     }");

			if (! string.IsNullOrEmpty(defaultValueConst))
			{
				code.AppendFormat("    const {0} {1} = ", simpleTypeName, defaultValueConst);

				AppendLiteralValue(code, simpleType, Assert.NotNull(parameter.DefaultValue));

				code.AppendLine(";");
			}

			code.AppendLine();

			// write Method to get, set TestParameterValue
			code.AppendFormat("    private {0} {1};", testParamType,
			                  testParameterValueMemberName);
			code.AppendLine();
			code.AppendFormat("    public {0} {1}()", testParamType,
			                  ReflectionUtils.GetPropertyGetMethodName(name));
			code.AppendLine();
			code.AppendLine("    {");
			code.AppendFormat("      return {0};", testParameterValueMemberName);
			code.AppendLine();
			code.AppendLine("    }");
			code.AppendFormat("    public void {0}({1} value)",
			                  ReflectionUtils.GetPropertySetMethodName(name),
			                  testParamType);
			code.AppendLine();
			code.AppendLine("    {");
			code.AppendFormat("      {0} = value;", testParameterValueMemberName);
			code.AppendLine();
			code.AppendLine("    }");
			code.AppendLine();
		}

		private static void AppendLiteralValue([NotNull] CodeBuilder code,
		                                       [NotNull] Type simpleType,
		                                       [NotNull] object value)
		{
			if (simpleType.IsEnum)
			{
				// Nested type names are appended using '+' to their enclosing type --> replace with '.'
				string enumTypeName = simpleType.IsNested
					                      ? simpleType.FullName.Replace('+', '.')
					                      : simpleType.FullName;

				code.AppendFormat("{0}.{1}", enumTypeName, value);
			}
			else if (simpleType == typeof(string))
			{
				code.AppendFormat("\"{0}\"", value);
			}
			else if (simpleType == typeof(bool))
			{
				code.AppendFormat("{0}", (bool) value
					                         ? "true"
					                         : "false");
			}
			else
			{
				code.AppendFormat("{0}", value);
			}
		}

		private static void AppendTestParameterValueAssertion(
			[NotNull] CodeBuilder code,
			[NotNull] string propertyName,
			[NotNull] string testParameterMemberName)
		{
			code.AppendFormat(
				"         Assert.NotNull({0}, \"TestParameterValue for {1} not assigned\");",
				testParameterMemberName, propertyName);
			code.AppendLine();
		}

		[NotNull]
		private static string ReplaceNewLine([NotNull] string text,
		                                     [NotNull] string format)
		{
			text = text.Replace("\r", string.Empty);
			string[] lines = text.Split('\n');

			int lineCount = lines.Length;
			if (lineCount == 1)
			{
				return text;
			}

			var sb = new StringBuilder(lines[0]);
			for (var i = 1; i < lineCount; i++)
			{
				// append newline as separate string to preceding verbatim string
				sb.AppendLine("\" + \"\\r\\n\" +");
				sb.Append(format + "@\""); // start a new verbatim string
				sb.Append(lines[i]);
			}

			return sb.ToString();
		}

		[NotNull]
		private static TestParameterProperties GetProperties(
			[NotNull] TestParameter parameter, bool readOnly)
		{
			var props = new TestParameterProperties();

			Type simpleType;
			Type listType = null;
			Type listEditorType = ! readOnly
				                      ? typeof(ListEditor)
				                      : typeof(ReadOnlyListEditor);

			string listEditor = string.Format(
				"[Editor(typeof({0}), typeof({1}))]",
				ReflectionUtils.GetFullName(listEditorType),
				typeof(UITypeEditor));

			bool canClearDataset = false;

			Type converter = null;
			if (! TestParameterTypeUtils.IsDatasetType(parameter.Type))
			{
				simpleType = parameter.Type;
			}
			else
			{
				TestParameterType parameterType =
					TestParameterTypeUtils.GetParameterType(parameter.Type);
				switch (parameterType)
				{
					case TestParameterType.Dataset:
						simpleType = ! readOnly
							             ? typeof(DatasetConfig)
							             : typeof(ReadOnlyDatasetConfig);
						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.GeometricNetworkDataset:
						simpleType = ! readOnly
							             ? typeof(GeometricNetworkDatasetConfig)
							             : typeof(ReadOnlyGeometricNetworkDatasetConfig);
						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.ObjectDataset:
						if (! readOnly)
						{
							simpleType = typeof(TableDatasetConfig);
							listType = typeof(TableDatasetProperty);
							canClearDataset = ! parameter.IsConstructorParameter;
						}
						else
						{
							simpleType = typeof(ReadOnlyTableDatasetConfig);
							listType = typeof(ReadOnlyTableDatasetProperty);
						}

						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.TableDataset:
						if (! readOnly)
						{
							simpleType = typeof(TableDatasetConfig);
							listType = typeof(TableDatasetProperty);
							canClearDataset = ! parameter.IsConstructorParameter;
						}
						else
						{
							simpleType = typeof(ReadOnlyTableDatasetConfig);
							listType = typeof(ReadOnlyTableDatasetProperty);
						}

						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.TerrainDataset:
						simpleType = ! readOnly
							             ? typeof(TerrainDatasetConfig)
							             : typeof(ReadOnlyTerrainDatasetConfig);
						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.TopologyDataset:
						simpleType = ! readOnly
							             ? typeof(TopologyDatasetConfig)
							             : typeof(ReadOnlyTopologyDatasetConfig);
						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.VectorDataset:
						if (! readOnly)
						{
							simpleType = typeof(VectorDatasetConfig);
							listType = typeof(VectorDatasetProperty);
							canClearDataset = ! parameter.IsConstructorParameter;
						}
						else
						{
							simpleType = typeof(ReadOnlyVectorDatasetConfig);
							listType = typeof(ReadOnlyVectorDatasetProperty);
						}

						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.RasterMosaicDataset:
						if (! readOnly)
						{
							simpleType = typeof(RasterMosaicDatasetConfig);
							listType = typeof(RasterMosaicDatasetProperty);
						}
						else
						{
							simpleType = typeof(ReadOnlyRasterMosaicDatasetConfig);
							listType = typeof(ReadOnlyRasterMosaicDatasetProperty);
						}

						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					case TestParameterType.RasterDataset:
						if (! readOnly)
						{
							simpleType = typeof(RasterDatasetConfig);
							listType = typeof(RasterDatasetProperty);
						}
						else
						{
							simpleType = typeof(ReadOnlyRasterDatasetConfig);
							listType = typeof(ReadOnlyRasterDatasetProperty);
						}

						converter = typeof(DatasetConverter);
						props.CanCreateInstance = true;
						break;

					default:
						throw new InvalidOperationException(
							"Unhandled type " + parameterType);
				}
			}

			if (parameter.ArrayDimension == 0)
			{
				props.SimpleType = simpleType;
				props.TypeName = ReflectionUtils.GetFullName(simpleType);
				if (converter != null)
				{
					string uiText = null;
					if (canClearDataset)
					{
						uiText = string.Format(", Editor(typeof({0}), typeof({1}))",
						                       ReflectionUtils.GetFullName(
							                       typeof(DatasetClearEditor)),
						                       ReflectionUtils.GetFullName(typeof(UITypeEditor)));
					}

					props.Editor = string.Format("[TypeConverter(typeof({0})){1}]",
					                             ReflectionUtils.GetFullName(converter), uiText);
				}
			}
			else if (parameter.ArrayDimension == 1)
			{
				if (listType == null)
				{
					listType = simpleType;
				}

				props.SimpleType = simpleType;
				props.ListType = listType;
				props.TypeName = string.Format("List<{0}>", listType);
				props.CanCreateInstance = true;
				props.Editor = listEditor;
			}
			else
			{
				throw new InvalidOperationException("Cannot handle array dimension " +
				                                    parameter.ArrayDimension +
				                                    " (max = 1)");
			}

			return props;
		}

		#endregion

		#region Nested types

		private class TestParameterProperties
		{
			public bool CanCreateInstance;
			public string Editor;
			public Type ListType;
			public Type SimpleType;
			public string TypeName;
		}

		private class CodeBuilder
		{
			private readonly IFormatProvider _formatProvider = CultureInfo.InvariantCulture;
			private readonly StringBuilder _sb = new StringBuilder();

			public void AppendLine()
			{
				_sb.AppendLine();
			}

			public void AppendLine([CanBeNull] string value)
			{
				_sb.AppendLine(value);
			}

			[StringFormatMethod("format")]
			public void AppendFormat([NotNull] string format, params object[] args)
			{
				_sb.AppendFormat(_formatProvider, format, args);
			}

			public override string ToString()
			{
				return _sb.ToString();
			}
		}

		#endregion
	}
}
