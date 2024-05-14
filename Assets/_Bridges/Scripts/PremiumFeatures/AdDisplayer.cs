using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using EasyMobile.Internal;

namespace SgLib
{
    public class AdDisplayer : MonoBehaviour
    {
        // public static AdDisplayer Instance { get; private set; }
        public GameManager GM;
        public static event Action adsRemoved;

        [Header("BANNER AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show banner ad")]
        public bool showBannerAd = true;

        [Header("INTERSTITIAL AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show interstitial ad")]
        public bool showInterstitialAd = true;
        [Tooltip("Show interstitial ad every [how many] games")]
        public int gamesPerInterstitial = 3;
        [Tooltip("How many seconds after game over that interstitial ad is shown")]
        public float showInterstitialDelay = 2f;
        [HideInInspector]
        public bool adsReady = false;

        [Header("REWARDED AD DISPLAY CONFIG")]
        [Tooltip("Whether or not to show reward ad")]
        public bool showRewardAd = true;
        [Tooltip("Check to allow watching ad to continue a lost game")]
        public bool watchAdToContinueGame = true;
        [Tooltip("Check to allow watching ad to earn coins")]
        public bool watchAdToEarnCoins = true;
        [Tooltip("How many coins the user earns after watching a rewarded ad")]
        public int rewardedCoins = 50;

        public static event System.Action CompleteRewardedAdToRecoverLostGame;
        public static event System.Action CompleteRewardedAdToEarnCoins;
        public static event Action RewardedAdCompleted;

        private static int gameCount = 0;
        private const string AD_REMOVE_STATUS_PPKEY = "EM_REMOVE_ADS";
        private const int AD_ENABLED = 1;
        private const int AD_DISABLED = -1;

        private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromHours(4);
        private DateTime appOpenExpireTime;
        private AppOpenAd appOpenAd;
        private BannerView bannerView;
        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;
        private float deltaTime;

        [Header("Banner Ads Id")]
        public string bannerAndroid = "ca-app-pub-8469177233838439/2478410055";
        public string bannerIos = "";

        [Header("Interstial Ads Id")]
        public string interstialAndroid = "ca-app-pub-8469177233838439/8246447956";
        public string interstialIos = "";

        [Header("Reward Ads Id")]
        public string rewardAndroid = "ca-app-pub-8469177233838439/4774106807";
        public string rewardIos = "";

        [Header("Open App Ads Id")]
        public string openAppAndroid = "ca-app-pub-8469177233838439/3077881758";
        public string openAppIos = "";

        #region UNITY MONOBEHAVIOR METHODS
        public void Start()
        {
            if (PremiumFeaturesManager.Instance.testAds)
            {
#if UNITY_ANDROID || UNITY_IOS || !EDITOR
                MobileAds.SetiOSAppPauseOnBackground(true);

                List<String> deviceIds = new List<String>() { AdRequest.TestDeviceSimulator };

                // Add some test device IDs (replace with your own device IDs).
#if UNITY_IPHONE
                deviceIds.Add("96e23e80653bb28980d3f40beb58915c");
#elif UNITY_ANDROID
                deviceIds.Add("75EF8D155528C04DACBBA6F36F433035");
#endif
                // Configure TagForChildDirectedTreatment and test device IDs.
                RequestConfiguration requestConfiguration =
                    new RequestConfiguration.Builder()
                    .SetTagForChildDirectedTreatment(TagForChildDirectedTreatment.Unspecified)
                    .SetTestDeviceIds(deviceIds).build();
                MobileAds.SetRequestConfiguration(requestConfiguration);

                // Initialize the Google Mobile Ads SDK.
                MobileAds.Initialize(HandleInitCompleteAction);
#endif
            }


            if (PremiumFeaturesManager.Instance.dontShowOpenAppAd)
            {
                RequestAndLoadAppOpenAd();
                ShowAppOpenAd();
            }
            if (showBannerAd)
            {
                RequestBannerAd();
            }
            if (showRewardAd)
            {
                RequestAndLoadRewardedAd();
            }
        }

