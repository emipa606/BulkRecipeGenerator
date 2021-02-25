using RimWorld.Planet;

namespace Ad2mod
{
    internal class Ad2WorldComp : WorldComponent
    {
        //public int threshold;

        public Ad2WorldComp(World world) : base(world)
        {
            //threshold = Ad2Mod.settings.defaultThreshold;
            //Log.Message("WorldComp.ctr():  " + world.info.name + "  " + world.info.seedString);
        }
    }
}