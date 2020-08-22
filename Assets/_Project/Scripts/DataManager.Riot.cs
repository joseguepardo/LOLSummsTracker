using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using SRF.Components;
using SimpleJSON;
using System.Security.Cryptography.X509Certificates;

// runa 8347
namespace SummsTracker
{
    public partial class DataManager : SRSingleton<DataManager>
    {
        // Core.
        string riotKey = "RGAPI-98b8d0f4-2e46-4f0d-ae8e-d0695b3e84db";
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

        [Serializable]
        public class Summoner
        {
            public class SummonerSpell
            {
                public string id;
                public string name;
                public Sprite sprite;
                public float cooldown;
                public float currentCooldown;
                public string summonerTracker;

                public SummonerSpell(string id, string name, Sprite sprite, float cooldown, string summonerTracker)
                {
                    this.id = id;
                    this.name = name;
                    this.sprite = sprite;
                    this.cooldown = cooldown;
                    this.currentCooldown = cooldown;
                    this.summonerTracker = summonerTracker;
                }
            }

            public string id;
            public string name;
            public string teamId;
            public string championId;
            public Sprite icon;
            public SummonerSpell summonerSpell1;
            public SummonerSpell summonerSpell2;
            public bool hasSummonerCDRRune;

            public Summoner(string id, string name, string teamId, string championId, Sprite icon, SummonerSpell summonerSpell1, SummonerSpell summonerSpell2, bool hasSummonerCDRRune)
            {
                this.id = id;
                this.name = name;
                this.teamId = teamId;
                this.championId = championId;
                this.icon = icon;
                this.summonerSpell1 = summonerSpell1;
                this.summonerSpell2 = summonerSpell2;
                this.hasSummonerCDRRune = hasSummonerCDRRune;
            }
        }
        [ReadOnly]
        public List<Summoner> summoners;

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

        // Get summoner info.
        [Button, BoxGroup("Riot")]
        public void GetSummonerInfo(string summonerName)
        {
            StartCoroutine(GetSummonerInfoCO(summonerName));
        }

        IEnumerator GetSummonerInfoCO(string summonerName)
        {
            UnityWebRequest request = UnityWebRequest.Get(routing + summonerInfoRequestServiceURL + summonerName);
            request = AddWebRequestHeaders(request);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
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
                }
                else
                {
                    Debug.LogErrorFormat("Error - code: {0}", request.responseCode);
                }
            }
        }

        [Button, BoxGroup("Riot"), EnableIf("summonerLoaded")]
        // Get summoner's current match info.
        public void GetLiveMatchInfo()
        {
            StartCoroutine(GetLiveMatchInfoCO());
        }

        IEnumerator GetLiveMatchInfoCO()
        {
            UnityWebRequest request = UnityWebRequest.Get(routing + liveMatchRequestServiceURL + summonerId);
            request = AddWebRequestHeaders(request);
            yield return request.SendWebRequest();
            if (request.isNetworkError)
            {
                Debug.LogErrorFormat("Error: {0}", request.error);
            }
            else
            {
                // Ok.
                if (request.responseCode == 200)
                {
                    var json = JSON.Parse(request.downloadHandler.text);
                    Debug.LogFormat("game id: {0}", json["gameId"]);
                    int enemyTeamId = 0;
                    for (int i = 0; i < json["participants"].Count; i++)
                    {
                        if (json["participants"][i]["summonerId"] == summonerId)
                        {
                            enemyTeamId = json["participants"][i]["teamId"] == 100 ? 200 : 100;
                            break;
                        }
                    }
                    for (int i = 0; i < json["participants"].Count; i++)
                    {
                        Debug.LogFormat("json: {0}, variable: {1}", json["participants"][i]["teamId"], enemyTeamId);

                        if (json["participants"][i]["teamId"].AsInt == enemyTeamId)
                        {
                            Summoner.SummonerSpell summonerSpell1 = new Summoner.SummonerSpell(
                                json["participants"][i]["spell1Id"],
                                summonerSpells[json["participants"][i]["spell1Id"]].name,
                                summonerSpells[json["participants"][i]["spell1Id"]].sprite,
                                summonerSpells[json["participants"][i]["spell1Id"]].cooldown,
                                "");
                            Summoner.SummonerSpell summonerSpell2 = new Summoner.SummonerSpell(
                                json["participants"][i]["spell2Id"],
                                summonerSpells[json["participants"][i]["spell2Id"]].name,
                                summonerSpells[json["participants"][i]["spell2Id"]].sprite,
                                summonerSpells[json["participants"][i]["spell2Id"]].cooldown,
                                "");
                            List<string> perksIds = new List<string>();
                            for (int j = 0; j < json["participants"][i]["perks"]["perkIds"].Count; j++)
                            {
                                perksIds.Add(json["participants"][i]["perks"]["perkIds"][j]);
                            }
                            Summoner enemy = new Summoner(
                                json["participants"][i]["summonerId"],
                                json["participants"][i]["summonerName"],
                                json["participants"][i]["teamId"],
                                json["participants"][i]["championId"],
                                null,
                                summonerSpell1,
                                summonerSpell2,
                                perksIds.Contains("8347")
                                );

                            if (summoners == null) summoners = new List<Summoner>();
                            summoners.Add(enemy);
                            Debug.Log("hola :v");
                        }
                    }

                }
                else
                {
                    Debug.LogErrorFormat("Error - code: {0}", request.responseCode);
                }
            }
        }

        UnityWebRequest AddWebRequestHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("X-Riot-Token", riotKey);
            return request;
        }
    }
}
