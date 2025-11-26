using UnityEngine;

namespace Gameplay.Configuration
{
    /// <summary>
    ///     Data storage for all the valid strings used to create a player's name.
    /// </summary>
    // Note: Change to allow full customisation with these strings used for randomisation.
    [CreateAssetMenu(menuName = "GameData/NameGeneration", order = 2)]
    public class NameGenerationData : ScriptableObject
    {
        [Tooltip("The list of all possible strings the game can use as the first word of a player name.")]
        public string[] FirstWordList;

        [Tooltip("The list of all possible strings the game can use as the second word of a player name.")]
        public string[] SecondWordList;


        public string GenerateRandomName()
        {
            string firstWord = FirstWordList[Random.Range(0, FirstWordList.Length)];
            string secondWord = SecondWordList[Random.Range(0, SecondWordList.Length)];
            return firstWord + " " + secondWord;
        }
    }
}