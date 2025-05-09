using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration;
using Castle.Windsor.Configuration.Interpreters;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.IoC
{
	/// <summary>
	/// Abstract IoC container, inherited by concrete containers working
	/// on a specific container configuration file.
	/// </summary>
	public abstract class Container : IContainer
	{
		[NotNull] private readonly IWindsorContainer _inner;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IDictionary<string, Version> AssemblyRedirects { get; } =
			new Dictionary<string, Version>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Container"/> class.
		/// </summary>
		protected Container()
		{
			_inner = new WindsorContainer();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Container"/> class.
		/// </summary>
		/// <param name="parentContainer">The parent container.</param>
		protected Container([NotNull] Container parentContainer) : this()
		{
			parentContainer.AddChildRegistry(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Container"/> class.
		/// </summary>
		/// <param name="configFileName">Name of the config file.</param>
		/// <param name="propertyDefaultsXmlFragment">Optional string with xml fragment containing a &lt;properties&gt; 
		/// element defining default values for optional container configuration properties.</param>
		protected Container([NotNull] string configFileName,
		                    [CanBeNull] string propertyDefaultsXmlFragment = null)
			: this(null, configFileName, propertyDefaultsXmlFragment) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Container"/> class.
		/// </summary>
		/// <param name="parentContainer">The parent container.</param>
		/// <param name="configFileName">Name of the config file.</param>
		/// <param name="propertyDefaultsXmlFragment">Optional string with xml fragment containing a &lt;properties&gt; 
		/// element defining default values for optional container configuration properties.</param>
		protected Container([CanBeNull] Container parentContainer,
		                    [NotNull] string configFileName,
		                    [CanBeNull] string propertyDefaultsXmlFragment = null)
		{
			try
			{
				WireAssemblyResolve();

				// NOTE if the next line throws an exception saying that a castle dll could not be loaded:  
				// make sure that all projects build to the same directory. Recently this was observed after
				// changing the platform value of a configuration (setting to x86) --> this also changes
				// the output path.
				IConfigurationInterpreter configuration = GetConfiguration(
					configFileName, out string configurationFilePath, propertyDefaultsXmlFragment);

				_msg.DebugFormat("Creating IoC container using config file {0}",
				                 configurationFilePath);

				_inner = new WindsorContainer(configuration);

				parentContainer?.AddChildRegistry(this);
			}
			catch (Exception)
			{
				Assembly assembly = Assembly.GetExecutingAssembly();

				_msg.DebugFormat("Assembly location: {0}", assembly.Location);
				_msg.DebugFormat("Assembly full name: {0}", assembly.FullName);
				_msg.DebugFormat("Assembly codebase: {0}", assembly.CodeBase);

				throw;
			}
			finally
			{
				UnwireAssemblyResolve();
			}
		}

		#endregion

		#region IContainer Members

		void IContainer.Register(string key, Type classType)
		{
			_inner.Register(Component.For(classType).Named(key));
		}

		void IContainer.Register(string key, Type serviceType, Type classType)
		{
			_inner.Register(Component.For(serviceType).ImplementedBy(classType).Named(key));
		}

		public T Resolve<T>()
		{
			try
			{
				WireAssemblyResolve();

				return _inner.Resolve<T>();
			}
			catch (Exception e)
			{
				LogResolveException(e);
				throw;
			}
			finally
			{
				UnwireAssemblyResolve();
			}
		}

		public T Resolve<T>(string key)
		{
			try
			{
				WireAssemblyResolve();

				return _inner.Resolve<T>(key);
			}
			catch (Exception e)
			{
				LogResolveException(e);
				throw;
			}
			finally
			{
				UnwireAssemblyResolve();
			}
		}

		public void Release(object instance)
		{
			_inner.Release(instance);
		}

		public void Dispose()
		{
			_inner.Dispose();
		}

		#endregion

		protected object Inner => _inner;

		protected void Install(IWindsorInstaller installer)
		{
			_inner.Install(installer);
		}

		protected void AddChildRegistry([NotNull] Container childContainer)
		{
			IWindsorContainer child = childContainer.Inner as WindsorContainer;
			_inner.AddChildContainer(child);
		}

		protected virtual void OnLoadingConfiguration([NotNull] string configurationFilePath) { }

		[NotNull]
		protected abstract string GetConfigFilePath([NotNull] string configFileName);

		[NotNull]
		private IConfigurationInterpreter GetConfiguration(
			[NotNull] string configFileName,
			[NotNull] out string configurationFilePath,
			[CanBeNull] string propertyDefaultsXmlFragment = null)
		{
			configurationFilePath = GetConfigFilePath(configFileName);

			OnLoadingConfiguration(configurationFilePath);

			using (TextReader reader = new StreamReader(configurationFilePath))
			{
				string configuration = InsertPropertyDefaults(reader.ReadToEnd(),
				                                              propertyDefaultsXmlFragment);

				// Replace line breaks in config files. This is necessary, otherwise there are parsing errors:
				// https://github.com/castleproject/Windsor/issues/76
				// Additionally, replace tabs (and other white space) right next to line breaks to
				// avoid TGS-1518. Add a simple white space to avoid concatenating text or attribute
				// fragments that are on different lines. For example (* is the replaced line break):
				// <book category="children"*release="1995"...
				// This needs a blank at the position of the *
				configuration = Regex.Replace(configuration, @"\s*\r\n\s*", " ");

				_msg.VerboseDebug(
					() => "Configuring container using the following xml configuration:");
				_msg.VerboseDebug(() => configuration);

				// Make sure that non-embedded include files will be evaluated based config directory (and not the bin directory)
				string rootPath = Assert.NotNullOrEmpty(
					Path.GetDirectoryName(configurationFilePath));

				var resource = new RootedStaticContentResource(configuration, rootPath);

				return new XmlInterpreter(resource);
			}
		}

		[NotNull]
		private static string InsertPropertyDefaults(
			[NotNull] string configuration,
			[CanBeNull] string propertyDefaultsXmlFragment)
		{
			Assert.ArgumentNotNullOrEmpty(configuration, nameof(configuration));

			if (string.IsNullOrEmpty(propertyDefaultsXmlFragment))
			{
				return configuration;
			}

			var watch = _msg.DebugStartTiming();

			try
			{
				const string propertiesNodeName = "properties";
				const string configurationNodeName = "configuration";

				XmlDocument configDocument = ReadXmlDocument(configuration);
				XmlDocument propertyDocument = ReadXmlDocument(propertyDefaultsXmlFragment);

				XmlNode propertyNode = FindFirst(propertyDocument, propertiesNodeName);
				XmlNode configurationNode = FindFirst(configDocument, configurationNodeName);

				const bool deep = true;
				XmlNode importedNode = configDocument.ImportNode(propertyNode, deep);

				XmlNode firstChild = configurationNode.FirstChild;

				if (firstChild == null)
				{
					configurationNode.AppendChild(importedNode);
				}
				else
				{
					configurationNode.InsertBefore(importedNode, firstChild);
				}

				return configDocument.OuterXml;
			}
			finally
			{
				_msg.DebugStopTiming(watch, "Injected property defaults fragment in configuration");
			}
		}

		[NotNull]
		private static XmlDocument ReadXmlDocument([NotNull] string xml)
		{
			var result = new XmlDocument();
			result.LoadXml(xml);

			return result;
		}

		[NotNull]
		private static XmlNode FindFirst([NotNull] XmlNode parentNode,
		                                 [NotNull] string nodeName)
		{
			foreach (XmlNode node in parentNode.ChildNodes)
			{
				if (string.Equals(node.Name, nodeName, StringComparison.OrdinalIgnoreCase))
				{
					return node;
				}
			}

			throw new ArgumentException("No child node with name {0}", nodeName);
		}

		private void UnwireAssemblyResolve()
		{
			_msg.VerboseDebug(() => "Unwiring AssemblyResolve event");

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
			catch (Exception e)
			{
				_msg.Error("Error unwiring assembly resolve event", e);
			}
		}

		private void WireAssemblyResolve()
		{
			_msg.VerboseDebug(() => "Wiring AssemblyResolve event");

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		private static void LogResolveException(Exception exception)
		{
			_msg.Debug("Error resolving type.", exception);
		}

		#region Event handlers

		[CanBeNull]
		private Assembly CurrentDomain_AssemblyResolve(object sender,
		                                               ResolveEventArgs args)
		{
			string name = args.Name;
			string codeBase = Assembly.GetExecutingAssembly().CodeBase;

			if (AssemblyRedirects.Count == 0)
			{
				return AssemblyResolveUtils.TryLoadAssembly(name, codeBase, _msg.Debug);
			}

			// Check if a version redirect is defined for the assembly:
			AssemblyName assemblyName;

			try
			{
				assemblyName = new AssemblyName(name);
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error loading assembly name '{0}': {1}", name, e.Message);
				return null;
			}

			if (AssemblyRedirects.TryGetValue(assemblyName.Name, out Version version))
			{
				return AssemblyResolveUtils.TryLoadAssembly(
					name, codeBase, version, _msg.Debug);
			}

			return AssemblyResolveUtils.TryLoadAssembly(name, codeBase, _msg.Debug);
		}

		#endregion
	}
}
