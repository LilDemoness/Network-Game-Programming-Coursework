using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Players;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

            SetChatInputVisibility(true);
            StopMainTextChatFade();
            _isCapturingInput = true;

            // Subscribe to Submission Events.
            ClientInput.OnSubmitChatPerformed += SubmitChat;
            ClientInput.OnCancelChatPerformed += CancelChat;

            // Select Text Box.
            EventSystem.current.SetSelectedGameObject(_chatInput.gameObject);

            // Prevent Unrelated Input.
            ClientInput.PreventActions(typeof(ChatManager), ALL_ACTIONS_BUT_CHAT);
        }
        public void CloseChatInput()
        {
            if (!_isCapturingInput)
                return; // Chat Input is already closed.

            SetChatInputVisibility(false);
            StartMainTextChatFade();
            _isCapturingInput = false;

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

        #endregion

        public void SubmitChat()
        {
            SendChatMessage();
            CloseChatInput();
        }
        public void CancelChat() => CloseChatInput();


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


            SendChatMessageServerRpc(_persistentPlayer.NetworkNameState.Name.Value, _chatInput.text);
            _chatInput.text = "";
        }

        [Rpc(SendTo.Server)]
        private void SendChatMessageServerRpc(FixedPlayerName senderName, string message) => ReceiveChatMessageClientRpc(senderName, message);
        [Rpc(SendTo.ClientsAndHost)]
        private void ReceiveChatMessageClientRpc(FixedPlayerName senderName, string message) => ReceiveChatMessage(senderName, message);

        public void ReceiveChatMessage(string name, string message)
        {
            AddMessage(name, message);
            StartMainTextChatFade();
        }


        private void AddMessage(string name, string message)
        {
            ChatMessage chatMessage = Instantiate<ChatMessage>(_chatMessagePrefab, _chatMessageContainer);
            chatMessage.SetChatText(name, message);
        }


        #region Main Chat Fading

        private void StartMainTextChatFade()
        {
            StopMainTextChatFade();
            _fadeMainTextChatCoroutine = StartCoroutine(FadeMainTextChat());
        }
        private void StopMainTextChatFade()
        {
            if (_fadeMainTextChatCoroutine != null)
                StopCoroutine(_fadeMainTextChatCoroutine);

            _chatMessagesCanvasGroup.alpha = 1.0f;
        }

        private readonly WaitForSeconds WAIT_TO_START_FADING = new WaitForSeconds(4.0f);
        private const float FADE_DURATION = 0.5f;
        private IEnumerator FadeMainTextChat()
        {
            yield return WAIT_TO_START_FADING;

            float fadeRate = 1.0f / FADE_DURATION;
            while (_chatMessagesCanvasGroup.alpha > 0.0f)
            {
                _chatMessagesCanvasGroup.alpha -= fadeRate * Time.deltaTime;
                yield return null;
            }

            _chatMessagesCanvasGroup.alpha = 0.0f;
        }

        #endregion
    }
}