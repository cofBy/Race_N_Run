using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class lobby : MonoBehaviour
{
    [Header("choosing name")]
    public TMP_InputField nameInput;
    public GameObject createJoinPanel;
    public GameObject namePanel;
    public Button doneChoosing;

    [Header("making lobbys UI")]
    public Button makeLobby;
    public TMP_InputField lobbyName;
    public Toggle isPublic;

    [Header("Joining lobbies")]
    public Button joinButton;
    public TMP_InputField codeInput;

    public Button quickJoinButton;

    [Header("searching for lobbys UI")]
    public float timeBetweenRefreshes;
    public TMP_InputField searchBar;
    bool isReady = false;
    bool isQuerying = false;
    float searchCooldown = 0f;

    [Header("showing existing lobbies")]
    public Button lobbyObject;
    public GameObject lobbiesParent;

    List<Button> spawnedLobbies = new List<Button>();

    [Header("heartBeeting")]
    Lobby hostLobby;
    Lobby joinedLobby;

    float beatingTimer;
    float pollTimer;

    [Header("displaying the lobby")]
    public GameObject lobbyPanel;
    public TMP_Dropdown mapSelection;
    public TextMeshProUGUI selectedMap;

    public GameObject playerCard;
    List<GameObject> allPlayerCards = new List<GameObject>();

    public Transform playerCardsParent;

    int lastMap = -1;
    bool isPolling = false;

    private void Awake()
    {
        createJoinPanel.SetActive(false);
        namePanel.SetActive(true);

        makeLobby.onClick.AddListener(creatLobby);
        joinButton.onClick.AddListener(() => joinLobby(codeInput.text, false));
        quickJoinButton.onClick.AddListener(quickJoin);
        doneChoosing.onClick.AddListener(() => { createJoinPanel.SetActive(true); namePanel.SetActive(false); });

        beatingTimer = 0;
        pollTimer = 0;
    }

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        isReady = true;
        await listLobbies();
    }

    void Update()
    {
        sendHeartBeat();
        sendPoll();
        displayLobby();

        searchCooldown -= Time.deltaTime;
        if (searchCooldown < 0)
        {
            _ = listLobbies();
            searchCooldown = timeBetweenRefreshes;
        }
    }

    async void sendHeartBeat()
    {
        if (hostLobby != null)
        {
            beatingTimer -= Time.deltaTime;

            if (beatingTimer < 0)
            {
                beatingTimer = 3f;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    async void sendPoll()
    {
        if (joinedLobby == null || isPolling) return;

        pollTimer -= Time.deltaTime;
        if (pollTimer < 0)
        {
            pollTimer = 1f;
            isPolling = true;
            try
            {
                Lobby l = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = l;
            }
            catch (LobbyServiceException exc)
            {
                Debug.LogWarning(exc);
            }
            finally
            {
                isPolling = false;
            }
        }
    }

    void displayLobby()
    {
        lobbyPanel.SetActive(joinedLobby != null);

        if (hostLobby != null && joinedLobby != null)
        {
            mapSelection.gameObject.SetActive(true);
            selectedMap.gameObject.SetActive(false);

            if (mapSelection.value != lastMap)
            {
                lastMap = mapSelection.value;
                updateLobby("map", mapSelection.captionText.text);
            }
        }
        else if (joinedLobby != null)
        {
            mapSelection.gameObject.SetActive(false);
            selectedMap.gameObject.SetActive(true);

            if (joinedLobby.Data != null && joinedLobby.Data.ContainsKey("map"))
            {
                selectedMap.text = "selected map : " + joinedLobby.Data["map"].Value;
            }
        }

        if (joinedLobby != null)
        {
            foreach (GameObject card in allPlayerCards)
            {
                Destroy(card);
            }
            allPlayerCards.Clear();

            foreach (Player player in joinedLobby.Players)
            {
                GameObject spawnedCard = Instantiate(playerCard, playerCardsParent);
                allPlayerCards.Add(spawnedCard);

                TextMeshProUGUI playerDisplayedName = spawnedCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                playerDisplayedName.text = player.Data["playerName"].Value;
            }
        }
    }
    async void updateLobby(string key, string value)
    {
        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject> { { key, new DataObject(DataObject.VisibilityOptions.Public, value) } };

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, options);
        }
        catch (LobbyServiceException exc)
        {
            Debug.Log(exc);
        }
    }

    async void creatLobby()
    {
        try
        {
            if (lobbyName.text.Length != 0)
            {
                Player firstPlayer = getPlayer();
                CreateLobbyOptions options = new CreateLobbyOptions { IsPrivate = !isPublic.isOn, Player = firstPlayer};

                Lobby newLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName.text, 4, options);
                hostLobby = newLobby;
                joinedLobby = hostLobby;

                Debug.Log($"lobby's code: {newLobby.LobbyCode}");
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }

    async Task listLobbies()
    {
        if (!isReady || isQuerying) return;
        isQuerying = true;

        try
        {
            List<QueryFilter> filters = new List<QueryFilter>();
            if (searchBar.text != "")
            {
                filters.Add(new QueryFilter(QueryFilter.FieldOptions.Name, searchBar.text, QueryFilter.OpOptions.CONTAINS));
            }

            QueryLobbiesOptions qlOptions = new QueryLobbiesOptions { Filters = filters };
            QueryResponse shownLobbies = await LobbyService.Instance.QueryLobbiesAsync(qlOptions);

            foreach (Button button in spawnedLobbies)
            {
                Destroy(button.gameObject);
            }
            spawnedLobbies.Clear();

            foreach (Lobby l in shownLobbies.Results)
            {
                Lobby captured = l;
                Button newLobby = Instantiate(lobbyObject, lobbiesParent.transform);

                spawnedLobbies.Add(newLobby);

                newLobby.onClick.AddListener(() => joinLobby(captured.Id, true));
                newLobby.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = captured.Name;
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
        finally
        {
            isQuerying = false;
        }
    }

    Player getPlayer()
    {
        return new Player { Data = new Dictionary<string, PlayerDataObject> { { "playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nameInput.text) } } };
    }

    async void joinLobby(string code, bool useID)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }
        try
        {
            if (useID)
            {
                Lobby l = await LobbyService.Instance.JoinLobbyByIdAsync(code, new JoinLobbyByIdOptions { Player = getPlayer() });
                joinedLobby = l;
            }
            else
            {
                Lobby l = await LobbyService.Instance.JoinLobbyByCodeAsync(code, new JoinLobbyByCodeOptions { Player = getPlayer() });
                joinedLobby = l;
            }
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }

    async void quickJoin()
    {
        try 
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException exc)
        {
            Debug.LogWarning(exc);
        }
    }
}
