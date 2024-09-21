using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageTextIndicator : MonoBehaviour
{
	[SerializeField] private GameObject damageTextObject;
	[SerializeField] private GameObject hasProgressGameObject;

	private IDamageable character;

	private void Start()
	{
		character = hasProgressGameObject.GetComponent<IDamageable>();
		if (character == null)
			Debug.LogError("Gameobject does not have a component that implements Character");

		character.OnDamageTaken += CharacterDamageTaken;

	}

	private void CharacterDamageTaken(object sender, IDamageable.OnDamageTakenEventArgs e)
	{
		Entity characterHead = sender as Entity;
		Vector3 spawnPoint = characterHead.transform.position + new Vector3(0, 2, 0);
		GameObject damageText = Instantiate(damageTextObject, spawnPoint, Quaternion.identity);
		damageText.transform.SetParent(characterHead.transform);
		damageText.GetComponent<TextMeshPro>().text = e.damage.ToString();
		if (gameObject.activeSelf)
			StartCoroutine(MoveAndDestroyText(damageText));
	}

	private IEnumerator MoveAndDestroyText(GameObject textObject)
	{
		float duration = 0.5f;
		float elapsedTime = 0f;
		Vector3 direction = Vector3.up;

		while (elapsedTime < duration && gameObject.activeSelf)
		{
			textObject.transform.position += direction * Time.deltaTime;
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		Destroy(textObject);
	}
}
