using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif

namespace Utils
{
    /// <summary>
    ///     Manages the various Profiles of a client
    /// </summary>
    public class ProfileManager
    {
        public const string AUTH_PROFILE_COMMAND_LINE_ARGUMENT = "-AuthProfile";

        private string _profile;
        public string Profile
        {
            get
            {
                if (_profile == null)
                    _profile = GetProfile();

                return _profile;
            }
            set
            {
                _profile = value;
                OnProfileChanged?.Invoke();
            }
        }


        public event System.Action OnProfileChanged;

        private List<string> _availableProfiles;
        public ReadOnlyCollection<string> AvailableProfiles
        {
            get
            {
                if (_availableProfiles == null)
                    LoadProfiles();

                return _availableProfiles.AsReadOnly();
            }
        }


        public void CreateProfile(string profile)
        {
            _availableProfiles.Add(profile);
            SaveProfiles();
        }
        public void DeleteProfile(string profile)
        {
            _availableProfiles.Remove(profile);
            SaveProfiles();
        }

        private static string GetProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for(int i = 0; i < arguments.Length; ++i)
            {
                if (arguments[i] == AUTH_PROFILE_COMMAND_LINE_ARGUMENT)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

            #if UNITY_EDITOR

            // While running in the Editor, make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual projects.
            // "Since only a single instance of the Editor can be open for a specific DataPath, uniqueness is ensured."
            //      Note: This is actually incorrect, but we're not utilising the editor multi-play-mode functions yet so it's fine for now.
            var hashedBytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);

            // The authentication service allows profile names of a maximum 30 characters.
            //  We're generating a GUID based on the profiect's path.
            //  Truncating the first 30 characters of said GUID string will suffice for uniqueness.
            return new Guid(hashedBytes).ToString("N")[..30];

            #else
            // No profile arguments found.
            return "";
            #endif
        }

        /// <summary>
        ///     Save the available user profiles using the <see cref="ClientPrefs"/> wrapper.
        /// </summary>
        private void LoadProfiles()
        {
            _availableProfiles = new List<string>();
            string loadedProfiles = ClientPrefs.GetAvailableProfiles();
            foreach(string profile in loadedProfiles.Split(','))    // This works as we're sanitizing our input strings.
            {
                if (profile.Length > 0)
                {
                    _availableProfiles.Add(profile);
                }
            }
        }
        /// <summary>
        ///     Save the available user profiles using the <see cref="ClientPrefs"/> wrapper.
        /// </summary>
        private void SaveProfiles()
        {
            string profilesToSave = "";
            foreach(string profile in _availableProfiles)
            {
                profilesToSave += profile + ",";
            }

            ClientPrefs.SetAvailableProfiles(profilesToSave);
        }
    }
}