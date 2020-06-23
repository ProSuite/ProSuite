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

## Logging

Utility methods should not log except at DEBUG level.

When an error is logged, an exception should be thrown.
