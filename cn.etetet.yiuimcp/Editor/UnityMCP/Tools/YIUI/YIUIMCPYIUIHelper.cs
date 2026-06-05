#if UNITY_EDITOR



using System;

using System.Collections.Generic;

using System.Reflection;

using UnityEditor;

using UnityEngine;

using YIUIFramework;

using Object = UnityEngine.Object;



namespace YIUIFramework.Editor.MCP

{

    internal static class YIUIMCPYIUIHelper

    {

        private const string DefaultPackageName = "yiuistatesync";



        public static string NormalizePackageName(string packageName)

        {

            return string.IsNullOrWhiteSpace(packageName)

                ? DefaultPackageName

                : packageName.Replace(" ", string.Empty).Replace("cn.etetet.", string.Empty).Trim();

        }



        public static string NormalizeModuleName(string moduleName)

        {

            if (string.IsNullOrWhiteSpace(moduleName)) throw new ArgumentException("moduleName不能为空");

            return NameUtility.ToFirstUpper(moduleName.Trim());

        }



        public static string NormalizeResName(string resName)

        {

            if (string.IsNullOrWhiteSpace(resName)) throw new ArgumentException("resName不能为空");

            return NameUtility.ToFirstUpper(resName.Trim());

        }



        public static string GetModuleRoot(string packageName, string moduleName)

        {

            return $"Packages/cn.etetet.{NormalizePackageName(packageName)}/Assets/GameRes/YIUI/{NormalizeModuleName(moduleName)}";

        }



        public static string GetPrefabPath(string packageName, string moduleName, string resName)

        {

            return $"{GetModuleRoot(packageName, moduleName)}/Prefabs/{NormalizeResName(resName)}.prefab";

        }



        public static GameObject LoadPrefabAsset(string prefabPath)

        {

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null) throw new InvalidOperationException($"找不到Prefab: {prefabPath}");

            return prefabAsset;

        }



        public static UIBindCDETable GetCdeTable(GameObject prefabRoot, string prefabPath)

        {

            var cdeTable = prefabRoot.GetComponent<UIBindCDETable>();

            if (cdeTable == null) throw new InvalidOperationException($"Prefab缺少UIBindCDETable: {prefabPath}");

            return cdeTable;

        }



        public static UIBindCDETable LoadPrefabCdeTable(string prefabPath, out GameObject prefabAsset)

        {

            prefabAsset = LoadPrefabAsset(prefabPath);

            return GetCdeTable(prefabAsset, prefabPath);

        }



        public static T EditPrefab<T>(string prefabPath, Func<GameObject, UIBindCDETable, T> edit)

        {

            var root = PrefabUtility.LoadPrefabContents(prefabPath);

            try

            {

                var cdeTable = GetCdeTable(root, prefabPath);

                var result = edit(root, cdeTable);

                MarkDirty(root);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                AssetDatabase.SaveAssets();

                return result;

            }

            finally

            {

                PrefabUtility.UnloadPrefabContents(root);

            }

        }



        public static UIBindCDETable EnsureCdeTables(GameObject root, string packageName, string moduleName, string resName, EUICodeType codeType)

        {

            var cdeTable = root.GetOrAddComponent<UIBindCDETable>();

            cdeTable.PkgName = NormalizeModuleName(moduleName);

            cdeTable.ResName = NormalizeResName(resName);

            cdeTable.UICodeType = codeType;

            SetPrivateOrSerializedField(cdeTable, "m_PackagesName", NormalizePackageName(packageName));



            cdeTable.ComponentTable = root.GetOrAddComponent<UIBindComponentTable>();

            cdeTable.DataTable = root.GetOrAddComponent<UIBindDataTable>();

            cdeTable.EventTable = root.GetOrAddComponent<UIBindEventTable>();

            cdeTable.ComponentTable.hideFlags = YIUIConstHelper.Const.DisplayOldCDEInspector ? HideFlags.None : HideFlags.HideInInspector;

            cdeTable.DataTable.hideFlags = YIUIConstHelper.Const.DisplayOldCDEInspector ? HideFlags.None : HideFlags.HideInInspector;

            cdeTable.EventTable.hideFlags = YIUIConstHelper.Const.DisplayOldCDEInspector ? HideFlags.None : HideFlags.HideInInspector;



            if (codeType == EUICodeType.Panel)

            {

                cdeTable.PanelLayer = EPanelLayer.Panel;

                cdeTable.PanelOption = EPanelOption.TimeCache;

                cdeTable.PanelStackOption = EPanelStackOption.VisibleTween;

                cdeTable.CachePanelTime = 10;

                cdeTable.Priority = 0;

            }

            return cdeTable;

        }



