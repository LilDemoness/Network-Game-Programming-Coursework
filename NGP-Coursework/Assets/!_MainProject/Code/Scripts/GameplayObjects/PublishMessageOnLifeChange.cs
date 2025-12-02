using Gameplay.GameplayObjects.Character;
using Gameplay.GameState;
using Gameplay.Messages;
using Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.GameplayObjects
{
    /// <summary>
    ///     Server-only components which publishes a message once the LifeState changes.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthComponent), typeof(ServerCharacter))]
    public class PublishMessageOnLifeChange : NetworkBehaviour
    {
        private NetworkHealthComponent _networkHealthComponent;
        private ServerCharacter _serverCharacter;


        [Inject]
        private IPublisher<LifeStateChangedEventMessage> _publisher;


        private void Awake()
        {
            _networkHealthComponent = GetComponent<NetworkHealthComponent>();
            _serverCharacter = GetComponent<ServerCharacter>();
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            _networkHealthComponent.OnLifeStateChanged += OnLifeStateChanged;

            GameStateBehaviour gameState = FindAnyObjectByType<GameStateBehaviour>();
            if (gameState != null)
                gameState.Container.Inject(this);
        }


        private void OnLifeStateChanged(ulong? inflicterObjectId, LifeState lifeState)
        {
            _publisher.Publish(new LifeStateChangedEventMessage(
                _serverCharacter.NetworkObjectId,
                inflicterObjectId,
                lifeState,
                _serverCharacter.CharacterName
            ));
        }
    }
}