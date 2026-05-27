namespace Watermelon.BubbleMerge
{
    public interface IRequirementObject
    {
        public BubbleData Data { get; set; }
        public void OnRequirementMet(RequirementBehavior requirementBehavior, RequirementCallback completeRequirement);
    }
}