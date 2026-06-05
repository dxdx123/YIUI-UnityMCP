#if UNITY_EDITOR



using System;

using System.Collections.Generic;

using System.Threading.Tasks;

using Sirenix.OdinInspector;

using UnityEngine;

using YIUIFramework;



namespace YIUIFramework.Editor.MCP

{

    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIAddComponentBindingParams : YIUIMCPBaseParams

    {

        public string prefabPath;

        public string bindName;

        public string targetPath;

        public string componentType;

    }



    [YIUIMCPTools("YIUIAddComponentBinding", "通过YIUI API给Prefab添加Component绑定")]

    public class YIUIMCPTools_YIUIAddComponentBinding : YIUIMCPBaseExecutor<YIUIAddComponentBindingParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIAddComponentBindingParams data)

        {

            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");

            if (string.IsNullOrWhiteSpace(data.bindName)) return YIUIMCPResult.FailureLog("bindName不能为空");

            if (string.IsNullOrWhiteSpace(data.componentType)) return YIUIMCPResult.FailureLog("componentType不能为空");



            var message = YIUIMCPYIUIHelper.EditPrefab(data.prefabPath, (root, cdeTable) =>

            {

                if (cdeTable.ComponentTable == null) cdeTable.ComponentTable = root.GetOrAddComponent<UIBindComponentTable>();



                var target = YIUIMCPYIUIHelper.FindChildByPath(root.transform, data.targetPath);

                var component = YIUIMCPYIUIHelper.FindComponentByType(target, data.componentType);

                var bindDic = YIUIMCPYIUIHelper.GetPrivateDictionary<Dictionary<string, Component>>(cdeTable.ComponentTable, "m_AllBindDic");

                bindDic[data.bindName] = component;



                YIUIMCPYIUIHelper.MarkDirty(cdeTable.ComponentTable);

                YIUIMCPYIUIHelper.MarkDirty(cdeTable);

                return $"添加Component绑定成功: {data.bindName} -> {data.prefabPath}";

            });



            await Task.CompletedTask;

            return YIUIMCPResult.Success(message);

        }

    }



    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIExportCodeParams : YIUIMCPBaseParams

    {

        public string prefabPath;

        public string packageName = "yiuistatesync";

        public bool refresh = true;

        public bool tips = false;

    }



    [YIUIMCPTools("YIUIExportCode", "调用YIUI自动化工具为Prefab导出生成代码")]

    public class YIUIMCPTools_YIUIExportCode : YIUIMCPBaseExecutor<YIUIExportCodeParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIExportCodeParams data)

        {

            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");



            var packageName = YIUIMCPYIUIHelper.NormalizePackageName(data.packageName);

            var cdeTable = YIUIMCPYIUIHelper.LoadPrefabCdeTable(data.prefabPath, out _);

            if (!YIUIMCPYIUIHelper.AutoCheck(cdeTable)) return YIUIMCPResult.FailureLog($"YIUI AutoCheck失败: {data.prefabPath}");



            UICreateModule.CreatePackages(cdeTable, data.refresh, data.tips, packageName);

            await Task.CompletedTask;

            return YIUIMCPResult.Success($"YIUI代码导出完成: {data.prefabPath}, package={packageName}");

        }

    }



    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIInspectPrefabParams : YIUIMCPBaseParams

    {

        public string prefabPath;

    }



    [YIUIMCPTools("YIUIInspectPrefab", "读取Prefab上的YIUI CDE绑定信息")]

    public class YIUIMCPTools_YIUIInspectPrefab : YIUIMCPBaseExecutor<YIUIInspectPrefabParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIInspectPrefabParams data)

        {

            if (string.IsNullOrWhiteSpace(data.prefabPath)) return YIUIMCPResult.FailureLog("prefabPath不能为空");



            var cdeTable = YIUIMCPYIUIHelper.LoadPrefabCdeTable(data.prefabPath, out _);

            var components = cdeTable.ComponentTable == null ? Array.Empty<string>() : new List<string>(cdeTable.ComponentTable.AllBindDic.Keys).ToArray();

            var datas = cdeTable.DataTable == null ? Array.Empty<string>() : new List<string>(cdeTable.DataTable.DataDic.Keys).ToArray();

            var events = cdeTable.EventTable == null ? Array.Empty<string>() : new List<string>(cdeTable.EventTable.EventDic.Keys).ToArray();

            await Task.CompletedTask;



            return YIUIMCPResult.Success(

                $"prefabPath={data.prefabPath}\n" +

                $"pkgName={cdeTable.PkgName}\n" +

                $"resName={cdeTable.ResName}\n" +

                $"codeType={cdeTable.UICodeType}\n" +

                $"components=[{string.Join(",", components)}]\n" +

                $"datas=[{string.Join(",", datas)}]\n" +

                $"events=[{string.Join(",", events)}]");

        }

    }

}



#endif

