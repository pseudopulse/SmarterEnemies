using System;
using RoR2.Artifacts;

namespace SmarterEnemies.Tweaks {
    public class GlobalInventoryManager : MonoBehaviour {
        public Inventory inventory => _inventory;
        private Inventory _inventory;
        public static GlobalInventoryManager instance;

        public void Start() {
            Debug.Log("spawning inventory");
            instance = this;
            _inventory = GameObject.Instantiate(Utils.Paths.GameObject.MonsterTeamGainsItemsArtifactInventory.Load<GameObject>()).GetComponent<Inventory>();
            _inventory.GetComponent<TeamFilter>().teamIndex = TeamIndex.Monster;
            NetworkServer.Spawn(_inventory.gameObject);
            GameObject.DontDestroyOnLoad(this.gameObject);
            GameObject.DontDestroyOnLoad(_inventory.gameObject);
            _inventory.gameObject.RemoveComponent<ArtifactEnabledResponse>();
            _inventory.GetComponent<EnemyInfoPanelInventoryProvider>().enabled = true;
        }

        public void OnDestroy() {
            GameObject.Destroy(_inventory.gameObject);
            instance = null;
        }
    }
}