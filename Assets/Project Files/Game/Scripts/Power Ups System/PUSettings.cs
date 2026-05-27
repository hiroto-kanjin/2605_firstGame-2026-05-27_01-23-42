using UnityEngine;

namespace Watermelon
{
    public abstract class PUSettings : ScriptableObject
    {
        [Order(-1)]
        [SerializeField] PUType type;
        public PUType Type => type;

        [Group("Refs")]
        [Space]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [Group("Refs")]
        [SerializeField] GameObject behaviorPrefab;
        public GameObject BehaviorPrefab => behaviorPrefab;

        [Group("Refs")]
        [SerializeField] AudioClipToggle customAudioClip;
        public AudioClipToggle CustomAudioClip => customAudioClip;

        [Group("Variables")]
        [SerializeField] int defaultAmount;
        public int DefaultAmount => defaultAmount;

        [Group("Variables")]
        [SerializeField] string description;
        public string Description => description;

        [Group("Variables")]
        [SerializeField] int requiredLevel;
        public int RequiredLevel => requiredLevel;

        [LineSpacer("UI")]
        [Group("UI")]
        [SerializeField] bool visualiseActiveState = false;
        public bool VisualiseActiveState => visualiseActiveState;

        [Group("UI")]
        [SerializeField] Color backgroundColor = Color.white;
        public Color BackgroundColor => backgroundColor;

        [LineSpacer("Purchase")]
        [Group("Purchase")]
        [SerializeField] PurchaseType purchaseOption;
        public PurchaseType PurchaseOption => purchaseOption;

        [Group("Purchase"), ShowIf("IsCurrencyPurchaseType")]
        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;

        [Group("Purchase"), ShowIf("IsCurrencyPurchaseType")]
        [SerializeField] int price;
        public int Price => price;

        [Group("Purchase")]
        [SerializeField] int purchaseAmount;
        public int PurchaseAmount => purchaseAmount;

        [LineSpacer("Floating Text")]
        [SerializeField] string floatingMessage;
        public string FloatingMessage => floatingMessage;

        [System.NonSerialized]
        private PUSave save;
        public PUSave Save => save;

        public bool IsUnlocked { get => save.IsUnlocked; set => save.IsUnlocked = value; }

        public void InitialiseSave(PowerUpsRemoteConfigData.PowerUp remoteConfigData)
        {
            save = SaveController.GetSaveObject<PUSave>(string.Format("powerUp_{0}", type));

            if(remoteConfigData != null)
            {
                purchaseOption = PurchaseType.Currency;
                price = remoteConfigData.price;

                CurrencyType currency;
                if(System.Enum.TryParse(remoteConfigData.currency, false, out currency))
                    currencyType = currency;

                purchaseAmount = remoteConfigData.purchaseCount;
            }

            // Set default amount if amount is equal -1
            if (save.Amount == -1)
            {
                int amount = defaultAmount;

                if(remoteConfigData != null)
                    amount = remoteConfigData.defaultCount;

                save.Amount = amount;
            }
        }

        public abstract void Init();

        public bool HasEnoughCurrency()
        {
            return CurrencyController.HasAmount(currencyType, price);
        }

        private bool IsCurrencyPurchaseType()
        {
            return purchaseOption == PurchaseType.Currency;
        }

        public enum PurchaseType { Currency, RewardedVideo }
    }
}
