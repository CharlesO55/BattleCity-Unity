using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

using Random = UnityEngine.Random;


public class EnemyLogic : MonoBehaviour
{

    [Range(1, 3)] public int AgressionLevel;
    public float fSpeed = 250f;


    [Header("Initialize")]
    [SerializeField] LayerMask PlayerBasesLayer;
    [SerializeField] LayerMask ShootablesLayer;
    [SerializeField] LayerMask IgnoreEnemyLayer;
    [SerializeField] Tilemap EnemyBases;
    [SerializeField] Tilemap PlayerBases;
    public Rigidbody2D c_Rigidbody;
    public BoxCollider2D c_BoxCollider;

    //BULLET SHOOTING
    private float fCheckShootDistance;
    float fMaxCooldown = 1f;
    float fCurrCooldown = 0f;

    private float fBaseDetectionDistance;

    //HUNT TIME
    float fHuntMaxTime = 2f;
    float fHuntCurrTime = 0f;

    int nHorizontalPatrols = 0;
    int nVerticalPatrols = 0;

    enum AI_State
    {
        HUNTING_PLAYER,
        HUNTING_BASE,
        PATROLLING
    }
    AI_State CurrState;


    Dictionary<Direction, string> raycastDetections = new Dictionary<Direction, string>();

    //NAVIGATION
    enum Direction
    {
        UP = 0,
        LEFT = 1,
        DOWN = 2,
        RIGHT = 3,
    };
    private List<(Direction EMoveDir, float fRot)> patrolOrderList = new List<(Direction, float)>();


    private float fHorizontal;
    private float fVertical;
    private bool bStop = false;
    private bool bShoot= false;





    private List<Vector3> baseSpawnLocations;

    // Use this for initialization
    void Start()
    {
        CurrState = AI_State.PATROLLING;
        
        this.FindSpawnLocs();
        this.Respawn();

        //fHuntMaxTime              -   A cooldown timer that forces the player to search for the player & ignore base spots.
        //fCheckShootDistance       -   The range where agent will spot the player & enter PlayerHunt
        //fBaseDetectionDistance    -   The range where agent will detect a playerbase & enter BaseHunt
        switch (this.AgressionLevel)
        {
            case 1:
                this.fHuntMaxTime = 20f;
                this.fCheckShootDistance = 2f;
                this.fBaseDetectionDistance = 1f;
                patrolOrderList.Add((Direction.UP, 0f));
                patrolOrderList.Add((Direction.RIGHT, 270f));
                patrolOrderList.Add((Direction.LEFT, 90f));
                patrolOrderList.Add((Direction.DOWN, 180f));
                break;
            case 2:
                this.fHuntMaxTime = 8f;
                this.fCheckShootDistance = 5f;
                this.fBaseDetectionDistance = 4f;
                patrolOrderList.Add((Direction.RIGHT, 270f));
                patrolOrderList.Add((Direction.LEFT, 90f));
                patrolOrderList.Add((Direction.DOWN, 180f));
                patrolOrderList.Add((Direction.UP, 0f));
                break;
            case 3:
                this.fHuntMaxTime = 3f;
                this.fCheckShootDistance = 8f;
                this.fBaseDetectionDistance = 7f;
                patrolOrderList.Add((Direction.DOWN, 180f)); 
                patrolOrderList.Add((Direction.LEFT, 90f)); 
                patrolOrderList.Add((Direction.RIGHT, 270f));
                patrolOrderList.Add((Direction.UP, 0f));
                break;
        }
    }

    private void FindSpawnLocs()
    {
        //FINDS THE SPAWN LOCATIONS
        baseSpawnLocations = new List<Vector3>();

        foreach (var pos in EnemyBases.cellBounds.allPositionsWithin)
        {
            Vector3Int localPlace = new Vector3Int(pos.x, pos.y, pos.z);
            Vector3 place = EnemyBases.CellToWorld(localPlace);
            if (EnemyBases.HasTile(localPlace))
            {
                baseSpawnLocations.Add(place);
            }
        }
    }

    private void RecenterToTIlePos()
    {
        //RECENTERS TO TILE POS TO PREVENT COLLIDER BUGS
        Vector3Int cellPos = EnemyBases.WorldToCell(transform.position);
        this.transform.position = EnemyBases.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
    }

