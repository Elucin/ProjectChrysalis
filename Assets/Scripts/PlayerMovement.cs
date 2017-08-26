using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {
	//Constants
	public float RUN_SPEED = 8f;
	public float SPRINT_SPEED = 12f;
	public float SPRINT_TAP_DELAY = 0.5f;

	//Components
	Animator anim;
	Rigidbody rigidBody;
	Camera cam;

	//Animation Parameters
	float zSpeed;
	float xSpeed;

	//Other
	public float accel = 8f;
	float tapDelay = 0f;

	//Status Booleans
	bool isCheckingForSprint = false;
	bool isSprinting = false;
	bool isMoving = false;

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
		rigidBody = GetComponent<Rigidbody> ();
		cam = Camera.main;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	
	// Update is called once per frame
	void Update () {
		

		#region ZMovement
		bool fwHeld = Input.GetKey (KeyCode.W);
		bool bkHeld = Input.GetKey (KeyCode.S);

		if (fwHeld || bkHeld) {
			if (fwHeld) {
				if ((!bkHeld && zSpeed < -0.1) || bkHeld)
					zSpeed = Mathf.Lerp (zSpeed, 0, Time.deltaTime * accel * 6);
				else
					zSpeed += Time.deltaTime * accel;
			} else {
				if (zSpeed > 0.1) 
					zSpeed = Mathf.Lerp (zSpeed, 0, Time.deltaTime * accel * 6);
				else
					zSpeed -= Time.deltaTime * accel;
			}
		}else {
			zSpeed = Mathf.Lerp (zSpeed, 0, Time.deltaTime * accel);
		}
		#endregion

		#region XMovement
		bool leftHeld = Input.GetKey (KeyCode.A);
		bool rightHeld = Input.GetKey (KeyCode.D);

		if (rightHeld || leftHeld) {
			if (rightHeld) {
				if ((!leftHeld && xSpeed < -0.05) || leftHeld) 
					xSpeed = Mathf.Lerp (xSpeed, 0, Time.deltaTime * accel * 4);
				else
					xSpeed += Time.deltaTime * accel;
			} else {
				if (xSpeed > 0.05) 
					xSpeed = Mathf.Lerp (xSpeed, 0, Time.deltaTime * accel * 4);
				else
					xSpeed -= Time.deltaTime * accel;
			}
		} else {
			xSpeed = Mathf.Lerp (xSpeed, 0, Time.deltaTime * accel);
		}
		#endregion

		//Determine Direction
		Vector3 direction = ((cam.transform.forward * zSpeed).normalized + (cam.transform.right * xSpeed).normalized).normalized;
		direction.y = 0f;

		//Determine Speed
		float Speed;
		isMoving = Mathf.Abs (xSpeed) >= 1f || Mathf.Abs (zSpeed) >= 1f;
		isSprinting = CheckForSprint ();
		if (isMoving)
			Speed = isSprinting ? SPRINT_SPEED : RUN_SPEED;
		else
			Speed = 0;

		//Set Velocity
		rigidBody.velocity = direction * Speed;

		//Set Animation Parameters
		SetAnimationParameters ();
		transform.RotateAround (transform.position, Vector3.up, Input.GetAxis ("Mouse X"));

		//Move this somewhere else
		if (Input.GetKeyDown (KeyCode.Escape))
			Application.Quit ();

	}

	void SetAnimationParameters()
	{
		float zSpeedCap = isSprinting ? 3f : 2f;
		anim.SetFloat ("zSpeed", Mathf.Clamp(zSpeed, -2, zSpeedCap));
		anim.SetFloat ("xSpeed", Mathf.Clamp(xSpeed, -2, 2));
	}

	bool CheckForSprint()
	{

		if (Input.GetKeyUp (KeyCode.W) && isSprinting)
			return false;

		if (tapDelay > 0f && isCheckingForSprint) {
			if (Input.GetKeyDown (KeyCode.W)) {
				isCheckingForSprint = false;
				tapDelay = 0f;
				return true;
			}
			tapDelay -= Time.deltaTime;
		} else if (tapDelay <= 0f && isCheckingForSprint)
			isCheckingForSprint = false;
			

		//Check Sprinting
		if (Input.GetKeyDown (KeyCode.W) && !isCheckingForSprint) {
			isCheckingForSprint = true;
			tapDelay = SPRINT_TAP_DELAY;
		}

		return isSprinting;

	}
}
