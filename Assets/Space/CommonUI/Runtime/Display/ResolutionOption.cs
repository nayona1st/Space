using System;

namespace SpaceGame.CommonUI.Display
{
    [Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
        public int refreshRateNumerator;
        public int refreshRateDenominator;

        public ResolutionOption(
            int width,
            int height,
            int refreshRateNumerator,
            int refreshRateDenominator)
        {
            this.width = width;
            this.height = height;
            this.refreshRateNumerator = refreshRateNumerator;
            this.refreshRateDenominator = Math.Max(1, refreshRateDenominator);
        }

        public override string ToString()
        {
            if (refreshRateNumerator <= 0)
            {
                return $"{width} × {height}";
            }

            float refreshRate = (float)refreshRateNumerator /
                                Math.Max(1, refreshRateDenominator);
            return $"{width} × {height}  {refreshRate:0.##} Hz";
        }
    }
}