    public void Respawn()
    {
        c_Rigidbody.SetRotation(0f);
        this.CurrState = AI_State.PATROLLING;
        int nRNG = Random.Range(0, baseSpawnLocations.Count);
        this.gameObject.transform.position = baseSpawnLocations[nRNG] + new Vector3(0.5f, 0.5f, 0f);
    }





    void Update()
    {
        //SHOOT COOLDOWN
        fCurrCooldown += Time.deltaTime;
        if (bShoot) { 
            this.Shoot(); 
        }

        //FOCUS ON HUNTING THE PLAYER WHEN IN THIS STATE
        if (CurrState != AI_State.HUNTING_PLAYER)
        {
            this.CheckCardinalDirections(fBaseDetectionDistance, 1, PlayerBasesLayer);
        }

        //IGNORE PATROLS AND PLAYER WHEN BASE HUNTING
        if (CurrState == AI_State.HUNTING_BASE)
        {
            this.HuntBases();
            return;
        }

        //CHECK IF THE HUNT STATE HAS EXCEEDED THE TIME LIMIT SINCE LAST CONTACT W/ PLAYER
        if (CurrState == AI_State.HUNTING_PLAYER)
        {
            fHuntCurrTime += Time.deltaTime;

            if (fHuntCurrTime > fHuntMaxTime) {
                fHuntCurrTime = 0f;
                CurrState = AI_State.PATROLLING;
                Debug.Log("[STATE] : PLAYER HUNT ENDED");
            }
        }


        //SCANS FOR THE PLAYER
        if (fCurrCooldown > fMaxCooldown)
        {
            this.CheckShoot();
        }
    }


    private void HuntBases()
    {
        if (this.raycastDetections[Direction.UP] == "Player_Base") { c_Rigidbody.SetRotation(0f); }
        else if (this.raycastDetections[Direction.DOWN] == "Player_Base") { c_Rigidbody.SetRotation(180f); }
        else if (this.raycastDetections[Direction.LEFT] == "Player_Base") { c_Rigidbody.SetRotation(90f); }
        else if (this.raycastDetections[Direction.RIGHT] == "Player_Base") { c_Rigidbody.SetRotation(270f); }
        //RETURN WHEN NO MORE BASES

        else { this.CurrState = AI_State.PATROLLING;  }

        this.Shoot();
        this.bStop = false;
    }

    private void CheckCardinalDirections(float fDistanceCheck, float fOffset, LayerMask C_LayerMask)
    {
        Vector3[] vecDirections =
        {
            Vector3.up,
            Vector3.left,
            Vector3.down,
            Vector3.right
        };

        for (int i = 0; i < 4; i++)
        {
            //RaycastHit2D hitDetection = Physics2D.BoxCast(c_BoxCollider.bounds.center, c_BoxCollider.bounds.size, 0f, vecDirections[i], fDistanceCheck, ~IgnoreEnemyLayer);
            RaycastHit2D hitDetection = Physics2D.Raycast(this.gameObject.transform.position + (vecDirections[i] * fOffset), vecDirections[i], fCheckShootDistance, C_LayerMask);

            if (hitDetection.collider != null)
            {
                this.raycastDetections[(Direction)i] = hitDetection.collider.tag;

                if (hitDetection.collider.CompareTag("Player_Base")) { this.CurrState = AI_State.HUNTING_BASE; }
            }
            else
            {
                this.raycastDetections[(Direction)i] = "null";
            }
            Debug.DrawRay(this.gameObject.transform.position + (vecDirections[i] * fOffset), vecDirections[i] * fDistanceCheck, Color.red);
        }

    }

    private void CheckShoot()
    {
        Vector2[] vecDirections =
        {
            Vector2.up,
            Vector2.left,
            Vector2.down,
            Vector2.right
        };

        for (int i = 0; i < 4; i++)
        {
            RaycastHit2D hitDetection = Physics2D.Raycast(this.gameObject.transform.position, vecDirections[i], fCheckShootDistance, ShootablesLayer);

            Color rayColor = Color.red;
            if (hitDetection.collider != null && hitDetection.collider.CompareTag("Player"))
            {
                rayColor = Color.green;
                this.c_Rigidbody.SetRotation(i * 90f);

                //WHEN PLAYER WAS FOUND, HUNT AND REFRESH THE TIMER
                this.CurrState = AI_State.HUNTING_PLAYER;
                this.fHuntCurrTime = 0f;

                //this.Shoot();
                this.bShoot = true;
                return;
            }
            //Debug.DrawRay(this.gameObject.transform.position, vecDirections[i] * fCheckShootDistance, rayColor);
        }
    }

