using System;
using System.Collections.Generic;
using Gameplay.Actions;
using Gameplay.Actions.Definitions;
using Gameplay.Actions.Effects;
using Gameplay.Actions.HitEffects;
using Gameplay.Actions.Visuals;
using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class HitEffectManager : NetworkSingleton<HitEffectManager>
{
    [SerializeField] private Transform _hitMarkerContainer;
    [SerializeField] private HitMarker _hitEffectVisualPrefab;
    private ObjectPool<HitMarker> _hitMarkerPool;


    [SerializeField] private AudioClip _hitEffectAudioClip;
    [SerializeField] private AudioSource _hitEffectAudioSource;


    protected override void Awake()
    {
        base.Awake();
        _hitMarkerPool = new ObjectPool<HitMarker>(CreateHitMarkerInstance, HitMarkerPool_OnGet, HitMarkerPool_OnRelease);
    }
    private HitMarker CreateHitMarkerInstance() => Instantiate<HitMarker>(_hitEffectVisualPrefab, _hitMarkerContainer);
    private void HitMarkerPool_OnGet(HitMarker hitMarker) => hitMarker.gameObject.SetActive(true);
    private void HitMarkerPool_OnRelease(HitMarker hitMarker) => hitMarker.gameObject.SetActive(false);


    public override void OnNetworkSpawn()
    {
        NetworkHealthComponent.OnAnyHealthChange += NetworkHealthComponent_OnAnyHealthChange;
    }
    public override void OnNetworkDespawn()
    {
        NetworkHealthComponent.OnAnyHealthChange -= NetworkHealthComponent_OnAnyHealthChange;
    }


    private void NetworkHealthComponent_OnAnyHealthChange(NetworkHealthComponent.AnyHealthChangeEventArgs args)
    {
        if (args.Inflicter.OwnerClientId == NetworkManager.LocalClientId)
            PlayHitEffectAudio();
    }

    



    public static void PlayHitEffectsOnSelfAnticipate(Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientStart(null, hitPoint, hitNormal);
        }
    }
    public static void PlayHitEffectsOnSelf(bool isOwner, Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientStart(null, hitPoint, hitNormal);
        }

        if (isOwner)
            Instance.ShowHitEffectVisual(hitPoint);
    }
    public static void PlayHitEffectsOnTriggeringClient(ulong triggeringClientId, Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
        => Instance.PlayHitEffectsOnOwnerRpc(hitPoint, hitNormal, chargePercentage, actionId, Instance.RpcTarget.Group( new ulong[] { triggeringClientId }, RpcTargetUse.Temp));
    [Rpc(SendTo.SpecifiedInParams)] // SpecifiedInParams as this object is owned by the Server, not the client who triggered the attack.
    public void PlayHitEffectsOnOwnerRpc(Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId, RpcParams rpcParams = default)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientUpdate(null, hitPoint, hitNormal);
        }

        ShowHitEffectVisual(hitPoint);
    }
    public static void PlayHitEffectsOnNonTriggeringClients(ulong triggeringClientId, in ActionHitInformation hitInfo, float chargePercentage, ActionID actionId)
    {
        List<ulong> clientIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        clientIds.Remove(triggeringClientId);
        Instance.PlayHitEffectsClientRpc(new NetworkActionHitInformation(hitInfo), chargePercentage, actionId, Instance.RpcTarget.Group(clientIds.ToArray(), RpcTargetUse.Temp));
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void PlayHitEffectsClientRpc(NetworkActionHitInformation hitInfo, float chargePercentage, ActionID actionId, RpcParams rpcParams = default)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for(int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientUpdate(null, hitInfo.HitPoint, hitInfo.HitNormal);
        }
    }

    public static void SpawnHitEffect(Vector3 hitPosition) => Debug.Log("Visual Displayed at " + hitPosition);
    public void ShowHitEffectVisual(Vector3 hitPosition)
    {
        HitMarker hitMarker = _hitMarkerPool.Get();
        hitMarker.Setup(hitPosition, _hitMarkerPool);

        Debug.Log("Visual Displayed at " + hitPosition);
    }
    private System.Collections.IEnumerator DisableAfterDelay(float delay, GameObject objectToDisable)
    {
        yield return new WaitForSeconds(delay);
        objectToDisable.SetActive(false);
    }
    public void PlayHitEffectAudio()
    {
        if (_hitEffectAudioClip != null)
            _hitEffectAudioSource.PlayOneShot(_hitEffectAudioClip);
    }
}