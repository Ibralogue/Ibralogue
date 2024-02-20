namespace Ibralogue
{
    public interface IMetadata
    {
        /// <summary>
        /// Check if entity contains any metadata.
        /// </summary>
        bool HasMetadata(string key);

        /// <summary>
        /// Try to get the value of a metadata using its key, if it exists.
        /// </summary>
        /// <param name="key">The key of the provided metadata.</param>
        /// <param name="value">The value that it has associated with the key.</param
        bool TryGetMetadataValue(string key, out string value);
    }
}