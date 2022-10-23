public interface IMetadata
{
	bool HasTag(string key);
	bool TryGetTagValue(string key, out string value);
}