        public static T GetPrivateDictionary<T>(object target, string fieldName) where T : class

        {

            var type = target.GetType();

            while (type != null)

            {

                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)

                {

                    if (field.GetValue(target) is not T value) throw new InvalidCastException($"字段 {fieldName} 不是预期类型 {typeof(T).Name}");

                    return value;

                }

                type = type.BaseType;

            }

            throw new MissingFieldException(target.GetType().FullName, fieldName);

        }



        public static void SetPrivateOrSerializedField(object target, string fieldName, object value)

        {

            var type = target.GetType();

            while (type != null)

            {

                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)

                {

                    field.SetValue(target, value);

                    return;

                }

                type = type.BaseType;

            }

            throw new MissingFieldException(target.GetType().FullName, fieldName);

        }



        public static UIDataValue CreateDataValue(string dataValueType, string defaultValue)

        {

            switch ((dataValueType ?? "String").Trim().ToLowerInvariant())

            {

                case "string":

                    var stringValue = new UIDataValueString();

                    stringValue.SetValue(defaultValue ?? string.Empty, true, false);

                    return stringValue;

                case "bool":

                case "boolean":

                    var boolValue = new UIDataValueBool();

                    boolValue.SetValue(bool.TryParse(defaultValue, out var boolResult) && boolResult, true, false);

                    return boolValue;

                case "int":

                case "int32":

                    var intValue = new UIDataValueInt();

                    intValue.SetValue(int.TryParse(defaultValue, out var intResult) ? intResult : 0, true, false);

                    return intValue;

                case "long":

                case "int64":

                    var longValue = new UIDataValueLong();

                    longValue.SetValue(long.TryParse(defaultValue, out var longResult) ? longResult : 0, true, false);

                    return longValue;

                case "float":

                case "single":

                    var floatValue = new UIDataValueFloat();

                    floatValue.SetValue(float.TryParse(defaultValue, out var floatResult) ? floatResult : 0f, true, false);

                    return floatValue;

                default:

                    throw new ArgumentException($"暂不支持的YIUI数据类型: {dataValueType}");

            }

        }



        public static UIEventBase CreateEvent(string eventType, bool isTaskEvent, string eventName)

        {

            if ((eventType ?? "P0").Trim().ToUpperInvariant() != "P0") throw new ArgumentException($"MVP阶段仅支持P0事件，当前: {eventType}");

            return isTaskEvent ? new UITaskEventP0(eventName) : new UIEventP0(eventName);

        }



        public static Transform FindChildByPath(Transform root, string childPath)

        {

            if (string.IsNullOrWhiteSpace(childPath) || childPath == ".") return root;

            var current = root;

            foreach (var part in childPath.Replace('\\', '/').Trim('/').Split('/'))

            {

                current = current.Find(part);

                if (current == null) throw new InvalidOperationException($"找不到子节点: {childPath}");

            }

            return current;

        }



        public static Component FindComponentByType(Transform target, string componentType)

        {

            if (string.IsNullOrWhiteSpace(componentType)) throw new ArgumentException("componentType不能为空");

            var component = target.GetComponent(ResolveType(componentType));

            if (component == null) throw new InvalidOperationException($"节点 {target.name} 上没有组件 {componentType}");

            return component;

        }



        public static Type ResolveType(string typeName)

        {

            var type = Type.GetType(typeName);

            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())

            {

                type = assembly.GetType(typeName);

                if (type != null) return type;

            }

            throw new TypeLoadException($"找不到类型: {typeName}");

        }



        public static bool AutoCheck(UIBindCDETable cdeTable)

        {

            var method = typeof(UIBindCDETable).GetMethod("AutoCheck", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null) throw new MissingMethodException(typeof(UIBindCDETable).FullName, "AutoCheck");

            return method.Invoke(cdeTable, null) is true;

        }



        public static void MarkDirty(Object obj)

        {

            if (obj != null) EditorUtility.SetDirty(obj);

        }

    }

}



#endif

