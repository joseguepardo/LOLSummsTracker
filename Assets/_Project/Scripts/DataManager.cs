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

namespace SummsTracker
{
    public partial class DataManager : SRSingleton<DataManager>
    {
        public DatabaseReference databaseReference;

        public Action OnDataLoaded;
        public bool loggedIn;

        Dictionary<string, object> timestamp = new Dictionary<string, object>();
        private IEnumerator Start()
        {
            timestamp[".sv"] = "timestamp";
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
            FirebaseDatabase.DefaultInstance.GetReference(match.matchId).ValueChanged += OnMatchUpdated;
            matchLoaded = true;
        }

        async Task CreateMatchTableTask()
        {
            var offset = await FirebaseDatabase.DefaultInstance.GetReference(".info/serverTimeOffset").GetValueAsync();
            Debug.Log(offset.ToString());

            await databaseReference.Child(match.matchId).SetRawJsonValueAsync(JsonUtility.ToJson(match));

            for (int i = 0; i < match.summoners.Count; i++)
            {
                await UpdateTimestamp(i, false);
                await UpdateTimestamp(i, true);
            }
        }

        async Task UpdateTimestamp(int enemySummonerId, bool isSpell2)
        {
            await databaseReference.
                Child(match.matchId).
                Child("summoners").
                Child(enemySummonerId.ToString()).
                Child(isSpell2 ? "summonerSpell2" : "summonerSpell1").
                Child("timestamp").
                SetValueAsync(timestamp);
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
        #endregion

        public string TimeStamp()
        {
            return ServerValue.Timestamp.ToString();
        }

        [Button, BoxGroup("Riot"), EnableIf("matchLoaded")]
        public void SummonerSpellUsed(int enemySummonerId, bool isSpell2 = false)
        {
            if (isSpell2)
            {
                match.summoners[enemySummonerId].summonerSpell2.SpellUpdated(summonerId);
            }
            else
            {
                match.summoners[enemySummonerId].summonerSpell1.SpellUpdated(summonerId);
            }
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
                Child(isSpell2 ? "summonerSpell2" : "summonerSpell1").
                SetRawJsonValueAsync(JsonUtility.ToJson(isSpell2 ? match.summoners[enemySummonerId].summonerSpell2 : match.summoners[enemySummonerId].summonerSpell1));
            await UpdateTimestamp(enemySummonerId, isSpell2);
            return true;
        }

    }
}
