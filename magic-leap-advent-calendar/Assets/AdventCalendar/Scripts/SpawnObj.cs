using System.Collections;
using UnityEngine;

namespace AdventCalendar
{
    /// <summary>
    /// スポーンしたオブジェクト用スクリプト、5秒で破棄する.
    /// </summary>
    public class SpawnObj : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(AutoDeath());
        }


        private IEnumerator AutoDeath()
        {
            yield return new WaitForSeconds(5f);
            Destroy(gameObject);
        }

    }
    
}

