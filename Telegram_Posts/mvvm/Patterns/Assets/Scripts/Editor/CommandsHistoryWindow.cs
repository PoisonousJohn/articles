using UnityEngine;
using UnityEditor;
using Zenject;

public class CommandsHistoryWindow : EditorWindow
{

    [MenuItem("Window/CommandsHistoryWindow")]
    public static CommandsHistoryWindow GetOrCreateWindow()
    {
        var window = EditorWindow.GetWindow<CommandsHistoryWindow>();
        window.titleContent = new GUIContent("CommandsHistoryWindow");
        return window;
    }

    public void OnGUI()
    {

        // this part is required to get
        // DI context of the scene
        var sceneContext = GameObject.FindObjectOfType<SceneContext>();
        if (sceneContext == null || sceneContext.Container == null)
        {
            return;
        }
        // this guard ensures that OnGUI runs only when IGameStateCommandExecutor exists
        // in other words only in runtime
        var executor = sceneContext.Container.TryResolve<IGameStateCommandsExecutor>() as DebugCommandsExecutor;
        if (executor == null)
        {
            return;
        }

        // general buttons to load and save "snapshot"
        EditorGUILayout.BeginHorizontal();
        _replayName = EditorGUILayout.TextField("Replay name", _replayName);
        if (GUILayout.Button("Save"))
        {
            executor.SaveReplay(_replayName);
        }
        if (GUILayout.Button("Load"))
        {
            executor.LoadReplay(_replayName);
        }
        EditorGUILayout.EndHorizontal();

        // and the main block which allows us to walk through commands step by step
        EditorGUILayout.LabelField("Commands: " + executor.commandsHistory.Count);
        for (int i = 0; i < executor.commandsHistory.Count; ++i)
        {
            var cmd = executor.commandsHistory[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(cmd.ToString());
            if (GUILayout.Button("Step to"))
            {
                executor.Replay(_replayName, i + 1);
            }
            EditorGUILayout.EndHorizontal();

        }
    }

    private string _replayName;
}