using System;
using System.Collections.Generic;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// 发现并调用「Coplay 风格」工具的注册表。
    ///
    /// 与原生 <see cref="YIUIMCPToolsRegistry"/> 平行：原生工具是
    /// <c>[YIUIMCPTools] + YIUIMCPBaseExecutor&lt;T&gt;</c>，返回 <see cref="YIUIMCPResult"/>；
    /// 这里托管的是从 CoplayDev/unity-mcp 搬运来的工具——
    /// <c>[McpForUnityTool] + static object HandleCommand(JObject)</c>，返回 SuccessResponse/ErrorResponse。
    ///
    /// 反射扫描已加载程序集，建立 工具名 -> HandleCommand 方法 的映射。
    /// 本类只做发现与调用，无任何 InitializeOnLoad / 副作用，由
    /// <c>YIUIMCPServerHelper</c> 在启动时显式调用 <see cref="Initialize"/>。
    /// </summary>
    public static class YIUIMCPCoplayRegistry
    {
        public class CoplayToolInfo
        {
            public string Name;
            public string Description;
            public string Group;
            public MethodInfo Handler; // public static object HandleCommand(JObject)
        }

        private static readonly Dictionary<string, CoplayToolInfo> _tools = new();

        public static IReadOnlyDictionary<string, CoplayToolInfo> Tools => _tools;

        public static int ToolCount => _tools.Count;

        private static bool m_Initialized = false;

        public static void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Initialized = true;
            _tools.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    if (ShouldSkipAssembly(assembly))
                    {
                        continue;
                    }

                    foreach (var type in assembly.GetTypes())
                    {
                        McpForUnityToolAttribute attr;
                        try
                        {
                            attr = type.GetCustomAttribute<McpForUnityToolAttribute>();
                        }
                        catch
                        {
                            continue;
                        }

                        if (attr == null)
                        {
                            continue;
                        }

                        var method = type.GetMethod(
                            "HandleCommand",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new[] { typeof(JObject) },
                            null);

                        if (method == null)
                        {
                            YIUIMCPLog.LogError($"[Coplay] {type.FullName} 标了 [McpForUnityTool] 但缺少 'public static object HandleCommand(JObject)'，跳过");
                            continue;
                        }

                        var name = !string.IsNullOrEmpty(attr.Name)
                            ? attr.Name
                            : StringCaseUtility.ToSnakeCase(type.Name);

                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        if (_tools.ContainsKey(name))
                        {
                            YIUIMCPLog.LogError($"[Coplay] 工具名重复: {name} ({type.FullName})，跳过");
                            continue;
                        }

                        // 与原生 YIUIMCP 工具同名时，原生优先；这里跳过并告警
                        if (YIUIMCPToolsRegistry.Has(name))
                        {
                            YIUIMCPLog.LogError($"[Coplay] 工具名 {name} 与原生 YIUIMCP 工具冲突，跳过 ({type.FullName})");
                            continue;
                        }

                        _tools.Add(name, new CoplayToolInfo
                        {
                            Name = name,
                            Description = attr.Description,
                            Group = attr.Group,
                            Handler = method
                        });
                    }
                }
                catch (Exception e)
                {
                    YIUIMCPLog.LogError($"[Coplay] 扫描程序集失败 {assembly.FullName}: {e.Message}");
                }
            }

            YIUIMCPLog.Log($"[Coplay] 已注册 Coplay 风格工具数: {_tools.Count}");
        }

        public static bool Has(string name)
        {
            return !string.IsNullOrEmpty(name) && _tools.ContainsKey(name);
        }

        /// <summary>
        /// 调用一个 Coplay 工具。返回值是工具的响应对象（SuccessResponse/ErrorResponse/...），
        /// 或在工具为异步实现时返回 Task&lt;object&gt; —— 两者都由 YIUIMCPDispatcher 统一处理。
        /// 必须在主线程调用（经 YIUIMCPDispatcher.Dispatch）。
        /// </summary>
        public static object Invoke(string name, JObject @params)
        {
            if (!_tools.TryGetValue(name, out var info))
            {
                return new ErrorResponse($"Coplay tool '{name}' not found.");
            }

            try
            {
                return info.Handler.Invoke(null, new object[] { @params ?? new JObject() });
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                YIUIMCPLog.LogError($"[Coplay] 工具 '{name}' 执行异常: {inner}");
                return new ErrorResponse($"Coplay tool '{name}' error: {inner.Message}");
            }
            catch (Exception ex)
            {
                YIUIMCPLog.LogError($"[Coplay] 工具 '{name}' 调用异常: {ex}");
                return new ErrorResponse($"Coplay tool '{name}' error: {ex.Message}");
            }
        }

        private static bool ShouldSkipAssembly(Assembly assembly)
        {
            var fullName = assembly.FullName;
            return fullName.StartsWith("System") ||
                   fullName.StartsWith("Unity") ||
                   fullName.StartsWith("Microsoft") ||
                   fullName.StartsWith("mscorlib") ||
                   fullName.StartsWith("netstandard") ||
                   fullName.StartsWith("Newtonsoft");
        }
    }
}
