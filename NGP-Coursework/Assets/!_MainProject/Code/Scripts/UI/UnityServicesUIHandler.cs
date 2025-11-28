using Gameplay.UI.Popups;
using Infrastructure;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityServices;
using VContainer;

namespace Gameplay.UI
{
    public class UnityServicesUIHandler : MonoBehaviour
    {
        private ISubscriber<UnityServiceErrorMessage> _serviceErrorSubscriber;


        [Inject]
        private void InjectDependenciesAndInitialize(ISubscriber<UnityServiceErrorMessage> serviceErrorSubscriber)
        {
            _serviceErrorSubscriber = serviceErrorSubscriber;
            _serviceErrorSubscriber.Subscribe(ServiceErrorHandler);
        }

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
        private void OnDestroy()
        {
            if (_serviceErrorSubscriber != null)
                _serviceErrorSubscriber.Unsubscribe(ServiceErrorHandler);
        }

        private void ServiceErrorHandler(UnityServiceErrorMessage error)
        {
            switch (error.AffectedService)
            {
                case UnityServiceErrorMessage.Service.Session:
                {
                    HandleSessionError(error);
                    break;
                }
                case UnityServiceErrorMessage.Service.Authentication:
                {
                    PopupManager.ShowPopupPanel(
                        "Authentication Error",
                        $"{error.OriginalException.Message}\nTip: You can still use the Direct IP connection option.");
                    break;
                }
                default:
                {
                    PopupManager.ShowPopupPanel("Service Error: " + error.Title, error.Message);
                    break;
                }
            }
        }


        private void HandleSessionError(UnityServiceErrorMessage error)
        {
            if (error.OriginalException is not System.AggregateException { InnerException: SessionException sessionException })
                return; // We're only wanting to handle AggregateExceptions containing SessionExceptions.

            switch (sessionException.Error)
            {
                case SessionError.SessionNotFound:
                    PopupManager.ShowPopupPanel("Session Not Found", "Requested Session not found. The join code is incorrect or the session has ended.");
                    break;
                case SessionError.NotAuthorized:
                    PopupManager.ShowPopupPanel("Session Error", "Received HTTP error 401: Unauthorized; from Session Service.");
                    break;
                case SessionError.MatchmakerAssignmentTimeout:  // This can occur while using Quick Join.
                    PopupManager.ShowPopupPanel("Session Error", "Received HTTP error 408: Request Timed Out; from Session Service.");
                    break;
                default:
                    PopupManager.ShowPopupPanel("Unknown Error", sessionException.Message);
                    break;
            }
        }
    }
}