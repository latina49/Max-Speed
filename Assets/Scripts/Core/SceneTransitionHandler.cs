using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class SceneTransitionHandler : NetworkBehaviour
{
    static public SceneTransitionHandler sceneTransitionHandler { get; internal set; }

    public string DefaultMainMenu = "StartMenu";

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler OnClientLoadedScene;

    [HideInInspector]
    public delegate void SceneStateChangedDelegateHandler(SceneStates newState);
    [HideInInspector]
    public event SceneStateChangedDelegateHandler OnSceneStateChanged;

    private int _numberOfClientLoaded;
    
    public enum SceneStates
    {
        Init,
        Start,
        Lobby,
        Ingame
    }

    private SceneStates _sceneState;

    /// <summary>
    /// Làm cho nó di nhất nếu có phiên bản khác thì hủy.
    /// Set our scene state to INIT
    /// </summary>
    private void Awake()
    {
        if(sceneTransitionHandler != this && sceneTransitionHandler != null)
        {
            GameObject.Destroy(sceneTransitionHandler.gameObject);
        }
        sceneTransitionHandler = this;
        SetSceneState(SceneStates.Init);
        DontDestroyOnLoad(this);
       

    }

    /// <summary>
    ///  Đặt scene hiện tại để giúp chuyển dễ dàng sau này
    /// </summary>
    public void SetSceneState(SceneStates sceneState)
    {
        _sceneState = sceneState;
        if(OnSceneStateChanged != null)
        {
            OnSceneStateChanged.Invoke(_sceneState);
        }
    }

    public SceneStates GetCurrentSceneState()
    {
        return _sceneState;
    }

    /// <summary>
    /// Khởi chạy scene default
    /// </summary>
    private void Start()
    {
        if(_sceneState == SceneStates.Init)
        {
            SceneManager.LoadScene(DefaultMainMenu);
           // NetworkManager.Singleton.StartServer();
        }
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    /// <summary>
    /// Đăng ký event khi scene đã load xong
    /// </summary>
    public void RegisterCallbacks()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    public void SwitchScene(string scenename)
    {
        if(NetworkManager.Singleton.IsListening)
        {
            _numberOfClientLoaded = 0;
            NetworkManager.Singleton.SceneManager.LoadScene(scenename, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(scenename);
        }
    }
    //khi load xong + số client connect và invoke nó tới event khác bắt sự kiện
    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        _numberOfClientLoaded += 1;
        OnClientLoadedScene?.Invoke(clientId);
    }

    public bool AllClientsAreLoaded()
    {
        return _numberOfClientLoaded == NetworkManager.Singleton.ConnectedClients.Count;
    }

    /// <summary>
    /// ExitAndLoadStartMenu
    /// </summary>
    public void ExitAndLoadStartMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
        
        OnClientLoadedScene = null;
        SetSceneState(SceneStates.Start);
        SceneManager.LoadScene(1);
    }

    private void OnClientDisconnectCallback(ulong id)
    {
        if (IsClient)
        {
            Application.Quit();
        }
    }
}