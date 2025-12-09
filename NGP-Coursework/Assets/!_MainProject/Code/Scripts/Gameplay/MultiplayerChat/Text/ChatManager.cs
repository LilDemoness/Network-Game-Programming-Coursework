using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Players;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UserInput;
using Utils;

namespace Gameplay.MultiplayerChat.Text
{
    public class ChatManager : NetworkSingleton<ChatManager>
    {
        [SerializeField] private PersistentPlayerRuntimeCollection _persistentPlayerCollection;
        private PersistentPlayer _persistentPlayer;


        [Space(10)]
        [SerializeField] private CanvasGroup _chatMessagesCanvasGroup;
        [SerializeField] private CanvasGroup _chatInputCanvasGroup;

        [Space(10)]
        [SerializeField] private ChatMessage _chatMessagePrefab;
        [SerializeField] private Transform _chatMessageContainer;
        private Coroutine _fadeMainTextChatCoroutine;


        [Header("Chat Input")]
        [SerializeField] private TMP_InputField _chatInput;
        private bool _isCapturingInput = false;

        private const ClientInput.ActionTypes ALL_ACTIONS_BUT_CHAT = ClientInput.ActionTypes.Everything & ~ClientInput.ActionTypes.MultiplayerChat;


        protected override void Awake()
        {
            base.Awake();

            SetChatInputVisibility(false);
            _chatMessagesCanvasGroup.alpha = 0.0f;
        }
        public override void OnNetworkSpawn()
        {
            ClientInput.OnOpenChatPerformed += OpenChatInput;
        }
        public override void OnNetworkDespawn()
        {
            ClientInput.OnOpenChatPerformed -= OpenChatInput;
        }


        #region Opening/Closing

        public void OpenChatInput()
        {
            if (_isCapturingInput)
                return; // Chat Input is already open.
            _isCapturingInput = true;   // Mark the chat as open.

            // Show the Input and Main Chat.
            SetChatInputVisibility(true);
            StopMainTextChatFade();

            // Subscribe to Submission Events.
            ClientInput.OnSubmitChatPerformed += SubmitChat;
            ClientInput.OnCancelChatPerformed += CancelChat;

            // Select the Input Field.
            // We are deselecting and then reselecting the next frame to prevent an error with having the input field selected but the user being unable to interact with it.
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(ReselectInputAfterFrame());

            // Prevent Unrelated Input.
            ClientInput.PreventActions(typeof(ChatManager), ALL_ACTIONS_BUT_CHAT);
        }
        public void CloseChatInput()
        {
            if (!_isCapturingInput)
                return; // Chat Input is already closed.
            _isCapturingInput = false;  // Mark the chat as closed.

            // Hide the Input Box & Start the Main Chat Fading.
            SetChatInputVisibility(false);
            StartMainTextChatFade();

            // Unsubscribe from Submission Events.
            ClientInput.OnSubmitChatPerformed -= SubmitChat;
            ClientInput.OnCancelChatPerformed -= CancelChat;

            // Allow Unrelated Input.
            ClientInput.RemoveActionPrevention(typeof(ChatManager), ALL_ACTIONS_BUT_CHAT);
        }
        private void SetChatInputVisibility(bool isVisible)
        {
            _chatInputCanvasGroup.alpha = isVisible ? 1.0f : 0.0f;
            _chatInputCanvasGroup.blocksRaycasts = isVisible;
        }

        private IEnumerator ReselectInputAfterFrame() { yield return null; EventSystem.current.SetSelectedGameObject(_chatInput.gameObject); }

        #endregion

        /// <summary>
        ///     Submit the contents of '_chatInput' for processing and close the chat input window.
        /// </summary>
        public void SubmitChat()
        {
            SendChatMessage();
            CloseChatInput();
        }
        /// <summary>
        ///     Close the current chat input without clearing or processing the contents of '_chatInput'
        /// </summary>
        public void CancelChat() => CloseChatInput();


