# Third-Party Notices

This project includes third-party source code. The original copyright and
license notices are reproduced below as required.

---

## MCP for Unity (CoplayDev/unity-mcp)

- Project: https://github.com/CoplayDev/unity-mcp
- Copyright (c) 2025 CoplayDev
- License: MIT

A subset of tool and helper source files was vendored into this project (with
minimal changes) under:

```
cn.etetet.yiuimcp/Editor/UnityMCP/Coplay/
```

Vendored files (each also carries an attribution header in-source):

- `Tools/McpForUnityToolAttribute.cs`
- `Tools/FindGameObjects.cs`
- `Helpers/Response.cs`
- `Helpers/ToolParams.cs`
- `Helpers/ParamCoercion.cs`
- `Helpers/StringCaseUtility.cs`
- `Helpers/Pagination.cs`
- `Helpers/McpLog.cs` (modified: removed the upstream `Constants.EditorPrefKeys` dependency)
- `Helpers/UnityTypeResolver.cs`
- `Helpers/GameObjectLookup.cs`
- `Runtime/Helpers/UnityObjectIdCompat.cs`
- `Runtime/Helpers/UnityAssembliesCompat.cs`

Discovery/dispatch glue (`Editor/UnityMCP/Core/YIUIMCPCoplayRegistry.cs`) is
original YIUIMCP code and is licensed under this project's own license; it is
not part of the upstream MCP for Unity project.

### MIT License

```
MIT License

Copyright (c) 2025 CoplayDev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
