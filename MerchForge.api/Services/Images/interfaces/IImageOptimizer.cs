namespace MerchForge.api.Services.Images.interfaces
{
    /// <summary>
    /// Shrinks an uploaded image before it is stored, so the bucket holds something
    /// close to what a storefront actually renders rather than whatever came off a
    /// phone camera.
    ///
    /// Runs after the byte-signature check, never before: this hands the bytes to a
    /// decoder, and only a file already proved to be an image of an allowed type
    /// should get that far.
    /// </summary>
    public interface IImageOptimizer
    {
        /// <summary>
        /// Returns the bytes to store, along with the type they should be stored as
        /// and the dimensions they ended up with.
        ///
        /// Never throws for a decodable image and never fails an upload: anything it
        /// cannot improve on comes back unchanged, because storing a slightly larger
        /// file is always better than refusing the upload.
        /// </summary>
        OptimizedImage Optimize(byte[] bytes, string contentType, string extension);
    }

    /// <summary>
    /// The result of an optimization attempt. ContentType and Extension describe what
    /// is actually in <paramref name="Bytes"/>, which is not necessarily what was
    /// uploaded - a JPEG comes back as WebP.
    /// </summary>
    /// <param name="Width">Null only when the image could not be decoded at all.</param>
    public record OptimizedImage(
        byte[] Bytes,
        string ContentType,
        string Extension,
        int? Width,
        int? Height);
}
