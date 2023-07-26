namespace TailwindImagesTagHelpers.ImageScalingProviders;

public interface IImageScalingProvider
{
    string GetUrl(string url, int width, string? format = null);
}