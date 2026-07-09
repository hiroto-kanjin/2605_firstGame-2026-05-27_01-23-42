using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：ギミック（アイテム/エフェクト）のフォルダを読み、プレハブを番号順に集める共通ローダー
    // 使い回し前提：親フォルダのパスと、拾うプレハブ名を外から渡す
    public static class GimmickFolderLoader
    {
        // 1つのフォルダから拾った結果（番号・名前・プレハブ）
        public class LoadedPrefab
        {
            public int number;        // フォルダ番号（0000→0）
            public string folderName; // 番号の後ろの名前（box など）
            public GameObject prefab; // 拾ったプレハブ
        }

        // parentPath：親フォルダ（例：Assets/Project Files/Game/Images/Gimmick/Items）
        // prefabName：各フォルダから拾うプレハブ名（例：GimmickItem）
        public static List<LoadedPrefab> Load(string parentPath, string prefabName)
        {
            List<LoadedPrefab> results = new List<LoadedPrefab>();

            if (!Directory.Exists(parentPath))
            {
                Debug.LogWarning("GimmickFolderLoader: フォルダが見つかりません → " + parentPath);
                return results;
            }

            // 番号フォルダを集めて、番号順に並べる
            string[] dirs = Directory.GetDirectories(parentPath);
            List<string> sortedDirs = new List<string>(dirs);
            sortedDirs.Sort(); // 0000, 0001, 0002... の順に並ぶ

            foreach (string dir in sortedDirs)
            {
                string folder = Path.GetFileName(dir); // 例：0000_box

                // 先頭4桁を番号として取り出す
                int underscore = folder.IndexOf('_');
                string numberText = underscore >= 0 ? folder.Substring(0, underscore) : folder;
                string namePart = underscore >= 0 ? folder.Substring(underscore + 1) : "";

                if (!int.TryParse(numberText, out int number))
                    continue; // 番号で始まらないフォルダは飛ばす

                // フォルダ内の、決まった名前のプレハブを拾う
                string prefabPath = dir + "/" + prefabName + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null)
                {
                    Debug.LogWarning("GimmickFolderLoader: プレハブが見つかりません → " + prefabPath);
                    continue;
                }

                results.Add(new LoadedPrefab
                {
                    number = number,
                    folderName = namePart,
                    prefab = prefab
                });
            }

            return results;
        }
    }
}