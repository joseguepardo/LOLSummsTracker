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
using TMPro;

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
            if (FireBaseManager.HasInstance)
            {
                FireBaseManager.Instance.OnSignedIn -= InitializePlayerData;
            }
        }

        #region setup
        public void InitializePlayerData()
        {
            loggedIn = true;
        }
        //public void CreateRoomNode()
        //{
        //    StartCoroutine(CreateRoomNodeCO());
        //}

        IEnumerator CreateRoomNodeCO()
        {
            var roomExists = RoomNodeExistsTask();
            yield return new WaitUntil(() => roomExists.IsCompleted);
            if (!roomExists.Result)
            {
                // Firebase.
                var createRoomNodeTask = CreateRoomNodeTask();
                Debug.Log("Creating node in FireBase");
                yield return new WaitUntil(() => createRoomNodeTask.IsCompleted);
                AddRoomListeners();
                createRoomNodeCOCompletedSuccessfully = true;
            }
            else
            {
                createRoomNodeCOCompletedSuccessfully = false;
            }
            createRoomNodeCOCompleted = true;
        }

        public void AddRoomListeners()
        {
            FirebaseDatabase.DefaultInstance.GetReference(room.id).Child("match").Child("summoners").Child("0").ValueChanged += OnSummonerUpdated_0;
            FirebaseDatabase.DefaultInstance.GetReference(room.id).Child("match").Child("summoners").Child("1").ValueChanged += OnSummonerUpdated_1;
            FirebaseDatabase.DefaultInstance.GetReference(room.id).Child("match").Child("summoners").Child("2").ValueChanged += OnSummonerUpdated_2;
            FirebaseDatabase.DefaultInstance.GetReference(room.id).Child("match").Child("summoners").Child("3").ValueChanged += OnSummonerUpdated_3;
            FirebaseDatabase.DefaultInstance.GetReference(room.id).Child("match").Child("summoners").Child("4").ValueChanged += OnSummonerUpdated_4;
        }

        async Task CreateRoomNodeTask()
        {
            await databaseReference.Child(room.id).SetRawJsonValueAsync(JsonUtility.ToJson(room));
        }

        async Task<bool> RoomNodeExistsTask()
        {
            var dataSnapshot = await databaseReference.Child(room.id).GetValueAsync();
            return dataSnapshot.Exists;
        }
        async Task<Room> GetRoomNodeTask()
        {
            var dataSnapshot = await databaseReference.Child(room.id).GetValueAsync();
            return JsonUtility.FromJson<Room>(dataSnapshot.GetRawJsonValue());
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
            if (room.match.summoners[id].summonerSpell1.available != updatedSummoner.summonerSpell1.available)
            {
                room.match.summoners[id].summonerSpell1.OnToggle?.Invoke(room.match.summoners[id].summonerSpell1.available);
                room.match.summoners[id].summonerSpell1.available = updatedSummoner.summonerSpell1.available;
            }
            if (room.match.summoners[id].summonerSpell2.available != updatedSummoner.summonerSpell2.available)
            {
                room.match.summoners[id].summonerSpell2.OnToggle?.Invoke(room.match.summoners[id].summonerSpell2.available);
                room.match.summoners[id].summonerSpell2.available = updatedSummoner.summonerSpell2.available;
            }
            Debug.Log(id);
        }
        #endregion

        //public string TimeStamp()
        //{
        //    return ServerValue.Timestamp.ToString();
        //}

        [Button, BoxGroup("Riot"), EnableIf("roomLoaded")]
        public void SummonerSpellUsed(int enemySummonerId, bool isSpell2 = false, bool hasCDRBoots = false, int summonerLevel = 1)
        {
            room.match.summoners[enemySummonerId].SpellUpdated(isSpell2, summonerId, hasCDRBoots, summonerLevel);
            StartCoroutine(UpdateSummonerSpellNodeCO(enemySummonerId, isSpell2));
        }

        IEnumerator UpdateSummonerSpellNodeCO(int enemySummonerId, bool isSpell2)
        {
            // Firebase.
            var updateSummonerSpellNodeTask = UpdateSummonerSpellNodeTask(enemySummonerId, isSpell2);
            Debug.Log("Creating table in FireBase");
            yield return new WaitUntil(() => updateSummonerSpellNodeTask.IsCompleted);
        }

        async Task<bool> UpdateSummonerSpellNodeTask(int enemySummonerId, bool isSpell2)
        {
            await databaseReference.
                Child(room.match.matchId).
                Child("summoners").
                Child(enemySummonerId.ToString()).
                SetRawJsonValueAsync(JsonUtility.ToJson(room.match.summoners[enemySummonerId]));
            //await UpdateTimestamp(enemySummonerId, isSpell2);
            return true;
        }

    }
}
