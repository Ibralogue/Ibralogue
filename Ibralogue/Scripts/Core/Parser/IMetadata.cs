public interface IMetadata
{
	bool HasMetadata(string key);
	bool TryGetMetadataValue(string key, out string value);
}