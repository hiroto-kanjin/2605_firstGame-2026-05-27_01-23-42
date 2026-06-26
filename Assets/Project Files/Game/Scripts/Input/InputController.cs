using UnityEngine;
using UnityEngine.InputSystem;

namespace Watermelon
{
    [DefaultExecutionOrder(-1)]
    public class InputController : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;

        private InputAction mousePositionAction;

        public static InputActionAsset InputActionsAsset { get; private set; }

        public static Vector2 MousePosition { get; private set; }
        public static InputAction ClickAction { get; private set; }
        public static InputAction LauncherMoveLeftAction { get; private set; } // hk追加
        public static InputAction LauncherMoveRightAction { get; private set; } // hk追加

        private void Awake()
        {
            inputActions.FindActionMap("Player").Enable();
            InputActionsAsset = inputActions;

            mousePositionAction = inputActions.FindAction("Point");
            mousePositionAction.Enable();

            ClickAction = inputActions.FindAction("Click");
            ClickAction.Enable();

            LauncherMoveLeftAction = inputActions.FindAction("LauncherMoveLeft"); // hk追加
            LauncherMoveLeftAction.Enable(); // hk追加

            LauncherMoveRightAction = inputActions.FindAction("LauncherMoveRight"); // hk追加
            LauncherMoveRightAction.Enable(); // hk追加

            MousePosition = mousePositionAction.ReadValue<Vector2>();
        }

        private void Update()
        {
            MousePosition = mousePositionAction.ReadValue<Vector2>();
        }
    }
}
