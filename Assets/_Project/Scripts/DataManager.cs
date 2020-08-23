using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Sirenix.OdinInspector;
using SRF.Components;

namespace SummsTracker
{
    public partial class DataManager : SRSingleton<DataManager>
    {
        public DatabaseReference databaseReference;

        public Action OnDataLoaded;
        public bool loggedIn;
        private IEnumerator Start()
        {
            FireBaseManager.Instance.OnSignedIn += InitializePlayerData;

            yield return new WaitUntil(() => loggedIn);

            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            //Debug.Log(FirebaseDatabase.DefaultInstance.RootReference.);
            //StartCoroutine(InitializePlayerDataCO());

            // Initialize Riot data.
            InitializeRiotData();
        }

        public void OnDestroy()
        {
            //if (FireBaseManager.HasInstance())
            //FireBaseManager.Instance.OnSignedIn -= InitializePlayerData;
        }

        public void InitializePlayerData()
        {
            loggedIn = true;
        }

        //IEnumerator InitializePlayerDataCO()
        //{
        //    Debug.Log("#PlayerData# Starting data loading");
        //    var loadPlayerDataTask = LoadPlayerData();
        //    yield return new WaitUntil(() => loadPlayerDataTask.IsCompleted);

        //    playerData = loadPlayerDataTask.Result;
        //    PlayerData localPlayerData = LoadLocalPlayerData();

        //    if (playerData == null)
        //    {
        //        if (localPlayerData == null)
        //            playerData = new PlayerData();
        //        else
        //            playerData = localPlayerData;
        //    }
        //    else
        //    {
        //        if (localPlayerData != null)
        //        {
        //            playerData = playerData.maxLevelReachedId < localPlayerData.maxLevelReachedId ? localPlayerData : playerData;
        //        }
        //    }

        //    Debug.Log("#PlayerData# \nCurrentLevelID: " + playerData.currentLevelId + "\nMaxLevelReachedID: " + playerData.maxLevelReachedId);

        //    Debug.Log("#PlayerData# Starting skins loading");
        //    var loadPlayerSkinsTask = LoadPlayerSkins();
        //    yield return new WaitUntil(() => loadPlayerSkinsTask.IsCompleted);

        //    playerSkins = loadPlayerSkinsTask.Result;
        //    //PlayerSkins localPlayerSkins = LoadLocalPlayerSkins();
        //    PlayerSkins localPlayerSkins = new PlayerSkins(new List<int>() { 0, 2 }, new List<int>() { 0, 1 });

        //    if (playerSkins == null)
        //    {
        //        if (localPlayerSkins == null)
        //            playerSkins = new PlayerSkins();
        //        else
        //            playerSkins = localPlayerSkins;
        //    }
        //    else
        //    {
        //        if (localPlayerSkins != null)
        //        {
        //            playerSkins = playerSkins.skinsOwned?.Count < localPlayerSkins.skinsOwned?.Count || playerSkins.trailsOwned?.Count < localPlayerSkins.trailsOwned?.Count ? localPlayerSkins : playerSkins;
        //        }
        //    }
        //}

        //async Task<PlayerData> LoadPlayerData()
        //{
        //Debug.Log("#PlayerData# Trying to retrieve player data");
        ////if (SaveExists())
        ////    return JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(playerDataKey));

        //var dataSnapshot = await databaseReference.Child(playerDataKey).Child(FireBaseManager.Instance.userId).GetValueAsync();
        //if (!dataSnapshot.Exists)
        //    return null;

        //return JsonUtility.FromJson<PlayerData>(dataSnapshot.GetRawJsonValue());
        //}

        //PlayerData LoadLocalPlayerData()
        //{
        //if (!PlayerPrefs.HasKey(playerDataKey))
        //    return null;

        //return JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(playerDataKey));
        //}

        //async Task<PlayerSkins> LoadPlayerSkins()
        //{
        //    Debug.Log("#PlayerData# Trying to retrieve player skins");
        //    //if (SaveExists())
        //    //    return JsonUtility.FromJson<PlayerSkins>(PlayerPrefs.GetString(playerDataKey));

        //    var dataSnapshot = await databaseReference.Child(playerSkinsKey).Child(FireBaseManager.Instance.userId).GetValueAsync();
        //    if (!dataSnapshot.Exists)
        //        return null;

        //    return JsonUtility.FromJson<PlayerSkins>(dataSnapshot.GetRawJsonValue());
        //}

        //PlayerSkins LoadLocalPlayerSkins()
        //{
        //    if (!PlayerPrefs.HasKey(playerSkinsKey))
        //        return null;

        //    return JsonUtility.FromJson<PlayerSkins>(PlayerPrefs.GetString(playerSkinsKey));
        //}

        public void CreateMatchTable()
        {
            StartCoroutine(CreateMatchTableCO());
        }

        IEnumerator CreateMatchTableCO()
        {
            var matchExists = MatchTableExists();
            yield return new WaitUntil(() => matchExists.IsCompleted);
            if (!matchExists.Result)
            {
                // Firebase.
                var createMatchTableTask = CreateMatchTableTask();
                Debug.Log("Creating table in FireBase");
                yield return new WaitUntil(() => createMatchTableTask.IsCompleted);
            }
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).ValueChanged += OnMatchUpdated;
        }

        async Task<bool> CreateMatchTableTask()
        {
            await databaseReference.Child(match.matchId).SetRawJsonValueAsync(JsonUtility.ToJson(match));
            return true;
        }

        async Task<bool> MatchTableExists()
        {
            var dataSnapshot = await databaseReference.Child(match.matchId).GetValueAsync();
            return dataSnapshot.Exists;
        }

        void OnMatchUpdated(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
        }
    }
}
