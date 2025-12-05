using Unity.Netcode;
using UnityEngine;

namespace SceneLoading
{
    /// <summary>
    ///     Contains data on scene loading progress for the local instance and remote instances.
    /// </summary>
    public class LoadingProgressManager : NetworkBehaviour
    {
        public AsyncOperation LocalLoadOperation
        {
            set
            {
                _isLoading = true;
                LocalProgress = 0;
                _localLoadOperation = value;
            }
        }
        private AsyncOperation _localLoadOperation;
        private float _localLoadProgress;
        private bool _isLoading;


        public float LocalProgress
        {
            get => throw new System.NotImplementedException();//IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) ? ProgressTrackers[NetworkManager.LocalClientId].Progress.Value : _localLoadProgress;
            set
            {
                if (IsSpawned /*&& ProgressTrackers.ContainsKey(NetworkManager.LocalClientId) && ProgressTrackers[NetworkManager.LocalClientId].IsSpawned*/)
                {
                    //ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
                }
                else
                {
                    _localLoadProgress = value;
                }
            }
        }
    }
}