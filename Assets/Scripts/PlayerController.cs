using UnityEngine;
using System.Collections;

public class PlayerController : Photon.MonoBehaviour {

	public int maxHP;
	public float maxHorizontalSpeed;
	public float maxVerticalSpeed;	
	public float maxAngularSpeed;
	public float angularForce;
	public float bounceForce;
	public float jumpSpeed;
	public float acceleration;
	public float HP_dir;
	[HideInInspector]public int team;
	[HideInInspector]public int HP;

	private Rigidbody2D rb2D;
	private float distToGround;
	private Vector2 normsize;
	private Vector2 fallsize;
	private float maxPositionY;
	private float minPositionY;
	private BoxCollider2D bc2D;
	private bool isMine;
	private bool falling = false;
	private Vector3 correctPosition;
	private Quaternion correctRotation;

	// Use this for initialization
	void Start() {
		HP = maxHP;
		rb2D = GetComponent<Rigidbody2D>();
		bc2D = GetComponent<BoxCollider2D> ();
		distToGround = bc2D.bounds.extents.y * 1.5f;
		minPositionY = Camera.main.ViewportToWorldPoint(new Vector2(0, 0)).y;
		maxPositionY = Camera.main.ViewportToWorldPoint(new Vector2(0, 1)).y;
		normsize = bc2D.size;
		fallsize = new Vector2 (normsize.x * 1.3f, normsize.y * 1.3f);
		isMine = photonView.isMine;
		if (!isMine) {
			GetComponent<Rigidbody2D>().isKinematic = true;
		}
	}

	// Update is called once per frame
	void Update() {
		if (HP>0 && isMine) {
			float moveHorizontal = Input.GetAxisRaw("Horizontal");

			// Move
			if (moveHorizontal != 0) {
				Move(moveHorizontal);
			}

			// Jump
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				if (!falling && isGrounded()) {
					Jump();
				}
			}


			if (!Physics2D.IsTouchingLayers (GetComponent<BoxCollider2D> (), LayerMask.GetMask ("Ground"))) {
				if (falling) {
					bc2D.isTrigger = false;
					bc2D.size = normsize;
					falling = false;
				}
			} else {

				// Fall
				if (Input.GetKey (KeyCode.DownArrow)) {
					if (isGrounded ()) {
						Fall ();
					}
				}
			}
		}
	}

	void FixedUpdate() {
		ClampSpeed();
		ClampVerticalPosition();
		if (!isMine) {
			UpdatePlayerPosition();
		}
	}

	void Move(float moveHorizontal) {
		rb2D.AddForce(new Vector2(moveHorizontal * acceleration, 0));	
		rb2D.AddTorque(-moveHorizontal*angularForce);
	}

	void Jump() {
		rb2D.velocity = new Vector2(rb2D.velocity.x, jumpSpeed);
	}

	void Fall() {
		bc2D.isTrigger = true;
		bc2D.size = fallsize;
		falling = true;
	}

	void ClampSpeed() {
		float speedHorizontal = rb2D.velocity.x;
		float speedVertical = rb2D.velocity.y;
		speedHorizontal = Mathf.Clamp(speedHorizontal, -maxHorizontalSpeed, maxHorizontalSpeed);
		speedVertical = Mathf.Clamp(speedVertical, -maxVerticalSpeed, maxVerticalSpeed);
		rb2D.velocity = new Vector2(speedHorizontal, speedVertical);
		rb2D.angularVelocity = Mathf.Clamp(rb2D.angularVelocity, -maxAngularSpeed, maxAngularSpeed);
	}

	void ClampVerticalPosition() {
		float verticalPosition = transform.position.y;
		if (verticalPosition > maxPositionY) {
			verticalPosition = minPositionY;
		} else if (verticalPosition < minPositionY) {
			verticalPosition = maxPositionY;
		}
		transform.position = new Vector3(transform.position.x, verticalPosition, transform.position.z);
	}

	bool isGrounded(){
		RaycastHit2D[] results = new RaycastHit2D[3];
		Vector2 coordinate2D =new Vector2(transform.position.x,transform.position.y);
		return Physics2D.RaycastNonAlloc(coordinate2D, -Vector2.up, results, distToGround)>2;
	}

	void UpdatePlayerPosition() {
		if (Mathf.Abs(transform.position.y - correctPosition.y) > Mathf.Abs(maxPositionY - minPositionY) - 3.0) {
			transform.position = correctPosition;
		} else {
			transform.position = Vector3.Lerp(transform.position, correctPosition, Time.deltaTime * 10);
		}
		transform.rotation = Quaternion.Slerp(transform.rotation, correctRotation, Time.deltaTime * 10);
	}

	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
		if (stream.isWriting) {
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);
		} else {
			correctPosition = (Vector3)stream.ReceiveNext();
			correctRotation = (Quaternion)stream.ReceiveNext();
		}
	}

	void OnCollisionEnter2D(Collision2D coll){
		if (isMine && coll.gameObject.tag == "Player") {
			Vector2 colpos = coll.transform.position;
			Vector2 dir = new Vector2 (transform.position.x - colpos.x, transform.position.y -colpos.y);
			dir.Normalize ();
			rb2D.AddForce (new Vector2(dir.x * bounceForce, dir.y * bounceForce));
			if (-dir.y > HP_dir) {
				HP--;
				if (HP == 0) {
					transform.localScale = new Vector3 (transform.localScale.x * 2f, transform.localScale.y * 0.5f, 1);
				}
			}
		}
	}
}