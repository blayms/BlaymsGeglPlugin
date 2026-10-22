using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace BlaymsGeglPlugin
{
    internal static class GeglEffectIcons
    {
        public static readonly Image Dither = Embeds.Find<Image>("GeglDitherIcon");
        public static readonly Image Posterize = Embeds.Find<Image>("GeglPosterizeIcon");
    }

    internal static class Embeds
    {
        public static T Find<T>(string resourceNameKey) where T : class
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string fullResourceName = null;

            // Search manifest resources for matching name suffix or key
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("." + resourceNameKey + ".png", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("." + resourceNameKey, StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(resourceNameKey, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fullResourceName = name;
                    break;
                }
            }

            if (fullResourceName == null)
            {
                throw new FileNotFoundException($"Embedded resource matching '{resourceNameKey}' was not found.");
            }

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not load manifest resource stream for '{fullResourceName}'.");
                }

                if (typeof(T) == typeof(Image) || typeof(T) == typeof(Bitmap))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        return (T)(object)(new Bitmap(memoryStream));
                    }
                }

                throw new NotSupportedException($"Type {typeof(T).FullName} is not supported by Embeds.Find.");
            }
        }
    }
}