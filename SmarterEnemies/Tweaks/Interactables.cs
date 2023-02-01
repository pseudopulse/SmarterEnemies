using System;
using Mono.Cecil;
using MonoMod.Cil;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.AI;

namespace SmarterEnemies.Tweaks {
    public static class Interactables {
        public static void Hook() {
            On.RoR2.GenericPickupController.BodyHasPickupPermission += LetEnemiesPickup;
            On.RoR2.CharacterBody.Start += AddInteractableAI;
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += DirectorPays;
            // On.RoR2.CharacterMaster.OnBodyDeath += Died;
            IL.RoR2.GenericPickupController.AttemptGrant += HOPOOWHY;
            On.RoR2.CostTypeDef.PayCost += DirectorPays2;
            Run.onRunStartGlobal += (run) => {
                Debug.Log("adding global inventory manager");
                run.gameObject.AddComponent<GlobalInventoryManager>();
            };

            Run.onRunDestroyGlobal += (run) => {
                Debug.Log("removing global inventory manager");
                run.gameObject.RemoveComponent<GlobalInventoryManager>();
            };

            On.RoR2.GenericPickupController.AttemptGrant += (orig, self, body) => {
                orig(self, body);
                if (body.teamComponent.teamIndex == TeamIndex.Monster || body.teamComponent.teamIndex == TeamIndex.Void) {
                    if (GlobalInventoryManager.instance) {
                        #pragma warning disable
                        ItemDef def = ItemCatalog.GetItemDef(self.pickupIndex.itemIndex);
                        bool isScrap = def.ContainsTag(ItemTag.Scrap) || def.ContainsTag(ItemTag.PriorityScrap);
                        bool isBlacklisted = def.ContainsTag(ItemTag.AIBlacklist);
                        if (!isScrap) {
                            GlobalInventoryManager.instance.inventory.GiveItem(self.pickupIndex.itemIndex);
                        }
                        #pragma warning restore
                    }
                    else {
                        Debug.Log("no global inventory manager instance");
                    }
                }
                else if (body.master && body.master.minionOwnership && body.master.minionOwnership.ownerMaster) {
                    #pragma warning disable
                    body.master.minionOwnership.ownerMaster.inventory.GiveItem(self.pickupIndex.itemIndex);
                    #pragma warning restore
                }
            };

            On.RoR2.CharacterBody.Start += (orig, self) => {
                orig(self);
                if (self.teamComponent.teamIndex == TeamIndex.Monster && GlobalInventoryManager.instance) {
                    bool useAmbient = self.inventory.GetItemCount(RoR2Content.Items.UseAmbientLevel) > 0;
                    self.inventory.CopyItemsFrom(GlobalInventoryManager.instance.inventory, x => !ItemCatalog.GetItemDef(x).ContainsTag(ItemTag.AIBlacklist));
                    if (useAmbient) {
                        self.inventory.GiveItem(RoR2Content.Items.UseAmbientLevel);
                    }
                }
            };
        }

        private static bool DirectorPays(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, PurchaseInteraction self, Interactor interactor) {
            TeamComponent com = interactor.GetComponent<TeamComponent>();
            if (!com.body.isPlayerControlled) {
                return true;
            }
            else {
                return orig(self, interactor);
            }
        }

        private static CostTypeDef.PayCostResults DirectorPays2(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor interactor, GameObject purchased, Xoroshiro128Plus rng, ItemIndex avoided) {
            TeamComponent com = interactor.GetComponent<TeamComponent>();
            if (com && com.teamIndex == TeamIndex.Monster) {
                CostTypeDef.PayCostResults res = new();
                res.itemsTaken = new();
                res.equipmentTaken = new();
                CombatDirector.instancesList[0].monsterCredit -= cost;
                return res;
            }
            else {
                return orig(self, cost, interactor, purchased, rng, avoided);
            }
        }

        private static void HOPOOWHY(ILContext il) {
            ILCursor c = new(il);
            bool found = c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(0),
                x => x.MatchCallOrCallvirt<TeamComponent>("get_teamIndex")
            );

            if (found) {
                c.Index -= 2;
                c.Remove();
                c.Remove();
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
            }
            else {
                SmarterEnemies.ModLogger.LogError("Failed to apply HOPOOWHY IL hook");
            }
        }

        private static void AddInteractableAI(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self) {
            orig(self);
            if (NetworkServer.active && Util.CheckRoll(SmarterEnemies.InteractableAIChance)) {
                bool canInteract = SmarterEnemies.LetPlayerAlliesInteract ? true : self.teamComponent.teamIndex != TeamIndex.Player;
                if (canInteract && self.master && !self.isPlayerControlled && !self.isFlying && !self.isChampion) {
                    self.master.gameObject.AddComponent<InteractAI>();
                }
            }
        }

