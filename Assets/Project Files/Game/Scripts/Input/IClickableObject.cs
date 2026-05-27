using UnityEngine;

namespace Watermelon
{
    public interface IClickableObject
    {
        public void OnObjectClicked();
        public bool CanBeClicked();
        public void OnClickBlocked();
    }
}
