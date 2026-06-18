using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Scriptable Objects/Character")]
    public class CharacterSO : ScriptableObject
    {
        [Header("Main data")]
        public string CharacterName;
        public Sprite CharacterIcon;
        public List<TalentTreeSO> Trees;
    }
}
