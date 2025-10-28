using System.Collections.Generic;
using UnityEngine;

namespace VisualEffects
{
    [CreateAssetMenu(menuName = "Lists/SpecialFXList")]
    public class SpecialFXList : ScriptableObject
    {
        [System.NonSerialized]
        private static SpecialFXList s_allOptionsDatabase;
        public static SpecialFXList AllOptionsDatabase
        {
            get => s_allOptionsDatabase ??= Resources.Load<SpecialFXList>(ALL_OPTIONS_DATABASE_PATH);
        }
        private const string ALL_OPTIONS_DATABASE_PATH = "Lists/AllSpecialFXList";


        [SerializeField] private SpecialFXGraphic[] _specialFXGraphics;
        public SpecialFXGraphic[] SpecialFXGraphics => _specialFXGraphics;
        [System.NonSerialized] private Dictionary<SpecialFXGraphic, int> _graphicPrefabToIndexDict;


        private void InitialiseGraphicPrefabToIndexDict()
        {
            _graphicPrefabToIndexDict = new Dictionary<SpecialFXGraphic, int>();
            for (int i = 0; i < SpecialFXGraphics.Length; ++i)
            {
                _graphicPrefabToIndexDict.Add(SpecialFXGraphics[i], i);
            }
        }

        public SpecialFXGraphic GetSpecialFXGraphic(int index) => IsWithinBounds(index, SpecialFXGraphics.Length) ? SpecialFXGraphics[index] : null;
        public int GetIndexForSpecialFXGraphic(SpecialFXGraphic specialFXGraphic)
        {
            if (_graphicPrefabToIndexDict == null)
                InitialiseGraphicPrefabToIndexDict();
            return _graphicPrefabToIndexDict[specialFXGraphic];
        }

        private bool IsWithinBounds(int value, int arrayLength)
        {
            return value >= 0 && value < arrayLength;
        }
    }
}