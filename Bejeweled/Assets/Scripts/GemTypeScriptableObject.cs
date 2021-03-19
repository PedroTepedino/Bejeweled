using UnityEngine;

[CreateAssetMenu(menuName = "Gem", fileName = "NewGem", order = 0)]
public class GemTypeScriptableObject : ScriptableObject
{
    public int Type;
    public Sprite Sprite;
}
