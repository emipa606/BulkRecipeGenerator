namespace Ad2mod;

internal static class Util
{
    public static int Clamp(int val, int a, int b)
    {
        if (val < a)
        {
            return a;
        }

        return val > b ? b : val;
    }
}