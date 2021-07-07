using Verse;

namespace Ad2mod
{
    public class Ad2Settings : ModSettings
    {
        public int defaultThreshold = 60;
        public bool limitToX5;
        public bool useRightClickMenu = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultThreshold, "defaultThreshold", 60);
            Scribe_Values.Look(ref limitToX5, "limitToX5");
            Scribe_Values.Look(ref useRightClickMenu, "useRightClickMenu", true);
            base.ExposeData();
        }
    }
}