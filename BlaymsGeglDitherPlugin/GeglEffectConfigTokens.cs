using GeglSharp;
using PaintDotNet.Effects;

namespace BlaymsGeglPlugin
{
    internal sealed class DitherConfigToken : EffectConfigToken
    {
        public int Method { get; set; }
        public int RedLevels { get; set; }
        public int GreenLevels { get; set; }
        public int BlueLevels { get; set; }
        public int AlphaLevels { get; set; }
        public int Seed { get; set; }

        public DitherConfigToken()
        {
            Method = (int)DitherMethod.FloydSteinberg;
            RedLevels = 6;
            GreenLevels = 6;
            BlueLevels = 6;
            AlphaLevels = 256;
            Seed = 0;
        }

        public DitherConfigToken(DitherConfigToken other)
        {
            Method = other.Method;
            RedLevels = other.RedLevels;
            GreenLevels = other.GreenLevels;
            BlueLevels = other.BlueLevels;
            AlphaLevels = other.AlphaLevels;
            Seed = other.Seed;
        }

        public override object Clone()
        {
            return new DitherConfigToken(this);
        }
    }

    internal sealed class PosterizeConfigToken : EffectConfigToken
    {
        public int Levels { get; set; }

        public PosterizeConfigToken()
        {
            Levels = 8;
        }

        public PosterizeConfigToken(PosterizeConfigToken other)
        {
            Levels = other.Levels;
        }

        public override object Clone()
        {
            return new PosterizeConfigToken(this);
        }
    }
}
