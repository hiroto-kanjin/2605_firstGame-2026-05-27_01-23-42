using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：②RecipeData専用のCSV読み込み担当。①のBallDataCSVには触らない
    public static class RecipeDataCSV
    {
        private const int BlockRows = 20;   // 料理1つ＝20行のブロック
        private const int HeaderRows = 3;   // 各ブロックの上3行は見出し（飛ばす）

        // 列の位置（0始まり）。C・E列は飾りなので使わない
        private const int ColRecipeName = 0;    // A：料理名
        private const int ColRecipeId = 1;      // B：ID
        private const int ColEvoNumber = 3;     // D：進化ボールの番号
        private const int ColNormalCount = 5;   // F：通常レシピの個数
        private const int ColSpecialNumber = 6; // G：特殊ボールの番号
        private const int ColSpecialCount = 7;  // H：特殊ボールの個数
        private const int ColSecretFlag = 8;    // I：裏メニューあり/無し
        private const int ColSecretCookingName = 9; // J：hk追加：裏メニューの料理名
        private const int ColSecretNuisance = 10;   // K：お邪魔個数（右へ移動）
        private const int ColSecretIrregular = 11;  // L：イレギュラー個数（右へ移動）
        private const int ColRank = 12;         // M：完成データのランク名（右へ移動）
        private const int ColCookingName = 13;  // N：料理名（右へ移動）
        private const int ColScore = 14;        // O：点数下限（右へ移動）

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

            // 裏メニュー（先頭行だけ見る）
            entry.hasSecret = ParseBool(GetCell(head, ColSecretFlag));
            entry.secretCookingName = GetCell(head, ColSecretCookingName); // hk追加：裏メニューの料理名
            entry.secretNuisanceCount = ParseInt(GetCell(head, ColSecretNuisance), 0);
            entry.secretIrregularCount = ParseInt(GetCell(head, ColSecretIrregular), 0);

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

                // 完成データ：L列にランク名がある行だけ拾う（進化とは独立）
                string rank = GetCell(cells, ColRank);
                if (!string.IsNullOrEmpty(rank))
                {
                    CompletionStage stage = new CompletionStage();
                    stage.rankName = NormalizeRank(rank);              // ランク名を正しい表記に直す
                    stage.cookingName = GetCell(cells, ColCookingName); // hk追加：料理名を読む
                    stage.minScore = ParseInt(GetCell(cells, ColScore), 0);
                    stage.prefab = null; // プレハブはCSVでは扱わない
                    entry.completionStages.Add(stage);
                }
            }

            return entry;
        }

        // hk追加：CSVのランク名を、選択肢の正しい表記に合わせる。
        // 大文字小文字を無視して照合するので、masterpieceでもMASTERPIECEでも通る
        private static string NormalizeRank(string raw)
        {
            string trimmed = raw.Trim();
            foreach (string name in RANK_NAMES)
            {
                if (string.Equals(trimmed, name, System.StringComparison.OrdinalIgnoreCase))
                    return name; // 正しい表記に置き換える
            }
            return trimmed; // どれにも当てはまらなければ、そのまま入れる
        }

        // 1行をカンマで区切る（前後の空白と引用符を落とす）
        private static string[] SplitLine(string line)
        {
            if (line == null) return new string[0];
            string[] raw = line.Split(',');
            for (int i = 0; i < raw.Length; i++)
                raw[i] = raw[i].Trim().Trim('"');
            return raw;
        }

        // 指定の列を安全に取り出す（列が足りなければ空文字）
        private static string GetCell(string[] cells, int index)
        {
            if (cells == null || index < 0 || index >= cells.Length) return "";
            return cells[index];
        }

        // 文字を数字に。読めなければ既定値
        private static int ParseInt(string s, int fallback)
        {
            if (int.TryParse(s, out int v)) return v;
            return fallback;
        }

        // TRUE/true/1 を真とみなす
        private static bool ParseBool(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim().ToLower();
            return s == "true" || s == "1";
        }
    }
}