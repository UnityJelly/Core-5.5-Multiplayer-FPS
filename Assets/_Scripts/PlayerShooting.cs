﻿using UnityEngine;
using UnityEngine.Networking;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] float shotCooldown = .3f;
    [SerializeField] int killsToWin = 5; // TODO Increase or switch over to timer system
    [SerializeField] Transform firePosition;
    [SerializeField] ShotEffectsManager shotEffects;

    [SyncVar (hook = "OnScoreChanged")] int score;

    Player player;
    float elapsedTime;
    bool canShoot;

    void Start()
    {
        player = GetComponent<Player>();
        shotEffects.Initialize();

        if (isLocalPlayer)
            canShoot = true;
    }

    [ServerCallback]
    void OnEnable()
    {
        score = 0;
    }

    void Update()
    {
        if (!canShoot)
            return;

        elapsedTime += Time.deltaTime;

        if (Input.GetButtonDown("Fire1") && elapsedTime > shotCooldown)
        {
            elapsedTime = 0f;
            CmdFireShot(firePosition.position, firePosition.forward);
        }
    }

    [Command]
    void CmdFireShot(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;

        Ray ray = new Ray(origin, direction);
        Debug.DrawRay(ray.origin, ray.direction * 3f, Color.red, 1f);

        bool result = Physics.Raycast(ray, out hit, 50f);

        if (result)
        {
            PlayerHealth enemy = hit.transform.GetComponent<PlayerHealth>();

            if (enemy != null)
            {
                bool wasKillShot = enemy.TakeDamage();
                if (wasKillShot && ++score >= killsToWin)
                    player.Won();
            }
        }

        RpcProcessShotEffects(result, hit.point);
    }

    [ClientRpc]
    void RpcProcessShotEffects(bool playImpact, Vector3 point)
    {
        shotEffects.PlayShotEffects();

        if (playImpact)
            shotEffects.PlayImpactEffect(point);
    }

    void OnScoreChanged(int value)
    {
        score = value;
        if(isLocalPlayer)
            PlayerCanvas.canvas.SetKills(value);
    }

    public void FireAsBot()
    {
        CmdFireShot(firePosition.position, firePosition.forward);
    }
}