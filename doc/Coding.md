# ProSuite Coding Guidelines

Make sure your editor uses [.editorconfig](../.editorconfig)
in the repository root directory.

Read [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository
root directory.

Look at the **existing code** around and follow the **same style**.

For **Markdown** files, follow
<https://github.com/ujr/guides/blob/main/guides/MarkdownStyle.md>

## Pro SDK Related

Use `try`/`catch` on *all* entry points from the Framework:
this includes **base class overrides** (Button, MapTool, etc.)
and all **event handlers**.
Uncaught exceptions cause ArcGIS Pro to crash!

Event handlers shall begin with `_msg.Debug("NameOfMethod")`.
This helps a lot with debugging. Consider using `nameof` or
the `System.Runtime.CompilerServices.CallerMemberName` attribute.

## Details

Since C# 7.0 prefer `foo is null` over `foo == null`
(reason: `==` can be overloaded, which may lead to errors;
`is` cannot be overloaded).
