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

    


    /// <summary>
    ///     Play Anticipated Start HitEffects on the calling client.
    /// </summary>
    public static void PlayHitEffectsOnSelfAnticipate(Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientStart(null, hitPoint, hitNormal);
        }
    }
    /// <summary>
    ///     Play 'Update' HitEffects on the calling client.<br/>
    ///     Also triggers Hit Visuals if the client is the action's triggering client.
    /// </summary>
    /// <param name="isTriggeringClient"> Is this client the client that triggered the action this is being called from?</param>
    public static void PlayHitEffectsOnSelf(bool isTriggeringClient, Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientUpdate(null, hitPoint, hitNormal);
        }

        if (isTriggeringClient)
            Instance.ShowHitEffectVisual(hitPoint);
    }
    /// <summary>
    ///     Calls a RPC to play the Update effect on the action's triggering client.
    /// </summary>
    public static void PlayHitEffectsOnTriggeringClient(ulong triggeringClientId, Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId)
        => Instance.PlayHitEffectsOnTriggeringClientRpc(hitPoint, hitNormal, chargePercentage, actionId, Instance.RpcTarget.Group( new ulong[] { triggeringClientId }, RpcTargetUse.Temp));
    /// <summary>
    ///     Triggers the OnClientUpdate function of the passed action's HitVisuals,
    ///     along with triggering the TriggeringClient-Specific Hit Effects (E.g. HitMarkers).
    /// </summary>
    /// <remarks>
    ///     SpecifiedInParams as this object is owned by the Server, not the client who triggered the attack.
    /// </remarks>
    [Rpc(SendTo.SpecifiedInParams)]
    public void PlayHitEffectsOnTriggeringClientRpc(Vector3 hitPoint, Vector3 hitNormal, float chargePercentage, ActionID actionId, RpcParams rpcParams = default)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for (int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientUpdate(null, hitPoint, hitNormal);
        }

        ShowHitEffectVisual(hitPoint);
    }
    /// <summary>
    ///     Calls an RPC to play the Update effect on all clients BUT the action's triggering client.
    /// </summary>
    public static void PlayHitEffectsOnNonTriggeringClients(ulong triggeringClientId, in ActionHitInformation hitInfo, float chargePercentage, ActionID actionId)
    {
        List<ulong> clientIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        clientIds.Remove(triggeringClientId);
        Instance.PlayHitEffectsClientRpc(new NetworkActionHitInformation(hitInfo), chargePercentage, actionId, Instance.RpcTarget.Group(clientIds.ToArray(), RpcTargetUse.Temp));
    }
    /// <summary>
    ///     Triggers the OnClientUpdate function of the passed action's HitVisuals.
    /// </summary>
    [Rpc(SendTo.SpecifiedInParams)]
    private void PlayHitEffectsClientRpc(NetworkActionHitInformation hitInfo, float chargePercentage, ActionID actionId, RpcParams rpcParams = default)
    {
        ActionDefinition definition = GameDataSource.Instance.GetActionDefinitionByID(actionId);
        for(int i = 0; i < definition.HitVisuals.Length; ++i)
        {
            definition.HitVisuals[i].OnClientUpdate(null, hitInfo.HitPoint, hitInfo.HitNormal);
        }
    }

    /// <summary>
    ///     Spawn a HitMarker at the specified position.
    /// </summary>
    public void ShowHitEffectVisual(Vector3 hitPosition)
    {
        // Change to only perform if we are hitting an enemy?
        HitMarker hitMarker = _hitMarkerPool.Get();
        hitMarker.Setup(hitPosition, _hitMarkerPool);
    }
    public void PlayHitEffectAudio()
    {
        if (_hitEffectAudioClip != null)
            _hitEffectAudioSource.PlayOneShot(_hitEffectAudioClip);
    }
}