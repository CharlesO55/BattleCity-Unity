using System;
using System.Collections.Generic;
using Unity.VisualScripting;
// using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D c_Rigidbody;
    [SerializeField] Tilemap PlayerBases;


    public float fSpeed = 50f;

    float fHorizontal = 0f;
    float fVertical = 0f;
    bool bShoot = false;
    float fShootCooldown = 1f;
    float fCurrCooldown = 0f;

    private List<Vector3> baseSpawnLocations;

    private void Start()
    {
        this.FindSpawnLocs();
        this.Respawn();
    }
    // Update is called once per frame
    void Update()
    {
        this.ReadInputs();

        if (bShoot && fCurrCooldown > fShootCooldown)
        {
            Shoot();
        }
        fCurrCooldown += Time.deltaTime;
    }

    private void ReadInputs()
    {
        fHorizontal = Input.GetAxisRaw("Horizontal");
        fVertical = Input.GetAxisRaw("Vertical");

        bShoot = Input.GetKey(KeyCode.Space);
    }

    private void Shoot()
    {
        GameObject newBullet = BulletManagerScript.instance.RequestPoolable();

        if(newBullet != null)
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
                case -90:
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
            bulletMove.Init(fHorMov, fVerMov, true);
        }
    }

    void FixedUpdate()
    {
  
        if (fHorizontal != 0f)
        {
            c_Rigidbody.SetRotation(90f * -fHorizontal);
            c_Rigidbody.velocity = new Vector2(fHorizontal * Time.deltaTime * fSpeed, 0);
        }
        else if (fVertical != 0f)
        {
            float fDeg = 0f;
            if (fVertical < 0f) { fDeg = 180f;  }
            c_Rigidbody.SetRotation(fDeg);
            c_Rigidbody.velocity = new Vector2(0 , fVertical * Time.deltaTime * fSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy_Base"))
        {
            Tilemap tilemapRef = collision.GetComponent<Tilemap>();

            Vector3Int vecGridPos = tilemapRef.WorldToCell(this.transform.position);

            if (tilemapRef.GetTile(vecGridPos) == null)
            {
                vecGridPos += new Vector3Int((int)fHorizontal, (int)fVertical, 0);
            }
            tilemapRef.SetTile(vecGridPos, null);
        }
    }




    private void FindSpawnLocs()
    {
        //FINDS THE SPAWN LOCATIONS
        baseSpawnLocations = new List<Vector3>();

        foreach (var pos in PlayerBases.cellBounds.allPositionsWithin)
        {
            Vector3Int localPlace = new Vector3Int(pos.x, pos.y, pos.z);
            Vector3 place = PlayerBases.CellToWorld(localPlace);
            if (PlayerBases.HasTile(localPlace))
            {
                baseSpawnLocations.Add(place);
            }
        }
    }
    public void Respawn()
    {
        c_Rigidbody.SetRotation(0f);
        int nRNG = UnityEngine.Random.Range(0, baseSpawnLocations.Count);
        this.transform.position = baseSpawnLocations[nRNG] + new Vector3(0.5f, 0.5f, 0);
    }

}
