using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MovableObjectMotor : MonoBehaviour {
    [HideInInspector]
    public NavMeshAgent agent;           //reference to the NavMeshAgent on this object  Set in Start method
    private Animator objectAnimator;     //reference to the Animator on this object  Set in Start method
    private Rigidbody objectRigidbody;   //reference to the Rigidbody on this object  Set in Start method

    private void Awake()
    {
        EnsureReferences();
    }

    void Start () {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (objectAnimator == null) objectAnimator = GetComponent<Animator>();
        if (objectRigidbody == null) objectRigidbody = GetComponent<Rigidbody>();
    }
	
	void Update () {
        if (agent == null || !agent.isActiveAndEnabled) return;

		if(agent.velocity.magnitude > 0)  //check if we are moving
        {
            if (objectAnimator != null) objectAnimator.SetFloat("Speed_f", agent.velocity.magnitude);  //pass the speed we are moving to the Animator on this object
            if (objectRigidbody != null)
            {
                objectRigidbody.constraints = RigidbodyConstraints.None;       // make sure there are no translation contraints on the Rigidbody
                objectRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // make the Rigidbody can only rotate on the Y axis
            }
        }
        else  //if we are standing still
        {
            if (objectAnimator != null) objectAnimator.SetFloat("Speed_f", 0);  //tell the animator our movement is at 0
            if (objectRigidbody != null)
            {
                objectRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;  //make sure the Ridgidbody cannot rotate on any axis
            }
        }
	}

    public void MoveObjectTo(Vector3 location)  //called from the controller to tell the NavMeshAgent where to go
    {
        EnsureReferences();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.destination = location;  //set the NavMeshAgent destination to the passed in position
        }
    }

    public void Stop()  //called from the controller to tell the NavMeshAgent when to stop all movement
    {
        EnsureReferences();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;                    // tell the NavMeshAgent to stop calculating a path
            agent.velocity = Vector3.zero;             // stop movement on the NavMeshAgent
            agent.SetDestination(transform.position);  // make sure the current NavMeshAgent is set to whatever position it currently is in so it will not start moving to the old location when it starts back up
        }
        if (objectAnimator != null) objectAnimator.SetFloat("Speed_f", 0);     // tell the Animator that the speed is at 0 so it changed to an Idle animation
    }

    public void Go()   //called from the controller to tell the NavMeshAgent when to start movement again
    {
        EnsureReferences();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;                   // tell the NavMeshAgent to start calculating a path
        }
    }
}
