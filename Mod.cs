using MelonLoader;
using Steamworks;
using IAYBLeaderboard.Steam;
using IAYBLeaderboard.UI;

[assembly: MelonInfo(typeof(IAYBLeaderboard.Mod), "Leaderboard", "1.0.0", "Modder")]
[assembly: MelonGame("Strange Scaffold", "I Am Your Beast")]

namespace IAYBLeaderboard
{
    public class Mod : MelonMod
    {
        public static Mod Instance { get; private set; }
        public SteamLeaderboardManager LeaderboardManager { get; private set; }
        public bool SteamAvailable { get; private set; }

        private LeaderboardPanel _panel;
        public LeaderboardPanel Panel => EnsurePanel();

        private WorldPanel _worldPanel;
        public WorldPanel WorldPanel => EnsureWorldPanel();

        private LeaderboardPanel EnsurePanel()
        {
            if (_panel == null)
                _panel = new LeaderboardPanel();
            return _panel;
        }

        private WorldPanel EnsureWorldPanel()
        {
            if (_worldPanel == null)
                _worldPanel = new WorldPanel();
            return _worldPanel;
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            LeaderboardManager = new SteamLeaderboardManager(LoggerInstance);

            try { SteamAvailable = SteamAPI.IsSteamRunning(); }
            catch { SteamAvailable = false; }

            LoggerInstance.Msg("Leaderboard mod loaded!");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (_panel != null && _panel.IsVisible)
                _panel.Hide();
            if (_worldPanel != null && _worldPanel.IsVisible)
                _worldPanel.Hide();
        }

        public override void OnUpdate()
        {
            if (SteamAvailable)
            {
                try { SteamAPI.RunCallbacks(); }
                catch { }
            }

            LeaderboardManager?.ProcessQueue();

            if (_panel != null && _panel.IsVisible && _panel.CurrentCacheKey != null)
            {
                if (LeaderboardManager.IsCacheDirtyByKey(_panel.CurrentCacheKey))
                {
                    LeaderboardManager.ClearDirtyByKey(_panel.CurrentCacheKey);
                    if (_panel.CurrentLevelInfo != null)
                        _panel.RefreshForLevel(_panel.CurrentLevelInfo, _panel.CurrentCategory, _panel.CurrentLevelId);
                }
            }

            if (_worldPanel != null && _worldPanel.IsVisible && _worldPanel.CurrentWorldCacheKey != null)
            {
                if (LeaderboardManager.IsCacheDirtyByKey(_worldPanel.CurrentWorldCacheKey))
                {
                    LeaderboardManager.ClearDirtyByKey(_worldPanel.CurrentWorldCacheKey);
                    if (_worldPanel.CurrentLevelInfo != null)
                        _worldPanel.RefreshForLevel(_worldPanel.CurrentLevelInfo, _worldPanel.CurrentCategory, _worldPanel.CurrentLevelId);
                }
            }
        }

        public override void OnDeinitializeMelon()
        {
            _panel?.Destroy();
            _worldPanel?.Destroy();
        }
    }
}
