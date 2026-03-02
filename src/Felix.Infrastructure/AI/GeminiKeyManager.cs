using Microsoft.Extensions.Configuration;

namespace Felix.Infrastructure.AI;

public interface IGeminiKeyManager
{
    string GetCurrentKey();
    int GetCurrentKeyIndex();
    int GetTotalKeys();
    bool TryNextKey();
    void ResetToFirst();
    void IncrementRequestCount();
    int GetRequestCount(int keyIndex);
    int GetTotalRequestCount();
    string MaskKey(string apiKey);
    List<(int Index, string MaskedKey, int RequestCount)> GetAllKeysStatus();
}

public class GeminiKeyManager : IGeminiKeyManager
{
    private readonly List<string> _apiKeys;
    private readonly Dictionary<int, int> _requestCounts = new();
    private int _currentIndex;
    private readonly object _lock = new();

    public GeminiKeyManager(IConfiguration configuration)
    {
        _apiKeys = configuration.GetSection("Gemini:ApiKeys").Get<List<string>>() ?? [];

        if (_apiKeys.Count == 0)
        {
            throw new InvalidOperationException("至少需要一個 Gemini API Key");
        }

        _currentIndex = 0;

        for (var i = 0; i < _apiKeys.Count; i++)
        {
            _requestCounts[i] = 0;
        }
    }

    public string GetCurrentKey()
    {
        lock (_lock)
        {
            return _apiKeys[_currentIndex];
        }
    }

    public int GetCurrentKeyIndex()
    {
        lock (_lock)
        {
            return _currentIndex;
        }
    }

    public int GetTotalKeys()
    {
        return _apiKeys.Count;
    }

    public bool TryNextKey()
    {
        lock (_lock)
        {
            if (_currentIndex + 1 >= _apiKeys.Count)
            {
                return false;
            }

            _currentIndex++;
            return true;
        }
    }

    public void ResetToFirst()
    {
        lock (_lock)
        {
            _currentIndex = 0;
        }
    }

    public void IncrementRequestCount()
    {
        lock (_lock)
        {
            _requestCounts[_currentIndex]++;
        }
    }

    public int GetRequestCount(int keyIndex)
    {
        lock (_lock)
        {
            return _requestCounts.TryGetValue(keyIndex, out var count) ? count : 0;
        }
    }

    public int GetTotalRequestCount()
    {
        lock (_lock)
        {
            return _requestCounts.Values.Sum();
        }
    }

    public string MaskKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
        {
            return "***";
        }

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }

    public List<(int Index, string MaskedKey, int RequestCount)> GetAllKeysStatus()
    {
        lock (_lock)
        {
            var result = new List<(int, string, int)>();
            for (int i = 0; i < _apiKeys.Count; i++)
            {
                result.Add((i, MaskKey(_apiKeys[i]), _requestCounts[i]));
            }
            return result;
        }
    }
}
