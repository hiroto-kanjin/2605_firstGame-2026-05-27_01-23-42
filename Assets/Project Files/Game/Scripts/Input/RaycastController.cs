using UnityEngine;

namespace Watermelon
{
    [StaticUnload]
    public class RaycastController : MonoBehaviour
    {
        private static bool isActive;

        public static event SimpleCallback OnInputActivated;
        public static event SimpleCallback OnObjectTouched;

        public void Init()
        {
            isActive = true;
        }

        private void Update()
        {
            if (!isActive || UIController.IsPopupOpened) return;

            if (InputController.ClickAction.WasPressedThisFrame())
            {
                Ray ray = Camera.main.ScreenPointToRay(InputController.MousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    IClickableObject clickableObject = hit.transform.GetComponent<IClickableObject>();
                    if (clickableObject != null)
                    {
                        OnObjectTouched?.Invoke();

                        PUBehavior selectedPU = PUController.SelectedPU;
                        if(selectedPU != null)
                        {
                            Debug.Log(selectedPU.GetType().Name + " clicked on " + clickableObject.GetType().Name);
                            //PUController.ApplyToElement(clickableObject, hit.point);
                        }
                        else
                        {
                            if (clickableObject.CanBeClicked())
                            {
                                clickableObject.OnObjectClicked();
                            }
                            else
                            {
                                clickableObject.OnClickBlocked();
                            }
                        }
                    }
                }
            }
            else if(InputController.ClickAction.WasReleasedThisFrame())
            {
                Debug.Log("OnObjectReleased called");
                //LevelController.OnObjectReleased();
            }
        }

        public static void Disable()
        {
            isActive = false;
        }

        public static void Enable()
        {
            isActive = true;

            OnInputActivated?.Invoke();
        }

        private static void UnloadStatic()
        {
            isActive = false;

            OnInputActivated = null;
            OnObjectTouched = null;
        }
    }
}