        /// <summary>
        ///     Process the contents of the '_chatInput' InputField and, if valid, send to the server for relaying to other clients.
        /// </summary>
        public void SendChatMessage()
        {
            if (string.IsNullOrEmpty(_chatInput.text))
            {
                // Invalid Input.
                CloseChatInput();
                _chatInput.text = "";
                return;
            }
            if (_persistentPlayer == null && !_persistentPlayerCollection.TryGetPlayer(NetworkManager.LocalClientId, out _persistentPlayer))
                throw new System.Exception($"No PersistentPlayer found for client {NetworkManager.LocalClientId}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD

            if (Gameplay.DebugCheats.DebugCheatManager.IsCommand(_chatInput.text))
            {
                // Our input is a cheat.
                if (Gameplay.DebugCheats.DebugCheatManager.Instance != null)
                    Gameplay.DebugCheats.DebugCheatManager.Instance.PerformCheat(_chatInput.text);  // Instance Exists: Perform Cheat as Normal.
                else
                    ReceiveChatMessage(null, "Cheats Unavailable while DebugCheatManager is Uninitialised. Try again during gameplay"); // No Instance: Warn the user.

                _chatInput.text = "";   // Clear the input box for the next message.
                return;
            }

#endif
            // Send our message to the server which will then notify all clients.
            SendChatMessageServerRpc(_persistentPlayer.NetworkNameState.Name.Value, _chatInput.text);
            _chatInput.text = "";   // Clear the input box for the next message.
        }
        /// <summary>
        ///     Send a chat message directly without any checks, using the supplied name & message contents strings.
        /// </summary>
        public void SendChatMessage(string name, string message)
        {
            SendChatMessageServerRpc(name ?? "", message);
            _chatInput.text = "";
        }

        /// <summary>
        ///     Send a Chat Message to the Server to be Processed and Relayed to Clients.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SendChatMessageServerRpc(FixedPlayerName senderName, string message) => ReceiveChatMessageClientRpc(senderName, message);
        /// <summary>
        ///     Send a Chat Message to all Clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        private void ReceiveChatMessageClientRpc(FixedPlayerName senderName, string message) => ReceiveChatMessage(senderName, message);

        /// <summary>
        ///     Process a received Chat Message on this client.
        /// </summary>
        public void ReceiveChatMessage(string name, string message)
        {
            AddMessage(name, message);
            StartMainTextChatFade();
        }


        /// <summary>
        ///     Instantiate & Setup a Chat Message
        /// </summary>
        private void AddMessage(string name, string message)
        {
            ChatMessage chatMessage = Instantiate<ChatMessage>(_chatMessagePrefab, _chatMessageContainer);
            chatMessage.SetChatText(name, message);
        }


        #region Main Chat Fading

        /// <summary>
        ///    Fade the main text chat after a short delay.
        /// </summary>
        private void StartMainTextChatFade()
        {
            StopMainTextChatFade();
            _fadeMainTextChatCoroutine = StartCoroutine(FadeMainTextChat());
        }
        /// <summary>
        ///    Stop the current fade of main text chat and set its alpha to 1.
        /// </summary>
        private void StopMainTextChatFade()
        {
            if (_fadeMainTextChatCoroutine != null)
                StopCoroutine(_fadeMainTextChatCoroutine);

            // Show our chat messages to prevent issues with them not showing.
            _chatMessagesCanvasGroup.alpha = 1.0f;
        }

        private readonly WaitForSeconds WAIT_TO_START_FADING = new WaitForSeconds(4.0f);
        private const float FADE_DURATION = 0.5f;
        /// <summary>
        ///    Fade the main text chat after a short delay.
        /// </summary>
        private IEnumerator FadeMainTextChat()
        {
            yield return WAIT_TO_START_FADING;

            // Perform the fade over our desired duration.
            float fadeRate = 1.0f / FADE_DURATION;
            while (_chatMessagesCanvasGroup.alpha > 0.0f)
            {
                _chatMessagesCanvasGroup.alpha -= fadeRate * Time.deltaTime;
                yield return null;
            }

            // Ensure that our chat messages are fully hidden by the end of the fade operation.
            _chatMessagesCanvasGroup.alpha = 0.0f;
        }

        #endregion
    }
}