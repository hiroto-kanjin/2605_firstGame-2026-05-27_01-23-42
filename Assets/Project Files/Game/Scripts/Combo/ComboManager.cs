using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class ComboManager : MonoBehaviour
    {
        public ComboDatabase Database { get; private set; }

        public int Counter { get; private set; }
        public float LastTimeMerged { get; private set; }

        private PoolGeneric<ComboText> comboTextPool;
        private Pool explosionPool;
        private Pool nextLevelInitialPool;
        private Pool nextLevelProjectilePool;
        private Pool nextLevelHitPool;

        private TweenCase textCase;
        private ComboText comboText;
        private Vector3 lastBubblePos;

        public void Init(ComboDatabase database)
        {
            Database = database;

            LevelController.LevelBehavior.OnBubbleMerged += OnBubblesMerged;

            Counter = 0;

            comboTextPool = new PoolGeneric<ComboText>(database.comboTextPrefab, $"ComboText", transform);

            explosionPool = new Pool(database.explosionSettings.explosionPrefab, $"Explosion", transform);

            nextLevelInitialPool = new Pool(database.nextLevelSettings.initialParticlePrefab, $"NextLevelInitial", transform);
            nextLevelProjectilePool = new Pool(database.nextLevelSettings.projectilePrefab, $"NextLevelProjectile", transform);
            nextLevelHitPool = new Pool(database.nextLevelSettings.hitParticlePrefab, $"NextLevelHit", transform);

            LevelController.OnTurnChanged += OnTurnChanged;
        }

        private void OnDestroy()
        {
            PoolManager.DestroyPool(comboTextPool);
            PoolManager.DestroyPool(explosionPool);
            PoolManager.DestroyPool(nextLevelInitialPool);
            PoolManager.DestroyPool(nextLevelProjectilePool);
            PoolManager.DestroyPool(nextLevelHitPool);
        }

        private void OnBubblesMerged(BubbleBehavior newBubble)
        {
            Counter++;

            if (Counter >= Database.startCounter)
            {
                if (Database.shouldHideOnNext)
                {
                    textCase.KillActive();
                    if (comboText != null)
                    {
                        var oldText = comboText;
                        comboText.DOScale(0, Database.hideDuration).SetEasing(Database.hideEasing).OnComplete(() => oldText.gameObject.SetActive(false));
                    }
                }

                comboText = comboTextPool.GetPooledComponent();

                comboText.Spawn(string.Format(Database.comboText, Counter), newBubble.transform.position + Database.comboTextOffset);

                textCase = comboText.DOScale(Database.scaleCurve.Evaluate(Counter), Database.showDuration).SetEasing(Database.showEasing).OnComplete(() => {
                    textCase = comboText.DOScale(0, Database.hideDuration, Database.comboTextDuration - Database.showDuration - Database.hideDuration).SetEasing(Database.hideEasing).OnComplete(() => 
                    {
                        comboText.gameObject.SetActive(false);
                        comboText = null;
                    });
                });
            }

            LastTimeMerged = Time.time;
            lastBubblePos = newBubble.transform.position;
        }

        private void OnTurnChanged()
        {
            Counter = 0;
            LastTimeMerged = 0;
        }

        private void Update()
        {
            if(Time.time > LastTimeMerged + Database.comboBreakDuration)
            {
                var stage = Database.GetComboStage(Counter);

                Counter = 0;

                if (stage == null || stage.type == ComboType.None) return;

                switch (stage.type)
                {
                    case ComboType.Explosion:
                        StartCoroutine(Explosion());
                        break;

                    case ComboType.NextLevel:
                        StartCoroutine(NextLevel(stage.bubblesAmount));
                        break;
                }
            }
        }

        private IEnumerator NextLevel(int amount)
        {
            var settings = Database.nextLevelSettings;

            var initialPrefab = nextLevelInitialPool.GetPooledObject();
            initialPrefab.transform.position = lastBubblePos + settings.initialParticleOffset;

            Tween.DelayedCall(settings.initialParticleDuraion, () => initialPrefab.gameObject.SetActive(false));

            yield return new WaitForSeconds(settings.projectileStartDelay);

            var bubbles = LevelController.LevelBehavior.GetBubbles();
            var projectiles = new List<ProjectileBubbleData>();

            var count = amount > bubbles.Count ? bubbles.Count : amount;

            for(int i = 0; i < count; i++)
            {
                var bubble = bubbles[i];

                if (LevelController.IsLastStage(bubble.Data))
                {
                    amount++;
                    count = amount > bubbles.Count ? bubbles.Count : amount;
                    continue;
                }

                var projectile = nextLevelProjectilePool.GetPooledObject().transform;
                projectile.position = initialPrefab.transform.position;

                projectiles.Add(new ProjectileBubbleData { 
                    bubble = bubble, 
                    projectile = projectile, 
                    startPos = projectile.position, 
                    duration = Vector3.Distance(projectile.position, bubble.transform.position) / settings.projectileSpeed 
                });
            }

            var time = 0f;

            while(projectiles.Count > 0)
            {
                yield return null;

                time += Time.deltaTime;

                for(int i = 0; i < projectiles.Count; i++)
                {
                    var data = projectiles[i];
                    var bubble = data.bubble;
                    var projectile = data.projectile;

                    if(!bubble.gameObject.activeSelf || bubble.IsMerging)
                    {
                        projectiles.RemoveAt(i);
                        i--;

                        projectile.gameObject.SetActive(false);

                        continue;
                    }

                    var t = time / data.duration;

                    if(t >= 1f)
                    {
                        projectiles.RemoveAt(i);
                        i--;

                        projectile.gameObject.SetActive(false);

                        var hit = nextLevelHitPool.GetPooledObject();
                        hit.transform.position = bubble.transform.position + settings.hitParticleOffset;
                        Tween.DelayedCall(settings.hitParticleDuraion, () => hit.SetActive(false));

                        bubble.SwapData(LevelController.IncrementData(bubble.Data), () => { });

                        continue;
                    }

                    var endPoint = bubble.transform.position.SetZ(projectile.position.z);

                    var middlePoint = new Vector3(Mathf.Max(data.startPos.x, bubble.transform.position.x), Mathf.Max(data.startPos.y, bubble.transform.position.y), projectile.position.z);

                    projectile.position = Bezier.EvaluateQuadratic(data.startPos, middlePoint, bubble.transform.position.SetZ(projectile.position.z), t);
                }
            }
        }

        private IEnumerator Explosion()
        {
            var settings = Database.explosionSettings;
            var explosion = explosionPool.GetPooledObject();

            explosion.transform.position = lastBubblePos + settings.explosionOffset;

            yield return new WaitForSeconds(settings.explosionInitialDelay);

            var bubbles = LevelController.LevelBehavior.GetBubbles();

            var list = new List<BubbleExplosionData>();

            for(int i = 0; i < bubbles.Count; i++)
            {
                var bubble = bubbles[i];

                var distance = Vector3.Distance(bubble.transform.position, lastBubblePos);

                if (distance >= settings.explosionRadius) continue;

                var t = distance / settings.explosionRadius;

                var delay = settings.explosionDistDelayCurve.Evaluate(t);
                var force = settings.explosionForce * settings.explosionDistForceCurve.Evaluate(t) * (bubble.transform.position - lastBubblePos).SetZ(0).normalized.xy();

                list.Add(new BubbleExplosionData { bubble = bubble, delay = delay, force = force });
            }

            list.Sort((first, second) => (int)((second.delay - first.delay) * 100000));

            float prevDelay = 0;

            for(int i = 0; i < list.Count; i++)
            {
                var data = list[i];

                var delay = data.delay - prevDelay;

                data.bubble.RB.AddForce(data.force, ForceMode2D.Impulse);

                prevDelay = data.delay;
            }

            yield return new WaitForSeconds(settings.explosionDuration - settings.explosionInitialDelay - prevDelay);

            explosion.gameObject.SetActive(false);
        }

        private class BubbleExplosionData
        {
            public BubbleBehavior bubble;
            public float delay;
            public Vector2 force;
        }

        private class ProjectileBubbleData
        {
            public Transform projectile;
            public BubbleBehavior bubble;
            public Vector3 startPos;

            public float duration;
        }
    }
}