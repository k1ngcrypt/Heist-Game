using System;
using UnityEngine;

public enum CurrencyType
{
    Knowledge = 0,
    Money = 1,
    SwissMoney = 2
}

public class CurrencyManager : MonoBehaviour
{
    [Serializable]
    private class CurrencyProgressData
    {
        public int knowledge;
        public int money;
        public int swissMoney;
    }

    [Header("Startup")]
    [SerializeField] private bool loadProgressOnAwake = true;
    [SerializeField] private bool saveProgressOnChange = true;
    [SerializeField] private string saveSlot = "default";

    [Header("Starting Values")]
    [Min(0)][SerializeField] private int startingKnowledge;
    [Min(0)][SerializeField] private int startingMoney;
    [Min(0)][SerializeField] private int startingSwissMoney;

    private const int CurrencyCount = 3;
    private readonly int[] balances = new int[CurrencyCount];
    private string SaveKey => $"Currency.Progress.{saveSlot}";
    private bool isServiceInitialized;
    private static bool isApplicationQuitting;

    private static CurrencyManager instance;
    public static CurrencyManager Instance => EnsureInstance();

    public static event Action<CurrencyType, int, int> OnCurrencyChanged;
    public static event Action OnCurrenciesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static CurrencyManager EnsureInstance()
    {
        if (isApplicationQuitting)
        {
            return null;
        }

        if (instance != null)
        {
            return instance;
        }

        instance = FindAnyObjectByType<CurrencyManager>();
        if (instance != null)
        {
            return instance;
        }

        var serviceObject = new GameObject("[Service] CurrencyManager");
        instance = serviceObject.AddComponent<CurrencyManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (!isServiceInitialized)
        {
            isServiceInitialized = true;
            ResetToStartingValues(false, false);

            if (loadProgressOnAwake)
            {
                LoadProgress();
            }
            else
            {
                RaiseAllCurrencyChanged();
            }
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public int Get(CurrencyType currencyType)
    {
        return balances[(int)currencyType];
    }

    public bool CanAfford(CurrencyType currencyType, int amount)
    {
        return amount <= 0 || Get(currencyType) >= amount;
    }

    public void Add(CurrencyType currencyType, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        Set(currencyType, Get(currencyType) + amount);
    }

    public bool TrySpend(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (!CanAfford(currencyType, amount))
        {
            return false;
        }

        Set(currencyType, Get(currencyType) - amount);
        return true;
    }

    public void Set(CurrencyType currencyType, int amount)
    {
        var index = (int)currencyType;
        var clampedAmount = Mathf.Max(0, amount);
        var previousAmount = balances[index];

        if (previousAmount == clampedAmount)
        {
            return;
        }

        balances[index] = clampedAmount;
        RaiseCurrencyChanged(currencyType, clampedAmount, clampedAmount - previousAmount);

        if (saveProgressOnChange)
        {
            SaveProgress();
        }
    }

    public void ResetToStartingValues(bool saveAfterReset = true, bool notify = true)
    {
        balances[(int)CurrencyType.Knowledge] = Mathf.Max(0, startingKnowledge);
        balances[(int)CurrencyType.Money] = Mathf.Max(0, startingMoney);
        balances[(int)CurrencyType.SwissMoney] = Mathf.Max(0, startingSwissMoney);

        if (saveAfterReset && saveProgressOnChange)
        {
            SaveProgress();
        }

        if (notify)
        {
            RaiseAllCurrencyChanged();
        }
    }

    public void SaveProgress()
    {
        var data = new CurrencyProgressData
        {
            knowledge = balances[(int)CurrencyType.Knowledge],
            money = balances[(int)CurrencyType.Money],
            swissMoney = balances[(int)CurrencyType.SwissMoney]
        };

        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public bool LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            RaiseAllCurrencyChanged();
            return false;
        }

        var json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            RaiseAllCurrencyChanged();
            return false;
        }

        var data = JsonUtility.FromJson<CurrencyProgressData>(json);
        if (data == null)
        {
            RaiseAllCurrencyChanged();
            return false;
        }

        balances[(int)CurrencyType.Knowledge] = Mathf.Max(0, data.knowledge);
        balances[(int)CurrencyType.Money] = Mathf.Max(0, data.money);
        balances[(int)CurrencyType.SwissMoney] = Mathf.Max(0, data.swissMoney);

        RaiseAllCurrencyChanged();
        return true;
    }

    private void RaiseAllCurrencyChanged()
    {
        RaiseCurrencyChanged(CurrencyType.Knowledge, balances[(int)CurrencyType.Knowledge], 0);
        RaiseCurrencyChanged(CurrencyType.Money, balances[(int)CurrencyType.Money], 0);
        RaiseCurrencyChanged(CurrencyType.SwissMoney, balances[(int)CurrencyType.SwissMoney], 0);
    }

    private void RaiseCurrencyChanged(CurrencyType currencyType, int newValue, int delta)
    {
        OnCurrencyChanged?.Invoke(currencyType, newValue, delta);
        OnCurrenciesChanged?.Invoke();
    }
}
