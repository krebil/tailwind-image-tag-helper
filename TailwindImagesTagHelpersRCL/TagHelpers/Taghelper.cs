using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using TailwindImagesTagHelpers.ImageScalingProviders;
using TailwindImagesTagHelpers.Options;

namespace TailwindImagesTagHelpers.TagHelpers
{
    [HtmlTargetElement("tw-img", TagStructure = TagStructure.WithoutEndTag)]
    public class TailwindResponsiveImageTagHelper : ImageTagHelper
    {
        private readonly IImageScalingProvider _imageScalingProvider;
        private const string TagName = "img";
        private const string SizeAttributePrefix = "size-";

        public string FallbackSize { get; set; } = "100vw";
        public bool ConserveSrc { get; set; }

        private TailwindImageScalingOptions TailwindImageScalingOptions { get; init; }

        private Dictionary<string, SizeAtBreakPoint> BreakPointSizeOverride { get; set; } = new();
        
        

        public TailwindResponsiveImageTagHelper(
            IOptions<TailwindImageScalingOptions> tailwindImageScalingOptions,
            IImageScalingProvider imageScalingProvider,
            IFileVersionProvider fileVersionProvider,
            HtmlEncoder htmlEncoder,
            IUrlHelperFactory urlHelperFactory)
            : base(fileVersionProvider, htmlEncoder, urlHelperFactory)
        {
            _imageScalingProvider = imageScalingProvider;
            TailwindImageScalingOptions = tailwindImageScalingOptions.Value;
        }

        private List<TagHelperAttribute> SizeAttributes { get; set; } = new();

#pragma warning disable CS1998
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
#pragma warning restore CS1998
        {
            output.TagName = TagName;
            SizeAttributes = context.AllAttributes
                .Where<TagHelperAttribute>(a => a.Name.StartsWith(SizeAttributePrefix)).ToList();
            BreakPointSizeOverride = GenerateSizesFromAttributes();
            AddSizes(output);
            if (ConserveSrc)
                output.CopyHtmlAttribute(nameof(Src), context);
            foreach (var tagHelperAttribute in SizeAttributes)
                output.Attributes.RemoveAll(tagHelperAttribute.Name);
        }

        private Dictionary<string, int> CalculateImageSizes()
        {
            var imageSizes = new Dictionary<string, int>();
            foreach (var breakpoint in TailwindImageScalingOptions.Breakpoints)
            {
                if (BreakPointSizeOverride.TryGetValue(breakpoint.Key, out var sizeAtBreakPoint))
                {
                    switch (sizeAtBreakPoint.Unit)
                    {
                        case Units.ViewWidth:
                            imageSizes.Add(breakpoint.Key, (int)Math.Ceiling(sizeAtBreakPoint.Size * breakpoint.Value));
                            break;
                        case Units.Pixels:
                            imageSizes.Add(breakpoint.Key, (int)sizeAtBreakPoint.Size);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                    imageSizes.Add(breakpoint.Key, breakpoint.Value);
            }

            return imageSizes;
        }

        private IEnumerable<string> CalculateSizesTagItems()
        {
            foreach (var breakpoint in TailwindImageScalingOptions.Breakpoints)
            {
                if (!BreakPointSizeOverride.TryGetValue(breakpoint.Key, out var sizeAtBreakPoint)) continue;

                var unit = sizeAtBreakPoint.Unit;
                var imageSize = unit switch
                {
                    Units.ViewWidth => (sizeAtBreakPoint.Size * 100.0).ToString(CultureInfo.InvariantCulture) +
                                       "vw",
                    Units.Pixels => sizeAtBreakPoint.Size.ToString(CultureInfo.InvariantCulture) + "px",
                    _ => throw new ArgumentOutOfRangeException()
                };
                yield return $"(min-width: {breakpoint.Value.ToString()}px) {imageSize}";
            }
        }

        private void AddSizes(TagHelperOutput output)
        {
            var sizesTagItems = CalculateSizesTagItems().ToList();
            AddSizesTag(output, sizesTagItems);
            var array = CalculateImageSizes().Values.Distinct().ToArray();
            var source = new List<int>();
            if (TailwindImageScalingOptions.SupportedDevicePixelRatios.Any())
            {
                foreach (var devicePixelRatio in TailwindImageScalingOptions.SupportedDevicePixelRatios)
                {
                    var supportedDevicePixelRatio = devicePixelRatio;
                    source.AddRange(array.Select(cs => (int)Math.Ceiling(cs * supportedDevicePixelRatio)));
                }
            }
            else
                source = array.ToList();

            var list = source.Distinct().Select(size => _imageScalingProvider.GetUrl(Src, size) + $" {size}w").ToList();
            AddSrcsetTag(output, list);
        }

        private Dictionary<string, SizeAtBreakPoint> GenerateSizesFromAttributes()
        {
            var sizesFromAttributes = new Dictionary<string, SizeAtBreakPoint>();
            foreach (var sizeAttribute in SizeAttributes)
            {
                var strArray = sizeAttribute.Name.Split("-");
                if (strArray.Length < 2)
                    throw new ArgumentException("size- must be in the format size-{breakpoint}");

                var key = strArray[1];
                if (!double.TryParse(sizeAttribute.Value.ToString(), CultureInfo.InvariantCulture, out var result))
                    throw new ArgumentException("size- must be a number");

                if (result % 1.0 == 0.0 && (int)result != 1)
                {
                    sizesFromAttributes.Add(key, new SizeAtBreakPoint
                    {
                        Size = result
                    });
                }
                else
                {
                    if (result > 1.0)
                        throw new ArgumentException(
                            "The size-* attributes must be a decimal between 0 and 1 or a whole number larger than 1");
                    sizesFromAttributes.Add(key, new SizeAtBreakPoint
                    {
                        Size = result,
                        Unit = Units.ViewWidth
                    });
                }
            }

            return sizesFromAttributes;
        }

        private void AddSrcsetTag(TagHelperOutput output, List<string> srcset)
        {
            output.Attributes.SetAttribute("srcset", string.Join(",", srcset));
        }


        private void AddSizesTag(TagHelperOutput output, List<string> sizes)
        {
            var attr = "";
            if (sizes.Any())
            {
                attr = string.Join(',', sizes) + ", ";
            }
            attr += FallbackSize;
            output.Attributes.SetAttribute(nameof(sizes), attr);
        }

        private class SizeAtBreakPoint
        {
            public Units Unit { get; init; } = Units.Pixels;

            public double Size { get; init; }
        }

        private enum Units
        {
            ViewWidth,
            Pixels,
        }
    }
}