using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;

        [BoxGroup("Gameplay", "Gameplay")]
        public AudioClip loseSound;
        [BoxGroup("Gameplay")]
        public AudioClip winSound;
        [BoxGroup("Gameplay")]
        public AudioClip requirementMetSound;
        [BoxGroup("Gameplay")]
        public AudioClip bouncePlatformSound;
        [BoxGroup("Gameplay")]
        public AudioClip powerupSound;

        [BoxGroup("Bubble", "Bubble")]
        public AudioClip launchBubbleSound;
        [BoxGroup("Bubble")]
        public AudioClip bubbleHitSound;
        [BoxGroup("Bubble")]
        public AudioClip bubbleMergeSound;
        [BoxGroup("Bubble")]
        public AudioClip bubblePopSound;

        [BoxGroup("Effects", "Effects")]
        public AudioClip iceCrackSound;
        [BoxGroup("Effects")]
        public AudioClip crateCrackSound;
        [BoxGroup("Effects")]
        public AudioClip freezingSound;

        [BoxGroup("Cage", "Cage")]
        public AudioClip cageEatSound;
        [BoxGroup("Cage")]
        public AudioClip cageHitSound;
        [BoxGroup("Cage")]
        public AudioClip cageVomitSound;

        [BoxGroup("Bomb", "Bomb")]
        public AudioClip bombExplosionSound;
        [BoxGroup("Bomb")]
        public AudioClip bombWickSound;
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------