using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class PUMovesBehavior : PUBehavior
    {
        private int extraTurnsAmount;

        public override void Init()
        {
            PUMovesSettings powerUpTurnsSettings = (PUMovesSettings)settings;
            extraTurnsAmount = powerUpTurnsSettings.ExtraTurns;
        }

        public override bool Activate()
        {
            LevelController.AdjustTurnsLimit(extraTurnsAmount);

            return true;
        }

        public override string GetFloatingMessage()
        {
            return string.Format(settings.FloatingMessage, extraTurnsAmount);
        }
    }
}
