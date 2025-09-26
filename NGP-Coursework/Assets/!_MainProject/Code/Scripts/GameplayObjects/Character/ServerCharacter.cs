using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    public class ServerCharacter : NetworkBehaviour
    {

    }
}