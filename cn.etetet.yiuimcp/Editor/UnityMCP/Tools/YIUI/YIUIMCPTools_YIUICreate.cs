#if UNITY_EDITOR



using System;

using System.Threading.Tasks;

using Sirenix.OdinInspector;

using UnityEditor;

using UnityEngine;

using UnityEngine.UI;

using YIUIFramework;



namespace YIUIFramework.Editor.MCP

{

    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUIOpenAutoToolParams : YIUIMCPBaseParams

    {

    }



    [YIUIMCPTools("YIUIOpenAutoTool", "打开YIUI自动化工具窗口")]

    public class YIUIMCPTools_YIUIOpenAutoTool : YIUIMCPBaseExecutor<YIUIOpenAutoToolParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUIOpenAutoToolParams data)

        {

            YIUIAutoTool.OpenWindow();

            await Task.CompletedTask;

            return YIUIMCPResult.Success("已打开 ET/YIUI 自动化工具");

        }

    }



    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUICreateModuleParams : YIUIMCPBaseParams

    {

        public string moduleName;

        public string packageName = "yiuistatesync";

    }



    [YIUIMCPTools("YIUICreateModule", "通过YIUI自动化工具创建UI模块目录")]

    public class YIUIMCPTools_YIUICreateModule : YIUIMCPBaseExecutor<YIUICreateModuleParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUICreateModuleParams data)

        {

            if (string.IsNullOrWhiteSpace(data.moduleName)) return YIUIMCPResult.FailureLog("moduleName不能为空");



            var packageName = YIUIMCPYIUIHelper.NormalizePackageName(data.packageName);

            var moduleName = YIUIMCPYIUIHelper.NormalizeModuleName(data.moduleName);

            UICreateResModule.Create(moduleName, packageName);

            await Task.CompletedTask;



            var moduleRoot = YIUIMCPYIUIHelper.GetModuleRoot(packageName, moduleName);

            return YIUIMCPResult.Success($"模块创建完成: {moduleRoot}");

        }

    }



    [HideLabel]

    [HideReferenceObjectPicker]

    public class YIUICreatePrefabParams : YIUIMCPBaseParams

    {

        public string moduleName;

        public string resName;

        public string codeType = "Common";

        public string packageName = "yiuistatesync";

        public float width = 300;

        public float height = 150;

        public bool overwrite = false;

        public bool addDefaultGraphic = true;

    }



    [YIUIMCPTools("YIUICreatePrefab", "通过Unity/YIUI API创建Panel/View/Common Prefab")]

    public class YIUIMCPTools_YIUICreatePrefab : YIUIMCPBaseExecutor<YIUICreatePrefabParams>

    {

        protected override async Task<YIUIMCPResult> Run(YIUICreatePrefabParams data)

        {

            if (string.IsNullOrWhiteSpace(data.moduleName)) return YIUIMCPResult.FailureLog("moduleName不能为空");

            if (string.IsNullOrWhiteSpace(data.resName)) return YIUIMCPResult.FailureLog("resName不能为空");



            var packageName = YIUIMCPYIUIHelper.NormalizePackageName(data.packageName);

            var moduleName = YIUIMCPYIUIHelper.NormalizeModuleName(data.moduleName);

            var resName = YIUIMCPYIUIHelper.NormalizeResName(data.resName);

            var prefabPath = YIUIMCPYIUIHelper.GetPrefabPath(packageName, moduleName, resName);



            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null && !data.overwrite)

            {

                return YIUIMCPResult.FailureLog($"Prefab已存在，未覆盖: {prefabPath}");

            }



            var codeType = ParseCodeType(data.codeType);

            var root = new GameObject(resName, typeof(RectTransform));

            try

            {

                var rectTransform = root.GetComponent<RectTransform>();

                rectTransform.sizeDelta = new Vector2(data.width, data.height);

                rectTransform.anchorMin = Vector2.zero;

                rectTransform.anchorMax = Vector2.zero;

                rectTransform.pivot = new Vector2(0.5f, 0.5f);



                if (data.addDefaultGraphic)

                {

                    var image = root.AddComponent<Image>();

                    image.color = new Color(0.45f, 0.45f, 0.45f, 1f);

                }



                YIUIMCPYIUIHelper.EnsureCdeTables(root, packageName, moduleName, resName, codeType);



                var directory = System.IO.Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');

                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))

                {

                    System.IO.Directory.CreateDirectory(directory);

                }



                var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

                if (saved == null) return YIUIMCPResult.FailureLog($"Prefab保存失败: {prefabPath}");



                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

            }

            finally

            {

                UnityEngine.Object.DestroyImmediate(root);

            }



            await Task.CompletedTask;

            return YIUIMCPResult.Success($"Prefab创建完成: {prefabPath}");

        }



        private static EUICodeType ParseCodeType(string value)

        {

            if (Enum.TryParse<EUICodeType>(value, true, out var result)) return result;

            throw new ArgumentException($"未知YIUI代码类型: {value}");

        }

    }

}



#endif

