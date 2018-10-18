using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shoot : MonoBehaviour {

    [Header("Set these to the proper inputs")]
    [SerializeField]
    private string player_fire;
    [SerializeField]
    private string player_reload, player_movement_hor, player_movement_vert;

    [Header("Optional, an object that will spawn when the enemy is shot")]
    public GameObject bullet_hole;

    [Header("Sound of the gun firing, remove if unnecessary")]
    [SerializeField]
    private AudioSource firing_sound;

    [Header("Fire rate is frames between each shot, lower means faster firing")]
    [SerializeField]
    private int fireRate;
    // in this case it is 4 frames between each shot, meaning 15 rounds per second, which is 900 RPM (real life RPM of a P90)

    Vector3 direction;
    [Header("Maximum and Minimum size of the fire area while holding still")]
    [SerializeField]
    private float maxRecoilBase;
    [SerializeField]
    private float minRecoilBase;

    [Header("How much the fire area expands after each shot")]
    [SerializeField]
    private float recoilPerShot;

    // current max and min size, changes based on stance (becomes less accurate while moving or jumping, more accurate while still or crouching
    private float maxRecoil, minRecoil;

    private int fireWait; // time between shots in frames. 

    [Header("Minimum fire area size while moving, shots will not be any more accurate than this while moving")]
    [SerializeField]
    private float minRecoilMoving;

    [Header("Maximum fire area size while crouched, shots will not be any less accurate that this while crouched")]
    [SerializeField]
    private float maxRecoilCrouching;

    [Header("How quickly the fire area returns to base value. Higher = More Accurate")]
    [SerializeField]
    private float recovery;

    private float recoilDelta; // the max range of what the next shot could be (your crosshair growing), increases each shot

    private float nextAngle; // what the next shot is, where it goes inside the current corssair

    private Vector3 upOffset, rightOffset; // offset values for hor/vert shot


    // Use this for initialization
    void Start()
    {
        maxRecoil = maxRecoilBase;
        minRecoil = minRecoilBase;
        recoilDelta = minRecoil;

    } // end start

    // Update is called once per frame
    void Update()
    {

        if (recoilDelta >= maxRecoil)
        {
            recoilDelta = maxRecoil; // cannot go above max recoil
        }

        if (Input.GetAxis(player_movement_hor) != 0 || Input.GetAxis(player_movement_vert) != 0)
        {
            minRecoil = minRecoilMoving; // while moving, set min recoil to the movement value
        }
        else
        {
            minRecoil = minRecoilBase; // min recoil returns to normal when not moving
        }

        /* SEE BELOW FOR INFO REGARDING THESE FUNCTIONS

        if (GetIsGrounded() == false)
        {
            recoilDelta = maxRecoil; // while airborn, recoil goes to max (accuracy goes to min)
        }

        if (GetIsCrouched() == true)
        {
            maxRecoil = maxRecoilCrouching; // while crouched, max recoil is decresed
        }
        else
        {
            maxRecoil = maxRecoilBase; // max recoil returns to normal when not crouched
        }
        */



        // UNCOMMENT OUT THIS BLOCK TO SEE ALL RAYS AND CROSSHAIR IN DEBUG VIEWER
        //=====================================================================================================================================

        /*
        // in debug, the straight line shot is white
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);

        // in debug, these 4 rays are max recoil
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxRecoil, transform.up) * transform.forward * 1000, Color.yellow);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxRecoil * -1, transform.up) * transform.forward * 1000, Color.yellow);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxRecoil, transform.right) * transform.forward * 1000, Color.yellow);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(maxRecoil * -1, transform.right) * transform.forward * 1000, Color.yellow);

        // in debug, these 4 are current recoil (shrinking/growing crossair while resting/firing)
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(recoilDelta * -1, transform.right) * transform.forward * 1000, Color.red);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(recoilDelta * -1, transform.up) * transform.forward * 1000, Color.red);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(recoilDelta, transform.right) * transform.forward * 1000, Color.red);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(recoilDelta, transform.up) * transform.forward * 1000, Color.red);

        // in debug, these 4 are min recoil (adjusts based on stance)
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(minRecoil * -1, transform.right) * transform.forward * 1000, Color.blue);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(minRecoil * -1, transform.up) * transform.forward * 1000, Color.blue);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(minRecoil, transform.right) * transform.forward * 1000, Color.blue);
        Debug.DrawRay(transform.position, Quaternion.AngleAxis(minRecoil, transform.up) * transform.forward * 1000, Color.blue);

        //=====================================================================================================================================
        */

        if (fireWait <= 0) // time to wait since last shot, cannot drop below 0
        {
            fireWait = 0;
        }
        else
        {
            fireWait--;
        }

        if (Input.GetAxis(player_fire) > 0)
        {
            if (fireWait <= 0) // can fire
            {
                Fire(); // run the Fire function to generate the next shot
            }
        }
        else // while not holding down click/right trigger
        {
            if (recoilDelta > minRecoil)
            {
                recoilDelta -= recovery; // recovery, this is the crosshair shrinking while not firing, shooting in small bursts optimizes your accuracy
            }
        }
    } // end update

    // ############################################# FIRING ###################################################################

    private void Fire()
    {
        firing_sound.Play(); // comment out this line if you do not have a sound for the gun

        // picks a random angle between the negative and positive value of current recoil (recoilDelta), 
        // each consecutive shot makes this range bigger because the delta value is increased each consecutive shot
        nextAngle = Random.Range(recoilDelta * -1, recoilDelta);

        float randomDivider = Random.Range(0.1f, nextAngle);
        /*  with the offsets below, a parabola (arch) is always formed
            the parabola will never cross an axis, meaning you would always get weird crosses formed
            by dividing it you are randomly widening the parabola, and with all 4 parabolas firing randomly, 
            you have true random shots anywhere inside fire area
         */

        // the offset for the shot equal to the random angle. Prevents shots from always making a cross
        rightOffset = new Vector3(0, nextAngle / randomDivider, 0); // Offset for a shot that is straight up/down
        upOffset = new Vector3(nextAngle / randomDivider, 0, 0); // Offeset for a shot that is straight left/right
        // Note that these offset values may be 0, it is still possible to have a shot that is in line with the center

        int randomIntTransform = Random.Range(0, 4);

        switch (randomIntTransform)
        {
            case 0: // right shot
                direction = Quaternion.AngleAxis(nextAngle, transform.up + upOffset) * transform.forward;
                break;
            case 1: // left shot                                           
                direction = Quaternion.AngleAxis(nextAngle, transform.up + upOffset * -1) * transform.forward;
                break;
            case 2: // down shot
                direction = Quaternion.AngleAxis(nextAngle, transform.right + rightOffset * -1) * transform.forward;
                break;
            case 3: // up shot                                             
                direction = Quaternion.AngleAxis(nextAngle, transform.right + rightOffset) * transform.forward;
                break;
            default:
                randomIntTransform = 0; // in the event of a glitch, set it to up shot (will probably never happen)
                break;
        }

        // uncomment the line below to see the shot drawn in debug mode
        // Debug.DrawRay(transform.position, direction * 1000, Color.green);

        RaycastHit hit; // the object hit by the ray

        if (Physics.Raycast(transform.position, direction, out hit, Mathf.Infinity)) // if it hits something
        {
            // give a wall the tag of "target" to see the bullet hole prefab object spawn at the shot's location
            if (hit.collider.tag == "target")
            {
                Instantiate(bullet_hole, hit.point, Quaternion.identity); // spawn a bullet hole at the location (for testing only)
            }
        }
        // wait X frames before firing again. fireRate does not change, fireWait is always decreasing 
        fireWait = fireRate;

        // add a value to the recoilDelta. The next shot will have a possible range of whatever it just was, plus the value below
        // so each consecutive shot becomes less accurate (fire area growing). Resting makes the range smaller making the next shot more accurate (fire area shrinking)
        recoilDelta += recoilPerShot;
    }


    /* Uncomment out the functions below if you have a character controller with grounded and crouched properties, 
     * the shot becomes more/less accurate if you are crouched/jumping. Use these functions to get that from the character controller.
     
    private bool GetIsGrounded()
    {
        // return the character controller grounded
    }

    private bool GetIsCrouched()
    {
        // return the character controller for crouched
    }
    */
}
