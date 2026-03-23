using System;
using System.Collections.Generic;
using MelonLoader;
using Steamworks;

namespace IAYBLeaderboard.Steam
{
    public struct FriendScore
    {
        public CSteamID SteamId;
        public string PlayerName;
        public int ScoreCentiseconds;
        public int Rank;
        public bool IsLocalPlayer;

        public float TimeSeconds => ScoreCentiseconds / 100f;
    }

    public class SteamLeaderboardManager
    {
        public static SteamLeaderboardManager Instance { get; private set; }

        private readonly MelonLogger.Instance _logger;

        private readonly Dictionary<string, List<FriendScore>> _cache = new Dictionary<string, List<FriendScore>>();
        private readonly HashSet<string> _dirtyKeys = new HashSet<string>();

        private List<FriendScore> _worldTopEntry;
        private string _pendingWorldKey;

        private readonly Dictionary<string, SteamLeaderboard_t> _boardHandles = new Dictionary<string, SteamLeaderboard_t>();

        private readonly Queue<Action> _pendingOperations = new Queue<Action>();
        private bool _operationInProgress;

        private string _pendingDownloadKey;

        private CallResult<LeaderboardFindResult_t> _findResult;
        private CallResult<LeaderboardScoreUploaded_t> _uploadResult;
        private CallResult<LeaderboardScoresDownloaded_t> _downloadResult;

        public SteamLeaderboardManager(MelonLogger.Instance logger)
        {
            _logger = logger;
            Instance = this;
            _findResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
            _uploadResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
            _downloadResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnScoresDownloaded);
        }

        private static string MakeBoardName(string category, int levelId, bool isHorde)
        {
            return isHorde ? $"iayb_horde_{category}_{levelId}" : $"iayb_friends_{category}_{levelId}";
        }

        private static string MakeCacheKey(string category, int levelId)
        {
            return $"{category}_{levelId}";
        }

        private static void GetBoardSettings(bool isHorde,
            out ELeaderboardSortMethod sortMethod, out ELeaderboardDisplayType displayType)
        {
            sortMethod = isHorde
                ? ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending
                : ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending;
            displayType = isHorde
                ? ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric
                : ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds;
        }

        public void UploadScore(string category, int levelId, float timeSeconds)
        {
            int centiseconds = (int)(timeSeconds * 100f);
            UploadRawScore(category, levelId, centiseconds, false,
                $"{centiseconds}cs ({timeSeconds:F2}s)");
        }

        public void UploadHordeScore(string category, int levelId, int score)
        {
            UploadRawScore(category, levelId, score, true, $"{score}pts");
        }

        private void UploadRawScore(string category, int levelId, int score, bool isHorde, string displayStr)
        {
            string boardName = MakeBoardName(category, levelId, isHorde);
            string cacheKey = MakeCacheKey(category, levelId);

            _logger.Msg($"Queueing upload: {boardName} = {displayStr}");

            GetBoardSettings(isHorde, out var sortMethod, out var displayType);

            EnqueueOperation(() =>
            {
                _currentCacheKey = cacheKey;
                FindOrCreateBoard(boardName, sortMethod, displayType, (handle) =>
                {
                    var call = SteamUserStats.UploadLeaderboardScore(
                        handle,
                        ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                        score,
                        null, 0);
                    if (call == SteamAPICall_t.Invalid)
                    {
                        _logger.Error($"[Upload] UploadLeaderboardScore returned invalid handle for {boardName}");
                        FinishOperation();
                        return;
                    }
                    _uploadResult.Set(call);
                });
            });
        }

        public void DownloadFriendScores(string category, int levelId, bool isHorde, Action<List<FriendScore>> callback)
        {
            string cacheKey = MakeCacheKey(category, levelId);

            if (_cache.ContainsKey(cacheKey) && !_dirtyKeys.Contains(cacheKey))
            {
                _logger.Msg($"[Download] Using cached scores for {cacheKey} ({_cache[cacheKey].Count} entries)");
                callback?.Invoke(_cache[cacheKey]);
                return;
            }

            if (_pendingDownloadKey == cacheKey)
            {
                _logger.Msg($"[Download] Already pending for {cacheKey}, skipping duplicate");
                return;
            }

            string boardName = MakeBoardName(category, levelId, isHorde);
            _logger.Msg($"[Download] Enqueueing download for {boardName}");
            _pendingDownloadKey = cacheKey;

            GetBoardSettings(isHorde, out var sortMethod, out var displayType);

            EnqueueOperation(() =>
            {
                _currentDownloadCallback = callback;
                _currentCacheKey = cacheKey;
                FindOrCreateBoard(boardName, sortMethod, displayType, (handle) =>
                {
                    _logger.Msg($"[Download] Calling DownloadLeaderboardEntries for {boardName}");
                    var call = SteamUserStats.DownloadLeaderboardEntries(
                        handle,
                        ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
                        0, 100);
                    if (call == SteamAPICall_t.Invalid)
                    {
                        _logger.Error($"[Download] DownloadLeaderboardEntries returned invalid handle for {boardName}");
                        _pendingDownloadKey = null;
                        FinishOperation();
                        return;
                    }
                    _downloadResult.Set(call);
                });
            });
        }

