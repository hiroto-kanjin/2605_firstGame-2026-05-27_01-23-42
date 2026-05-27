using UnityEngine.EventSystems;

namespace Watermelon
{
    public class SettingsExitButton : SettingsButtonBase
    {
        public override void Init()
        {

        }

        public override void OnClick()
        {
            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.ShowExitPopUp();

            // Play button sound
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        public override void Select()
        {
            IsSelected = true;

            Button.Select();

            EventSystem.current.SetSelectedGameObject(null); //clear any previous selection (best practice)
            EventSystem.current.SetSelectedGameObject(Button.gameObject, new BaseEventData(EventSystem.current));
        }

        public override void Deselect()
        {
            IsSelected = false;

            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}