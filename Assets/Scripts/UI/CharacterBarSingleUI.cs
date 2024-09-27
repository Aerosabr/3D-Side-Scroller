using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterBarSingleUI : MonoBehaviour, IPointerDownHandler
{ 
    [SerializeField] private TextMeshProUGUI charCost;
    [SerializeField] private Image charSprite;
    [SerializeField] private Button button;
    [SerializeField] private GameObject shadow;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private CooldownUI cooldownUI;
    private int cost;
    private CardSO cardSO;
    public void UpdateCard(CardSO cardSO)
    {
        cost = cardSO.cardCost[cardSO.level - 1];
        charCost.text = cost.ToString();
        charSprite.sprite = cardSO.backgrounds[cardSO.level - 1];
        this.cardSO = cardSO;
        cooldownUI.InitializeCooldownUI(cardSO.spawnCooldown[cardSO.level - 1]);

        if(cardSO.cardType == CardSO.CardType.Character)
        {
			button.onClick.AddListener(() =>
			{
				PlayerControlManager.Instance.CardSelected(cardSO);
                cooldownUI.gameObject.SetActive(true);
            });
		}
        else
        {
            button.gameObject.SetActive(false);
        }

        PlayerBlue.Instance.OnGoldChanged += PlayerManager_OnGoldChanged;


	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if(cardSO.cardType == CardSO.CardType.Spell)
        {
			PlayerControlManager.Instance.CardSelected(cardSO);
            CharacterBarUI.Instance.SetCurrentButtonSelected(this.transform);
		}
	}

	private void PlayerManager_OnGoldChanged(object sender, System.EventArgs e)
    {
        if (PlayerBlue.Instance.GetGold() >= cost)
            shadow.SetActive(false);
        else
            shadow.SetActive(true);
    }

    private void OnDestroy()
    {
        PlayerBlue.Instance.OnGoldChanged -= PlayerManager_OnGoldChanged;
    }
}
