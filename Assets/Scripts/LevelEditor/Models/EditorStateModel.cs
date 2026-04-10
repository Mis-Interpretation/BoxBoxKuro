using System.Collections.Generic;
using UnityEngine;
using Zenject;

public enum DrawingMode { Point, Line, RectFill, RectEdge }
public enum EditorMode { Brush, Select }

/// <summary>
/// 编辑器运行时状态：当前关卡数据、选中的笔刷、已放置的对象。
/// </summary>
public class EditorStateModel : MonoBehaviour
{
    [System.NonSerialized]
    public IEntityConfigReader ConfigReader;

    [System.NonSerialized]
    public IEntityFactory EntityFactory;

    private void Awake()
    {
        ConfigReader = new JsonEntityConfigProvider();
        var container = new DiContainer();
        container.Bind<IEntityConfigReader>().FromInstance(ConfigReader).AsSingle();
        container.Bind<DiContainer>().FromInstance(container).AsSingle();
        EntityFactory = new EntityFactory(container, ConfigReader);
    }

    [HideInInspector]
    public EditorMode CurrentEditorMode = EditorMode.Brush;

    [HideInInspector]
    public int SelectedTypeIndex = 1; // default: Block

    [HideInInspector]
    public TextEntityPayload BrushTextPayload = new TextEntityPayload();

    [HideInInspector]
    public DrawingMode CurrentDrawingMode = DrawingMode.Line;

    [HideInInspector]
    public DrawingMode CurrentSelectShapeMode = DrawingMode.Point;

    [System.NonSerialized]
    public SelectionModel Selection = new SelectionModel();

    [HideInInspector]
    public LevelDataModel CurrentLevel = new LevelDataModel();

    /// <summary>
    /// 网格坐标 → 该位置上已放置的所有 GameObject（支持 Endpoint 与其他实体重叠）。
    /// </summary>
    [System.NonSerialized]
    public Dictionary<Vector2Int, List<GameObject>> PlacedObjects = new Dictionary<Vector2Int, List<GameObject>>();

    /// <summary>
    /// 通过工厂创建指定类型的实体。
    /// </summary>
    public GameObject CreateEntity(int typeIndex, Vector2Int cell)
    {
        return EntityFactory?.Create(typeIndex, cell);
    }

    public GameObject CreateEntity(EntityData data)
    {
        return EntityFactory?.Create(data);
    }

    public bool TryResolveSelectionAnchor(Vector2Int cell, out Vector2Int anchorCell)
    {
        if (PlacedObjects.ContainsKey(cell))
        {
            anchorCell = cell;
            return true;
        }

        foreach (var kvp in PlacedObjects)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj == null) continue;
                var runtime = obj.GetComponent<TextEntityRuntimeModel>();
                if (runtime == null) continue;

                if (TextEntityUtility.ContainsCell(kvp.Key, runtime.Payload, cell))
                {
                    anchorCell = kvp.Key;
                    return true;
                }
            }
        }

        anchorCell = default;
        return false;
    }
}
