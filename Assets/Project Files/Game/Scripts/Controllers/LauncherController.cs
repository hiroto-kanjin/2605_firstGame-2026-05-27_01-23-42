using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class LauncherController : MonoBehaviour // hk追加
    {
        [SerializeField] private float leftLimit = -1.1f;
        [SerializeField] private float rightLimit = 1.1f;
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            float input = 0f;

            if (Watermelon.InputController.LauncherMoveLeftAction.IsPressed())
                input = -1f;

            if (Watermelon.InputController.LauncherMoveRightAction.IsPressed())
                input = 1f;

            if (input == 0f) return;

            Vector3 pos = transform.position;
            pos.x += input * moveSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
            transform.position = pos;
        }
    }
}