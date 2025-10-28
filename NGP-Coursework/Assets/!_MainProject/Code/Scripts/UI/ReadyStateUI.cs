using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace UI.Lobby
{
    /// <summary>
    ///     A UI element that displays the ready states of all players in a lobby.
    /// </summary>
    public class ReadyStateUI : MonoBehaviour
    {
        [SerializeField] private Transform _readyCheckMarkRoot;
        [SerializeField] private ReadyCheckMark _readyCheckMarkPrefab;
        private Dictionary<ulong, ReadyCheckMark> _readyCheckMarkInstances = new Dictionary<ulong, ReadyCheckMark>();


        private void Awake()
        {
            LobbyManager.OnClientIsReady += SetClientReady;
            LobbyManager.OnClientNotReady += SetClientNotReady;
        }
        private void OnDestroy()
        {
            LobbyManager.OnClientIsReady -= SetClientReady;
            LobbyManager.OnClientNotReady -= SetClientNotReady;
        }


        /// <summary>
        ///     Mark a client as ready on the UI.
        /// </summary>
        private void SetClientReady(ulong clientID)
        {
            ReadyCheckMark readyCheckMark = GetReadyCheckMarkForClientID(clientID);
            readyCheckMark.SetToggleVisibility(true);
        }
        /// <summary>
        ///     Mark a client as not ready on the UI.
        /// </summary>
        private void SetClientNotReady(ulong clientID)
        {
            ReadyCheckMark readyCheckMark = GetReadyCheckMarkForClientID(clientID);
            readyCheckMark.SetToggleVisibility(false);
        }

        /// <summary>
        ///     Retrieve the ReadyCheckMark for the associated ClientID, creating a new one if none exist.
        /// </summary>
        private ReadyCheckMark GetReadyCheckMarkForClientID(ulong clientID)
        {
            if (_readyCheckMarkInstances.ContainsKey(clientID))
            {
                // Return the existing instance.
                return _readyCheckMarkInstances[clientID];
            }
            else
            {
                // Create the instance.
                ReadyCheckMark readyCheckMark = Instantiate<ReadyCheckMark>(_readyCheckMarkPrefab, _readyCheckMarkRoot);
                
                if (clientID == NetworkManager.Singleton.LocalClientId)
                {
                    // Set the check mark's name for debugging.
                    readyCheckMark.name = "ClientReadyMark";

                    // If this mark is the local client, ensure that it is positioned at the start of the list.
                    readyCheckMark.transform.SetAsFirstSibling();

                    // Have the display text tell the player that this is their check mark.
                    readyCheckMark.SetToggleText("(You)");
                }
                else
                {
                    // Set the check mark's name.
                    readyCheckMark.SetToggleText("Player" + clientID.ToString());
                }


                // Cache the instance for future reference.
                _readyCheckMarkInstances.Add(clientID, readyCheckMark);

                // Return the instance.
                return readyCheckMark;
            }
        }
    }
}