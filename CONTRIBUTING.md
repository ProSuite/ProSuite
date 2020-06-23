# Contributing Guidelines

ProSuite is maintained by The ProSuite Authors.

## Committing

Respect intellectual property rights and copyright laws.
Only commit content consistent with our license.

Avoid `git push --force`

## Naming

Use namespace infixes to indicate essential dependencies:
**AO** ArcObjects, **AGD** ArcGIS Desktop, **AGP** ArcGIS Pro,
**GP** GeoProcessing, **AO.Core** the subset of ArcObjects that
is also part of the Enterprise SDK.

Avoid company names, in identifiers as well as in comments.

## Build Instructions: Environment Variables Used in Project Files

The following properties of the csproj files can be changed using environment variables. As an example, the [bat file](src/ProSuite_VS19_net48.bat) next to the solution can be used to open the solution in Visual Studio 2019 with a .NET target framework 4.8 and the output directory being ProSuite/bin.

### Target Framework
The default (and minimum) .NET target framework can be overridden using the environment variable **TargetFrameworkVersion**.

### Output directory
In order to facilitate using these projects in a git subtree configuration in other repositories, the default output directory is ..\..\..\bin which is outside of this repository. However, it can be changed using the environment variable **OutputDirectory**.

### Key Pair File for Strong Naming
In order to compile the solution with a different identity, the environment variable **ProSuiteKeyFile** can be adapted.

## Logging

Utility methods should not log except at DEBUG level.

When an error is logged, an exception should be thrown.
