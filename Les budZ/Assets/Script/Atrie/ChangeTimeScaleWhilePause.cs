using System.Collections.Generic;
using UnityEngine;


public class ChangeTimeScaleWhilePause : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private int newLayer = 0;

    [SerializeField] private List<Transform> cachedTransforms = new List<Transform>();
    [SerializeField] private List<int> originalLayers = new List<int>();
    
    
    [SerializeField] private List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();
    [SerializeField] private List<Transform> gameobjectToDisable = new List<Transform>();

    private Rigidbody _ogRb;


    void Start()
    {
        _ogRb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (GameManager.instance != null)
            GameManager.instance.OnPauseChanged += OnPauseChanged;
    }

    private void OnDisable()
    {
        if (GameManager.instance != null)
            GameManager.instance.OnPauseChanged -= OnPauseChanged;

    }

    private void OnPauseChanged(bool paused)
    {

        if (_ogRb != null)
        {
            _ogRb.isKinematic = paused;
        }

        if (scriptsToDisable.Count > 0)
        {
            foreach (MonoBehaviour script in scriptsToDisable)
            {
                script.enabled = !paused;
            }
        }

        if (gameobjectToDisable.Count > 0)
        {
            foreach (Transform transform in gameobjectToDisable)
            {
                transform.gameObject.SetActive(!paused);
            }
        }
        

        if (paused)
        {
            cachedTransforms.Clear();
            originalLayers.Clear();

            CollectRecursive(transform);
                
            for (int i = 0; i < cachedTransforms.Count; i++)
            {
                if (cachedTransforms[i] == null) continue;
                cachedTransforms[i].gameObject.layer = newLayer;
            }
        }
        else
        {
            int count = Mathf.Min(cachedTransforms.Count, originalLayers.Count);

            for (int i = 0; i < count; i++)
            {
                var t = cachedTransforms[i];
                if (t == null) continue;

                int oldLayer = originalLayers[i];
                if (oldLayer < 0 || oldLayer > 31) continue;

                t.gameObject.layer = oldLayer;
            }

            cachedTransforms.Clear();
            originalLayers.Clear();
        }

    }
    private void CollectRecursive(Transform root)
    {
        cachedTransforms.Add(root);
        originalLayers.Add(root.gameObject.layer);
        
        for (int i = 0; i < root.childCount; i++)
        {
            CollectRecursive(root.GetChild(i));
        }
    }
}