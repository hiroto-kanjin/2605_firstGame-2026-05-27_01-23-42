using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [RequireComponent(typeof(TMP_Text))]
    public class ComboText : MonoBehaviour
    {
        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        public void Spawn(string text, Vector3 position)
        {
            transform.position = position;
            this.text.text = text;
        }
    }
}