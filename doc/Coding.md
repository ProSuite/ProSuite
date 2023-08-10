# ProSuite Coding Guidelines

Make sure your editor uses [.editorconfig](../.editorconfig)
in the repository root directory.

Read [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository
root directory.

Look at the **existing code** around and follow the **same style**.

For **Markdown** files, follow
<https://github.com/ujr/guides/blob/main/guides/MarkdownStyle.md>

## Details

Since C# 7.0 prefer `foo is null` over `foo == null`
(reason: `==` can be overloaded, which may lead to errors;
`is` cannot be overloaded).
