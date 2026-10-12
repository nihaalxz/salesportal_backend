using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Api.Controllers;

public static class Base64FileHelper
{
    public static Task<string?> SavePaymentProofAsync(string? base64Data, string webRootPath, CancellationToken ct = default)
    {
        return SaveFileAsync(base64Data, "payment_proofs", webRootPath, ct);
    }

    public static async Task<string?> SaveFileAsync(string? base64Data, string subFolder, string webRootPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            return null;

        // If it doesn't look like base64, assume it's already a saved path
        if (!base64Data.Contains("data:") || !base64Data.Contains(";base64,"))
        {
            return base64Data;
        }

        string extension = ".png";
        string pureBase64 = base64Data;

        if (base64Data.Contains(","))
        {
            var parts = base64Data.Split(',');
            var meta = parts[0];
            pureBase64 = parts[1];

            if (meta.Contains("application/pdf"))
                extension = ".pdf";
            else if (meta.Contains("image/png"))
                extension = ".png";
            else if (meta.Contains("image/jpeg") || meta.Contains("image/jpg"))
                extension = ".jpg";
            else if (meta.Contains("image/gif"))
                extension = ".gif";
            else if (meta.Contains("image/webp"))
                extension = ".webp";
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(pureBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid file base64 format.");
        }

        var webRoot = webRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRoot, "uploads", subFolder);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await File.WriteAllBytesAsync(filePath, bytes, ct);

        return $"/uploads/{subFolder}/{fileName}";
    }
}
