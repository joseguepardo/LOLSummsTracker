using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using SRF.Components;
using SimpleJSON;
using System.Security.Cryptography.X509Certificates;
using SRDebugger.Profiler;

// runa 8347
namespace SummsTracker
{
    public partial class DataManager : SRSingleton<DataManager>
    {
        // Core.
        public string riotKey = "RGAPI-98b8d0f4-2e46-4f0d-ae8e-d0695b3e84db";
        string routing = "https://la1.api.riotgames.com";

        // Request services.
        string championsJsonURL = "http://ddragon.leagueoflegends.com/cdn/10.16.1/data/en_US/champion.json";
        string summonerSpellsJsonURL = "http://ddragon.leagueoflegends.com/cdn/10.16.1/data/en_US/summoner.json";
        string championIconsURL = "http://ddragon.leagueoflegends.com/cdn/10.16.1/img/champion/";
        string summonerSpellIconsURL = "http://ddragon.leagueoflegends.com/cdn/10.16.1/img/spell/";
        string summonerInfoRequestServiceURL = "/lol/summoner/v4/summoners/by-name/";
        string liveMatchRequestServiceURL = "/lol/spectator/v4/active-games/by-summoner/";

        // Game data.
        [ShowInInspector, BoxGroup("Riot/GameData")]
        public Dictionary<int, GameItem> champions;
        [ShowInInspector, BoxGroup("Riot/GameData")]
        public Dictionary<int, GameItem> summonerSpells;

        // Coroutine flags.
        [HideInInspector]
        public bool getSummonerInfoCOCompleted;
        [HideInInspector]
        public bool getSummonerInfoCOCompletedSuccessfully;
        [HideInInspector]
        public bool getMatchLiveInfoCOCompleted;
        [HideInInspector]
        public bool getMatchLiveInfoCOCompletedSuccessfully;
        [HideInInspector]
        public bool createRoomNodeCOCompleted;
        [HideInInspector]
        public bool createRoomNodeCOCompletedSuccessfully;

        [Serializable]
        public class GameItem
        {
            public string id;
            public string name;
            public float cooldown;
            [PreviewField]
            public Sprite sprite;

            public GameItem(string id, string name, float cooldown, Sprite sprite)
            {
                this.id = id;
                this.name = name;
                this.cooldown = cooldown;
                this.sprite = sprite;
            }
        }

        // User data.
        [BoxGroup("Riot/SummonerData"), ReadOnly]
        public bool summonerLoaded;
        [BoxGroup("Riot/SummonerData"), ReadOnly]
        public string summonerId;
        [BoxGroup("Riot/SummonerData"), ReadOnly]
        public bool roomLoaded;

        [Serializable]
        public class Summoner
        {
            [Serializable]
            public class SummonerSpell
            {
                public string id;
                public string name;
                [PreviewField, NonSerialized]
                public Sprite icon;
                public float cooldown;
                public float currentCooldown;
                public bool available;
                //public string summonerTracker;

                public SummonerSpell(string id, string name, Sprite icon, float cooldown, bool available)
                {
                    this.id = id;
                    this.name = name;
                    this.icon = icon;
                    this.cooldown = cooldown;
                    this.currentCooldown = cooldown;
                    this.available = available;
                    //this.summonerTracker = summonerTracker;
                }

                public Action<bool> OnToggle;
            }

            [Serializable]
            public class Champion
            {
                public string id;
                public string name;
                public string nameId;
                [PreviewField, NonSerialized]
                public Sprite icon;

                public Champion(string id, string name, string nameId, Sprite icon)
                {
                    this.id = id;
                    this.name = name;
                    this.nameId = nameId;
                    this.icon = icon;
                }
            }

            public string id;
            public string name;
            public string teamId;
            public Champion champion;
            public SummonerSpell summonerSpell1;
            public SummonerSpell summonerSpell2;
            public bool hasSummonerCDRRune;
            public bool hasCDRBoots;
            public int summonerLevel;

