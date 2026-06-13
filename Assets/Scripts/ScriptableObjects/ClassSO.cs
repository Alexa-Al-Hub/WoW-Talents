using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewClass", menuName = "Scriptable Objects/Class")]
public class ClassSO : ScriptableObject
{
    public string ClassName;
    public Sprite ClassIcon;
    public List<TalentTreeSO> Trees;
}
