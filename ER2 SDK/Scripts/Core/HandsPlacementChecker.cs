using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HandsPlacementChecker : MonoBehaviour
{
    public GenericGun attached_weapon;

    public Transform left_hand;
    public Transform right_hand;

    private void Update()
    {
        if (left_hand && right_hand && attached_weapon && transform.parent==null)
        {
            right_hand.transform.localPosition = Vector3.zero;
            right_hand.transform.localScale = left_hand.transform.localScale = transform.localScale = Vector3.one;
            transform.position = attached_weapon.transform.position;

            if (attached_weapon.leftHandHoldPosition)
            {
                left_hand.transform.position = attached_weapon.leftHandHoldPosition.position;
                left_hand.transform.rotation = attached_weapon.leftHandHoldPosition.rotation;
                if (!left_hand.gameObject.activeSelf)
                    left_hand.gameObject.SetActive(true);
            }
            else
            {
                if (left_hand.gameObject.activeSelf)
                    left_hand.gameObject.SetActive(false);
            }
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}