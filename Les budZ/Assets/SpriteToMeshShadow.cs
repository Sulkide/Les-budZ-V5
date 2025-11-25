using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChildSpriteFollower : MonoBehaviour
{
    [Header("Références")]
    public SpriteRenderer parentSpriteRenderer;   // si null, on prend celui du GameObject

    [Header("Enfant généré")]
    public string childName = "ChildSprite";
    private Vector3 localOffset = new Vector3(0f, 0f, 0.1f);

    [Tooltip("Décalage du Sorting Order par rapport au parent")]
    public int sortingOrderOffset = -1;

    [Header("Matérial de l'enfant")]
    public bool overrideMaterial = false;
    public Material childMaterialOverride;        // utilisé seulement si overrideMaterial = true

    private SpriteRenderer childSpriteRenderer;

    // pour éviter de setter le sprite à chaque frame si rien ne change
    private Sprite _lastSprite;
    private bool _lastFlipX;
    private bool _lastFlipY;

    private void Awake()
    {
        if (parentSpriteRenderer == null)
            parentSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        CreateChild();
        SyncSpriteNow();  // synchro initiale
    }

    private void CreateChild()
    {
        // Crée le GameObject enfant
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform);
        child.transform.localPosition = localOffset;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;   // ou transform.localScale si tu veux copier

        // Ajoute le SpriteRenderer
        childSpriteRenderer = child.AddComponent<SpriteRenderer>();

        // Copie les infos de tri
        childSpriteRenderer.sortingLayerID = parentSpriteRenderer.sortingLayerID;
        childSpriteRenderer.sortingOrder = parentSpriteRenderer.sortingOrder + sortingOrderOffset;

        // Matérial
        if (overrideMaterial && childMaterialOverride != null)
        {
            childSpriteRenderer.material = childMaterialOverride;
        }
        else
        {
            // même material que le parent
            childSpriteRenderer.material = parentSpriteRenderer.material;
        }
    }

    private void LateUpdate()
    {
        if (parentSpriteRenderer == null || childSpriteRenderer == null)
            return;

        // Si le sprite ou les flips changent, on met à jour
        if (parentSpriteRenderer.sprite != _lastSprite
            || parentSpriteRenderer.flipX != _lastFlipX
            || parentSpriteRenderer.flipY != _lastFlipY)
        {
            SyncSpriteNow();
        }
    }

    private void SyncSpriteNow()
    {
        _lastSprite = parentSpriteRenderer.sprite;
        _lastFlipX = parentSpriteRenderer.flipX;
        _lastFlipY = parentSpriteRenderer.flipY;

        childSpriteRenderer.sprite = _lastSprite;
        childSpriteRenderer.flipX = _lastFlipX;
        childSpriteRenderer.flipY = _lastFlipY;

        // si tu veux aussi copier la couleur du parent, décommente :
        // childSpriteRenderer.color = parentSpriteRenderer.color;
    }
}
