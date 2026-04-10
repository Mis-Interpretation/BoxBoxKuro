public static class SceneNameModel
{
    public static string StartScene => startScene ;
    public static string LevelScene => levelScene;
    public static string EditorScene => editorScene;
    public static string ArrangeScene => arrangeScene;
    public static string SelectScene => selectScene;
    public static string EntityConfigScene => entityConfigScene;

    private static string startScene = "MainScene";
    private static string levelScene = "PlayLevel";
    private static string editorScene = "LevelEditorScene";
    private static string arrangeScene = "LevelArrangementScene";
    private static string selectScene = "LevelSelectScene";
    private static string entityConfigScene = "EntityConfigScene";
}