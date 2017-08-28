using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour {
	//Constants
	public float RUN_SPEED;
	public float SPRINT_SPEED;
	public float CROUCH_SPEED;
	public float SPRINT_TAP_DELAY;
	public float JUMP_HEIGHT;

	//Layermasks
	LayerMask terrainonly = 1 << Layermasks.TERRAIN;

	//Components
	Animator anim;
	Rigidbody rigidBody;
	Camera cam;
	CapsuleCollider coll;

	//Animation Parameters
	float zSpeed;
	float xSpeed;

	//Other
	public float accel;
	float tapDelay = 0f;
	float mass;
	Direction moveDirection = Direction.None;
	PlayerActions playerActions;

	//Status Booleans
	bool isCheckingForSprint = false;
	bool isSprinting = false;
	bool isMoving = false;
	bool isCrouching = false;
	bool isGrounded = true;
	bool isLanding = false;
	bool isDodging = false;

	//Animation States
	AnimatorStateInfo currentBaseState;
	int fallingState = Animator.StringToHash("Base Layer.Falling");

	void OnEnable()
	{
		playerActions = PlayerActions.CreateWithDefaultBindings ();
	}

	void OnDisable()
	{
		playerActions.Destroy();
	}

	enum Direction
	{
		None,
		Forward,
		ForwardRight,
		Right,
		BackRight,
		Back,
		BackLeft,
		Left,
		ForwardLeft
	};

	// Use this for initialization
	void Start () {

		anim = GetComponent<Animator> ();
		rigidBody = GetComponent<Rigidbody> ();
		mass = rigidBody.mass;
		coll = GetComponent<CapsuleCollider> ();
		cam = Camera.main;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	
	// Update is called once per frame
	void Update () {

		currentBaseState = anim.GetCurrentAnimatorStateInfo (0);
		#region Crouching
		if ((playerActions.Crouch.WasPressed && !isMoving) || playerActions.Crouch.IsPressed && isDodging) {
			isCrouching = true;
		}

		if (playerActions.Crouch.WasReleased && isCrouching) {
			isCrouching = false;
		}
		#endregion

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

		//Collider
		coll = DoColliderChanges();

		//Determine Direction
		Vector3 direction = ((cam.transform.forward * zSpeed).normalized + (cam.transform.right * xSpeed).normalized).normalized;
		direction.y = 0f;
		moveDirection = GetKeyDirection ();

		//Determine Speed
		float Speed;
		isMoving = Mathf.Abs (xSpeed) >= 1f || Mathf.Abs (zSpeed) >= 1f;
		isDodging = IsDodging ();
		isGrounded = IsGrounded ();

		//Only check for landing if falling. Landing is used to trigger animation transition early
		if (!isGrounded && rigidBody.velocity.y < 0)
			isLanding = IsLanding ();



		if(!isCrouching && isGrounded)
			isSprinting = CheckForSprint ();
		
		if (isMoving) {
			if (isSprinting)
				Speed = SPRINT_SPEED;
			else if (isCrouching)
				Speed = CROUCH_SPEED;
			else
				Speed = RUN_SPEED;
		}
		else
			Speed = 0;

		if (playerActions.Jump.WasPressed) {
			anim.SetTrigger ("Jump");
			if (isGrounded) {
				isLanding = false;
				rigidBody.AddForce (transform.up * JUMP_HEIGHT * mass, ForceMode.Impulse);
			}
		}
		else if (playerActions.Jump.WasReleased)
			anim.ResetTrigger ("Jump");

		if (playerActions.Crouch.WasPressed) {
			anim.SetTrigger ("Dodge");
		}
		
		//Set Velocity
		Vector3 velocity = direction * Speed;
		velocity.y = rigidBody.velocity.y;
		if(isGrounded)
			rigidBody.velocity = velocity;

		//Set Animation Parameters
		SetAnimationParameters ();
		transform.RotateAround (transform.position, Vector3.up, Input.GetAxis ("Mouse X"));
		cam.transform.RotateAround (transform.position + cam.transform.localPosition.y * Vector3.up, cam.transform.right, -Input.GetAxis ("Mouse Y"));

		//Move this somewhere else
		if (Input.GetKeyDown (KeyCode.Escape))
			Application.Quit ();

	}

	void SetAnimationParameters()
	{
		float zSpeedCap = isSprinting && !isCrouching ? 3f : 2f;
		anim.SetFloat ("zSpeed", Mathf.Clamp(zSpeed, -2, zSpeedCap));
		anim.SetFloat ("xSpeed", Mathf.Clamp(xSpeed, -2, 2));
		anim.SetBool ("Crouching", isCrouching);
		anim.SetBool ("Grounded", isGrounded);
		anim.SetBool ("Landing", isLanding);
		anim.SetInteger ("Direction", (int)moveDirection);
	}

	bool CheckForSprint()
	{

		if (playerActions.Forward.WasReleased && isSprinting)
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
		if (playerActions.Forward.WasPressed && !isCheckingForSprint) {
			isCheckingForSprint = true;
			tapDelay = SPRINT_TAP_DELAY;
		}
			
		return isSprinting;

	}

	CapsuleCollider DoColliderChanges()
	{
		CapsuleCollider newCol = coll;
		if (currentBaseState.fullPathHash == fallingState) {
			newCol.height = 1f;
			newCol.center = Vector3.up * 0.5f;
		} else {
			newCol.height = 1.8f;
			newCol.center = Vector3.up * 0.9f;
		}
		return newCol;
	}

	Direction GetKeyDirection()
	{
		bool keyW = playerActions.Forward.WasPressed;
		bool keyA = playerActions.Left.WasPressed;
		bool keyS = playerActions.Back.WasPressed;
		bool keyD = playerActions.Right.WasPressed;

		if (keyW) {
			if (keyD)
				return Direction.ForwardRight;
			else if (keyA)
				return Direction.ForwardLeft;
			else
				return Direction.Forward;
		} else if (keyS) {
			if (keyD)
				return Direction.BackRight;
			else if (keyA)
				return Direction.BackLeft;
			else
				return Direction.Back;
		} else if (keyD)
			return Direction.Right;
		else if (keyA)
			return Direction.Left;
		else
			return Direction.None;
	}
		
	bool IsDodging()
	{
		return currentBaseState.IsTag ("Dodge");
	}

	bool IsGrounded()
	{
		return Physics.Raycast (transform.position + coll.center.y * Vector3.up, -Vector3.up, coll.height / 2f + 0.1f, terrainonly, QueryTriggerInteraction.Ignore);
	}

	bool IsLanding()
	{
		return Physics.Raycast (transform.position, -Vector3.up, 1f, terrainonly, QueryTriggerInteraction.Ignore);
	}
}