        public void DownloadWorldScores(string category, int levelId, bool isHorde, Action<List<FriendScore>> callback)
        {
            string cacheKey = "world_" + MakeCacheKey(category, levelId);

            if (_cache.ContainsKey(cacheKey) && !_dirtyKeys.Contains(cacheKey))
            {
                _logger.Msg($"[World] Using cached scores for {cacheKey} ({_cache[cacheKey].Count} entries)");
                callback?.Invoke(_cache[cacheKey]);
                return;
            }

            if (_pendingWorldKey == cacheKey)
            {
                _logger.Msg($"[World] Already pending for {cacheKey}, skipping duplicate");
                return;
            }

            string boardName = MakeBoardName(category, levelId, isHorde);
            _logger.Msg($"[World] Enqueueing world download for {boardName}");
            _pendingWorldKey = cacheKey;

            GetBoardSettings(isHorde, out var sortMethod, out var displayType);

            _worldTopEntry = null;
            EnqueueOperation(() =>
            {
                _currentCacheKey = null;
                _currentDownloadCallback = (topScores) =>
                {
                    _worldTopEntry = topScores;
                    _logger.Msg($"[World] Got top entry: {(topScores != null && topScores.Count > 0 ? topScores[0].PlayerName : "none")}");
                };
                FindOrCreateBoard(boardName, sortMethod, displayType, (handle) =>
                {
                    var call = SteamUserStats.DownloadLeaderboardEntries(
                        handle, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, 1);
                    if (call == SteamAPICall_t.Invalid)
                    {
                        _logger.Error("[World] DownloadLeaderboardEntries (top) returned invalid handle");
                        _pendingWorldKey = null;
                        FinishOperation();
                        return;
                    }
                    _downloadResult.Set(call);
                });
            });

            EnqueueOperation(() =>
            {
                _currentCacheKey = cacheKey;
                _currentDownloadCallback = (aroundScores) =>
                {
                    var merged = MergeWorldScores(_worldTopEntry, aroundScores);
                    _worldTopEntry = null;

                    _cache[cacheKey] = merged;
                    _dirtyKeys.Remove(cacheKey);
                    _pendingWorldKey = null;

                    _logger.Msg($"[World] Merged {merged.Count} entries for {cacheKey}");
                    callback?.Invoke(merged);
                };
                FindOrCreateBoard(boardName, sortMethod, displayType, (handle) =>
                {
                    var call = SteamUserStats.DownloadLeaderboardEntries(
                        handle, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -3, 3);
                    if (call == SteamAPICall_t.Invalid)
                    {
                        _logger.Error("[World] DownloadLeaderboardEntries (around) returned invalid handle");
                        _pendingWorldKey = null;
                        FinishOperation();
                        return;
                    }
                    _downloadResult.Set(call);
                });
            });
        }

        private List<FriendScore> MergeWorldScores(List<FriendScore> topScores, List<FriendScore> aroundScores)
        {
            var result = new List<FriendScore>();
            FriendScore? topEntry = null;

            if (topScores != null && topScores.Count > 0)
                topEntry = topScores[0];

            if (aroundScores == null)
                aroundScores = new List<FriendScore>();

            bool topAlreadyIncluded = false;
            if (topEntry.HasValue)
            {
                foreach (var s in aroundScores)
                {
                    if (s.Rank == topEntry.Value.Rank)
                    {
                        topAlreadyIncluded = true;
                        break;
                    }
                }
            }

            if (topEntry.HasValue && !topAlreadyIncluded)
            {
                result.Add(topEntry.Value);
                result.Add(new FriendScore { Rank = -1, PlayerName = "---" });
            }

            result.AddRange(aroundScores);
            return result;
        }

        public void InvalidateCache(string category, int levelId)
        {
            string cacheKey = MakeCacheKey(category, levelId);
            _dirtyKeys.Add(cacheKey);
            _dirtyKeys.Add("world_" + cacheKey);
        }

