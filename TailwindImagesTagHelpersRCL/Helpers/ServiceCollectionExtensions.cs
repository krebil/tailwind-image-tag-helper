using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TailwindImagesTagHelpers.ImageScalingProviders;
using TailwindImagesTagHelpers.ImageScalingProviders.ImageSharp;
using TailwindImagesTagHelpers.Options;

namespace TailwindImagesTagHelpers.Helpers;

public static class ServiceCollectionExtensions
{
    public static void AddBreakpoints(this WebApplicationBuilder builder) => builder.Services.Configure<TailwindImageScalingOptions>((IConfiguration) builder.Configuration.GetSection("TailwindImageScaling"));

    public static void AddBreakpoints(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.Configure<TailwindImageScalingOptions>(configuration.GetSection("TailwindImageScaling"));
    }

    public static void AddImageSharpImageScalingProvider(this IServiceCollection serviceCollection) => serviceCollection.AddTransient<IImageScalingProvider, ImageSharpScalingProvider>();
}