using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> { }

public class Player : NetworkBehaviour
{
    [SyncVar (hook = "OnNameChanged")] public string playerName;
    [SyncVar(hook = "OnColorChanged")] public Color playerColor;

    [SerializeField] ToggleEvent onToggleShared;
    [SerializeField] ToggleEvent onToggleLocal;
    [SerializeField] ToggleEvent onToggleRemote;
    [SerializeField] float respawnTime = 5f;

    static List<Player> players = new List<Player>();


    GameObject mainCamera;
    NetworkAnimator anim; 

    void Start()
    {
        anim = GetComponent<NetworkAnimator>();
        mainCamera = Camera.main.gameObject;

        EnablePlayer();
    }

    [ServerCallback]
    void OnEnable()
    {
        {
            if (!players.Contains(this))
                players.Add(this);
        }
    }

    [ServerCallback]
    void OnDisable()
    {
        if (players.Contains(this))
            players.Remove(this);
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        anim.animator.SetFloat("Speed", Input.GetAxis("Vertical"));
        anim.animator.SetFloat("Strafe", Input.GetAxis("Horizontal"));
    }

    void DisablePlayer()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.HideReticule();
            mainCamera.SetActive(true);
        }

        onToggleShared.Invoke(false);

        if (isLocalPlayer)
            onToggleLocal.Invoke(false);
        else
            onToggleRemote.Invoke(false);
    }

    void EnablePlayer()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.Initialize();
            mainCamera.SetActive(false);
        }

        onToggleShared.Invoke(true);

        if (isLocalPlayer)
            onToggleLocal.Invoke(true);
        else
            onToggleRemote.Invoke(true);
    }

    public void Die()
    {
        if (isLocalPlayer || playerControllerId == -1)
            anim.SetTrigger("Died");
            PlayerCanvas.canvas.PlayDeathAudio();

        if (isLocalPlayer)
        {
            PlayerCanvas.canvas.WriteGameStatusText("You Died HAHA!");
            PlayerCanvas.canvas.PlayDeathAudio();
            anim.SetTrigger("Died");
        }

        DisablePlayer();

        Invoke("Respawn", respawnTime);
    }

    void Respawn()
    {
        if (isLocalPlayer || playerControllerId == -1)
            anim.SetTrigger("Restart");

        if (isLocalPlayer)
        {
            Transform spawn = NetworkManager.singleton.GetStartPosition();
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;

            anim.SetTrigger("Restart");
        }

        EnablePlayer();
    }

    void OnNameChanged(string value)
    {
        playerName = value;
        gameObject.name = playerName;
        GetComponentInChildren<Text>(true).text = playerName;
    }

    void OnColorChanged(Color value)
    {
        playerColor = value;
        GetComponentInChildren<RendererToggler>().ChangeColor(playerColor);
    }

    [Server]
    public void Won()
    {
        for (int i = 0; i < players.Count; i++)
            players[i].RpcGameOver(netId, name);

        Invoke("BackToLobby", 5f);
    }

    [ClientRpc]
    void RpcGameOver(NetworkInstanceId networkID, string name)
    {
        DisablePlayer();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isLocalPlayer)
        {
            if (netId == networkID)
                PlayerCanvas.canvas.WriteGameStatusText("You Won!");
            else
                PlayerCanvas.canvas.WriteGameStatusText("Game Over!\n" + name + " Won");
        }
    }

    void BackToLobby()
    {
        FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby();
    }
}