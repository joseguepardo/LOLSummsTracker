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
        #endregion

        public string TimeStamp()
        {
            return ServerValue.Timestamp.ToString();
        }

        [Button]
        public void SummonerSpellUsed(int enemySummonerId, bool isSpell2 = false)
        {
            if (isSpell2)
            {
                match.summoners[enemySummonerId].summonerSpell2.SpellUpdated(summonerId, TimeStamp());
            }
            else
            {
                match.summoners[enemySummonerId].summonerSpell1.SpellUpdated(summonerId, TimeStamp());
            }
            StartCoroutine(UpdateSummonerSpellTableCO(enemySummonerId, isSpell2));
        }

        IEnumerator UpdateSummonerSpellTableCO(int enemySummonerId, bool isSpell2)
        {
            // Firebase.
            var createMatchTableTask = UpdateSummonerSpellTableTask(enemySummonerId, isSpell2);
            Debug.Log("Creating table in FireBase");
            yield return new WaitUntil(() => createMatchTableTask.IsCompleted);
        }

        async Task<bool> UpdateSummonerSpellTableTask(int enemySummonerId, bool isSpell2)
        {
            await databaseReference.
                Child(match.matchId).
                Child("summoners").
                Child(enemySummonerId.ToString()).
                Child(isSpell2 ? "summonerSpell2" : "summonerSpell1").
                SetRawJsonValueAsync(JsonUtility.ToJson(isSpell2 ? match.summoners[enemySummonerId].summonerSpell2 : match.summoners[enemySummonerId].summonerSpell1));
            return true;
        }

    }
}
