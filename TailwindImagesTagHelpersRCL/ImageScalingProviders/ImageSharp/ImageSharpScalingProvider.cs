using System.Runtime.CompilerServices;

namespace TailwindImagesTagHelpers.ImageScalingProviders.ImageSharp;

public class ImageSharpScalingProvider : IImageScalingProvider
{
    public string GetUrl(string url, int width, string? format = null)
    {
        url = !url.Contains('?') ? url + "?" : url + "&";

        url = url + "width=" + width;
        
        if (!string.IsNullOrWhiteSpace(format))
            url = url + "&format=" + format;
        
        return url;
    }
}