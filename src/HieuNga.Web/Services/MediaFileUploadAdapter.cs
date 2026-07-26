using HieuNga.Application.Media;

namespace HieuNga.Web.Services;

public static class MediaFileUploadAdapter
{
    public static async Task<MediaFileUpload> FromFormFileAsync(IFormFile file, string? relativePath = null, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;
        return new MediaFileUpload
        {
            Content = ms,
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Length = file.Length,
            RelativePath = relativePath ?? file.FileName
        };
    }

    public static async Task<IReadOnlyList<MediaFileUpload>> FromFormFilesAsync(IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        var list = new List<MediaFileUpload>();
        foreach (var f in files.Where(x => x.Length > 0))
            list.Add(await FromFormFileAsync(f, ct: ct));
        return list;
    }
}
