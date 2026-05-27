#pragma warning disable 0414

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Watermelon
{
    public static class LevelAutoRun
    {
        private static readonly string AUTO_RUN_LEVEL_SAVE_NAME = "autoRunLevel";
        private static readonly string AUTO_RUN_LEVEL_INDEX_SAVE_NAME = "autoRunLevelIndex";

#if UNITY_EDITOR
        private static bool AutoRunLevelInEditor
        {
            get { return EditorPrefs.GetBool(AUTO_RUN_LEVEL_SAVE_NAME, false); }
            set { EditorPrefs.SetBool(AUTO_RUN_LEVEL_SAVE_NAME, value); }
        }

        private static int AutoRunLevelIndex
        {
            get { return EditorPrefs.GetInt(AUTO_RUN_LEVEL_INDEX_SAVE_NAME, 0); }
            set { EditorPrefs.SetInt(AUTO_RUN_LEVEL_INDEX_SAVE_NAME, value); }
        }
#endif

        public static int GetLevelIndex()
        {
#if UNITY_EDITOR
            return AutoRunLevelIndex;
#else
            return 0;
#endif
        }

        public static bool CheckIfNeedToAutoRunLevel()
        {
#if UNITY_EDITOR
            if (AutoRunLevelInEditor)
            {
                AutoRunLevelInEditor = false;

                return true;
            }

            AutoRunLevelInEditor = false;
#endif

            return false;
        }

        public static void EnableAutoRun(int levelIndex)
        {
#if UNITY_EDITOR
            AutoRunLevelIndex = levelIndex;
            AutoRunLevelInEditor = true;
#endif
        }
    }
}
