using System.Collections.Generic;
using UnityEngine;

public class BulletManagerScript : MonoBehaviour
{
    public static BulletManagerScript instance;

    private List<GameObject> vecBullets = new List<GameObject>();
    
    
    [SerializeField] private GameObject bulletPrefab;
    public int nInstances = 5;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }


    void Start()
    {
        for (int i = 0; i < nInstances; i++)
        {
            GameObject newBulllet = Instantiate(bulletPrefab);
            newBulllet.SetActive(false);
            vecBullets.Add(newBulllet);
        }
    }

    public GameObject RequestPoolable()
    {
        for (int i = 0;i < nInstances;i++)
        {
            if (!vecBullets[i].activeInHierarchy)
            {
                return vecBullets[i];
            }
        }
        return null;
    }
}
