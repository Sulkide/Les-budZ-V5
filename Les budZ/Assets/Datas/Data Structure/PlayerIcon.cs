using UnityEngine;

[CreateAssetMenu(menuName = "Les budZ/Player Icon", fileName = "NewPlayerIcon")]
public class PlayerIcon : ScriptableObject
{
    [System.Serializable]
    public class IconCategory
    {
        public Sprite[] frames;
        [Min(0.01f)] public float frameTime = 0.12f;
    }

    [Header("Animations d'icône")]
    public IconCategory idle;
    public IconCategory attacking;
    public IconCategory damage;
    public IconCategory dead;
}