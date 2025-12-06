using TMPro;
using UnityEngine;

namespace Gameplay.MultiplayerChat.Text
{
    public class ChatMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;

        public void SetChatText(string senderName, string message) => _messageText.text = $"{senderName}: {message}";
    }
}