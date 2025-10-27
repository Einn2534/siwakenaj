using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class NotesGenerator : MonoBehaviour
{
    [SerializeField]
    GameObject notes;
    [SerializeField] 
    float interval = 1f; // 生成間隔(秒)
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Generator());
    }

    // Update is called once per frame
    void Update()
    {

       
    }

    IEnumerator Generator()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            // このオブジェクトの位置にノーツを生成
            Instantiate(notes, transform.position, Quaternion.identity);
            yield return wait;
        }
    }
}