            public Summoner(string id, string name, string teamId, Champion champion, SummonerSpell summonerSpell1, SummonerSpell summonerSpell2, bool hasSummonerCDRRune)
            {
                this.id = id;
                this.name = name;
                this.teamId = teamId;
                this.champion = champion;
                this.summonerSpell1 = summonerSpell1;
                this.summonerSpell2 = summonerSpell2;
                this.hasSummonerCDRRune = hasSummonerCDRRune;
                this.hasCDRBoots = false;
                this.summonerLevel = 1;
            }

            public void SpellUpdated(bool isSpell2, string summonerTracker, bool hasCDRBoots, int summonerLevel)
            {
                this.hasCDRBoots = hasCDRBoots;
                this.summonerLevel = summonerLevel;
                // This means that the current summoner spell is teleport.
                SummonerSpell summonerSpell = isSpell2 ? summonerSpell2 : summonerSpell1;
                if (summonerSpell.id == "12")
                {
                    summonerSpell.cooldown = 430.588f - (10.588f * summonerLevel);
                }

                float currentCDR = (hasCDRBoots ? 0.1f : 0f) + (hasSummonerCDRRune ? 0.05f : 0f);
                summonerSpell.currentCooldown = summonerSpell.cooldown * (1 - currentCDR);
                //summonerSpell.summonerTracker = summonerTracker;
            }
        }

        [Serializable]
        public class Room
        {
            [Serializable]
            public class Match
            {
                //[NonSerialized]
                public string matchId;
                [ReadOnly]
                public List<Summoner> summoners;

                public Match()
                {
                    summoners = new List<Summoner>();
                }
            }

            [Serializable]
            public class Participant
            {
                public string id;

                public Participant(string id)
                {
                    this.id = id;
                }
            }

            public string id;
            public string password;
            public Match match;
            public List<Participant> participants;

            public Room(string id, string password)
            {
                this.id = id;
                this.password = password;
                match = new Match();
                participants = new List<Participant>();
            }
        }

        [BoxGroup("Riot")]
        public Room room;

        // Methods.
        public void InitializeRiotData()
        {
            GetChampions();
            GetSummonerSpells();
        }

        public void GetChampions()
        {
            StartCoroutine(GetChampionsCO());
        }

        IEnumerator GetChampionsCO()
        {
            UnityWebRequest request = UnityWebRequest.Get(championsJsonURL);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
            }
            else
            {
                //Debug.Log(request.downloadHandler.text);
                champions = new Dictionary<int, GameItem>();
                var json = JSON.Parse(request.downloadHandler.text)["data"];
                for (int i = 0; i < json.Count; i++)
                {
                    // Don't download the champion's icon yet, only the ones that are going to be used.
                    champions.Add(json[i]["key"], new GameItem(json[i]["id"], json[i]["name"], 0, null));
                }
            }
        }

        public void GetSummonerSpells()
        {
            StartCoroutine(GetSummonerSpellsCO());
        }

