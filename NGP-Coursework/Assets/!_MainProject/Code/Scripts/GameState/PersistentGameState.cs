using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace Gameplay.GameState
{
    /// <summary>
    ///     A class containing some data that needs to be passed between states to represent the session's win state.<br/>
    /// </summary>
    public class PersistentGameState
    {
        public GameMode GameMode { get; set; }

        private PersistentDataContainer _test;


        public void SetContainer<T>() where T : PersistentDataContainer, new() => _test = new T();
        public void SetContainer<T>(T newValue) where T : PersistentDataContainer => _test = newValue;
        public T GetContainer<T>() where T : PersistentDataContainer => _test as T;

        public void AssertContainerType<T>() where T : PersistentDataContainer
        {
            Debug.Assert(_test != null, $"We are trying to check the container type of {nameof(_test)} but it is still unset");
            Debug.Assert(_test.GetType() != typeof(T), $"Container Types Don't Match (Desired: '{typeof(T).ToString()}'. Actual: '{_test.GetType().ToString()}')");
        }


        public void Reset()
        {
            _test = null;
        }
    }


    public abstract class PersistentDataContainer
    { }
    public class FFAPersistentData : PersistentDataContainer
    {
        public FFAPostGameData[] GameData { get; set; }
    }
    public class TDMPersistentData : PersistentDataContainer
    {
        public int Test { get; set; }
    }
}