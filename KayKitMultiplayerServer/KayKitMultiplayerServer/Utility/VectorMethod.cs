using UnityEngine;

namespace KayKitMultiplayerServer.Utility
{
    public static class VectorMethod
    {
        public static float[] ToFloatArray(this Vector3 vector3)
        {
            float[] newVector3 = new float[3];
            newVector3[0] = vector3.x;
            newVector3[1] = vector3.y;
            newVector3[2] = vector3.z;

            return newVector3;
        }

        public static Vector3 ToVector3(this float[] floats)
        {
            return new Vector3(floats[0], floats[1], floats[2]);
        }

        public static Vector3 ToVector3(this string sVector)
        {
            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));

            return result;
        }

        public static float[] ToFloatArray(this Vector2 vector2)
        {
            float[] newVector3 = new float[2];
            newVector3[0] = vector2.x;
            newVector3[1] = vector2.y;

            return newVector3;
        }

        public static Vector2 ToVector2(this float[] floats)
        {
            return new Vector2(floats[0], floats[1]);
        }
    }
}