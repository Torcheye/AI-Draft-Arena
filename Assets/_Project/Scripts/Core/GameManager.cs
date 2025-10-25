using UnityEngine;

namespace AdaptiveDraftArena.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        public GameConfig Config => config;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Services will be initialized here
            Debug.Log("GameManager initialized");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
