using Verse;

namespace Ad2mod;

public class Ad2Settings : ModSettings
{
    //public int defaultThreshold = 60;
    public bool LimitToX5;
    public bool MakeBulkForQuality = true;
    public int MaxTimeCutoff = 120;
    public bool UseRightClickMenu = true;
    public bool UseSameQualityForAll;
    public bool VerboseLogging;

    public override void ExposeData()
    {
        //Scribe_Values.Look(ref defaultThreshold, "defaultThreshold", 60);
        Scribe_Values.Look(ref MaxTimeCutoff, "maxtimecutoff", 120);
        Scribe_Values.Look(ref LimitToX5, "limitToX5");
        Scribe_Values.Look(ref VerboseLogging, "verboseLogging");
        Scribe_Values.Look(ref UseRightClickMenu, "useRightClickMenu", true);
        Scribe_Values.Look(ref MakeBulkForQuality, "makeBulkForQuality", true);
        Scribe_Values.Look(ref UseSameQualityForAll, "useSameQualityForAll");
        base.ExposeData();
    }
}