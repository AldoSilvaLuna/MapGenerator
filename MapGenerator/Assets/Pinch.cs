using UnityEngine;
using System.Collections;

public class Pinch : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnPinch(Vector3 pinch_position) {
		// ...
		// Check if we pinched a movable object and grab the closest one.
		Collider[] close_things = Physics.OverlapSphere(pinch_position, PINCH_DISTANCE, layer_mask_);
		Vector3 distance = new Vector3(PINCH_DISTANCE, 0.0f, 0.0f);
		for (int j = 0; j < close_things.Length; ++j) {
			Vector3 new_distance = pinch_position - close_things[j].transform.position;
			if (close_things[j].GetComponent<Rigidbody>() != null && new_distance.magnitude < distance.magnitude) {
				grabbed_object_ = close_things[j];
				distance = new_distance;
			}
		}
	}
}
