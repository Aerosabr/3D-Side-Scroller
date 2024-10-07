using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SnowStorm : Spell
{
	protected override IEnumerator HandleHitBox()
	{
		float duration = 0.3f;
		float elapsedTime = 0f;
		float initialSize = hitBox.size.x;

		while (elapsedTime < duration)
		{
			hitBox.size = new Vector3(Mathf.Lerp(initialSize, cardSO.Size, elapsedTime / duration), hitBox.size.y, hitBox.size.z);
			elapsedTime += Time.deltaTime;
			yield return null;
		}
		hitBox.size = new Vector3(cardSO.Size, hitBox.size.y, hitBox.size.z);
	}

	protected override IEnumerator HandleAttack()
	{
		float elapsedTime = 0f;
		while (elapsedTime < cardSO.Duration)
		{
			for (int i = 0; i < characters.Count; i++)
			{
				Character character = characters[i];
				if (character != null)
				{
					if (character.GetCurrentHealth() > 0)
					{
						character.transform.GetComponent<IEffectable>().Slowed(cardSO.Attack[cardSO.level-1]);
					}
				}
			}
			yield return null;
			elapsedTime += Time.deltaTime;
		}
		foreach (Character character in characters)
		{
			if (character.GetCurrentHealth() > 0)
			{
				character.transform.GetComponent<IEffectable>().UnSlowed(cardSO.Attack[cardSO.level - 1]);
				yield return null;
			}
		}
		yield return null;
		Destroy(gameObject);
	}

	#region Entities in Range Handler
	void OnTriggerEnter(Collider collision)
	{
		if (collision.gameObject.layer == targetLayer)
		{
			Character collidedCharacter = collision.gameObject.GetComponent<Character>();
			if (collidedCharacter != null)
			{
				if (!characters.Contains(collidedCharacter))
				{
					characters.Add(collidedCharacter);
					collidedCharacter.GetComponent<IEffectable>().Slowed(cardSO.Attack[cardSO.level-1]);
				}
			}
		}
	}
	void OnTriggerExit(Collider collision)
	{
		if (collision.gameObject.layer == targetLayer)
		{
			Character collidedCharacter = collision.gameObject.GetComponent<Character>();
			if (collidedCharacter != null)
			{
				if (characters.Contains(collidedCharacter))
				{
					characters.Remove(collidedCharacter);
					collidedCharacter.GetComponent<IEffectable>().UnSlowed(cardSO.Attack[cardSO.level - 1]);
				}
			}
		}
	}

	#endregion
}

