namespace Ad2mod;

internal class Util
{
    public static int Clamp(int val, int a, int b)
    {
        return val < a ? a : val > b ? b : val;
    }
}