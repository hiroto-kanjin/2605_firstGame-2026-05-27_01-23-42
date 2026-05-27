using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class TrajectoryController : MonoBehaviour
    {
        private static TrajectoryController trajectoryController;

        [Space]
        [SerializeField] GameObject trajectoryObject;
        [SerializeField] SpriteRenderer trajectorySprite;

        [SerializeField] ArrowData[] arrows;

        [Space]
        [SerializeField] float trajectoryMinDistance = 0.5f;
        [SerializeField] float trajectoryMaxDistance = 2.5f;

        [SerializeField] float appearDuration = 0.5f;

        private static bool enableTrajectory = false;
        private static bool isDragging = false;

        private static BubbleBehavior currentCharacter;

        public void Init()
        {
            trajectoryController = this;

            for (int i = 0; i < arrows.Length; i++)
            {
                arrows[i].Init();
            }
        }

        public static void BeginDrag(BubbleBehavior bubbleBehavior)
        {
            enableTrajectory = true;
            currentCharacter = bubbleBehavior;

            for (int i = 0; i < trajectoryController.arrows.Length; i++)
            {
                trajectoryController.arrows[i].ResetVibration();
            }
        }

        public static void Drag(Vector3 characterPosition, Vector3 direction, float force)
        {
            if (enableTrajectory)
            {
                trajectoryController.trajectoryObject.SetActive(true);

                enableTrajectory = false;
                isDragging = true;

                trajectoryController.trajectorySprite.color = trajectoryController.trajectorySprite.color.SetAlpha(0.0f);
                trajectoryController.trajectorySprite.DOFade(1.0f, trajectoryController.appearDuration);
            }

            Vector3 targetPosition = characterPosition + direction;
            targetPosition.z = -0.1f;
            characterPosition.z = -0.1f;

            float distance = Vector3.Distance(characterPosition, targetPosition);

            trajectoryController.trajectoryObject.transform.position = characterPosition;
            trajectoryController.trajectoryObject.transform.eulerAngles = new Vector3(trajectoryController.trajectorySprite.transform.eulerAngles.x, 0, Mathf.Atan2(targetPosition.x - characterPosition.x, characterPosition.y - targetPosition.y) * 180 / Mathf.PI);

            float trajectoryState = Mathf.InverseLerp(trajectoryController.trajectoryMinDistance, trajectoryController.trajectoryMaxDistance, force);

            for (int i = 0; i < trajectoryController.arrows.Length; i++)
            {
                trajectoryController.arrows[i].UpdateState(trajectoryState);
            }
        }

        public static void EndDrag()
        {
            isDragging = false;

            trajectoryController.trajectoryObject.SetActive(false);

            currentCharacter = null;
        }

        public static void OnBubblePoped(BubbleBehavior bubbleBehavior)
        {
            if (isDragging && bubbleBehavior == currentCharacter)
            {
                EndDrag();
            }
        }

        #region Tutorial
        public static void BeginTutorialDrag(Transform transform, Vector3 direction)
        {
            if (isDragging)
                return;

            trajectoryController.trajectoryObject.SetActive(true);

            Vector3 targetPosition = transform.position + direction;
            targetPosition.z = -0.1f;

            trajectoryController.trajectoryObject.transform.position = transform.position;
            trajectoryController.trajectoryObject.transform.eulerAngles = new Vector3(trajectoryController.trajectorySprite.transform.eulerAngles.x, 0, Mathf.Atan2(targetPosition.x - transform.position.x, transform.position.y - targetPosition.y) * 180 / Mathf.PI);

            trajectoryController.trajectorySprite.color = trajectoryController.trajectorySprite.color.SetAlpha(0.0f);
            trajectoryController.trajectorySprite.DOFade(1.0f, trajectoryController.appearDuration);

            for (int i = 0; i < trajectoryController.arrows.Length; i++)
            {
                trajectoryController.arrows[i].UpdateTutorialState(0);
            }
        }

        public static void TutorialDrag(float state)
        {
            if (isDragging)
                return;

            for (int i = 0; i < trajectoryController.arrows.Length; i++)
            {
                trajectoryController.arrows[i].UpdateTutorialState(state);
            }
        }

        public static void EndTutorialDrag()
        {
            if (isDragging)
                return;

            trajectoryController.trajectoryObject.SetActive(false);
        }
        #endregion

        
    }
}