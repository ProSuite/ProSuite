using System.Reflection;

[assembly: AssemblyProduct("ProSuite")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("© 2020-2022 The ProSuite Authors")]
[assembly: AssemblyTrademark("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]
#endif
