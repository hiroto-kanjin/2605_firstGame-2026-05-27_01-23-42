using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Game Data", menuName = "Data/Game Data")]
    public class GameData : ScriptableObject
    {
        [SerializeField] bool infiniteLevels;
        public bool InfiniteLevels => infiniteLevels;
    }
}
