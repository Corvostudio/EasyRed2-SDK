// RigidbodyCompat.cs
using UnityEngine;

public static class RigidbodyCompat
{
    // ---------- Rigidbody 3D ----------

    public static Vector3 GetVelocity(this Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(this Rigidbody rb, Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    public static float GetLinearDamping(this Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearDamping;
#else
        return rb.drag;
#endif
    }

    public static void SetLinearDamping(this Rigidbody rb, float damping)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = damping;
#else
        rb.drag = damping;
#endif
    }

    public static float GetAngularDamping(this Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.angularDamping;
#else
        return rb.angularDrag;
#endif
    }

    public static void SetAngularDamping(this Rigidbody rb, float damping)
    {
#if UNITY_6000_0_OR_NEWER
        rb.angularDamping = damping;
#else
        rb.angularDrag = damping;
#endif
    }

    // Optional old-name aliases, useful while migrating.

    public static float GetDrag(this Rigidbody rb)
    {
        return rb.GetLinearDamping();
    }

    public static void SetDrag(this Rigidbody rb, float drag)
    {
        rb.SetLinearDamping(drag);
    }

    public static float GetAngularDrag(this Rigidbody rb)
    {
        return rb.GetAngularDamping();
    }

    public static void SetAngularDrag(this Rigidbody rb, float angularDrag)
    {
        rb.SetAngularDamping(angularDrag);
    }


    // ---------- Rigidbody2D ----------

    public static Vector2 GetVelocity(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(this Rigidbody2D rb, Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    public static float GetLinearDamping(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearDamping;
#else
        return rb.drag;
#endif
    }

    public static void SetLinearDamping(this Rigidbody2D rb, float damping)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = damping;
#else
        rb.drag = damping;
#endif
    }

    public static float GetAngularDamping(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.angularDamping;
#else
        return rb.angularDrag;
#endif
    }

    public static void SetAngularDamping(this Rigidbody2D rb, float damping)
    {
#if UNITY_6000_0_OR_NEWER
        rb.angularDamping = damping;
#else
        rb.angularDrag = damping;
#endif
    }

    public static float GetDrag(this Rigidbody2D rb)
    {
        return rb.GetLinearDamping();
    }

    public static void SetDrag(this Rigidbody2D rb, float drag)
    {
        rb.SetLinearDamping(drag);
    }

    public static float GetAngularDrag(this Rigidbody2D rb)
    {
        return rb.GetAngularDamping();
    }

    public static void SetAngularDrag(this Rigidbody2D rb, float angularDrag)
    {
        rb.SetAngularDamping(angularDrag);
    }
}