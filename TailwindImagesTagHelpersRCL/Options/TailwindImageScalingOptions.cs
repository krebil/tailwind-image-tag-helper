namespace TailwindImagesTagHelpers.Options
{
    public class TailwindImageScalingOptions
    {
        public const string TailwindImageScalingName = "TailwindImageScaling";

        public Dictionary<string, int> Breakpoints { get; set; } = new();

        public double[] SupportedDevicePixelRatios { get; set; }
    }
}