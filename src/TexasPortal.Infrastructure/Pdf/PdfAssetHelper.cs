using System;
using System.IO;
using System.Reflection;

namespace TexasPortal.Infrastructure.Pdf;

public static class PdfAssetHelper
{
    private const string EmbeddedLogoResourceName = "TexasPortal.Infrastructure.Assets.texas-logo.png";

    public static byte[]? ResolveLogoBytes(string? webRootPath = null, string? configuredLogoPath = null)
    {
        var trimmedLogo = configuredLogoPath?.TrimStart('/', '\\');
        var candidates = new[]
        {
            !string.IsNullOrWhiteSpace(webRootPath) && !string.IsNullOrWhiteSpace(trimmedLogo)
                ? Path.Combine(webRootPath, trimmedLogo)
                : null,
            !string.IsNullOrWhiteSpace(webRootPath) ? Path.Combine(webRootPath, "texas-logo.png") : null,
            !string.IsNullOrWhiteSpace(webRootPath) ? Path.Combine(webRootPath, "static", "texas-logo.png") : null,
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "texas-logo.png"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "static", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "static", "texas-logo.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "TexasPortal.Api", "wwwroot", "texas-logo.png")
        };

        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try { return File.ReadAllBytes(path); } catch { }
            }
        }

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedLogoResourceName);
            if (stream != null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        catch
        {
            // Ignore embedded resource fallback failure
        }

        return null;
    }
}
