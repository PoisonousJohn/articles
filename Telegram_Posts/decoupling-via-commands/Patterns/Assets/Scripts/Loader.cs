using Zenject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loader : MonoBehaviour {

    [Inject]
    public void Init(IGameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    private void Awake()
    {
        Debug.Log("Loading started");
        _gameStateManager.Load();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Quitting application");
        _gameStateManager.Save();
    }

    private IGameStateManager _gameStateManager;
}
