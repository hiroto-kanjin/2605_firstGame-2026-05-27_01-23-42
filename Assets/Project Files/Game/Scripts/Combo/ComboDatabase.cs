using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Combo Database", menuName = "Data/Combo Database")]
    public class ComboDatabase : ScriptableObject
    {
        public float comboBreakDuration;

        public List<ComboStage> stages;

        [Header("Combo Text")]
        public GameObject comboTextPrefab;
        [Space]
        public int startCounter;
        public Vector3 comboTextOffset;
        public string comboText;

        [Space]
        public float showDuration;
        public Ease.Type showEasing;

        [Space]
        public float hideDuration;
        public Ease.Type hideEasing;

        [Space]
        public float comboTextDuration;
        public bool shouldHideOnNext;

        [Space]
        public AnimationCurve scaleCurve;

        public ExplosionSettings explosionSettings;
        public JokerSettings jokerSettings;
        public NextLevelSettings nextLevelSettings;

        public ComboStage GetComboStage(int counter)
        {
            for(int i = 0; i < stages.Count; i++)
            {
                if (stages[i].counter == counter) return stages[i];
            }

            return stages[^1];
        }

        [System.Serializable]
        public struct JokerSettings
        {
            public GameObject initialParticlePrefab;
            public Vector3 initialParticleOffset;

            [Space]
            public GameObject projectilePrefab;
            public float projectileSpeed;
            public float projectileStartDelay;

            [Space]
            public GameObject hitParticlePrefab;
        }

        [System.Serializable]
        public struct ExplosionSettings
        {
            public GameObject explosionPrefab;
            public Vector3 explosionOffset;

            [Space]
            public float explosionForce;
            public float explosionRadius;
            public float explosionInitialDelay;
            public float explosionDuration;
            public AnimationCurve explosionDistForceCurve;
            public AnimationCurve explosionDistDelayCurve;

        }

        [System.Serializable]
        public struct NextLevelSettings
        {
            public GameObject initialParticlePrefab;
            public Vector3 initialParticleOffset;
            public float initialParticleDuraion;

            [Space]
            public GameObject projectilePrefab;
            public float projectileSpeed;
            public float projectileStartDelay;

            [Space]
            public GameObject hitParticlePrefab;
            public Vector3 hitParticleOffset;
            public float hitParticleDuraion;
        }
    }

    [System.Serializable]
    public enum ComboType
    {
        None,
        Explosion,
        Joker,
        NextLevel,
    }
}