        public bool IsCacheDirtyByKey(string key)
        {
            return _dirtyKeys.Contains(key);
        }

        public void ClearDirtyByKey(string key)
        {
            _dirtyKeys.Remove(key);
        }

        private Action<SteamLeaderboard_t> _pendingBoardCallback;
        private Action<List<FriendScore>> _currentDownloadCallback;
        private string _currentCacheKey;

        private void FindOrCreateBoard(string boardName, ELeaderboardSortMethod sortMethod,
            ELeaderboardDisplayType displayType, Action<SteamLeaderboard_t> onFound)
        {
            if (_boardHandles.ContainsKey(boardName))
            {
                _logger.Msg($"[FindBoard] Cache hit for {boardName}, calling onFound synchronously");
                onFound(_boardHandles[boardName]);
                return;
            }
            _logger.Msg($"[FindBoard] Cache miss for {boardName}, calling FindOrCreateLeaderboard");

            _pendingBoardCallback = onFound;
            var call = SteamUserStats.FindOrCreateLeaderboard(boardName, sortMethod, displayType);
            _findResult.Set(call);
        }

        private void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bLeaderboardFound == 0)
            {
                _logger.Error($"Failed to find/create leaderboard (ioFailure={ioFailure})");
                FinishOperation();
                return;
            }

            string name = SteamUserStats.GetLeaderboardName(result.m_hSteamLeaderboard);
            _boardHandles[name] = result.m_hSteamLeaderboard;
            _logger.Msg($"Leaderboard ready: {name}");

            try
            {
                _pendingBoardCallback?.Invoke(result.m_hSteamLeaderboard);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in board callback: {ex}");
                FinishOperation();
            }
            _pendingBoardCallback = null;
        }

        private void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bSuccess == 0)
            {
                _logger.Error($"Score upload failed (ioFailure={ioFailure})");
            }
            else
            {
                _logger.Msg($"Score uploaded: {result.m_nScore}cs (changed={result.m_bScoreChanged})");
                if (_currentCacheKey != null)
                    _dirtyKeys.Add(_currentCacheKey);
            }
            FinishOperation();
        }

        private void OnScoresDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
        {
            var scores = new List<FriendScore>();

            _logger.Msg($"[OnScoresDownloaded] ioFailure={ioFailure}, entryCount={result.m_cEntryCount}, cacheKey={_currentCacheKey}");

            if (!ioFailure)
            {
                CSteamID localId = SteamUser.GetSteamID();

                for (int i = 0; i < result.m_cEntryCount; i++)
                {
                    LeaderboardEntry_t entry;
                    SteamUserStats.GetDownloadedLeaderboardEntry(
                        result.m_hSteamLeaderboardEntries, i, out entry, null, 0);

                    string name = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
                    bool isLocal = entry.m_steamIDUser == localId;
                    _logger.Msg($"[OnScoresDownloaded]   #{i}: {name} = {entry.m_nScore}cs (rank={entry.m_nGlobalRank}, isLocal={isLocal})");

                    scores.Add(new FriendScore
                    {
                        SteamId = entry.m_steamIDUser,
                        PlayerName = name,
                        ScoreCentiseconds = entry.m_nScore,
                        Rank = entry.m_nGlobalRank,
                        IsLocalPlayer = isLocal
                    });
                }
            }
            else
            {
                _logger.Error("[OnScoresDownloaded] Failed to download leaderboard entries");
            }

            if (_currentCacheKey != null)
            {
                _cache[_currentCacheKey] = scores;
                _dirtyKeys.Remove(_currentCacheKey);
            }

            _pendingDownloadKey = null;
            _logger.Msg($"[OnScoresDownloaded] Invoking callback with {scores.Count} scores");
            _currentDownloadCallback?.Invoke(scores);
            _currentDownloadCallback = null;
            FinishOperation();
        }

        private void EnqueueOperation(Action op)
        {
            _pendingOperations.Enqueue(op);
            TryProcessNext();
        }

        private void TryProcessNext()
        {
            if (_operationInProgress || _pendingOperations.Count == 0)
                return;
            _operationInProgress = true;
            _logger.Msg($"[Queue] Starting next operation ({_pendingOperations.Count} remaining after this)");
            var op = _pendingOperations.Dequeue();
            op();
        }

        // Deferred to OnUpdate to avoid re-entrancy issues with CallResult.Set() inside Steam callbacks.
        public void ProcessQueue()
        {
            TryProcessNext();
        }

        private void FinishOperation()
        {
            _operationInProgress = false;
            // Don't call TryProcessNext() here — re-entrancy issues inside Steam callbacks.
        }
    }
}
