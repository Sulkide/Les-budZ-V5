using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    
    [Header("Player information")]
    public List<PlayerMovement3D> allPlayers = new List<PlayerMovement3D>();
    public bool is3d;
    public int nextPlayerID = 1;
    
    [Header("main camera")]
    public Camera mainCamera;
    public Transform targetTransform;
    public Vector3 targetOffset = new Vector3(0f, 3f, -5f); 
    public float targetSmoothTime = 0.15f;
    private Vector3 targetVelocity;
    public CameraFlip3D2D cameraFlip3D;
    
    [Header("Time options")]
    private float elapsedTime = 0f;      
    public string gameTime = "00:00:00";
    public TMP_Text gameTimeText;
    public string realTime = "00:00:00";
    public TMP_Text realTimeText;
    private int lastSecond = -1;
    
    [Header("Current scene information")]
    public string currentSceneName;
    
 
    
    [Header("Versus (Party Timer)")]
    public bool useVersusTimer = false;
    public float versusMatchDuration = 170f;
    [NonSerialized] public bool versusMatchStarted = false;
    [NonSerialized] public bool versusMatchFinished = false;
    private float versusTimer = 0f;
    public float VersusTimer => versusTimer;
    


    public event System.Action<bool> OnDimensionChanged;

    
    void Start()
    {
        
        if (realTimeText == null)
        {
            return;
        }

        UpdateDisplay(DateTime.Now);
        
        StartCoroutine(RefreshLoop());
        
        System.Collections.IEnumerator RefreshLoop()
        {
            while (true)
            {
                DateTime now = DateTime.Now; 
                if (now.Second != lastSecond)
                {
                    UpdateDisplay(now);
                    lastSecond = now.Second;
                }
                
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;

    }
    

    private void Update()
    {
        UpdateVersusTimer();
        CheckRestartVotes();
        UpdatePlayTime();


    }
    private void LateUpdate()
    {
        UpdateCameraTargetPosition();
    }

    #region GENERAL

    public int AssignePlayerID()
    {
        
        nextPlayerID++;
        int assignedID = nextPlayerID;
        
        if (useVersusTimer && !versusMatchStarted)
        {
            int playerCount = assignedID; 

            if (playerCount >= 3)
            {
                StartVersusMatch();
            }
        }

        return assignedID;
    }
    
    private void UpdatePlayTime()
    {
        elapsedTime += Time.deltaTime;

        TimeSpan ts = TimeSpan.FromSeconds(elapsedTime);
        int hours   = ts.Hours;
        int minutes = ts.Minutes;
        int seconds = ts.Seconds;

        gameTime = $"{hours:00}:{minutes:00}:{seconds:00}";
        //gameTimeText.text = gameTime;
    }
    
    void UpdateDisplay(DateTime dt)
    {
        string formatted = dt.ToString("HH:mm:ss");
        realTime = formatted;
        //realTimeText.text = formatted;
    }
    
    public void RegisterPlayer(PlayerMovement3D p)
    {
        if (!allPlayers.Contains(p))
            allPlayers.Add(p);
    }

    #endregion

    #region VERSUS MODE

    public void CheckRestartVotes()
    {
        if (!useVersusTimer || !versusMatchFinished)
            return;
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        int totalPlayers   = 0;
        int votesForRestart = 0;

        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
        if (connectedClients != null && connectedClients.Count > 0)
        {
            foreach (var client in connectedClients)
            {
                var playerObj = client.PlayerObject;
                if (playerObj == null) 
                    continue;

                var p = playerObj.GetComponentInChildren<PlayerMovement3D>();
                if (p == null) 
                    continue;
                if (!p.IsSpawned) 
                    continue;

                totalPlayers++;

                if (p.restartVote.Value)
                    votesForRestart++;
            }
        }
        else
        {
            PlayerMovement3D[] allPlayers = FindObjectsOfType<PlayerMovement3D>();
            foreach (var p in allPlayers)
            {
                if (p == null) continue;
                if (!p.IsSpawned) continue;

                totalPlayers++;
                if (p.restartVote.Value)
                    votesForRestart++;
            }
        }

        Debug.Log($"[Versus] Votes restart : {votesForRestart}/{totalPlayers}");
        
        if (totalPlayers > 0 && votesForRestart == totalPlayers)
        {
            RestartVersusMatch();
        }
    }

    
    public void SyncVersusStateFromServer(float timerValue)
    {
        versusTimer = timerValue;
        versusMatchStarted = true;
        versusMatchFinished = false;
    }




    
    public void RestartVersusMatch()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        Debug.Log("[Versus] All players voted -> restarting match.");
        
        versusTimer = versusMatchDuration;
        versusMatchStarted = true;
        versusMatchFinished = false;
        
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        foreach (var p in allPlayers)
        {
            if (p == null) continue;
            if (!p.IsSpawned) continue;

            p.SyncVersusStateClientRpc(versusTimer);

            p.cannotMove = false;
            p.SetCannotMoveClientRpc(false);

            p.restartVote.Value = false;
            p.scoreVersus.Value = 0;
            p.hasSuperCollectible.Value = false;
            p.isDead.Value = false;

            p.Respawn();
        }
    }

    private void UpdateVersusTimer()
    {
        if (!useVersusTimer) return;
        if (!versusMatchStarted || versusMatchFinished) return;

        versusTimer -= Time.deltaTime;
        if (versusTimer <= 0f)
        {
            versusTimer = 0f;
            EndVersusMatch();
        }
    }
    
    public void StartVersusMatch()
    {
        versusTimer = versusMatchDuration;
        versusMatchStarted = true;
        versusMatchFinished = false;
        Debug.Log("[Versus] Partie lancée, durée = " + versusMatchDuration + "s");
    }

    private void EndVersusMatch()
    {
        if (versusMatchFinished) return;

        versusMatchFinished = true;
        versusTimer = 0f;
        Debug.Log("[Versus] Fin de partie, calcul du classement.");
        
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i] != null)
            {
                allPlayers[i].cannotMove = true;
            }
        }
        
    }
    
    public string GetFormattedVersusTimer()
    {
        float clamped = Mathf.Max(0f, versusTimer);
        TimeSpan ts = TimeSpan.FromSeconds(clamped);
        return $"{ts.Minutes:00}:{ts.Seconds:00}";
    }
    
    #endregion
    
    #region CAMERA
    private void UpdateCameraTargetPosition()
    {
        if (targetTransform == null)
        {
            Debug.Log("camera cannot find target !!!");
            return;
        }

        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (var player in allPlayers)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                sum += player.transform.position;
                count++;
            }
        }

        if (count == 0)
            return;

        Vector3 center = sum / count;

        Vector3 desiredPosition = center + targetOffset;
        
        targetTransform.position = Vector3.SmoothDamp(
            targetTransform.position,
            desiredPosition,
            ref targetVelocity,
            targetSmoothTime
        );
    }

    public void RegisterCameraOption(Camera camera, CameraFlip3D2D flip)
    {
        mainCamera = camera;
        cameraFlip3D =  flip;
    }

    #endregion
    
    #region DIMENSION MODE
    
    public void ChangeDimensionState(bool is3DValue)
    {
        is3d = is3DValue;
        OnDimensionChanged?.Invoke(is3d);
    }
    
    public void ChangeDimension()
    {
        if (is3d)
        {
            cameraFlip3D.Flip3Dto2D();
        }
        else
        {
            cameraFlip3D.Flip2Dto3D();
        }

    }
    
    #endregion
}