using UnityEngine;

namespace Utils
{
    /// <summary>
    ///     Singleton class which saves & lods local-client settings.
    /// </summary>
    /// <remarks> This is just a wrapper around the PlayerPrefs system so that all the calls are in the same place.</remarks>
    public static class ClientPrefs
    {
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        // SFX (Weapons, Movement, etc), Chat?, etc?

        private const string CLIENT_GUID_KEY = "client_guid";
        private const string AVAILABLE_PROFILES_KEY = "AvailableProfiles";


        private const float DEFAULT_MASTER_VOLUME = 0.5f;
        private const float DEFAULT_MUSIC_VOLUME = 0.8f;



        public static float GetMasterVolume() => PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
        public static void SetMasterVolume(float volume) => PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);

        public static float GetMusicVolume() => PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
        public static void SetMusicVolume(float volume) => PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);


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