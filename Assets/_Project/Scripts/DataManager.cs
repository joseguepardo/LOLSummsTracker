using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Sirenix.OdinInspector;
using SRF.Components;
using UnityEditor.UIElements;
using System.Security.Cryptography.X509Certificates;

namespace SummsTracker
{
    public partial class DataManager : SRSingleton<DataManager>
    {
        public DatabaseReference databaseReference;

        public Action OnDataLoaded;
        public bool loggedIn;

        //Dictionary<string, object> timestamp = new Dictionary<string, object>();
        private IEnumerator Start()
        {
            //timestamp[".sv"] = "timestamp";
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
            FireBaseManager.Instance.OnSignedIn -= InitializePlayerData;
        }

        #region setup
        public void InitializePlayerData()
        {
            loggedIn = true;
        }

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
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).Child("summoners").Child("0").ValueChanged += OnSummonerUpdated_0;
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).Child("summoners").Child("1").ValueChanged += OnSummonerUpdated_1;
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).Child("summoners").Child("2").ValueChanged += OnSummonerUpdated_2;
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).Child("summoners").Child("3").ValueChanged += OnSummonerUpdated_3;
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).Child("summoners").Child("4").ValueChanged += OnSummonerUpdated_4;
            matchLoaded = true;
        }

        async Task CreateMatchTableTask()
        {
            var offset = await FirebaseDatabase.DefaultInstance.GetReference(".info/serverTimeOffset").GetValueAsync();
            Debug.Log(offset.ToString());

            await databaseReference.Child(match.matchId).SetRawJsonValueAsync(JsonUtility.ToJson(match));
        }

        async Task<bool> MatchTableExists()
        {
            var dataSnapshot = await databaseReference.Child(match.matchId).GetValueAsync();
            return dataSnapshot.Exists;
        }

        void OnSummonerUpdated_0(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
            Summoner updatedSummoner = JsonUtility.FromJson<Summoner>(args.Snapshot.GetRawJsonValue());
            OnSummonerUpdated(updatedSummoner, 0);
        }

        void OnSummonerUpdated_1(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
            Summoner updatedSummoner = JsonUtility.FromJson<Summoner>(args.Snapshot.GetRawJsonValue());
            OnSummonerUpdated(updatedSummoner, 1);
        }

        void OnSummonerUpdated_2(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
            Summoner updatedSummoner = JsonUtility.FromJson<Summoner>(args.Snapshot.GetRawJsonValue());
            OnSummonerUpdated(updatedSummoner, 2);
        }

        void OnSummonerUpdated_3(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
            Summoner updatedSummoner = JsonUtility.FromJson<Summoner>(args.Snapshot.GetRawJsonValue());
            OnSummonerUpdated(updatedSummoner, 3);
        }

        void OnSummonerUpdated_4(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            Debug.Log(args.Snapshot.GetRawJsonValue());
            Summoner updatedSummoner = JsonUtility.FromJson<Summoner>(args.Snapshot.GetRawJsonValue());
            OnSummonerUpdated(updatedSummoner, 4);
        }

        void OnSummonerUpdated(Summoner updatedSummoner, int id)
        {
            if (match.summoners[id].summonerSpell1.summonerTracker != updatedSummoner.summonerSpell1.summonerTracker)
            {
                match.summoners[id].summonerSpell1.OnToggle?.Invoke(string.IsNullOrEmpty(match.summoners[id].summonerSpell1.summonerTracker));
                match.summoners[id].summonerSpell1.summonerTracker = updatedSummoner.summonerSpell1.summonerTracker;
            }
            if (match.summoners[id].summonerSpell2.summonerTracker != updatedSummoner.summonerSpell2.summonerTracker)
            {
                match.summoners[id].summonerSpell2.OnToggle?.Invoke(string.IsNullOrEmpty(match.summoners[id].summonerSpell2.summonerTracker));
                match.summoners[id].summonerSpell2.summonerTracker = updatedSummoner.summonerSpell2.summonerTracker;
            }
            Debug.Log(id);
        }
        #endregion

        //public string TimeStamp()
        //{
        //    return ServerValue.Timestamp.ToString();
        //}

        [Button, BoxGroup("Riot"), EnableIf("matchLoaded")]
        public void SummonerSpellUsed(int enemySummonerId, bool isSpell2 = false, bool hasCDRBoots = false, int summonerLevel = 1)
        {
            match.summoners[enemySummonerId].SpellUpdated(isSpell2, summonerId, hasCDRBoots, summonerLevel);
            StartCoroutine(UpdateSummonerSpellTableCO(enemySummonerId, isSpell2));
        }

        IEnumerator UpdateSummonerSpellTableCO(int enemySummonerId, bool isSpell2)
        {
            // Firebase.
            var updateSummonerSpellTableTask = UpdateSummonerSpellTableTask(enemySummonerId, isSpell2);
            Debug.Log("Creating table in FireBase");
            yield return new WaitUntil(() => updateSummonerSpellTableTask.IsCompleted);
        }

        async Task<bool> UpdateSummonerSpellTableTask(int enemySummonerId, bool isSpell2)
        {
            await databaseReference.
                Child(match.matchId).
                Child("summoners").
                Child(enemySummonerId.ToString()).
                SetRawJsonValueAsync(JsonUtility.ToJson(match.summoners[enemySummonerId]));
            //await UpdateTimestamp(enemySummonerId, isSpell2);
            return true;
        }

    }
}
