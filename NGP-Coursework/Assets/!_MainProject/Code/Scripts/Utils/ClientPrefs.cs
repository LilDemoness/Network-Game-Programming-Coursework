using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     Singleton class which saves & lods local-client settings.
    /// </summary>
    /// <remarks> This is just a wrapper around the PlayerPrefs system so that all the calls are in the same place.</remarks>
    public static class ClientPrefs
    {
        // Note: Volumes are stored as Percentages between 0.0f * 1.0f.
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        // SFX (Weapons, Movement, etc), Chat?, etc?


        private const string HORIZONTAL_RESOLUTION_KEY = "ResolutionHorizontal";
        private const string VERTICAL_RESOLUTION_KEY = "ResolutionVertical";
        private const string FULLSCREEN_MODE_KEY = "FullscreenMode";
        private const string VSYNC_ENABLED_KEY = "VSyncEnabled";


        private const string CLIENT_GUID_KEY = "client_guid";
        private const string AVAILABLE_PROFILES_KEY = "AvailableProfiles";


        private const float DEFAULT_MASTER_VOLUME = 0.75f;
        private const float DEFAULT_MUSIC_VOLUME = 0.8f;

        private const int DEFAULT_FULLSCREEN_MODE = (int)FullScreenMode.Windowed;
        private const int DEFAULT_VSYNC_VALUE = 1;


        public static float GetMasterVolume() => PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
        public static void SetMasterVolume(float volume) => PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);

        public static float GetMusicVolume() => PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        public static void SetMusicVolume(float volume) => PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);


        public static Vector2 GetResolution() => new Vector2(PlayerPrefs.GetFloat(HORIZONTAL_RESOLUTION_KEY, Screen.currentResolution.width), PlayerPrefs.GetFloat(VERTICAL_RESOLUTION_KEY, Screen.currentResolution.height));
        public static void SetResolution(Resolution resolution) => SetResolution(resolution.width, resolution.height);
        public static void SetResolution(Vector2 resolution) => SetResolution(resolution.x, resolution.y);
        public static void SetResolution(float width, float height)
        {
            PlayerPrefs.SetFloat(HORIZONTAL_RESOLUTION_KEY, width);
            PlayerPrefs.SetFloat(VERTICAL_RESOLUTION_KEY, height);
        }

        public static FullScreenMode GetFullscreenMode() => (FullScreenMode)(PlayerPrefs.GetInt(FULLSCREEN_MODE_KEY, DEFAULT_FULLSCREEN_MODE));
        public static void SetFullscreenMode(FullScreenMode fullscreenMode) => SetFullscreenMode((int)fullscreenMode);
        public static void SetFullscreenMode(int fullscreenModeIndex) => PlayerPrefs.SetInt(FULLSCREEN_MODE_KEY, fullscreenModeIndex);

        public static bool GetVSyncEnabled() => PlayerPrefs.GetInt(VSYNC_ENABLED_KEY, DEFAULT_VSYNC_VALUE) == 1;
        public static void SetVSyncEnabled(bool enabled) => PlayerPrefs.SetInt(VSYNC_ENABLED_KEY, enabled ? 1 : 0);


        /// <summary>
        ///     Either loads a Guid string from Unity preferences, or creates one and checkpoints it before returning it.
        /// </summary>
        /// <returns> The Guid that uniquely identifies this client install, in string form.</returns>
        public static string GetGuid()
        {
            if (PlayerPrefs.HasKey(CLIENT_GUID_KEY))
            {
                // We already have a GUID.
                return PlayerPrefs.GetString(CLIENT_GUID_KEY);
            }

            // We don't have a Guid. Create one.
            System.Guid guid = System.Guid.NewGuid();
            string guidString = guid.ToString();

            // Save and return our new Guid.
            PlayerPrefs.SetString(CLIENT_GUID_KEY, guidString);
            return guidString;
        }

        public static string GetAvailableProfiles() => PlayerPrefs.GetString(AVAILABLE_PROFILES_KEY, "");
        public static void SetAvailableProfiles(string availableProfiles) => PlayerPrefs.SetString(AVAILABLE_PROFILES_KEY, availableProfiles);
    }
}