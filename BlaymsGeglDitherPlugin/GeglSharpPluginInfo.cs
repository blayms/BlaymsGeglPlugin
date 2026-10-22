using PaintDotNet;

namespace BlaymsGeglPlugin
{
    public sealed class GeglSharpPluginInfo : IPluginSupportInfo
    {
        public string DisplayName
        {
            get { return "GeglSharp"; }
        }

        public string Author
        {
            get { return "Blayms"; }
        }

        public string Copyright
        {
            get { return "Copyright © Blayms"; }
        }

        public Version Version
        {
            get { return new Version(1, 1, 0, 0); }
        }

        public Uri WebsiteUri
        {
            get { return new Uri("https://blayms.github.io/about-me/"); }
        }
    }
}
