using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Messages;
using Gameplay.MultiplayerChat.Text;
using Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Gameplay.DebugCheats
{
    /// <summary>
    ///     Handles debug cheat events, applying them on the server and logging them on all clients.<br/>
    ///     This class is only avaliable in the Editor or on Development Builds.
    /// </summary>
    // Based off of the following sources:
    //  - 'https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/DebugCheats/DebugCheatsManager.cs'
    //  - 'https://github.com/BenPyton/cheatconsole'
    //  - Dapper Dino: 'https://www.youtube.com/watch?v=usShGWFLvUk'
    public class DebugCheatManager : NetworkSingleton<DebugCheatManager>
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD

        public const char COMMAND_MESSAGE_IDENTIFIER = '/';
        private static Dictionary<string, ConsoleCommand> s_consoleCommands = new()
        {
            { "Log", new LogCommand() },
            { "ToggleMouse", new ToggleMouseLockCommand() },
            { "Kill", new KillPlayerCommand() },
            { "SetGameTimeRemaining", new SetGameTimeRemainingCommand() },
        };


        [Inject]
        private IPublisher<CheatUsedMessage> _cheatUsedMessagePublisher;



        public void PerformCheat(string cheatText)
        {
            // Perform initial checks on the client to preserve bandwidth.
            // These checks will be re-performed on the server to ensure security.

            if (IsEmptyCommand(cheatText))  // Check that the command isn't empty.
            {
                HandleEmptyCommand(); // Notify the user.
                return;
            }
            if (!IsValidCommand(cheatText, out string identifier))  // Check that the command is valid.
            {
                HandleUnknownCommand("/" + identifier);   // Notify the user.
                return;
            }

            ProcessCheatServerRpc(cheatText);
        }
        



        [Rpc(SendTo.Server)]
        private void ProcessCheatServerRpc(string command, RpcParams rpcParams = default)
        {   
            // While Error Handling occurs on the client, we also want to do a quick check on the server so that we aren't blindly trusting them.
            // Check for Empty Command.
            if (IsEmptyCommand(command))
            {
                // The Command was Command.
                CheatInvalidResponseRpc(RpcTarget.Group(new ulong[] { rpcParams.Receive.SenderClientId }, RpcTargetUse.Temp));
                return;
            }

            // Parse the command to get its identifier and parameters.
            ParseCommand(command, out string identifier, out string[] parameters);

            // Check that our identifier matches one of our commands (Note: Case Sensitive).
            if (!IsValidCommand(identifier))
            {
                // The Command was Invalid.
                CheatInvalidResponseRpc(RpcTarget.Group(new ulong[] { rpcParams.Receive.SenderClientId }, RpcTargetUse.Temp));
                return;
            }

            // The command is valid. Process it.
            ProcessCommand(rpcParams.Receive.SenderClientId, identifier, parameters);
        }


        #region Validation Functions

        /// <summary>
        ///     Returns true if the command is fully empty or only contains the command identifier.
        /// </summary>
        private static bool IsEmptyCommand(string cheatText) => string.IsNullOrWhiteSpace(cheatText) || cheatText.Length == 1;
        /// <summary>
        ///     Returns true if the passed text is a command (Starts with the Command starts with the identifier).
        /// </summary>
        public static bool IsCommand(string command) => command.StartsWith(COMMAND_MESSAGE_IDENTIFIER);
        /// <summary>
        ///     Returns true if the command is valid (The identifier matches one of our console commands).
        /// </summary>
        private static bool IsValidCommand(string command, out string identifier)
        {
            identifier = command.Remove(0, 1).Split(' ')[0];
            return s_consoleCommands.ContainsKey(identifier);
        }
        /// <inheritdoc cref="IsValidCommand(string, out string)"/>
        private static bool IsValidCommand(string identifier) => s_consoleCommands.ContainsKey(identifier);
        /// <summary>
        ///     Attempt to parse the given command string, returning true if the parse was successful or not.
        /// </summary>
        private static bool ParseCommand(string command, out string identifier, out string[] parameters)
        {
            string[] splitCheat = command
                .Remove(0, 1)   // Remove the initial '/'
                .Split(' ');    // Split by spaces

            identifier = splitCheat[0];
            parameters = splitCheat.Skip(1).ToArray();

            return !string.IsNullOrWhiteSpace(identifier);
        }

        #endregion

        /// <summary>
        ///     Process the given command on the Server and perform it.
        /// </summary>
        private bool ProcessCommand(ulong triggeringClientId, string identifier, string[] parameters)
        {
            if (!s_consoleCommands.TryGetValue(identifier, out var consoleCommand))
            {
                // Unknown Cheat.
                HandleUnknownCommand(identifier);
                return false;
            }

            // Check Parameter Validity (Count).
            if (!consoleCommand.CheckParameterCount(parameters.Length))
            {
                HandleInvalidParameterCount(parameters.Length, identifier);
                return false;
            }

            // Check Parameter Validity (Value).
            for(int i = 0; i < parameters.Length; ++i)
            {
                if (!consoleCommand.TestParameter(i, parameters[i]))
                {
                    HandleInvalidParameter(consoleCommand.GetParameterName(i), parameters[i]);
                    return false;
                }
            }

            // The Cheat is Valid.
            if (consoleCommand.RunOnTriggeringClient)
            {
                string parameterString = "";
                for(int i = 0; i < parameters.Length; ++i)
                    parameterString += parameters[i] + ",";
                SendCommandToClientRpc(identifier, parameterString, RpcTarget.Group(new ulong[] { triggeringClientId }, RpcTargetUse.Temp));
            }
            else
            {
                consoleCommand.Process(triggeringClientId, parameters);
            }
            return true;
        }
        

        /// <summary>
        ///     Notify the specified client that a valid client-side command has been performed.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        public void SendCommandToClientRpc(string identifier, string parameterString, RpcParams rpcParams = default)
        {
            // Retrieve the command's parameters from the passed parameterString.
            string[] parameters = string.IsNullOrWhiteSpace(parameterString) ? null : parameterString.Split(',');
            // Process the client-side command.
            s_consoleCommands[identifier].Process(NetworkManager.LocalClientId, parameters);
        }



#region Command Response RPCs

        [Rpc(SendTo.SpecifiedInParams)]
        private void CheatInvalidResponseRpc(RpcParams rpcParams = default) => HandleInvalidCommandSent();
        private void HandleInvalidCommandSent()
        {
            ChatManager.Instance.ReceiveChatMessage(null, "An Invalid Cheat passed checks on this client but was caught by the Server");
        }
        private void HandleUnknownCommand(string identifier)
        {
            ChatManager.Instance.ReceiveChatMessage(null, $"Unknown Cheat Used: {identifier}");
        }
        private void HandleInvalidParameter(string parameterName, string parameter)
        {
            ChatManager.Instance.ReceiveChatMessage(null, $"'{parameter}' is invalid for parameter {parameterName}");
        }
        private void HandleInvalidParameterCount(int actualParamCount, string identifier)
        {
            ChatManager.Instance.ReceiveChatMessage(null, $"{actualParamCount} is an invalid number of parameters for command '/{identifier}'");
        }
        private void HandleEmptyCommand()
        {
            ChatManager.Instance.ReceiveChatMessage(null, $"Empty Cheat Used");
        }

#endregion

#endif
    }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public abstract class ConsoleCommand
    {
        /// <summary>
        ///     Returns true if the passed parameter count is valid for this command.
        /// </summary>
        public abstract bool CheckParameterCount(int count);
        /// <summary>
        ///     Returns true if the parameter string is valid for the given parameter index.
        /// </summary>
        public abstract bool TestParameter(int paramIndex, string parameterAsString);
        /// <summary>
        ///     Returns the name of the parameter for the given parameter index.
        /// </summary>
        public abstract string GetParameterName(int paramIndex);


        /// <summary>
        ///     If true, run on the Triggering Client once processed by the server. If false, run on the Server itself.
        /// </summary>
        public virtual bool RunOnTriggeringClient => false;


        /// <summary>
        ///     A
        /// </summary>
        public abstract void Process(ulong triggeringClientId, string[] parameters);
    }
    /// <summary>
    ///     Console Command which displays a log message on all Clients.
    /// </summary>
    public class LogCommand : ConsoleCommand
    {
        public override bool CheckParameterCount(int count) => count > 0;
        public override bool TestParameter(int paramIndex, string parameterAsString) => true;
        public override string GetParameterName(int paramIndex) => "Message";

        public override void Process(ulong triggeringClientId, string[] parameters)
        {
            string logMessage = string.Join(' ', parameters);

            ChatManager.Instance.SendChatMessage($"Client {triggeringClientId}", logMessage);
        }
    }
    /// <summary>
    ///     Client-Side Console Command which toggles the Cursor Lock State between 'Locked' and 'None'
    /// </summary>
    public class ToggleMouseLockCommand : ConsoleCommand
    {
        public override bool CheckParameterCount(int count) => count == 0;
        public override bool TestParameter(int paramIndex, string parameterAsString) => false;
        public override string GetParameterName(int paramIndex) => "Null";

        public override bool RunOnTriggeringClient => true; // Only perform on the triggering client.

        public override void Process(ulong triggeringClientId, string[] parameters)
        {
            CursorLockMode newLockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.lockState = newLockState;

            ChatManager.Instance.ReceiveChatMessage(null, newLockState == CursorLockMode.Locked ? "Mouse Cursor Locked" : "Mouse Cursor Unlocked");
        }
    }
    /// <summary>
    ///     Console Command which kills a player based on their name or a special identifier.
    /// </summary>
    public class KillPlayerCommand : ConsoleCommand
    {
        public override bool CheckParameterCount(int count) => count == 1;
        public override bool TestParameter(int paramIndex, string parameterAsString) => true;
        public override string GetParameterName(int paramIndex) => paramIndex == 1 ? "Player Name" : "Invalid";

        private const string KILL_ALL_PARAMETER = "@a";
        private const string KILL_ALL_PLAYERS_PARAMETER = "@p";
        private const string KILLSELF_PARAMETER = "@s";


        public override void Process(ulong triggeringClientId, string[] parameters)
        {
            // Get Inflicer ServerCharacter.
            Gameplay.GameplayObjects.Character.ServerCharacter inflicter = GameObject.FindObjectsByType<Gameplay.GameplayObjects.Players.Player>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.OwnerClientId == triggeringClientId).ServerCharacter;


            if (parameters[0] == KILL_ALL_PARAMETER)
            {
                foreach(var target in GameObject.FindObjectsByType<Gameplay.GameplayObjects.Character.ServerCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    target.NetworkHealthComponent.SetLifeState_Server(null, GameplayObjects.Character.LifeState.Dead);
                    ChatManager.Instance.SendChatMessage(null, $"{inflicter.CharacterName} killed {target.CharacterName}");
                }
            }
            else if (parameters[0] == KILL_ALL_PLAYERS_PARAMETER)
            {
                foreach (var targetPlayer in GameObject.FindObjectsByType<Gameplay.GameplayObjects.Players.Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    targetPlayer.ServerCharacter.NetworkHealthComponent.SetLifeState_Server(null, GameplayObjects.Character.LifeState.Dead);
                    ChatManager.Instance.SendChatMessage(null, $"{inflicter.CharacterName} killed {targetPlayer.ServerCharacter.CharacterName}");
                }
            }
            else if (parameters[0] == KILLSELF_PARAMETER)
            {
                // Check through our Players to find the triggering player (There will never be more players than ServerCharacters, but there may be less, so checking players should be slightly more performant)
                Gameplay.GameplayObjects.Character.ServerCharacter target = GameObject.FindObjectsByType<Gameplay.GameplayObjects.Players.Player>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.OwnerClientId == triggeringClientId).ServerCharacter;

                // Kill the triggering player.
                target.NetworkHealthComponent.SetLifeState_Server(null, GameplayObjects.Character.LifeState.Dead);
                ChatManager.Instance.SendChatMessage(null, $"{inflicter.CharacterName} killed themselves");
            }
            else
            {
                // Get Target ServerCharacter.
                Gameplay.GameplayObjects.Character.ServerCharacter target = GameObject.FindObjectsByType<Gameplay.GameplayObjects.Character.ServerCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => string.Concat(t.CharacterName.Split(' ')) == parameters[0]);
                if (target == null)
                    return; // Find a way to log this.

                target.NetworkHealthComponent.SetLifeState_Server(null, GameplayObjects.Character.LifeState.Dead);
                ChatManager.Instance.SendChatMessage(null, $"{inflicter.CharacterName} killed {target.CharacterName}");
            }
        }
    }
    /// <summary>
    ///     Console Command which sets the remaining time left in a match.
    /// </summary>
    public class SetGameTimeRemainingCommand : ConsoleCommand
    {
        public override bool CheckParameterCount(int count) => count == 1;
        public override bool TestParameter(int paramIndex, string parameterAsString) => float.TryParse(parameterAsString, out float _);
        public override string GetParameterName(int paramIndex) => paramIndex == 1 ? "Server Time" : "Invalid";

        public override void Process(ulong triggeringClientId, string[] parameters)
        {
            if (!float.TryParse(parameters[0], out float desiredTime))
                return; // Find a way to log this.

            GameObject.FindAnyObjectByType<Gameplay.GameState.ServerFreeForAllState>().GetComponent<Utils.NetworkTimer>().SetTimerRemainingTime(desiredTime);

            // Log the Cheat.
            Gameplay.GameplayObjects.Character.ServerCharacter inflicter = GameObject.FindObjectsByType<Gameplay.GameplayObjects.Players.Player>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.OwnerClientId == triggeringClientId).ServerCharacter;
            ChatManager.Instance.SendChatMessage(null, $"{inflicter.CharacterName} set the remaining game time to {desiredTime}");
        }
    }

#endif
}