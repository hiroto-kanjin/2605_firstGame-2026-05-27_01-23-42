using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class SettingsButtonInfo
    {
        public SettingsButtonBase SettingsButton { get => settingsButton; set => settingsButton = value; }
        [SerializeField] SettingsButtonBase settingsButton;
    }
}
