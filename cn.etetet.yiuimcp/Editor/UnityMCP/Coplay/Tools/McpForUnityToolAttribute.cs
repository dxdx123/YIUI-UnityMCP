// ----------------------------------------------------------------------------
// Ported from MCP for Unity — https://github.com/CoplayDev/unity-mcp
// Copyright (c) 2025 CoplayDev. Licensed under the MIT License.
// Full license text: see THIRD-PARTY-NOTICES.md at the repository root.
// Vendored into YIUI-UnityMCP (cn.etetet.yiuimcp) with minimal changes.
//
// Tools marked with [McpForUnityTool] are discovered and dispatched by
// YIUIFramework.Editor.MCP.YIUIMCPCoplayRegistry (not by Coplay's own
// CommandRegistry, which is NOT vendored).
// ----------------------------------------------------------------------------
using System;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Marks a class as an MCP tool handler
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class McpForUnityToolAttribute : Attribute
    {
        /// <summary>
        /// Tool name (if null, derived from class name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tool description for LLM
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this tool returns structured output
        /// </summary>
        public bool StructuredOutput { get; set; } = true;

        /// <summary>
        /// Controls whether this tool is automatically registered.
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// Tool group for dynamic visibility.
        /// Valid groups: core, vfx, animation, ui, scripting_ext, testing, menu.
        /// </summary>
        public string Group { get; set; } = "core";

        /// <summary>
        /// Enables the polling middleware for long-running tools.
        /// </summary>
        public bool RequiresPolling { get; set; } = false;

        /// <summary>
        /// The action name to use when polling for status. Defaults to "status".
        /// </summary>
        public string PollAction { get; set; } = "status";

        /// <summary>
        /// Maximum seconds to poll before timing out. 0 means use the server default.
        /// </summary>
        public int MaxPollSeconds { get; set; } = 0;

        /// <summary>
        /// The command name used to route requests to this tool.
        /// If not specified, defaults to the PascalCase class name converted to snake_case.
        /// </summary>
        public string CommandName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Create an MCP tool attribute with auto-generated command name.
        /// </summary>
        public McpForUnityToolAttribute()
        {
            Name = null; // Will be auto-generated
        }

        /// <summary>
        /// Create an MCP tool attribute with explicit command name.
        /// </summary>
        public McpForUnityToolAttribute(string name = null)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Describes a tool parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ToolParameterAttribute : Attribute
    {
        /// <summary>
        /// Parameter description for LLM
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this parameter is required
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Default value (as string)
        /// </summary>
        public string DefaultValue { get; set; }

        public ToolParameterAttribute(string description)
        {
            Description = description;
        }
    }
}