    private void Shoot()
    {
        if (fCurrCooldown < fMaxCooldown)
        {
            this.bStop = true;
            return;
        }
        else
        {
            fCurrCooldown = 0;
            this.bShoot = false;
            this.bStop = false;
        }

        GameObject newBullet = BulletManagerScript.instance.RequestPoolable();
        


        if (newBullet != null)
        {
            this.fCurrCooldown = 0f;

            newBullet.SetActive(true);

            BulletScript bulletMove = newBullet.GetComponent<BulletScript>();


            float fHorMov = 0f;
            float fVerMov = 0f;

            Vector3 offSet = Vector3.zero;
            switch (c_Rigidbody.rotation)
            {
                case 90:
                    fHorMov = -1f;
                    break;
                case 270:
                    fHorMov = 1f;
                    break;
                case 0:
                    fVerMov = 1f;
                    break;
                case 180:
                    fVerMov = -1f;
                    break;
            }

            newBullet.transform.position = this.transform.position + offSet;
            bulletMove.Init(fHorMov, fVerMov, false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //RESOLVES CLIPPING ISSUES
        this.RecenterToTIlePos();

        this.CheckCardinalDirections(2, 0, ~IgnoreEnemyLayer);
        this.FindNewMoveDir();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player_Base"))
        {
            Tilemap tilemapRef = collision.GetComponent<Tilemap>();

            Vector3Int vecGridPos = tilemapRef.WorldToCell(this.transform.position);

            if (tilemapRef.GetTile(vecGridPos) == null)
            {
                vecGridPos += new Vector3Int((int)fHorizontal, (int)fVertical, 0);
            }
            tilemapRef.SetTile(vecGridPos, null);

            //STOP BASE HUNTING
            Debug.Log("[STATE] BASE HUNT END");
            this.CurrState = AI_State.PATROLLING;
        }
    }



    private void FindNewMoveDir()
    {
        //CHECK FOR A VALID MOVE DIR
        for (int i = 0; i < 4; i++)
        {
            if (raycastDetections[patrolOrderList[i].EMoveDir] == "null")
            {
                float fRotation = patrolOrderList[i].fRot;

                if (fRotation == 0f || fRotation == 180f)
                {
                    this.nVerticalPatrols++;
                }
                else
                {
                    this.nHorizontalPatrols++;
                }

                if (this.nHorizontalPatrols >1)
                {
                    fRotation = 180f;
                    nHorizontalPatrols = 0;
                    this.bShoot = true;
                }
                else if (this.nVerticalPatrols > 1)
                {
                    fRotation = 90f;
                    
                    if (EnemyBases.WorldToCell(this.transform.position).x + Random.Range(0, 5) > 5) { fRotation = 270f; }
                    nVerticalPatrols = 0;
                    this.bShoot = true;
                }

                c_Rigidbody.SetRotation(fRotation);
                return;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (raycastDetections[patrolOrderList[i].EMoveDir] == "Destructible")
            {
                c_Rigidbody.SetRotation(patrolOrderList[i].fRot);
                this.bShoot = true;
                return;
            }
        }
    }

    void FixedUpdate()
    {
        if (bStop) return;

        //MOVES BASED ON RIGIDBODY FACING
        switch (c_Rigidbody.rotation)
        {
            case 0:
                fVertical = 1f;
                fHorizontal = 0f;
                break;
            case 90:
                fHorizontal = -1f;
                fVertical = 0f;
                break;
            case 180:
                fVertical = -1f;
                fHorizontal = 0f;
                break;
            case 270:
                fHorizontal = 1f;
                fVertical = 0f;
                break;
        }
        c_Rigidbody.velocity = new Vector2(fHorizontal * Time.deltaTime * fSpeed, fVertical * Time.deltaTime * fSpeed);
    }

}