        private static void Died(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body) {
            if (SmarterEnemies.DropOnDeath && self.GetComponent<InteractAI>() && !Language.GetString(body.baseNameToken).Contains("Engineer")) {
                foreach (ItemIndex index in self.inventory.itemAcquisitionOrder) {
                    ItemDef def = ItemCatalog.GetItemDef(index);
                    #pragma warning disable
                    if (!def.inDroppableTier) {
                        continue;
                    }
                    #pragma warning restore
                    for (int i = 0; i < self.inventory.GetItemCount(index); i++) {
                        GenericPickupController.CreatePickupInfo info = new();
                        info.pickupIndex = PickupCatalog.FindPickupIndex(index);
                        info.position = body.corePosition;
                        info.rotation = Quaternion.identity;
                        float x = Run.instance.runRNG.RangeFloat(0, 100);
                        float y = Run.instance.runRNG.RangeFloat(0, 100);
                        float z = Run.instance.runRNG.RangeFloat(0, 100);
                        PickupDropletController.CreatePickupDroplet(info, body.corePosition, new Vector3(x, y, z));
                    }
                }
            }
            orig(self, body);
        }

        private static bool LetEnemiesPickup(On.RoR2.GenericPickupController.orig_BodyHasPickupPermission orig, CharacterBody body) {
            return true;
        }

        private class InteractAI : MonoBehaviour {
            public CharacterBody body => base.GetComponent<CharacterMaster>().GetBody();
            public CharacterMaster master => base.GetComponent<CharacterMaster>();
            public Interactor interactor => master.GetBody().GetComponent<Interactor>();
            public BaseAI ai => master.aiComponents[0];
            public enum InteractableType {
                Printer,
                Scrapper,
                Chest,
                None
            }
            public float stopwatch = 0f;
            private float searchDelay = 2f;
            public bool isOnCooldown = false;
            public float cooldownTimer = 0f;
            private float cooldownDelay = 5f;
            public bool isChasingTeleporter = false;
            public InteractableType last = InteractableType.None;
            public AISkillDriver targetDriver;
            private List<PurchaseInteraction> printers;
            public bool canFocusTeleporter;
            private List<ScrapperController> scrappers;
            private List<PurchaseInteraction> miscs;
            public GameObject target;
            private void Start() {
                canFocusTeleporter = Util.CheckRoll(SmarterEnemies.TeleporterAIChance);
                targetDriver = master.gameObject.AddComponent<AISkillDriver>();
                targetDriver.customName = "ChaseInteractable";
                targetDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
                targetDriver.moveTargetType = AISkillDriver.TargetType.Custom;
                targetDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
                targetDriver.maxDistance = Mathf.Infinity;
                targetDriver.minDistance = 0f;
                targetDriver.skillSlot = SkillSlot.None;
                targetDriver.resetCurrentEnemyOnNextDriverSelection = true;
                targetDriver.shouldSprint = true;
                targetDriver.requireSkillReady = false;
                targetDriver.selectionRequiresAimTarget = false;
                targetDriver.selectionRequiresOnGround = false;
                targetDriver.activationRequiresTargetLoS = false;
                targetDriver.activationRequiresAimTargetLoS = false;
                targetDriver.activationRequiresAimConfirmation = false;
                // targetDriver.ignoreNodeGraph = true;
                List<AISkillDriver> drivers = ai.skillDrivers.ToList();
                drivers.Add(targetDriver);
                ai.skillDrivers = drivers.ToArray();

                printers = GameObject.FindObjectsOfType<PurchaseInteraction>().Where(x => x.gameObject.name.Contains("Duplicator")).ToList();
                scrappers = GameObject.FindObjectsOfType<ScrapperController>().ToList();
                miscs = GameObject.FindObjectsOfType<PurchaseInteraction>().Where(
                    x => x.costType == CostTypeIndex.Money && x.available && !x.name.Contains("Quest")
                ).ToList();

                NavMeshObstacle[] obstacles = GameObject.FindObjectsOfType<NavMeshObstacle>();

                for (int i = 0; i < obstacles.Length; i++) {
                    GameObject.Destroy(obstacles[i]);
                }
            }
            private void FixedUpdate() {
                stopwatch += Time.fixedDeltaTime;
                if (stopwatch >= searchDelay && !isOnCooldown && !isChasingTeleporter) {
                    stopwatch = 0f;
                    miscs = GameObject.FindObjectsOfType<PurchaseInteraction>().Where(
                        x => x.costType == CostTypeIndex.Money || x.costType == CostTypeIndex.PercentHealth && x.available && !x.name.Contains("Quest")
                    ).ToList();

                    CheckPickups();
                    DecideBestInteractable();
                }

                if (Stage.instance && TeleporterInteraction.instance && !TeleporterInteraction.instance.isCharging && canFocusTeleporter) {
                    if (Run.instance.GetRunStopwatch() - Stage.instance.entryTime.t > SmarterEnemies.TeleporterTargetingDelay) {
                        isChasingTeleporter = true;
                        ai.customTarget.gameObject = TeleporterInteraction.instance.gameObject;
                    }
                }

                if (ai.customTarget.gameObject) {
                    target = ai.customTarget.gameObject;
                }
                else {
                    target = null;
                }

                if (isOnCooldown) {
                    cooldownTimer += Time.fixedDeltaTime;
                    if (cooldownTimer >= cooldownDelay) {
                        cooldownTimer = 0f;
                        isOnCooldown = false;
                    }
                }

                float distance = 7;
                if (ai.customTarget.gameObject && ai.customTarget.gameObject.GetComponent<GenericPickupController>()) {
                    distance = 6;
                }

                if (body && interactor && ai.customTarget.gameObject && Vector3.Distance(body.corePosition, ai.customTarget.gameObject.transform.position) < distance) {
                    GameObject target = ai.customTarget.gameObject;
                    if (target.GetComponent<ScrapperController>()) {
                        ScrapperController controller = target.GetComponent<ScrapperController>();
                        controller.AssignPotentialInteractor(interactor);
                        controller.BeginScrapping(master.inventory.GetRandomScrappableIndex());
                        isOnCooldown = true;
                        ai.customTarget.gameObject = null;
                    }
                    else {
                        interactor.maxInteractionDistance = 10f;
                        interactor.AttemptInteraction(target);
                        ai.customTarget.gameObject = null;
                        if (distance == 6) {
                            isOnCooldown = true;
                        }
                    }
                }

                if (ai.customTarget.gameObject && ai.customTarget.gameObject.GetComponent<PurchaseInteraction>() != null) {
                    PurchaseInteraction interaction = ai.customTarget.gameObject.GetComponent<PurchaseInteraction>();
                    if (interaction.available == false) {
                        ai.customTarget.gameObject = null;
                    }
                }

                ForceChaseInteractable();

                if (ai.customTarget.gameObject) {
                    ai.customTarget.Update();
                    ai.SetGoalPosition(ai.customTarget.gameObject.transform.position);
                    ai.localNavigator.Update(Time.fixedDeltaTime);
                }
            }

