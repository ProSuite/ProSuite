# ProSuite Open Source Knowledge

This is the open-source heart of ProSuite — a productivity extension for ArcGIS focused on data production, quality assurance, and cartographic refinement.

## Key Modules

| Module | Purpose |
|--------|----------|
| `ProSuite.Commons` | Core utilities and helpers — **check here first before writing new code!** |
| `ProSuite.Commons.Essentials` | Foundational interfaces and attributes |
| `ProSuite.Commons.Logging` | Logging abstractions |
| `ProSuite.Commons.Orm.NHibernate` | NHibernate ORM utilities |
| `ProSuite.DomainModel.Core` | Domain entities (Data Dictionary, QA Specs) — Esri-independent |
| `ProSuite.DomainModel.AO` | Domain model for ArcGIS Objects (ArcMap/Server) |
| `ProSuite.DomainServices.Core` | Domain services — Esri-independent |
| `ProSuite.DomainServices.AO` | Domain services for ArcGIS Objects |
| `ProSuite.QA.Core` | QA interfaces, issue codes, base classes |
| `ProSuite.Microservices.*` | gRPC client/server and Protobuf definitions |
| `ProSuite.AGP.QA` | QA components for ArcGIS Pro |
| `ProSuite.DdxEditor.*` | Data Dictionary Editor framework and content |

## Development Guidelines

### 🔴 Error Handling: Warn & Throw!

**Never swallow exceptions.** Log and rethrow so errors bubble up to where they can be properly handled.

```csharp
// ✅ GOOD
catch (Exception ex)
{
    _msg.Warn("Failed to process", ex);
    throw;
}

// ❌ BAD — error is hidden
catch (Exception ex)
{
    _msg.Warn("Failed to process", ex);
    return null;
}
```

### 🔴 Reuse Existing Code

Before writing anything new:
1. Search `ProSuite.Commons` for existing utilities
2. Check adjacent files for similar patterns
3. Look for base classes you can extend

### 🔴 Keep It Simple

- Avoid defensive `if/else` chains — use early returns and guard clauses
- Trust the type system
- Make minimal changes — every existing line likely has a purpose

## Code Style (per ReSharper)

- **Braces:** Always required for `if`, `for`, `foreach`, `while`, `using`
- **Naming:** Private fields `_camelCase`, public `PascalCase`, locals `camelCase`
- **`var`:** Use only when type is evident from initializer
- **Line wrap:** 100 characters

## ArcObjects (AO) vs ArcGIS Pro (AGP)

ProSuite supports both API paradigms:

| Namespace | API | Used For |
|-----------|-----|----------|
| `ProSuite.Commons.AO.*` | ArcObjects (COM) | Server, legacy ArcMap |
| `ProSuite.Commons.AGP.*` | ArcGIS Pro SDK | Pro Add-Ins |
| `ProSuite.Commons.AO.Geodatabase.GdbSchema` | Custom wrappers | Platform-agnostic code |

**Custom Geodatabase Wrappers:**
- `IReadOnlyTable`, `IReadOnlyRow` — abstract interfaces
- `GdbRow`, `GdbTable`, `GdbFeature`, `ReadOnlyTable` — wrapper classes
- Prefer these over raw COM interfaces in new code

## gRPC/Protobuf Microservices

ProSuite provides the foundation for gRPC-based microservices:

### Key Modules

| Module | Purpose |
|--------|--------|
| `ProSuite.Microservices.Client` | Client-side gRPC utilities (`GrpcClientUtils`) |
| `ProSuite.Microservices.Client.GrpcNet` | .NET gRPC client implementation |
| `ProSuite.Microservices.Client.GrpcCore` | Grpc.Core client implementation |
| `ProSuite.Microservices.Server.AO` | Server-side gRPC utilities (`GrpcServerUtils`) |
| `ProSuite.Microservices.Definitions.*` | Generated Protobuf message/service classes |

### Utility Classes

- **`ProtobufGeometryUtils`** — Convert ArcObjects geometries ↔ `ShapeMsg`
- **`ProtobufGdbUtils`** — Convert geodatabase objects ↔ `GdbObjectMsg`
- **`GrpcServerUtils.ExecuteServiceCall()`** — Wrap server-side operations with proper error handling
- **`GrpcClientUtils.TryAsync()`** — Wrap client-side calls with timeout/cancellation

### Proto Files

Shared proto files in `ProSuite/src/protos/` define common messages like `ShapeMsg`, `GdbObjectMsg`, `ObjectClassMsg`, etc.

## Dependencies

⚠️ **This is open-source code.** It cannot depend on `ProSuite.Shared`, `Swisstopo.GoTop`, or `Swisstopo.TopGen`. Those projects depend on *this* code, not the reverse.
