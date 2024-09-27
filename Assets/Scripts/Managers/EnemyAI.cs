using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyAI : MonoBehaviour
{
    private State state;
    [SerializeField] private List<AIGameStateSO> gameStates;

    private float gameStateTimer;
    private float gameStateTimerMax = 1f;
    private AlertMetrics alertMetrics;
    private ActionType actionType;
    private CardSO actionCard;

    private CardSO worker; // 1
    private CardSO defense; // 1
    private CardSO economy; // 0 or 1
    private List<CardSO> meleeUnits = new List<CardSO>(); // 2+
    private List<CardSO> rangedUnits = new List<CardSO>(); // 1+
    private List<CardSO> spells = new List<CardSO>(); // 1+

    private void Awake()
    {
        state = State.Idle;
    }

    private void Start()
    {
        ReadLoadout();
        /*
        for (int i = 0; i < 10; i++)
        {
            PlayerRed.Instance.SpawnCharacter(worker);
        }
        Debug.Log(DevelopOrUpgradeEconomy());
        ExecuteAction();
        */
    }

    private void Update()
    {
        /*
        gameStateTimer += Time.deltaTime;
        if (gameStateTimer >= gameStateTimerMax)
        {
            DetermineGameState();
            gameStateTimer = 0f;
        }
        */
    }

    private void ReadLoadout()
    {
        foreach (CardSO card in PlayerRed.Instance.GetLoadout())
        {
            switch (card.cardType)
            {
                case CardSO.CardType.Building:
                    if (card.spawnableObject.gameObject.GetComponent<Building>().buildingType == BuildingType.Defense)
                        defense = card;
                    else
                        economy = card;
                    break;
                case CardSO.CardType.Character:
                    CharacterType type = card.spawnableObject.gameObject.GetComponent<Character>().characterType;
                    switch (type)
                    {
                        case CharacterType.Melee:
                            meleeUnits.Add(card);
                            break;
                        case CharacterType.Ranged:
                            rangedUnits.Add(card);
                            break;
                        case CharacterType.Worker:
                            worker = card;
                            break;
                    }
                    break;
                case CardSO.CardType.Spell:
                    spells.Add(card);
                    break;
            }
        }
    }

    #region Game State Calculations
    private void DetermineGameState()
    {
        AlertMetrics newAlertMetrics;
        CalculateAOC(out newAlertMetrics.areaOfControl);
        CalculateEMS(out newAlertMetrics.effectiveMilitaryStrength);
        CalculateGPM(out newAlertMetrics.goldPerMinute);

        if (!alertMetrics.Equals(newAlertMetrics))
        {
            alertMetrics = newAlertMetrics;
            ChooseAction();
        }
    }

    private void CalculateAOC(out AlertLevel areaOfControl)
    {
        float mapSizeMultiplier = GameManager.Instance.GetMapSize() / 30;
        float AOC = PlayerRed.Instance.GetFurthestControlledArea() / PlayerBlue.Instance.GetFurthestControlledArea();

        if (AOC > mapSizeMultiplier)
            areaOfControl = AlertLevel.Favored;
        else if (AOC < (1 / mapSizeMultiplier))
            areaOfControl = AlertLevel.Unfavored;
        else
            areaOfControl = AlertLevel.Even;
    }

    private void CalculateEMS(out AlertLevel effectiveMilitaryStrength)
    {
        float EMS = GetEMSFromList(PlayerRed.Instance.GetSpawnedMilitary()) / (float)GetEMSFromList(PlayerBlue.Instance.GetSpawnedMilitary());

        if (EMS > 2)
            effectiveMilitaryStrength = AlertLevel.Favored;
        else if (EMS < .5f)
            effectiveMilitaryStrength = AlertLevel.Unfavored;
        else
            effectiveMilitaryStrength = AlertLevel.Even;
    }

    private int GetEMSFromList(List<GameObject> MilitaryUnits)
    {
        int EMS = 0;
        foreach (GameObject unit in MilitaryUnits)
            EMS += unit.GetComponent<Character>().GetUnitStrength();

        return EMS;
    }

    private void CalculateGPM(out AlertLevel goldPerMinute)
    {
        float GPM = GetGPMFromList(PlayerRed.Instance.GetSpawnedEconomy()) / (float)GetGPMFromList(PlayerBlue.Instance.GetSpawnedEconomy());

        if (GPM > 1.5f)
            goldPerMinute = AlertLevel.Favored;
        else if (GPM < .666f)
            goldPerMinute = AlertLevel.Unfavored;
        else
            goldPerMinute = AlertLevel.Even;
    }

    private int GetGPMFromList(List<GameObject> EconomyUnits)
    {
        int GPM = 600;
        foreach (GameObject unit in EconomyUnits)
        {
            if (unit.GetComponent<Character>() != null)
                GPM += unit.GetComponent<Character>().GetAttack() * 3;
            else if (unit.GetComponent<Building>() != null)
                GPM += unit.GetComponent<Building>().GetAttack() * 60;
        }

        return GPM;
    }
    #endregion

    #region Deciding Actions
    private void ChooseAction()
    {
        List<int> actionOrder = new List<int>();

        foreach (AIGameStateSO gameStateSO in gameStates)
        {
            if (alertMetrics.areaOfControl == gameStateSO.AOC && alertMetrics.effectiveMilitaryStrength == gameStateSO.EMS && alertMetrics.goldPerMinute == gameStateSO.GPM)
            {
                actionOrder = GetActionOrder(gameStateSO.DecisionTable);
                break;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            bool actionFound = false;
            switch (actionOrder[i])
            {
                case 1: //Develop or Upgrade Economy
                    actionFound = DevelopOrUpgradeEconomy();
                    break;
                case 2: //Upgrade Military
                    actionFound = UpgradeMilitary();
                    break;
                case 3: //Build Defense
                    actionFound = BuildDefense();
                    break;
                case 4: //Develop Military or Cast Spell
                    actionFound = DevelopMilitaryOrCastSpell();
                    break;
            }

            if (actionFound)
                break;
        }
    }

    private List<int> GetActionOrder(List<int> decisionTable)
    {
        int totalWeight = 100;
        List<int> weights = new List<int> { 60, 30, 8, 2 };
        List<int> actionOrder = new List<int>();
        
        for (int i = 4; i > 0; i--)
        {
            int rand = Random.Range(0, totalWeight);
 
            for (int j = 0; j < i; j++)
            {
                if (rand < weights[j])
                {
                    actionOrder.Add(decisionTable[j]);
                    totalWeight -= weights[j];
                    decisionTable.Remove(decisionTable[j]);
                    weights.Remove(weights[j]);            
                    break;
                }

                rand -= weights[j];
            }
        }

        return actionOrder;
    }

    private bool DevelopOrUpgradeEconomy()
    {
        int numWorkers = PlayerRed.Instance.GetNumberOfWorkers();
        if (numWorkers < GameManager.Instance.GetMaxWorkerAmount() / 2)
        {
            actionType = ActionType.Spawn;
            actionCard = worker;
            return true;
        }
        else if (numWorkers < GameManager.Instance.GetMaxWorkerAmount())
        {
            switch (alertMetrics.goldPerMinute)
            {
                case AlertLevel.Unfavored:
                    return EconomyBranch1();
                case AlertLevel.Even:
                    int chanceBlue = 50;

                    if (worker.level != worker.upgradeCost.Count)
                        return EconomyBranch2(chanceBlue);
                    else if (economy != null)
                        if (economy.level != economy.upgradeCost.Count)
                            return EconomyBranch2(chanceBlue);

                    return EconomyBranch1();
                case AlertLevel.Favored:
                    chanceBlue = 25;

                    if (worker.level != worker.upgradeCost.Count)
                        return EconomyBranch2(chanceBlue);
                    else if (economy != null)
                        if (economy.level != economy.upgradeCost.Count)
                            return EconomyBranch2(chanceBlue);

                    return EconomyBranch1();
            }
        }
        else 
        {
            if (worker.level != worker.upgradeCost.Count)
            {
                actionType = ActionType.Upgrade;
                actionCard = worker;
                return true;
            }
            else if (economy != null)
            {
                if (!MapManager.Instance.buildingSlots[2].GetComponent<BuildingSlot>().ContainsBuilding())
                {
                    actionType = ActionType.Build;
                    actionCard = economy;
                    return true;
                }
                else if (MapManager.Instance.buildingSlots[2].GetComponent<BuildingSlot>().GetBuilding().buildingType == BuildingType.Farm
                    && economy.level != economy.upgradeCost.Count)
                {
                    actionType = ActionType.Upgrade;
                    actionCard = economy;
                    return true;
                }
            }
        }

        return false;
    }
    private bool EconomyBranch1() //Build Economy
    {
        if (economy == null)
        {
            actionType = ActionType.Spawn;
            actionCard = worker;
        }
        else
        {
            int x = (GameManager.Instance.GetMaxWorkerAmount() - PlayerRed.Instance.GetNumberOfWorkers()) * 10;
            if (MapManager.Instance.buildingSlots[2].GetComponent<BuildingSlot>().ContainsBuilding())
                x += 0;
            else
                x += 50;

            int rand = Random.Range(0, x);

            if (x > 50 && rand <= 50)
            {
                actionType = ActionType.Build;
                actionCard = economy;
            }
            else
            {
                actionType = ActionType.Spawn;
                actionCard = worker;
            }
        }

        return true;
    }
    private bool EconomyBranch2(int chanceBlue) //Upgrade Economy
    {
        int rand = Random.Range(0, 100);

        if (rand < chanceBlue)
            return EconomyBranch1();

        actionType = ActionType.Upgrade;

        if (economy == null)
            actionCard = worker;
        else if (economy.level == economy.upgradeCost.Count)
            actionCard = worker;
        else if (worker.level == worker.upgradeCost.Count)
            actionCard = economy;
        else
        {
            int x = 0;
            Building building = MapManager.Instance.buildingSlots[2].GetComponent<BuildingSlot>().GetBuilding();
            if (building != null)
            {
                if (building.buildingType == BuildingType.Farm)
                    x = 50;
                else
                    x = -100;
            }
            else
                x = -50;

            rand = Random.Range(-100, 100) + x;

            if (rand < 0)
                actionCard = worker;
            else
                actionCard = economy;
        } 

        return true;
    }

    private bool UpgradeMilitary()
    {
        List<CardSO> cards = new List<CardSO>();
        foreach (CardSO card in meleeUnits)
        {
            if (card.level == card.upgradeCost.Count)
                continue;
            
            for (int i = 0; i < card.timesCasted; i++)
                cards.Add(card);
        }

        foreach (CardSO card in rangedUnits)
        {
            if (card.level == card.upgradeCost.Count)
                continue;

            for (int i = 0; i < card.timesCasted; i++)
                cards.Add(card);
        }

        if (defense.level != defense.upgradeCost.Count)
            for (int i = 0; i < defense.timesCasted; i++)
                cards.Add(defense);

        if (cards.Count == 0)
            return false;


        return true;
    }
    private bool BuildDefense()
    {
        return false;
    }
    private bool DevelopMilitaryOrCastSpell()
    {
        return false;
    }
    #endregion  
    
    private void ExecuteAction()
    {
        int Gold = PlayerRed.Instance.GetGold();
        switch (actionType)
        {
            case ActionType.Spawn:
                if (Gold >= actionCard.cardCost[actionCard.level])
                    PlayerRed.Instance.SpawnCharacter(actionCard);
                break;
            case ActionType.Upgrade:
                if (Gold >= actionCard.upgradeCost[actionCard.level - 1])
                {
                    PlayerRed.Instance.SubtractGold(actionCard.upgradeCost[actionCard.level - 1]);
                    actionCard.IncreaseCardLevel();
                }
                break;
            case ActionType.Build:
                if (Gold >= actionCard.cardCost[actionCard.level])
                    PlayerRed.Instance.BuildBuilding(actionCard, MapManager.Instance.buildingSlots[2]);
                break;
            case ActionType.Cast:

                break;   
        }
    }
    
}

public enum AlertLevel
{
    Unfavored,
    Even,
    Favored
}

public enum State
{
    Idle,
    DevelopingEconomy,
    DevelopingMilitary,
    CreatingDefenses,
    LaunchingAttack,
    UpgradingEconomy,
    UpgradingMilitary,
}

[System.Serializable]
public struct AlertMetrics
{
    public AlertLevel areaOfControl;
    public AlertLevel effectiveMilitaryStrength;
    public AlertLevel goldPerMinute;
}

public enum ActionType
{
    Spawn,
    Upgrade,
    Build,
    Cast
}