using Verse;

namespace Ad2mod;

public class Ad2Settings : ModSettings
{
    //public int defaultThreshold = 60;
    public bool limitToX5;
    public bool makeBulkForQuality = true;
    public int maxtimecutoff = 120;
    public bool useRightClickMenu = true;
    public bool useSameQualityForAll;
    public bool verboseLogging;

    public override void ExposeData()
    {
        //Scribe_Values.Look(ref defaultThreshold, "defaultThreshold", 60);
        Scribe_Values.Look(ref maxtimecutoff, "maxtimecutoff", 120);
        Scribe_Values.Look(ref limitToX5, "limitToX5");
        Scribe_Values.Look(ref verboseLogging, "verboseLogging");
        Scribe_Values.Look(ref useRightClickMenu, "useRightClickMenu", true);
        Scribe_Values.Look(ref makeBulkForQuality, "makeBulkForQuality", true);
        Scribe_Values.Look(ref useSameQualityForAll, "useSameQualityForAll");
        base.ExposeData();
    }
}