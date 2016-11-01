﻿///////////////////////////////////////////////////////////////
///															///
/// 		Written By Wesley Haws April 2016				///
/// 				Tested with Unity 5						///
/// 	Anyone can use this for any reason. No limitations. ///
/// 														///
/// This script is to be used in conjunction with			///
/// "AnimController.cs" and "AIBehavior.cs" script. 		///
///	This script is capable of the following:				///
/// *Detect direction hit (Play dir sepcific animations)	///
/// *Signal object death									///
/// *Play sounds when damaged								///
/// *Display visual when damaged							///
/// *Signal death when health is zero						///
/// 														///
///////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {
	[SerializeField] private Animator[] anim;
	[SerializeField] private float health = 100.0f;			//total health of object
	[SerializeField] private float regeneration = 0.0f;		//slowly regenerate health
	[SerializeField] private Texture2D guiOnHit;			//visual display when this object is damaged
	[SerializeField] private float guiFadeSpeed = 2;		//this will divide Time.deltaTime
	[Range(0.0F, 1.0F)]
	[SerializeField] private float hitSoundsVolume = 0.5f;	//how loud do you want to play these sounds?
	[SerializeField] private Camera playerCamera;			//playerCamera to show gui effects on
	[SerializeField] private AudioClip[] hitSounds;			//sounds to play when this object is damaged
	[SerializeField] private AudioClip[] gainHealthSounds;	//sound to play when gaining health
	[SerializeField] private AudioSource audioSource;		//auto filled if none applied(can be dangerous sound wise)
	[SerializeField] private GameObject deathCamera;		//for Different camera angle
	[SerializeField] private bool debugHealth = false;		//for debugging
	[SerializeField] private bool debugDirHit = false;		//for debugging
	[Space(10)]
	[Header("==== Following Requires: Animator")]
	[SerializeField] private bool staggerOnEveryHit = false;
	[SerializeField] private GameObject deathPosition;
	private float damageNumber = 0.0f;
	private bool gotHit = false;
	private float guiAlpha = 1.0f;
	private float originalVolume;
	private bool ragdolled = false;
	private bool rdLastState = false;
	private Camera originalCamera;

	void Start() {
		GameObject[] remaining = GameObject.FindGameObjectsWithTag("Player");
		string myname = this.gameObject.name+"(Clone)";
		foreach (GameObject clone in remaining) {
			if(clone.name == myname){
				GameObject.Destroy(clone);
			}
		}
		SetRagdollState (false);
		originalCamera = this.GetComponentInChildren<Camera> ();
		if (deathPosition == null) {
			deathPosition = this.transform.GetChild (6).GetChild (1).GetChild (2).gameObject;
		}
		if (deathCamera != null) {
			deathCamera.SetActive (false);
		}
		if (anim.Length < 1 || anim[0] == null) {
			if (this.GetComponentInChildren<Animator> ()) {
				anim[0] = this.GetComponentInChildren<Animator> ();
			} 
		}
		if (playerCamera == null && guiOnHit != null) {
			if (this.GetComponent<Camera> ()) {
				playerCamera = this.GetComponent<Camera> ();
			} else {
				playerCamera = this.GetComponentInChildren<Camera> ();
			}
		}
		if (audioSource == null) {
			audioSource = this.GetComponent<AudioSource> ();
		}
		originalVolume = audioSource.volume;
		health = GameObject.FindGameObjectWithTag ("GameManager").GetComponent<PlayerManager> ().currentPlayerHealth;
		regeneration = GameObject.FindGameObjectWithTag ("GameManager").GetComponent<PlayerManager> ().currentPlayerRegen;
	}
	// Update is called once per frame
	void Update () {
		if (debugHealth == true) {
			Debug.Log("Health: "+health);
		}
		if (health <= 0) {
			Death ();
		}
		if (regeneration > 0) {
			health += regeneration * Time.deltaTime;
		}
		if (gotHit == true) {
			guiAlpha -= Time.deltaTime / guiFadeSpeed;
			if (guiAlpha <= 0) {
				gotHit = false;
			}
		}
		if (ragdolled != rdLastState && health > 0 && anim[0].GetBool("grounded") == true) {
			StartCoroutine (PlayGetUpAnim ());
		}

	}
	IEnumerator PlayGetUpAnim() {
		deathCamera.SetActive (true);
		originalCamera.gameObject.SetActive(false);
		this.GetComponent<MouseLook> ().enabled = false;
		yield return new WaitForSeconds (2);
		if (anim[0].GetBool ("grounded") == true) {
			ragdolled = false;
			Vector3 currentLoc = deathPosition.transform.position;
			foreach (Animator animator in anim) {
				animator.SetTrigger ("GetUpFromBack");
			}
			this.transform.position = currentLoc;
			SetRagdollState (false);

			yield return new WaitForSeconds (5);
			originalCamera.gameObject.SetActive(true);
			this.GetComponent<MouseLook> ().enabled = true;
			deathCamera.SetActive (false);
		}
	}
	IEnumerator PlayHitSound(){
		audioSource.volume = hitSoundsVolume;
		audioSource.clip = hitSounds[UnityEngine.Random.Range(0,hitSounds.Length)];
		audioSource.Play ();
		yield return new WaitForSeconds (audioSource.clip.length);
		audioSource.volume = originalVolume;
	}
	public void ApplyDamage(float damage, GameObject sender = null, bool stagger = false) {
		health -= damage;
		guiAlpha = 1.0f;
		gotHit = true;
		//if was falling play ragdoll
		if (anim[0].GetCurrentAnimatorStateInfo (0).IsName("Falling")) {
			SetRagdollState (true);
		}
		if (hitSounds.Length > 0) {
			StartCoroutine (PlayHitSound ());
		}
		if ( (staggerOnEveryHit == true || stagger == true) && anim[0] != null) {
			foreach (Animator animator in anim) {
				animator.SetTrigger ("damaged");
			}
			if (sender == null) {
				damageNumber = 0.0f;
				foreach (Animator animator in anim) {
					animator.SetFloat ("damagedNumber", damageNumber);
					animator.SetTrigger ("damaged");
				}
			}
			else {
				Vector3 direction = (sender.transform.position - this.transform.position).normalized;
				float angle = Vector3.Angle (direction, this.transform.forward);
				if(angle > 50 && angle < 130) {//side hit
					Vector3 pos = transform.TransformPoint(sender.transform.position);
					if (pos.x < 0) {
						if(debugDirHit == true) {
							Debug.Log("Left");
						}
						damageNumber = 0.6f;
						foreach (Animator animator in anim) {
							animator.SetFloat ("damagedNumber", damageNumber);
						}
					}
					else {
						if(debugDirHit == true){
							Debug.Log("Right");
						}
						damageNumber = 1.0f;
						foreach (Animator animator in anim) {
							animator.SetFloat ("damagedNumber", damageNumber);
						}
					}
				}
				else if(angle < 50 && angle > -1) {
					if(debugDirHit == true){
						Debug.Log("Front Hit");
					}
					damageNumber = 0.0f;
					foreach (Animator animator in anim) {
						animator.SetFloat ("damagedNumber", damageNumber);
					}
				}
				else if(angle > 130 && angle < 270) {
					if(debugDirHit == true){
						Debug.Log("Back Hit");
					}
					damageNumber = 0.3f;
					foreach (Animator animator in anim) {
						animator.SetFloat ("damagedNumber", damageNumber);
					}
				}
				foreach (Animator animator in anim) {
					animator.SetTrigger ("damaged");
				}
			}
		}
		if (this.GetComponent<AIBehavior> ()) {
			this.GetComponent<AIBehavior>().memory.currentState = "Hostile";
		}
		if (this.GetComponent<AnimController> ()) {
			this.GetComponent<AnimController> ().updateState ("Hostile");
		}
	}
	public void ApplyHealth(float amount) {
		health += amount;
		if (health > 100) {
			health = 100;
		}
		if (gainHealthSounds.Length > 0) {
			audioSource.clip = gainHealthSounds [Random.Range (0, gainHealthSounds.Length)];
			audioSource.Play ();
		}
	}
	void Death(){
		if (deathCamera != null) {
			deathCamera.SetActive (true);
			originalCamera.gameObject.SetActive(false);
			this.GetComponent<MouseLook> ().enabled = false;
		}
		foreach (Animator animator in anim) {
			animator.SetBool ("dead", true);
		}
		SetRagdollState (true);
	}
	void OnGUI(){
		Color color = GUI.color;
		if (gotHit && guiOnHit != null) {
			color.a = guiAlpha;
			GUI.color = color;
			GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), guiOnHit, ScaleMode.StretchToFill);
		}
	}
	public float GetHealth() {
		return health;
	}
	public float GetRegeneration() {
		return regeneration;
	}
	public void SetRagdollState(bool newValue) {
		ragdolled = newValue;
		newValue = !newValue;
		//Get an array of components that are of type Rigidbody
		Rigidbody[] bodies=GetComponentsInChildren<Rigidbody>();

		//For each of the components in the array, treat the component as a Rigidbody and set its isKinematic property
		foreach (Rigidbody rb in bodies)
		{
//			rb.useGravity = ragdolled; 
			rb.isKinematic=newValue;
		}
		if (newValue == false) {
			GetComponent<Animator> ().enabled = false;
			this.GetComponent<MovementController> ().moveLocked = true;
		} else {
			GetComponent<Animator> ().enabled = true;
			this.GetComponent<MovementController> ().moveLocked = false;
		}
	}
}