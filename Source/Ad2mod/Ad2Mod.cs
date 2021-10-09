using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod
{
    internal class Ad2Mod : Mod
    {
        public static Ad2Settings settings;
        private readonly NumField defaultThresholdField = new NumField();

        public Ad2Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Ad2Settings>();
        }

        public override string SettingsCategory()
        {
            return "Bulk recipe generator";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var x = inRect.x;
            var y = inRect.y;
            var LH = Text.LineHeight;
            y += LH;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "Global settings (requires restart)");
            y += Text.LineHeight + 2;
            Text.Font = GameFont.Small;
            if (defaultThresholdField.DoField(y, "Default target time", ref settings.defaultThreshold))
            {
                Messages.Message("Default target time changed to " + settings.defaultThreshold,
                    MessageTypeDefOf.NeutralEvent);
            }

            TooltipHandler.TipRegion(new Rect(x, y, 360, LH),
                "Has no effect if 'Put recipes in context menu' is checked");

            y += LH;
            var r1 = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(r1, "Limit to x5 recipes", ref settings.limitToX5);
            TooltipHandler.TipRegion(r1, "Add only x5 recipes");
            y += LH;
            var r2 = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(r2, "Put recipes in context menu", ref settings.useRightClickMenu);
            TooltipHandler.TipRegion(r2,
                "Put recipes to context menu of source recipe instead of adding after it in the same list.");
            y += LH;
            var r3 = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(r3, "Same quality for all items", ref settings.useSameQualityForAll);
            TooltipHandler.TipRegion(r3,
                "Recipes that make items that have quality will use the same quality for all created items. Inspiration will apply to all items created.");

            y += (2 * LH) + 2;

            if (Current.Game == null)
            {
                //lastGame = null;
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "World settings");
            y += Text.LineHeight + 2;
            Text.Font = GameFont.Small;

            var bills = Ad2.FindRecipesUses();
            var s = $"Remove modded recipes from save ({bills.Count} found)";
            var w = Text.CalcSize(s).x + 64;
            if (!Widgets.ButtonText(new Rect(x, y, w, 32), s))
            {
                return;
            }

            foreach (var bill in bills)
            {
                bill.billStack.Delete(bill);
            }

            Messages.Message(bills.Count + " bills removed", MessageTypeDefOf.NeutralEvent);
        }
        //Game lastGame;

        private class NumField
        {
            private readonly int min, max;
            private string buffer;

            public NumField(int min = 0, int max = 120)
            {
                this.min = min;
                this.max = max;
            }

            public bool DoField(float y, string label, ref int val)
            {
                var LH = Text.LineHeight;
                if (buffer == null)
                {
                    buffer = val.ToString();
                }

                float x = 0;
                var anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, y, 200, LH), label);
                Text.Anchor = anchor;
                x += 200;
                buffer = Widgets.TextField(new Rect(x, y, 60, LH), buffer);
                x += 60;
                if (!Widgets.ButtonText(new Rect(x, y, 100, LH), "Apply"))
                {
                    return false;
                }

                if (int.TryParse(buffer, out var resInt))
                {
                    val = Util.Clamp(resInt, min, max);
                    buffer = val.ToString();
                    return true;
                }

                buffer = val.ToString();

                return false;
            }
        }
    }
}