using Microsoft.Extensions.Options;

namespace Felix.Infrastructure.AI;

public interface IGeminiKeyManager
{
    string GetCurrentKey();
    bool TryNextKey();
    void ResetToFirst();
}

public class GeminiKeyManager : IGeminiKeyManager
{
    private readonly List<string> _apiKeys;
    private int _currentIndex;
    private readonly object _lock = new();

    public GeminiKeyManager(IOptions<GeminiOptions> options)
    {
        _apiKeys = options.Value.ApiKeys;

        if (_apiKeys.Count == 0)
        {
            throw new InvalidOperationException("At least one Gemini API key is required");
        }

        _currentIndex = 0;
    }

    public string GetCurrentKey()
    {
        lock (_lock)
        {
            return _apiKeys[_currentIndex];
        }
    }

    public bool TryNextKey()
    {
        lock (_lock)
        {
            if (_currentIndex + 1 >= _apiKeys.Count)
            {
                return false; // 沒有更多的 key 了
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
}
