using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：エディタ上でレイアウトプレビューできるUIの共通基底クラス。
    // これを継承したMonoBehaviourには、共通エディタが「プレビュー生成／クリア」ボタンを自動で出す。
    public abstract class PreviewableBehaviour : MonoBehaviour
    {
        // プレビュー（ダミー）を生成する。各UIが中身を実装する。
        public abstract void BuildPreview();

        // プレビュー（ダミー）を消す。各UIが中身を実装する。
        public abstract void ClearPreview();
    }
}