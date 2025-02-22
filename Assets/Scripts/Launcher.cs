﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{
	public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
	[SerializeField] TMP_Text errorText;
	[SerializeField] TMP_Text roomNameText;
	[SerializeField] Transform roomListContent;
	[SerializeField] GameObject roomListItemPrefab;
	[SerializeField] Transform playerListContent;
	[SerializeField] GameObject playerListItemPrefab;
	[SerializeField] GameObject mapListContent;
	[SerializeField] TMP_Text mapSelectedText;
	[SerializeField] GameObject startGameButton;
	[SerializeField] TMP_Text nicknameText;
	[SerializeField] TMP_InputField nicknameInputField;
	[SerializeField] GameObject nicknameGUIContainer;
	[SerializeField] GameObject nicknameContainer;
	[SerializeField] GameObject botListContent;
	[SerializeField] TMP_Text botCountText;

	private Dictionary<string, RoomInfo> cachedRoomList;
	private Dictionary<string, GameObject> roomListEntries;
	private Dictionary<int, GameObject> playerListEntries;

	private int level = 1;

	void Awake()
	{
		Instance = this;
		cachedRoomList = new Dictionary<string, RoomInfo>();
		roomListEntries = new Dictionary<string, GameObject>();

		try
		{
			// temporary fix for deaths and kills not being zeroed after a match
			PhotonNetwork.LocalPlayer.CustomProperties.Clear();
		} 
		catch (System.Exception e)
		{
			Debug.Log(e);
		}
	}

	// Start is called before the first frame update
	void Start()
    {
		if (PhotonNetwork.CurrentRoom != null)
		{
			// open room menu with room info and player list prefabs
			MenuManager.Instance.OpenMenu("Loading");
			OnJoinedRoom();
			return;
		}

		Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    // Callback called by Photon on successful connection to the master server
	public override void OnConnectedToMaster()
	{
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
		PhotonNetwork.AutomaticallySyncScene = true;
    }

	public override void OnJoinedLobby()
	{
		// temporary fix for deaths and kills not being zeroed after a match
		PhotonNetwork.LocalPlayer.CustomProperties.Clear();

		MenuManager.Instance.OpenMenu("Main");
        Debug.Log("Joined Lobby");

		// whenever this joins a new lobby, clear any previous room lists
		cachedRoomList.Clear();
		ClearRoomListView();

        if (PhotonNetwork.LocalPlayer.NickName.Length <= 1)
            SetNickname("Player " + Random.Range(0, 1000).ToString("0000"));
        else
            SetNickname(PhotonNetwork.LocalPlayer.NickName);
    }

	public override void OnLeftLobby()
	{
		cachedRoomList.Clear();
		ClearRoomListView();
	}

	public void CreateRoom()
	{
        if (string.IsNullOrEmpty(roomNameInputField.text))
		{
            return;
		}
        PhotonNetwork.CreateRoom(roomNameInputField.text);
        MenuManager.Instance.OpenMenu("Loading");
	}

	public void JoinRoom(RoomInfo info)
	{
		PhotonNetwork.JoinRoom(info.Name);
		MenuManager.Instance.OpenMenu("Loading");
	}

	public override void OnJoinedRoom()
	{
		// joining (or entering) a room invalidates any cached lobby room list (even if LeaveLobby was not called due to just joining a room)
		cachedRoomList.Clear();

		PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "RoomManager"), Vector3.zero, Quaternion.identity);

		roomNameText.text = PhotonNetwork.CurrentRoom.Name;
		MenuManager.Instance.OpenMenu("Room");
		PhotonNetwork.CurrentRoom.SetCustomProperties(PhotonNetwork.CurrentRoom.CustomProperties);

		Player[] players = PhotonNetwork.PlayerList;

		if (playerListEntries == null)
		{
			playerListEntries = new Dictionary<int, GameObject>();
		}

		foreach (Player p in PhotonNetwork.PlayerList)
		{
			GameObject entry = Instantiate(playerListItemPrefab, playerListContent);
			entry.GetComponent<PlayerListItem>().SetUp(p);

			playerListEntries.Add(p.ActorNumber, entry);
		}

		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
		mapListContent.SetActive(PhotonNetwork.IsMasterClient);
		botListContent.SetActive(PhotonNetwork.IsMasterClient);
		if (PhotonNetwork.CurrentRoom.CustomProperties["bots"] != null)
			botCountText.text = "Bot Count: " + (int)PhotonNetwork.CurrentRoom.CustomProperties["bots"];
		else
			botCountText.text = "Bot Count: 0";
	}

	public void ReturnToRoomMenu()
	{

	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
		mapListContent.SetActive(PhotonNetwork.IsMasterClient);
		botListContent.SetActive(PhotonNetwork.IsMasterClient);
		if (PhotonNetwork.CurrentRoom.CustomProperties["bots"] != null)
			botCountText.text = "Bot Count: " + (int)PhotonNetwork.CurrentRoom.CustomProperties["bots"];
		else
			botCountText.text = "Bot Count: 0";
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		errorText.text = "Room creation failed: " + message;
		MenuManager.Instance.OpenMenu("Error");
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		errorText.text = "Room join failed: " + message;
		MenuManager.Instance.OpenMenu("Error");
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		MenuManager.Instance.OpenMenu("Loading");
	}

	public override void OnLeftRoom()
	{
		MenuManager.Instance.OpenMenu("Main");

		foreach (GameObject entry in playerListEntries.Values)
		{
			Destroy(entry.gameObject);
		}

		playerListEntries.Clear();
		playerListEntries = null;
		botCountText.text = "Bot Count: 0";
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		ClearRoomListView();

		UpdateCachedRoomList(roomList);
		UpdateRoomListView();
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		GameObject entry = Instantiate(playerListItemPrefab, playerListContent);
		entry.GetComponent<PlayerListItem>().SetUp(newPlayer);

		playerListEntries.Add(newPlayer.ActorNumber, entry);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		Destroy(playerListEntries[otherPlayer.ActorNumber].gameObject);
		playerListEntries.Remove(otherPlayer.ActorNumber);
	}

	public void StartGame()
	{
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.LoadLevel(level);
	}

	public void LeaveGame()
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LoadLevel(0);
	}

	private void ClearRoomListView()
	{
		foreach (GameObject entry in roomListEntries.Values)
		{
			Destroy(entry.gameObject);
		}

		roomListEntries.Clear();
	}

	private void UpdateCachedRoomList(List<RoomInfo> roomList)
	{
		foreach (RoomInfo info in roomList)
		{
			// Remove room from cached room list if it got closed, became invisible or was marked as removed
			if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
			{
				if (cachedRoomList.ContainsKey(info.Name))
				{
					cachedRoomList.Remove(info.Name);
				}

				continue;
			}

			// Update cached room info
			if (cachedRoomList.ContainsKey(info.Name))
			{
				cachedRoomList[info.Name] = info;
			}
			// Add new room info to cache
			else
			{
				cachedRoomList.Add(info.Name, info);
			}
		}
	}

	private void UpdateRoomListView()
	{
		foreach (RoomInfo info in cachedRoomList.Values)
		{
			GameObject entry = Instantiate(roomListItemPrefab, roomListContent);
			entry.GetComponent<RoomListItem>().SetUp(info);

			roomListEntries.Add(info.Name, entry);
		}
	}

	public void SelectMap(int level)
	{
		this.level = level;
		mapSelectedText.text = "Map " + level + " Selected!";
	}

	public void OnClickQuit()
	{
		Application.Quit();
	}


	public void OpenChangeNicknameContainer()
	{
		nicknameGUIContainer.SetActive(true);
		nicknameContainer.SetActive(false);
	}

	public void CloseChangeNicknameContainer()
	{
		nicknameGUIContainer.SetActive(false);
		nicknameContainer.SetActive(true);
		nicknameInputField.text = "";
	}

	public void OnChangeNicknameSubmit()
	{
		if (nicknameInputField.text == "" || nicknameInputField.text.Length > 11)
			return;
		SetNickname(nicknameInputField.text);
		CloseChangeNicknameContainer();
	}

	private void SetNickname(string nickname)
	{
		PhotonNetwork.NickName = nickname;
		nicknameText.text = "Nickname: " + PhotonNetwork.NickName;
	}

	public void AddBot()
	{
		Hashtable hash;
		hash = PhotonNetwork.CurrentRoom.CustomProperties;
		int botCount = (int)hash["bots"];
		if (botCount == 10)
			return;
		botCount++;
		hash.Remove("bots");
		hash.Add("bots", botCount);
		PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
		botCountText.text = "Bot Count: " + (int)PhotonNetwork.CurrentRoom.CustomProperties["bots"];
	}

	public void RemoveBot()
	{
		Hashtable hash;
		hash = PhotonNetwork.CurrentRoom.CustomProperties;
		int botCount = (int)hash["bots"];
		if (botCount == 0)
			return;
		botCount--;
		hash.Remove("bots");
		hash.Add("bots", botCount);
		PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
		botCountText.text = "Bot Count: " + (int)PhotonNetwork.CurrentRoom.CustomProperties["bots"];
	}
}
