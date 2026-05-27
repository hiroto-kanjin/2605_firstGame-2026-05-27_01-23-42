namespace Watermelon.BubbleMerge
{
    public class CageSpecialEffect : BubbleSpecialEffect
    {
        private CageBehavior cageBehavior;
        public CageBehavior CageBehavior => cageBehavior;

        public void LinkCageBehavior(CageBehavior cageBehavior)
        {
            this.cageBehavior = cageBehavior;
        }

        public override void OnBubbleCollided(BubbleBehavior bubbleBehavior)
        {

        }

        public override void OnBubbleMerged()
        {

        }

        public override void OnBubbleDisabled()
        {
            Destroy(gameObject);
        }

        public override void OnCreated()
        {

        }

        public override bool IsMergeAllowed()
        {
            return false;
        }

        public override bool IsDragAllowed()
        {
            return false;
        }

        public override bool IsBubbleActive()
        {
            return false;
        }
    }
}