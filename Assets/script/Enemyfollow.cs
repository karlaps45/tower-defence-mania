using UnityEngine;

public class Enemyfollow : MonoBehaviour
{
    public GameObject[] waypoints;
    public int waypointindex = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3.MoveTowards(transform.position, waypoints[waypointindex].transform.position, Time.deltaTime);
        waypointindex++;


        if (waypointindex >= waypoints.Length)
        {
            damageplayer();
        }



    }



    private void damageplayer()
    {

    }
}
