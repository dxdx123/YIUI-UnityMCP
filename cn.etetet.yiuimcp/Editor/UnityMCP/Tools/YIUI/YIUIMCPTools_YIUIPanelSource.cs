#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using YIUIFramework;
using Object = UnityEngine.Object;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// 创建标准 YIUI Panel 源数据(Source) 参数
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUICreatePanelSourceParams : YIUIMCPBaseParams
    {
        /// <summary>ET 分包名(默认 yiuistatesync)</summary>
        public string packageName = "yiuistatesync";

        /// <summary>YIUI 模块名(目录), 如 Test</summary>
        public string moduleName;

        /// <summary>面板逻辑名(默认=moduleName)。最终: {resName}PanelSource → 拆分后 {resName}Panel</summary>
        public string resName;

        /// <summary>已存在时是否覆盖</summary>
        public bool overwrite = false;
    }

    /// <summary>
    /// 创建标准 YIUI Panel 源数据(Source)。
    /// 等价右键 Assets/YIUI/Create UIPanelSource —— 通过
    /// MenuItemYIUIPanelSource.CreateYIUIPanel() 生成含
    /// UIBlockBG / AllViewParent / AllPopupViewParent 的完整面板(IsSplitData=true),
    /// 存到 模块/Source/{resName}PanelSource.prefab。
    /// 之后用 YIUISourceSplit 拆分到 Prefabs/。
    /// </summary>
    [YIUIMCPTools("YIUICreatePanelSource", "创建标准YIUI Panel源数据(Source,含UIBlockBG/AllViewParent/AllPopupViewParent)")]
    public class YIUIMCPTools_YIUICreatePanelSource : YIUIMCPBaseExecutor<YIUICreatePanelSourceParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUICreatePanelSourceParams data)
        {
            if (string.IsNullOrWhiteSpace(data.moduleName)) return YIUIMCPResult.FailureLog("moduleName不能为空");

            var pkg = YIUIMCPYIUIHelper.NormalizePackageName(data.packageName);
            var module = YIUIMCPYIUIHelper.NormalizeModuleName(data.moduleName);
            var baseName = string.IsNullOrWhiteSpace(data.resName)
                ? module
                : YIUIMCPYIUIHelper.NormalizeResName(data.resName);

            var sourceFolder = $"Packages/cn.etetet.{pkg}/Assets/GameRes/YIUI/{module}/{YIUIConstHelper.Const.UISource}";
            if (!Directory.Exists(sourceFolder))
            {
                Directory.CreateDirectory(sourceFolder);
                AssetDatabase.Refresh();
            }

            var saveName = $"{baseName}{YIUIConstHelper.Const.UIPanelSourceName}";
            var savePath = $"{sourceFolder}/{saveName}.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(savePath) != null)
            {
                if (!data.overwrite) return YIUIMCPResult.FailureLog($"源面板已存在,未覆盖: {savePath}");
                AssetDatabase.DeleteAsset(savePath);
            }

            // 与官方一致: 生成完整结构的 Source 面板
            var panel = MenuItemYIUIPanelSource.CreateYIUIPanel();
            if (panel == null) return YIUIMCPResult.FailureLog("CreateYIUIPanel 返回 null");

            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(panel, savePath);
                if (saved == null) return YIUIMCPResult.FailureLog($"保存源面板失败: {savePath}");
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            await Task.CompletedTask;
            return YIUIMCPResult.Success($"源面板创建完成: {savePath} (拆分后将生成 {baseName}{YIUIConstHelper.Const.UIPanelName})");
        }
    }

    /// <summary>
    /// 源数据拆分参数
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    public class YIUISourceSplitParams : YIUIMCPBaseParams
    {
        /// <summary>源面板 prefab 路径(Source 下的 *PanelSource.prefab)</summary>
        public string sourcePrefabPath;
    }

    /// <summary>
    /// 源数据拆分: 把 Source 面板拆分并生成正式面板到 Prefabs/ 目录。
    /// 等价 CDE 检视面板上的「源数据拆分」按钮 —— 调用 UIPanelSourceSplit.Do(cdeTable)。
    /// </summary>
    [YIUIMCPTools("YIUISourceSplit", "源数据拆分: 将Source面板拆分生成到Prefabs目录(等价CDE的源数据拆分按钮)")]
    public class YIUIMCPTools_YIUISourceSplit : YIUIMCPBaseExecutor<YIUISourceSplitParams>
    {
        protected override async Task<YIUIMCPResult> Run(YIUISourceSplitParams data)
        {
            if (string.IsNullOrWhiteSpace(data.sourcePrefabPath)) return YIUIMCPResult.FailureLog("sourcePrefabPath不能为空");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.sourcePrefabPath);
            if (prefab == null) return YIUIMCPResult.FailureLog($"找不到源面板: {data.sourcePrefabPath}");

            var cdeTable = prefab.GetComponent<UIBindCDETable>();
            if (cdeTable == null) return YIUIMCPResult.FailureLog($"源面板缺少 UIBindCDETable: {data.sourcePrefabPath}");

            // IsSplitData 是 ET.YIUIFramework 程序集 internal 成员, 跨程序集用反射读取
            var isSplitField = typeof(UIBindCDETable).GetField("IsSplitData", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var isSplit = isSplitField != null && isSplitField.GetValue(cdeTable) is true;
            if (!isSplit) return YIUIMCPResult.FailureLog($"该面板不是源数据(IsSplitData=false),无法拆分: {data.sourcePrefabPath}");

            // 先自动检查(与官方流程一致)
            YIUIMCPYIUIHelper.AutoCheck(cdeTable);

            UIPanelSourceSplit.Do(cdeTable);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            await Task.CompletedTask;
            return YIUIMCPResult.Success($"源数据拆分完成: {data.sourcePrefabPath}");
        }
    }
}
#endif