        private void HandleInitCompleteAction(InitializationStatus initstatus)
        {
            Debug.Log("Initialization complete.");

            // Callbacks from GoogleMobileAds are not guaranteed to be called on
            // the main thread.
            // In this example we use MobileAdsEventExecutor to schedule these calls on
            // the next Update() loop.
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
            });
        }

        #endregion

        #region HELPER METHODS

        private AdRequest CreateAdRequest()
        {
            return new AdRequest.Builder()
                .AddKeyword("unity-admob-sample")
                .Build();
        }

        #endregion

        #region INTERSTITIAL ADS

        public void RequestAndLoadInterstitialAd()
        {
            PrintStatus("Requesting Interstitial ad.");

#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = interstialAndroid;
#elif UNITY_IPHONE
        string adUnitId = interstialIos;
#else
            string adUnitId = "unexpected_platform";
#endif

            // Clean up interstitial before using it
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
            }

            // Load an interstitial ad
            InterstitialAd.Load(adUnitId, CreateAdRequest(),
                (InterstitialAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("Interstitial ad failed to load with error: " +
                            loadError.GetMessage());
                        return;
                    }
                    else if (ad == null)
                    {
                        PrintStatus("Interstitial ad failed to load.");
                        return;
                    }

                    PrintStatus("Interstitial ad loaded.");
                    interstitialAd = ad;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("Interstitial ad opening.");
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("Interstitial ad closed.");
                        GM.RestartGame(0.01f);

                    };
                    ad.OnAdImpressionRecorded += () =>
                    {
                        PrintStatus("Interstitial ad recorded an impression.");
                    };
                    ad.OnAdClicked += () =>
                    {
                        PrintStatus("Interstitial ad recorded a click.");
                    };
                    ad.OnAdFullScreenContentFailed += (AdError error) =>
                    {
                        PrintStatus("Interstitial ad failed to show with error: " +
                                    error.GetMessage());
                    };
                    ad.OnAdPaid += (AdValue adValue) =>
                    {
                        string msg = string.Format("{0} (currency: {1}, value: {2}",
                                                   "Interstitial ad received a paid event.",
                                                   adValue.CurrencyCode,
                                                   adValue.Value);
                        PrintStatus(msg);
                    };
                });
        }

        public void ShowInterstitialAd()
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
            }
            else
            {
                PrintStatus("Interstitial ad is not ready yet.");
            }
        }

        public void DestroyInterstitialAd()
        {
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
            }
        }

        #endregion

        #region REWARDED ADS

        public void RequestAndLoadRewardedAd()
        {
            PrintStatus("Requesting Rewarded ad.");
#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = rewardAndroid;
#elif UNITY_IPHONE
        string adUnitId = rewardIos;
#else
            string adUnitId = "unexpected_platform";
#endif

            // create new rewarded ad instance
            RewardedAd.Load(adUnitId, CreateAdRequest(),
                (RewardedAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("Rewarded ad failed to load with error: " +
                                    loadError.GetMessage());
                        return;
                    }
                    else if (ad == null)
                    {
                        PrintStatus("Rewarded ad failed to load.");
                        return;
                    }

                    PrintStatus("Rewarded ad loaded.");
                    rewardedAd = ad;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("Rewarded ad opening.");
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("Rewarded ad closed.");
                    };
                    ad.OnAdImpressionRecorded += () =>
                    {
                        PrintStatus("Rewarded ad recorded an impression.");
                    };
                    ad.OnAdClicked += () =>
                    {
                        PrintStatus("Rewarded ad recorded a click.");
                    };
                    ad.OnAdFullScreenContentFailed += (AdError error) =>
                    {
                        PrintStatus("Rewarded ad failed to show with error: " +
                                   error.GetMessage());
                    };
                    ad.OnAdPaid += (AdValue adValue) =>
                    {
                        string msg = string.Format("{0} (currency: {1}, value: {2}",
                                                   "Rewarded ad received a paid event.",
                                                   adValue.CurrencyCode,
                                                   adValue.Value);
                        PrintStatus(msg);
                    };
                });
        }

        public void ShowRewardedAd()
        {
            if (rewardedAd != null)
            {
                rewardedAd.Show((Reward reward) =>
                {
                    // PrintStatus("Rewarded ad granted a reward: " + reward.Amount);
                    if (RewardedAdCompleted != null)
                    {
                        RewardedAdCompleted(); // Gọi sự kiện khi người dùng nhận được phần thưởng
                    }
                });
            }
            else
            {
                PrintStatus("Rewarded ad is not ready yet.");
            }
        }

        #endregion

        #region BANNER ADS

        public void RequestBannerAd()
        {
            PrintStatus("Requesting Banner ad.");

            // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = bannerAndroid;
#elif UNITY_IPHONE
        string adUnitId = bannerIos;
#else
            string adUnitId = "unexpected_platform";
#endif

            // Clean up banner before reusing
            if (bannerView != null)
            {
                bannerView.Destroy();
            }

            // Create a 320x50 banner at top of the screen
            bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

            // Add Event Handlers
            bannerView.OnBannerAdLoaded += () =>
            {
                PrintStatus("Banner ad loaded.");
            };
            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                PrintStatus("Banner ad failed to load with error: " + error.GetMessage());
            };
            bannerView.OnAdImpressionRecorded += () =>
            {
                PrintStatus("Banner ad recorded an impression.");
            };
            bannerView.OnAdClicked += () =>
            {
                PrintStatus("Banner ad recorded a click.");
            };
            bannerView.OnAdFullScreenContentOpened += () =>
            {
                PrintStatus("Banner ad opening.");
            };
            bannerView.OnAdFullScreenContentClosed += () =>
            {
                PrintStatus("Banner ad closed.");
            };
            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                string msg = string.Format("{0} (currency: {1}, value: {2}",
                                            "Banner ad received a paid event.",
                                            adValue.CurrencyCode,
                                            adValue.Value);
                PrintStatus(msg);
            };

            // Load a banner ad
            bannerView.LoadAd(CreateAdRequest());
        }

        public void DestroyBannerAd()
        {
            if (bannerView != null)
            {
                bannerView.Destroy();
            }
        }

        #endregion

        #region APPOPENADS

        public bool IsAppOpenAdAvailable
        {
            get
            {
                return (appOpenAd != null
                        && appOpenAd.CanShowAd()
                        && DateTime.Now < appOpenExpireTime);
            }
        }

        public void OnAppStateChanged(AppState state)
        {
            // Display the app open ad when the app is foregrounded.
            UnityEngine.Debug.Log("App State is " + state);

            // OnAppStateChanged is not guaranteed to execute on the Unity UI thread.
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (state == AppState.Foreground)
                {
                    ShowAppOpenAd();
                }
            });
        }

        public void RequestAndLoadAppOpenAd()
        {
            PrintStatus("Requesting App Open ad.");
#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = openAppAndroid;
#elif UNITY_IPHONE
        string adUnitId = openAppIos;
#else
            string adUnitId = "unexpected_platform";
#endif

            // destroy old instance.
            if (appOpenAd != null)
            {
                DestroyAppOpenAd();
            }

            // Create a new app open ad instance.
            AppOpenAd.Load(adUnitId, ScreenOrientation.Portrait, CreateAdRequest(),
                (AppOpenAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null)
                    {
                        PrintStatus("App open ad failed to load with error: " +
                            loadError.GetMessage());
                        return;
                    }
                    else if (ad == null)
                    {
                        PrintStatus("App open ad failed to load.");
                        return;
                    }

                    PrintStatus("App Open ad loaded. Please background the app and return.");
                    this.appOpenAd = ad;
                    this.appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                    ad.OnAdFullScreenContentOpened += () =>
                    {
                        PrintStatus("App open ad opened.");
                    };
                    ad.OnAdFullScreenContentClosed += () =>
                    {
                        PrintStatus("App open ad closed.");
                    };
                    ad.OnAdImpressionRecorded += () =>
                    {
                        PrintStatus("App open ad recorded an impression.");
                    };
                    ad.OnAdClicked += () =>
                    {
                        PrintStatus("App open ad recorded a click.");
                    };
                    ad.OnAdFullScreenContentFailed += (AdError error) =>
                    {
                        PrintStatus("App open ad failed to show with error: " +
                            error.GetMessage());
                    };
                    ad.OnAdPaid += (AdValue adValue) =>
                    {
                        string msg = string.Format("{0} (currency: {1}, value: {2}",
                                                   "App open ad received a paid event.",
                                                   adValue.CurrencyCode,
                                                   adValue.Value);
                        PrintStatus(msg);
                    };
                });
        }

        public void DestroyAppOpenAd()
        {
            if (this.appOpenAd != null)
            {
                this.appOpenAd.Destroy();
                this.appOpenAd = null;
            }
        }

        public void ShowAppOpenAd()
        {
            if (!IsAppOpenAdAvailable)
            {
                return;
            }
            appOpenAd.Show();
            PremiumFeaturesManager.Instance.dontShowOpenAppAd = false;
        }

        #endregion

        #region Utility

        ///<summary>
        /// Log the message and update the status text on the main thread.
        ///<summary>
        private void PrintStatus(string message)
        {
            Debug.Log(message);
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
            });
        }

        #endregion

        void OnEnable()
        {
            GameManager.NewGameEvent += OnNewGameEvent;
        }

        void OnDisable()
        {
            GameManager.NewGameEvent -= OnNewGameEvent;
        }

        void OnNewGameEvent(GameEvent e)
        {
            if (e == GameEvent.GameOver)
            {
                // Show interstitial ad
                if (showInterstitialAd)
                {
                    gameCount++;

                    if (gameCount >= gamesPerInterstitial)
                    {
                        adsReady = true;
                        RequestAndLoadInterstitialAd();
                        gameCount = 0;
                    }
                    else
                    {
                        adsReady = false;
                    }
                }
            }
        }
        public bool CanShowRewardedAd()
        {
            if (rewardedAd != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsAdRemoved()
        {
            return (StorageUtil.GetInt(AD_REMOVE_STATUS_PPKEY, AD_ENABLED) == AD_DISABLED);
        }

        public void RemoveAds()
        {
            // Destroy banner ad if any.
            DestroyBannerAd();
            bannerAndroid = "";
            bannerIos = "";
            interstialAndroid = "";
            interstialIos = "";
            rewardAndroid = "";
            rewardIos = "";
            openAppAndroid = "";
            openAppIos = "";

            // Store ad removal status.
            StorageUtil.SetInt(AD_REMOVE_STATUS_PPKEY, AD_DISABLED);
            StorageUtil.Save();

            // Fire event
            if (adsRemoved != null)
                adsRemoved();

            Debug.Log("Ads were removed.");
        }

        public static void ResetRemoveAds()
        {
            // Update ad removal status.
            StorageUtil.SetInt(AD_REMOVE_STATUS_PPKEY, AD_ENABLED);
            StorageUtil.Save();

            Debug.Log("Ads were re-enabled.");
        }

        public void ShowRewardedAdToRecoverLostGame()
        {
            if (CanShowRewardedAd() && showRewardAd)
            {
                RewardedAdCompleted += OnCompleteRewardedAdToRecoverLostGame;
                ShowRewardedAd();
            }
        }

        void OnCompleteRewardedAdToRecoverLostGame()
        {
            // Unsubscribe
            RewardedAdCompleted -= OnCompleteRewardedAdToRecoverLostGame;

            // Fire event
            if (CompleteRewardedAdToRecoverLostGame != null)
            {
                CompleteRewardedAdToRecoverLostGame();
            }
        }

        public void ShowRewardedAdToEarnCoins()
        {
            if (CanShowRewardedAd() && showRewardAd)
            {
                RewardedAdCompleted += OnCompleteRewardedAdToEarnCoins;
                ShowRewardedAd();
            }
        }

        void OnCompleteRewardedAdToEarnCoins()
        {
            // Unsubscribe
            RewardedAdCompleted -= OnCompleteRewardedAdToEarnCoins;

            // Fire event
            if (CompleteRewardedAdToEarnCoins != null)
            {
                CompleteRewardedAdToEarnCoins();
            }
        }
    }
}
