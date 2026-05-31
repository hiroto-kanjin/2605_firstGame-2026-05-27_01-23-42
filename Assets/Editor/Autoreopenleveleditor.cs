#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Play Modeが終わったら自動でLevel Editorを開き直すスクリプト
[InitializeOnLoad]
public static class AutoReopenLevelEditor
{
    // 設定のオン・オフを保存するキー名
    private const string PREF_KEY = "AutoReopenLevelEditor_Enabled";
    // Toolsメニューの表示名
    private const string MENU_PATH = "Tools/Auto Reopen Level Editor";

    // Unity起動時・スクリプト再読み込み時に自動で実行される
    static AutoReopenLevelEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // Play Modeの状態が変わったときに呼ばれる
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Play Modeが終了した瞬間 かつ オンになっているとき
        if (state == PlayModeStateChange.EnteredEditMode && IsEnabled())
        {
            // Tools → Level Editor を自動で開く
            EditorApplication.ExecuteMenuItem("Tools/Level Editor");
        }
    }

    // オン・オフの状態を取得する（デフォルトはオン）
    private static bool IsEnabled()
    {
        return EditorPrefs.GetBool(PREF_KEY, true);
    }

    // Toolsメニューに項目を追加する
    [MenuItem(MENU_PATH)]
    private static void ToggleEnabled()
    {
        // 現在の状態を反転する
        EditorPrefs.SetBool(PREF_KEY, !IsEnabled());
    }

    // メニューにチェックマークを表示する
    [MenuItem(MENU_PATH, true)]
    private static bool ToggleEnabledValidate()
    {
        Menu.SetChecked(MENU_PATH, IsEnabled());
        return true;
    }
}
#endif