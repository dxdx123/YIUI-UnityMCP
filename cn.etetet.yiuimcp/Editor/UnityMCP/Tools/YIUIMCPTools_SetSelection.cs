using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// 设置选中参数
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    public class SetSelectionParams : YIUIMCPBaseParams
    {
        /// <summary>
        /// 目标 GameObject 的 instanceId（优先）。为 0 时改用 searchTerm 查找。
        /// </summary>
        [LabelText("实例ID")]
        public int instanceId = 0;

        /// <summary>
        /// 查找词（当 instanceId 为 0 时使用）
        /// </summary>
        [LabelText("查找词")]
        public string searchTerm;

        /// <summary>
        /// 查找方式：by_name / by_path / by_id / by_tag / by_layer / by_component
        /// </summary>
        [LabelText("查找方式")]
        public string searchMethod = "by_name";
    }

    /// <summary>
    /// 设置 Unity 编辑器当前选中的 GameObject。
    /// 这是用 ExecuteMenu 驱动 "GameObject/YIUI/*" 控件创建菜单的前置：
    /// 那些菜单会在「当前选中」物体下创建子控件，所以需要先用本工具选中父物体。
    /// 复用搬运自 Coplay 的 GameObjectLookup 做解析（支持场景与 Prefab 编辑态）。
    /// </summary>
    [YIUIMCPTools("SetSelection", "设置编辑器选中的GameObject(供 GameObject/YIUI/* 菜单与 ExecuteMenu 定位父物体)")]
    public class YIUIMCPTools_SetSelection : YIUIMCPBaseExecutor<SetSelectionParams>
    {
        protected override Task<YIUIMCPResult> Run(SetSelectionParams data)
        {
            GameObject go = null;

            if (data.instanceId != 0)
            {
                go = GameObjectLookup.FindById(data.instanceId);
                if (go == null)
                {
                    return Task.FromResult(YIUIMCPResult.Failure($"instanceId={data.instanceId} 未解析到 GameObject"));
                }
            }
            else if (!string.IsNullOrEmpty(data.searchTerm))
            {
                var method = string.IsNullOrEmpty(data.searchMethod) ? "by_name" : data.searchMethod;
                var ids = GameObjectLookup.SearchGameObjects(method, data.searchTerm, true, 1);
                if (ids.Count > 0)
                {
                    go = GameObjectLookup.FindById(ids[0]);
                }

                if (go == null)
                {
                    return Task.FromResult(YIUIMCPResult.Failure($"未找到 GameObject: searchTerm='{data.searchTerm}', searchMethod='{method}'"));
                }
            }
            else
            {
                return Task.FromResult(YIUIMCPResult.Failure("必须提供 instanceId 或 searchTerm"));
            }

            Selection.activeGameObject = go;
            Selection.objects = new Object[] { go };
            EditorGUIUtility.PingObject(go);

            var path = GameObjectLookup.GetGameObjectPath(go);
            return Task.FromResult(YIUIMCPResult.Success($"已选中: {path} (instanceId={go.GetInstanceID()})"));
        }
    }
}
