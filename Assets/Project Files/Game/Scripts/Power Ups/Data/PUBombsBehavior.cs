using System.Collections.Generic;
using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class PUBombsBehavior : PUBehavior
    {
        private PUBombsSettings bombsSettings;

        private TweenCase[] bombDelaysCases;
        private TweenCase disableTweenCase;

        public override void Init()
        {
            bombsSettings = (PUBombsSettings)settings;
        }

        public override bool Activate()
        {
            IsBusy = true;

            bombDelaysCases = new TweenCase[bombsSettings.BombsAmount];
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < bombDelaysCases.Length; i++)
            {
                int bombIndex = i;

                bombDelaysCases[bombIndex] = Tween.DelayedCall(0.2f * i, () =>
                {
                    BombBehavior bombBehavior = LevelController.LevelBehavior.SpawnPUBomb(usedPositions);
                    usedPositions.Add(bombBehavior.transform.position);

                    bombDelaysCases[bombIndex] = Tween.DelayedCall(0.15f, () =>
                    {
                        bombBehavior.ForceExplode(0.8f);
                    });
                });
            }

            disableTweenCase = Tween.DelayedCall(2.0f, () =>
            {
                IsBusy = false;
            });

            return true;
        }

        public override void ResetBehavior()
        {
            IsBusy = false;

            disableTweenCase.KillActive();

            if (!bombDelaysCases.IsNullOrEmpty())
            {
                for (int i = 0; i < bombDelaysCases.Length; i++)
                {
                    bombDelaysCases[i].KillActive();
                }
            }
        }
    }
}
