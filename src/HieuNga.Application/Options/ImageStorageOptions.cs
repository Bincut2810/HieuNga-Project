namespace HieuNga.Application.Options;

public sealed class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    /// <summary>Local or Cloudinary</summary>
    public string Provider { get; set; } = "Local";

    public int MaxFileSizeMb { get; set; } = 5;

    public CloudinaryOptions Cloudinary { get; set; } = new();

    public bool UseCloudinary =>
        string.Equals(Provider, "Cloudinary", StringComparison.OrdinalIgnoreCase);
}

public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
