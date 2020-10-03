using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Sirenix.OdinInspector;
using SRF.Components;

namespace SummsTracker
{
    public class FireBaseManager : SRSingleton<FireBaseManager>
    {
        public FirebaseAuth auth;

        [BoxGroup("User")]
        public string userId;
        [BoxGroup("User")]
        public string displayName;
        [BoxGroup("User")]
        public string email;
        bool signedIn;

        public Action OnSignedIn;

        private void Start()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(continuationAction: task =>
            {
                if (task.Exception != null)
                    Debug.LogError($"Failed to initialize Firebase with {task.Exception}");
                InitializeFirebase();
            });
        }

        void InitializeFirebase()
        {
            Debug.Log("Initializing Firebase");
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += AuthStateChanged;
            AuthStateChanged(this, null);
            SignIn();
        }

        public void OnDestroy()
        {
            auth.StateChanged -= AuthStateChanged;
        }

        void AuthStateChanged(object sender, System.EventArgs eventArgs)
        {
            //Debug.Log("AuthStateChanged");
            if (auth.CurrentUser == null)
            {
                UserChanged();
                return;
            }
            if (auth.CurrentUser.UserId != userId)
            {
                UserChanged();
                return;
            }
        }
        void UserChanged()
        {
            signedIn = auth.CurrentUser != null;
            if (!signedIn && !string.IsNullOrEmpty(userId))
            {
                Debug.Log("Signed out " + userId);
            }

            if (signedIn)
            {
                userId = auth.CurrentUser.UserId;
                displayName = auth.CurrentUser.DisplayName ?? "";
                email = auth.CurrentUser.Email ?? "";
                Debug.LogFormat("Firebase user signed in: [{0}] [{1}]", userId, email);
            }
        }

        //[Button]
        //public void SignIn(string email, string password)
        //{
        //    auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        //    {
        //        if (task.IsCanceled)
        //        {
        //            Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
        //            return;
        //        }
        //        if (task.IsFaulted)
        //        {
        //            Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
        //            return;
        //        }

        //        Firebase.Auth.FirebaseUser newUser = task.Result;
        //        //Debug.LogFormat("User signed in successfully: {0} ({1}) ({2})", newUser.DisplayName, newUser.UserId, newUser.Email);
        //    });
        //}

        [Button]
        void SignIn()
        {
            auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInAnonymouslyAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result;
                //Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                OnSignedIn?.Invoke();
            });
        }
    }
}
