﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
	public static GameManager Instance;
    GameObject controller;
	int botCount;

	private void Awake()
	{
		Instance = this;
		botCount = (int)PhotonNetwork.CurrentRoom.CustomProperties["bots"];
	}

	private void Start()
	{
		SpawnHazards();
		for (int i = 0; i < botCount; i++)
		{
			if (PhotonNetwork.IsMasterClient)
				SpawnAI();
		}
	}

	void SpawnHazards()
	{
        HazardSpawnPoint[] spawnPoints = SpawnManager.Instance.GetHazardSpawnPoints();

        // Iterate through hazard spawn points
        foreach (HazardSpawnPoint spawnPoint in spawnPoints) {

            if (Random.value < 0.6) {
                PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "ExplosiveBarrel"), spawnPoint.transform.position, Quaternion.identity);
            }
            else {
                PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "ExplosiveBattery"), spawnPoint.transform.position, Quaternion.identity);
            }
            

        }
        
	}


    IEnumerator RespawnHazard(Vector3 position) {
        yield return new WaitForSeconds(10);
        PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "ExplosiveBarrel"), position, Quaternion.identity);
    }
    

    // used to spawn an AI character...
    void SpawnAI()
    {
		// instantiate AI controller
		Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();
		GameObject bot = PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "AI"), spawnPoint.position, spawnPoint.rotation);
		string id = bot.GetComponent<AIScript>().GetId();
		Hashtable hash = PhotonNetwork.MasterClient.CustomProperties;

		hash.Add(id + "_kills", 0);
		hash.Add(id + "_deaths", 0);
		PhotonNetwork.MasterClient.SetCustomProperties(hash);
	}

	public void DestroyHazard(GameObject hazard)
	{
        StartCoroutine(RespawnHazard(hazard.transform.position));
		PhotonNetwork.Destroy(hazard);
	}

	public void DestroyAI(GameObject AI)
	{
		PhotonNetwork.Destroy(AI);
		Invoke("SpawnAI", 3);
	}

	
	public override void OnLeftRoom()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		SceneManager.LoadScene(0);
	}
	

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (!PhotonNetwork.IsMasterClient)
			return;
		
		Hashtable hash = PhotonNetwork.MasterClient.CustomProperties;
		AIScript[] bots = FindObjectsOfType<AIScript>();

		foreach (AIScript bot in bots)
		{
			hash.Add(bot.GetId() + "_kills", bot.GetKills());
			hash.Add(bot.GetId() + "_deaths", bot.GetDeaths());
		}

		PhotonNetwork.MasterClient.SetCustomProperties(hash);
	}

	public void UpdatePlayerKills(Player shooter)
	{
		PlayerManager[] playerManagers = FindObjectsOfType<PlayerManager>();
		foreach (PlayerManager p in playerManagers)
		{
			if (p.gameObject.GetPhotonView().Owner == PhotonNetwork.MasterClient)
				p.UpdateKills(shooter);
		}
	}
}
