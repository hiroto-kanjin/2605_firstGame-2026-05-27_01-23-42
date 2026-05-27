using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class PUMagnetBehavior : PUBehavior
    {
        private PUTimer timer;
        private PUMagnetSettings powerUpMagnetSettings;

        public override void Init()
        {
            powerUpMagnetSettings = (PUMagnetSettings)settings;

            timer = null;
        }

        public override bool Activate()
        {
            IsBusy = true;

            LevelController.SetAttractionSettings(powerUpMagnetSettings.AttractionSettings);

            timer = new PUTimer(powerUpMagnetSettings.MagnetDuration, () =>
            {
                timer = null;
                IsBusy = false;

                LevelController.ResetAttractionSettings();
            });

            return true;
        }

        public override PUTimer GetTimer()
        {
            return timer;
        }

        public override void ResetBehavior()
        {
            IsBusy = false;

            if(timer != null)
            {
                timer.Disable();
                timer = null;
            }

            LevelController.ResetAttractionSettings();
        }
    }
}
