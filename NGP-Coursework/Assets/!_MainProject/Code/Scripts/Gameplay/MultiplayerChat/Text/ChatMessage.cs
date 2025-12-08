using TMPro;
using UnityEngine;

namespace Gameplay.MultiplayerChat.Text
{
    public class ChatMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;

        public void SetChatText(string senderName, string message)
        {
            if (string.IsNullOrWhiteSpace(senderName))
                _messageText.text = message;    // Server Log Message.
            else
                _messageText.text = $"{senderName}: {message}";
        }
    }
}