        IEnumerator GetSummonerSpellsCO()
        {
            UnityWebRequest request = UnityWebRequest.Get(summonerSpellsJsonURL);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
            }
            else
            {
                //Debug.Log(request.downloadHandler.text);
                summonerSpells = new Dictionary<int, GameItem>();
                var json = JSON.Parse(request.downloadHandler.text)["data"];
                for (int i = 0; i < json.Count; i++)
                {
                    // Download the summoner spell icon first.
                    UnityWebRequest summonerIconRequest = UnityWebRequestTexture.GetTexture(summonerSpellIconsURL + json[i]["id"] + ".png");
                    yield return summonerIconRequest.SendWebRequest();
                    if (summonerIconRequest.isNetworkError)
                    {
                        Debug.LogErrorFormat("Error downloading summoner spell icon: {0}", summonerIconRequest.error);
                        yield break;
                    }
                    Texture2D texture = DownloadHandlerTexture.GetContent(summonerIconRequest);
                    summonerSpells.Add(json[i]["key"], new GameItem(json[i]["id"], json[i]["name"], json[i]["cooldownBurn"].AsFloat, Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero)));
                }
            }
        }

        [Button, BoxGroup("Riot")]
        public void CreateRoom(string id, string password, string summonerName)
        {
            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(password))
            {
                Debug.LogError("#CreateRoomCO# id or password empty");
                return;
            }
            room = new Room(id, password);
            StartCoroutine(CreateRoomCO(id, password, summonerName));
        }

        IEnumerator CreateRoomCO(string id, string password, string summonerName)
        {
            getSummonerInfoCOCompleted = getSummonerInfoCOCompleted = createRoomNodeCOCompleted = false;
            StartCoroutine(GetSummonerInfoCO(summonerName));
            yield return new WaitUntil(() => getSummonerInfoCOCompleted);
            if (getSummonerInfoCOCompletedSuccessfully)
            {
                StartCoroutine(GetLiveMatchInfoCO());
                yield return new WaitUntil(() => getMatchLiveInfoCOCompleted);
                if (getMatchLiveInfoCOCompletedSuccessfully)
                {
                    room.participants.Add(new Room.Participant(FireBaseManager.Instance.auth.CurrentUser.UserId));
                    StartCoroutine(CreateRoomNodeCO());
                    yield return new WaitUntil(() => createRoomNodeCOCompleted);
                    if (createRoomNodeCOCompletedSuccessfully)
                    {
                        roomLoaded = true;
                        Debug.Log("#CreateRoomCO# Room created successfully :)");
                    }
                    else
                    {
                        Debug.LogError("#CreateRoomCO# Room already exists");
                    }
                }
                else
                {
                    Debug.LogError("#CreateRoomCO# Error getting match live info");
                }
            }
            else
            {
                Debug.LogError("#CreateRoomCO# Error getting summoner info");
            }
        }

        [Button, BoxGroup("Riot")]
        public void JoinRoom(string id, string password)
        {
            StartCoroutine(JoinRoomCO(id, password));
        }

        IEnumerator JoinRoomCO(string id, string password)
        {
            room.id = id;
            var roomExistsTask = RoomNodeExistsTask();
            yield return new WaitUntil(() => roomExistsTask.IsCompleted);
            if (roomExistsTask.Result)
            {
                var getRoomNodeTask = GetRoomNodeTask();
                yield return new WaitUntil(() => getRoomNodeTask.IsCompleted);
                if (getRoomNodeTask.IsFaulted)
                {
                    Debug.LogError("#JoinRoomCO# Could not load room info");
                }
                else
                {
                    if (password == getRoomNodeTask.Result.password)
                    {
                        room = getRoomNodeTask.Result;
                        Debug.Log("#JoinRoomCO# Room info loaded successfully");

                        // Participant added to the room.
                        var updateParticipantsNodeTask = UpdateParticipantsNodeTask(FireBaseManager.Instance.userId);
                        yield return new WaitUntil(() => updateParticipantsNodeTask.IsCompleted);
                        if (updateParticipantsNodeTask.Result)
                        {
                            Debug.Log("#JoinRoomCO# Participant added to the room");
                        }
                    }
                    else
                    {
                        Debug.LogError("#JoinRoomCO# Wrong password :c");
                    }
                }
            }
            else
            {
                Debug.LogError("#JoinRoomCO# does not exist");
            }
        }

        // Get summoner info.
        //[Button, BoxGroup("Riot")]
        //public void GetSummonerInfo(string summonerName)
        //{
        //    StartCoroutine(GetSummonerInfoCO(summonerName));
        //}

        IEnumerator GetSummonerInfoCO(string summonerName)
        {
            UnityWebRequest request = UnityWebRequest.Get(routing + summonerInfoRequestServiceURL + summonerName);
            request = AddWebRequestHeaders(request);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
                getSummonerInfoCOCompletedSuccessfully = false;
            }
            else
            {
                // Ok.
                if (request.responseCode == 200)
                {
                    Debug.Log(request.downloadHandler.text);
                    var json = JSON.Parse(request.downloadHandler.text);
                    summonerId = json["id"].Value;
                    summonerLoaded = true;
                    getSummonerInfoCOCompletedSuccessfully = true;
                }
                else
                {
                    Debug.LogErrorFormat("Error - code: {0}", request.responseCode);
                    getSummonerInfoCOCompletedSuccessfully = false;
                }
            }
            getSummonerInfoCOCompleted = true;
        }

        [Button, BoxGroup("Riot"), EnableIf("summonerLoaded")]
        // Get summoner's current match info.
        public void GetLiveMatchInfo()
        {
            //match = new Match();
            StartCoroutine(GetLiveMatchInfoCO());
        }

        IEnumerator GetLiveMatchInfoCO()
        {
            yield return null;
            UnityWebRequest request = UnityWebRequest.Get(routing + liveMatchRequestServiceURL + summonerId);
            request = AddWebRequestHeaders(request);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
                getMatchLiveInfoCOCompletedSuccessfully = false;
            }
            else
            {
                // Ok.
                if (request.responseCode == 200)
                {
                    var json = JSON.Parse(request.downloadHandler.text);
                    Debug.Log(request.downloadHandler.text);
                    Debug.LogFormat("game id: {0}", json["gameId"]);
                    int enemyTeamId = 0;
                    for (int i = 0; i < json["participants"].Count; i++)
                    {
                        if (json["participants"][i]["summonerId"] == summonerId)
                        {
                            enemyTeamId = json["participants"][i]["teamId"] == 100 ? 200 : 100;
                            room.match.matchId = json["gameId"] + enemyTeamId.ToString();
                            break;
                        }
                    }
                    for (int i = 0; i < json["participants"].Count; i++)
                    {
                        if (json["participants"][i]["teamId"].AsInt == enemyTeamId)
                        {
                            Summoner.Champion champion = new Summoner.Champion(
                                json["participants"][i]["championId"],
                                champions[json["participants"][i]["championId"]].name,
                                champions[json["participants"][i]["championId"]].id,
                                null);

                            Summoner.SummonerSpell summonerSpell1 = new Summoner.SummonerSpell(
                                json["participants"][i]["spell1Id"],
                                summonerSpells[json["participants"][i]["spell1Id"]].name,
                                summonerSpells[json["participants"][i]["spell1Id"]].sprite,
                                summonerSpells[json["participants"][i]["spell1Id"]].cooldown,
                                true);
                            Summoner.SummonerSpell summonerSpell2 = new Summoner.SummonerSpell(
                                json["participants"][i]["spell2Id"],
                                summonerSpells[json["participants"][i]["spell2Id"]].name,
                                summonerSpells[json["participants"][i]["spell2Id"]].sprite,
                                summonerSpells[json["participants"][i]["spell2Id"]].cooldown,
                                true);
                            List<string> perksIds = new List<string>();
                            for (int j = 0; j < json["participants"][i]["perks"]["perkIds"].Count; j++)
                            {
                                perksIds.Add(json["participants"][i]["perks"]["perkIds"][j]);
                            }
                            Summoner enemy = new Summoner(
                                json["participants"][i]["summonerId"],
                                json["participants"][i]["summonerName"],
                                json["participants"][i]["teamId"],
                                champion,
                                summonerSpell1,
                                summonerSpell2,
                                perksIds.Contains("8347")
                                );

                            room.match.summoners.Add(enemy);
                            StartCoroutine(GetChampionIconCO(room.match.summoners[room.match.summoners.Count - 1]));
                        }
                    }
                    //CreateMatchTable();
                    getMatchLiveInfoCOCompletedSuccessfully = true;
                }
                else
                {
                    Debug.LogErrorFormat("Error - code: {0}", request.responseCode);
                    getMatchLiveInfoCOCompletedSuccessfully = false;
                }
            }
            getMatchLiveInfoCOCompleted = true;
        }

        IEnumerator GetChampionIconCO(Summoner summoner)
        {
            // Download the summoner spell icon first.
            UnityWebRequest championIconRequest = UnityWebRequestTexture.GetTexture(championIconsURL + summoner.champion.nameId + ".png");
            yield return championIconRequest.SendWebRequest();
            if (championIconRequest.isNetworkError)
            {
                Debug.LogErrorFormat("Error downloading summoner spell icon: {0}", championIconRequest.error);
                yield break;
            }
            Texture2D texture = DownloadHandlerTexture.GetContent(championIconRequest);
            summoner.champion.icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        UnityWebRequest AddWebRequestHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("X-Riot-Token", riotKey);
            return request;
        }
    }
}
