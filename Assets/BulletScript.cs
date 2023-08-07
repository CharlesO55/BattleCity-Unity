using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BulletScript : MonoBehaviour
{
    public float fSpeed = 800f;

    float fHorizontal;
    float fVertical;
    public bool isPlayerBullet;
    public void Init(float fHorizontal, float fVertical, bool isPlayerBullet)
    {
        this.fHorizontal = fHorizontal;
        this.fVertical = fVertical;
        this.isPlayerBullet = isPlayerBullet;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(fSpeed * fHorizontal * Time.deltaTime, fSpeed * fVertical * Time.deltaTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Indestructible") || collision.CompareTag("Destructible") || 
            collision.CompareTag("Player") && !this.isPlayerBullet ||
            collision.CompareTag("Enemy") && this.isPlayerBullet)
        {
            this.gameObject.SetActive(false);
            /*Debug.Log("[Shot] : " + collision.name);*/

            

            
            switch (collision.tag)
            {
                case "Player":
                    PlayerController playerScript = collision.GetComponent<PlayerController>();
                    playerScript.Respawn();
                    break;
                case "Enemy":
                    EnemyLogic enemyScript = collision.GetComponent<EnemyLogic>();
                    enemyScript.Respawn();
                    break;
                case "Destructible":
                    this.DestroyWall(collision);
                    break;
            }
        }
    }

    

    private void DestroyWall(Collider2D collision)
    {
        Tilemap tilemapRef = collision.GetComponent<Tilemap>();

        Vector3Int vecGridPos = tilemapRef.WorldToCell(this.transform.position);
        /*Debug.Log(vecGridPos.ToString());*/

        //OFFSET HELPS TO IDENTIFY THE POS BUT MAY CAUSE ERRORS
        if (tilemapRef.GetTile(vecGridPos) == null)
        {
            vecGridPos += new Vector3Int((int)fHorizontal, (int)fVertical, 0);
        }
        tilemapRef.SetTile(vecGridPos, null);
        
    }
}
