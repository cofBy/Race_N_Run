using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class playerSpawner : NetworkBehaviour
{
    [Header("References")]
    public NetworkObject[] playersPrefaps;
    public lobby lobbyLogic;

    private HashSet<ulong> spawnedClients = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            StartCoroutine(spawnDelayed());
        }
    }

    IEnumerator spawnDelayed()
    {
        yield return null;
        Debug.Log("[Cleint] send");
        spawnServerRpc(lobbyLogic.choosenCharacter.value, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void spawnServerRpc(int prefabIndex, ulong clientId)
    {
        Debug.Log($"[Server] Received");
        if (spawnedClients.Contains(clientId))
        {
            Debug.LogWarning($"Spawn request denied. Client {clientId} already has a character.");
            return;
        }

        // Validate prefab bounds
        if (prefabIndex < 0 || prefabIndex >= playersPrefaps.Length)
        {
            Debug.LogError($"Client {clientId} requested an out-of-bounds prefab index: {prefabIndex}");
            return;
        }

        spawnedClients.Add(clientId);

        NetworkObject playerInstance = Instantiate(playersPrefaps[prefabIndex]);
        playerInstance.SpawnAsPlayerObject(clientId);
    }
}