            private void DecideBestInteractable() {
                if (ai.customTarget.gameObject == null) {
                    PurchaseInteraction printer = printers.GetClosest(body.corePosition);
                    ScrapperController scrapper = scrappers.GetClosest(body.corePosition);
                    PurchaseInteraction misc = miscs.GetClosest(body.corePosition);

                    if (last != InteractableType.Printer && last == InteractableType.Scrapper && printer && master.inventory && master.inventory.HasScrapItems(CostToTier(printer.costType))) {
                        last = InteractableType.Printer;
                        ai.customTarget.gameObject = printer.gameObject;
                        Debug.Log("Chose printer: " + printer.name);
                    }
                    else if (last != InteractableType.Scrapper && last != InteractableType.Printer && scrapper && master.inventory && master.inventory.GetTotalItemCount() > 0 && !master.inventory.HasScrapItems()) {
                        last = InteractableType.Scrapper;
                        ai.customTarget.gameObject = scrapper.gameObject;
                        Debug.Log("Chose scrapper: " + scrapper.name);
                    }
                    else if (misc) {
                        last = InteractableType.Chest;
                        //  master.money += (uint)misc.cost;
                        ai.customTarget.gameObject = misc.gameObject;
                        Debug.Log("Chose misc: " + misc.name);
                    }
                }
            }

            private void CheckPickups() {
                #pragma warning disable
                GenericPickupController pickup = GameObject.FindObjectsOfType<GenericPickupController>().Where(pickup => pickup.pickupIndex.coinValue == 0 && !pickup.name.Contains("Quest")).ToList().GetClosest(body.corePosition);
                #pragma warning restore
                if (pickup && Vector3.Distance(body.corePosition, pickup.transform.position) < 25) {
                    Debug.Log("Chose pickup: " + pickup.gameObject.name);
                    ai.customTarget.gameObject = pickup.gameObject;
                }
            }

            private void ForceChaseInteractable() {
                if (body && body.outOfDanger) {
                    if (ai.customTarget.gameObject) {
                        ai.skillDriverUpdateTimer = 3f;
                    }
                    if (!isOnCooldown && ai.customTarget.gameObject && ai.skillDriverEvaluation.dominantSkillDriver != targetDriver) {
                        ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation {
                            dominantSkillDriver = targetDriver,
                            aimTarget = ai.customTarget,
                            target = ai.customTarget
                        };
                       //  Debug.Log("forcing next driver");
                    }
                    else {
                        if (ai.customTarget.gameObject == null && ai.skillDriverEvaluation.dominantSkillDriver == targetDriver) {
                            // Debug.Log("evaluating drivers");
                            ai.EvaluateSkillDrivers();
                        }
                    }
                }
                else {
                    // Debug.Log("not out of danger, resuming normal drivers");
                    ai.EvaluateSkillDrivers();
                }
            }

            public ItemTier CostToTier(CostTypeIndex index) {
                switch (index) {
                    case CostTypeIndex.WhiteItem:
                        return ItemTier.Tier1;
                    case CostTypeIndex.GreenItem:
                        return ItemTier.Tier2;
                    case CostTypeIndex.RedItem:
                        return ItemTier.Tier3;
                    default:
                        return ItemTier.NoTier;
                }
            }
        }
    }
}