// RecipeDataCSV.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Watermelon.BubbleMerge
{
    // hk追加：②RecipeData専用のCSV読み込み担当。①のBallDataCSVには触らない
    public static class RecipeDataCSV
    {
        private const int BlockRows = 20;   // 料理1つ＝20行のブロック
        private const int HeaderRows = 3;   // 各ブロックの上3行は見出し（飛ばす）

        // 列の位置（0始まり）。C・E列は飾りなので使わない
        private const int ColRecipeName = 0;    // A：料理名（次の行の同じ列＝ローマ字）
        private const int ColRecipeId = 1;      // B：ID
        private const int ColEvoNumber = 3;     // D：進化ボールの番号
        private const int ColNormalCount = 5;   // F：通常レシピの個数
        private const int ColSpecialNumber = 6; // G：特殊ボールの番号
        private const int ColSpecialCount = 7;  // H：特殊ボールの個数
        private const int ColSecretFlag = 8;    // I：裏メニューあり/無し
        private const int ColSecretCookingName = 9; // J：裏メニューの料理名
        private const int ColSecretNuisance = 10;   // K：お邪魔個数
        private const int ColSecretIrregular = 11;  // L：イレギュラー個数
        private const int ColRank = 12;         // M：完成データのランク名
        private const int ColCookingName = 13;  // N：料理名
        private const int ColScore = 14;        // O：点数下限
        private const int ColCategoryId = 15;   // P：hk追加：カテゴリID
        private const int ColCategoryName = 16; // Q：hk追加：カテゴリ名（フォルダ名に使う）

        // hk追加：カテゴリ画像のフォルダと固定ファイル名
        private const string CATEGORY_ROOT = "Assets/Project Files/Game/Images/Category";
        private const string DISH_IMAGE_FILE = "Dish_Images.png";

        // hk追加：ランク画像・ランクプレハブ（完成データ・裏メニュー）のフォルダ
        private const string RANK_ROOT = "Assets/Project Files/Game/Images/Results";
        private const string RANK_COMMON_FOLDER = "common";
        private const string ANOMALY_FILE_NAME = "anomaly";

        // ランク名の正しい表記。CSVが大文字でも小文字でも、これに合わせて入れ直す
        private static readonly string[] RANK_NAMES = { "Masterpiece", "Delicious", "Good", "Mediocre", "Bad" };

        // CSVの文字を読んで、料理のリストを組み立てて返す
        public static List<RecipeEntry> Parse(string csvText)
        {
            List<RecipeEntry> result = new List<RecipeEntry>();
            if (string.IsNullOrEmpty(csvText)) return result;

            // 1行ずつに分ける（改行コードの違いを吸収）
            string[] lines = csvText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            // 20行ずつのブロックに切って、行がなくなるまで繰り返す
            for (int blockStart = 0; blockStart < lines.Length; blockStart += BlockRows)
            {
                RecipeEntry entry = ParseBlock(lines, blockStart);
                if (entry != null)
                    result.Add(entry);
            }

            return result;
        }

        // ブロック1つ（料理1つ）を読む。中身が無ければnullを返す
        private static RecipeEntry ParseBlock(string[] lines, int blockStart)
        {
            int firstDataRow = blockStart + HeaderRows; // 見出し3行を飛ばした先頭データ行
            if (firstDataRow >= lines.Length) return null;

            string[] head = SplitLine(lines[firstDataRow]);

            // A列（料理名）が空のブロックは料理なしとして飛ばす
            string recipeName = GetCell(head, ColRecipeName);
            if (string.IsNullOrEmpty(recipeName)) return null;

            RecipeEntry entry = new RecipeEntry();
            entry.recipeName = recipeName;
            entry.recipeId = ParseInt(GetCell(head, ColRecipeId), 0);

            // hk追加：ローマ字は、料理名の1行下・同じA列に書く運用
            if (firstDataRow + 1 < lines.Length)
            {
                string[] nextRow = SplitLine(lines[firstDataRow + 1]);
                entry.recipeNameRomaji = GetCell(nextRow, ColRecipeName);
            }

            // 裏メニュー（先頭行だけ見る）
            entry.hasSecret = ParseBool(GetCell(head, ColSecretFlag));
            entry.secretCookingName = GetCell(head, ColSecretCookingName);
            entry.secretNuisanceCount = ParseInt(GetCell(head, ColSecretNuisance), 0);
            entry.secretIrregularCount = ParseInt(GetCell(head, ColSecretIrregular), 0);

            // hk追加：カテゴリ（先頭行だけ見る）
            entry.categoryId = ParseInt(GetCell(head, ColCategoryId), 0);
            string categoryName = GetCell(head, ColCategoryName);
            entry.dishSprite = LoadCategorySprite(entry.categoryId, categoryName);

            entry.evolutionChain = new List<int>();
            entry.requiredList = new List<RequiredItem>();
            entry.specialList = new List<RequiredItem>();
            entry.completionStages = new List<CompletionStage>();

            bool evolutionOpen = true; // 進化の枠がまだ続いているか

            // ブロック内のデータ行を上から順に見る（見出しの次から、ブロック終わりまで）
            for (int i = firstDataRow; i < blockStart + BlockRows && i < lines.Length; i++)
            {
                string[] cells = SplitLine(lines[i]);

                // 進化の枠：D列に番号があるあいだ拾う。空が来たら以降打ち切り
                if (evolutionOpen)
                {
                    string evoStr = GetCell(cells, ColEvoNumber);
                    if (string.IsNullOrEmpty(evoStr))
                    {
                        evolutionOpen = false; // ここから下の進化は無視
                    }
                    else
                    {
                        int evoNumber = ParseInt(evoStr, -1);
                        if (evoNumber >= 0)
                        {
                            entry.evolutionChain.Add(evoNumber);

                            // 通常レシピ：同じ行のF列（個数）が1以上なら追加。0は使わない
                            int count = ParseInt(GetCell(cells, ColNormalCount), 0);
                            if (count > 0)
                            {
                                RequiredItem item = new RequiredItem();
                                item.number = evoNumber;
                                item.count = count;
                                entry.requiredList.Add(item);
                            }
                        }
                    }
                }

                // 特殊ボール：G列に番号がある行だけ拾う（進化とは独立）
                string spStr = GetCell(cells, ColSpecialNumber);
                if (!string.IsNullOrEmpty(spStr))
                {
                    int spNumber = ParseInt(spStr, -1);
                    int spCount = ParseInt(GetCell(cells, ColSpecialCount), 0);
                    if (spNumber >= 0 && spCount > 0)
                    {
                        RequiredItem item = new RequiredItem();
                        item.number = spNumber;
                        item.count = spCount;
                        entry.specialList.Add(item);
                    }
                }

                // 完成データ：M列にランク名がある行だけ拾う（進化とは独立）
                string rank = GetCell(cells, ColRank);
                if (!string.IsNullOrEmpty(rank))
                {
                    CompletionStage stage = new CompletionStage();
                    stage.rankName = NormalizeRank(rank);
                    stage.cookingName = GetCell(cells, ColCookingName);
                    stage.minScore = ParseInt(GetCell(cells, ColScore), 0);
                    stage.prefab = null;
                    entry.completionStages.Add(stage);
                }
            }

            // hk追加：ランク画像・ランクプレハブ・裏メニュー分を、Resultsフォルダから読み込む
            LoadRankImages(entry);

            return entry;
        }

        // hk追加：categoryIdから、Categoryフォルダ内の該当フォルダを用意し、Dish_Images.pngを読み込む
        private static Sprite LoadCategorySprite(int categoryId, string categoryName)
        {
#if UNITY_EDITOR
            string idText = categoryId.ToString("0000");

            // ID基準で、既存フォルダ（0000始まり）を探す
            string actualFolderName = FindFolderByIdPrefix(CATEGORY_ROOT, idText);

            // 無ければ作る（ID重複は作らない）
            if (string.IsNullOrEmpty(actualFolderName))
            {
                string wantName = string.IsNullOrEmpty(categoryName) ? idText : idText + "_" + categoryName;
                if (!AssetDatabase.IsValidFolder(CATEGORY_ROOT))
                {
                    Debug.LogWarning("Categoryの親フォルダが見つかりません： " + CATEGORY_ROOT);
                    return null;
                }
                AssetDatabase.CreateFolder(CATEGORY_ROOT, wantName);
                AssetDatabase.Refresh();
                actualFolderName = wantName;
            }

            string imagePath = CATEGORY_ROOT + "/" + actualFolderName + "/" + DISH_IMAGE_FILE;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (sprite == null)
                Debug.LogWarning("カテゴリ画像が見つかりません（dishSprite空）： " + imagePath);
            return sprite;
#else
            return null;
#endif
        }

        // hk追加：レシピIDのフォルダを用意し、ランクごとの画像・プレハブ・裏メニュー分を読み込んでentryに詰める
        private static void LoadRankImages(RecipeEntry entry)
        {
#if UNITY_EDITOR
            string folderName = EnsureRecipeFolder(entry.recipeId, entry.recipeNameRomaji);

            foreach (CompletionStage stage in entry.completionStages)
            {
                string rankNameLower = stage.rankName.ToLower();
                stage.rankSprite = LoadRankSprite(folderName, rankNameLower);
                stage.prefab = LoadRankPrefab(folderName, rankNameLower);
            }

            if (entry.hasSecret)
            {
                entry.anomalySprite = LoadRankSprite(folderName, ANOMALY_FILE_NAME);
                entry.secretPrefab = LoadRankPrefab(folderName, ANOMALY_FILE_NAME);
            }
#endif
        }

#if UNITY_EDITOR
        // hk追加：レシピID用のフォルダを探す。無ければ作る。名前がズレていればリネームする
        private static string EnsureRecipeFolder(int recipeId, string romaji)
        {
            if (!AssetDatabase.IsValidFolder(RANK_ROOT))
            {
                Debug.LogWarning("Resultsの親フォルダが見つかりません： " + RANK_ROOT);
                return "";
            }

            string idText = recipeId.ToString("0000");
            string existingFolderName = FindFolderByIdPrefix(RANK_ROOT, idText);
            string wantName = string.IsNullOrEmpty(romaji) ? idText : idText + "_" + romaji;

            if (string.IsNullOrEmpty(existingFolderName))
            {
                AssetDatabase.CreateFolder(RANK_ROOT, wantName);
                AssetDatabase.Refresh();
                return wantName;
            }

            if (existingFolderName != wantName)
            {
                string error = AssetDatabase.RenameAsset(RANK_ROOT + "/" + existingFolderName, wantName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning("レシピフォルダのリネームに失敗： " + error);
                    return existingFolderName;
                }
                AssetDatabase.Refresh();
                return wantName;
            }

            return existingFolderName;
        }

        // hk追加：フォルダの中から「番号_ランク名」形式のSpriteを、ランク名だけで探す。無ければcommonフォルダを見る
        private static Sprite LoadRankSprite(string folderName, string rankNameLower)
        {
            if (string.IsNullOrEmpty(folderName)) return null;

            Sprite sprite = FindAssetByRankName<Sprite>(RANK_ROOT + "/" + folderName, "t:Sprite", rankNameLower);
            if (sprite == null)
                sprite = FindAssetByRankName<Sprite>(RANK_ROOT + "/" + RANK_COMMON_FOLDER, "t:Sprite", rankNameLower);

            if (sprite == null)
                Debug.LogWarning("ランク画像が見つかりません： " + rankNameLower + "（" + folderName + " / common どちらにも無し）");

            return sprite;
        }

        // hk追加：フォルダの中から「番号_ランク名」形式のPrefabを、ランク名だけで探す。無ければcommonフォルダを見る
        private static GameObject LoadRankPrefab(string folderName, string rankNameLower)
        {
            if (string.IsNullOrEmpty(folderName)) return null;

            GameObject prefab = FindAssetByRankName<GameObject>(RANK_ROOT + "/" + folderName, "t:Prefab", rankNameLower);
            if (prefab == null)
                prefab = FindAssetByRankName<GameObject>(RANK_ROOT + "/" + RANK_COMMON_FOLDER, "t:Prefab", rankNameLower);

            if (prefab == null)
                Debug.LogWarning("ランクプレハブが見つかりません： " + rankNameLower + "（" + folderName + " / common どちらにも無し）");

            return prefab;
        }

        // hk追加：指定フォルダの中を検索し、ファイル名に「ランク名」が含まれる資産を返す（番号の飾りは無視）
        private static T FindAssetByRankName<T>(string folderPath, string typeFilter, string rankNameLower) where T : Object
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) return null;

            string[] guids = AssetDatabase.FindAssets(typeFilter, new[] { folderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
                if (fileName.Contains(rankNameLower))
                    return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            return null;
        }

        // hk追加：親フォルダの中で、指定ID（4桁）で始まるフォルダ名を返す。無ければ空文字
        private static string FindFolderByIdPrefix(string parentFolder, string idText)
        {
            if (!AssetDatabase.IsValidFolder(parentFolder)) return "";

            string[] subFolders = AssetDatabase.GetSubFolders(parentFolder);
            foreach (string sub in subFolders)
            {
                string folderName = Path.GetFileName(sub);
                if (folderName.StartsWith(idText))
                    return folderName;
            }
            return "";
        }
#endif

        private static string NormalizeRank(string raw)
        {
            string trimmed = raw.Trim();
            foreach (string name in RANK_NAMES)
            {
                if (string.Equals(trimmed, name, System.StringComparison.OrdinalIgnoreCase))
                    return name;
            }
            return trimmed;
        }

        private static string[] SplitLine(string line)
        {
            if (line == null) return new string[0];
            string[] raw = line.Split(',');
            for (int i = 0; i < raw.Length; i++)
                raw[i] = raw[i].Trim().Trim('"');
            return raw;
        }

        private static string GetCell(string[] cells, int index)
        {
            if (cells == null || index < 0 || index >= cells.Length) return "";
            return cells[index];
        }

        private static int ParseInt(string s, int fallback)
        {
            if (int.TryParse(s, out int v)) return v;
            return fallback;
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim().ToLower();
            return s == "true" || s == "1";
        }
    }
}