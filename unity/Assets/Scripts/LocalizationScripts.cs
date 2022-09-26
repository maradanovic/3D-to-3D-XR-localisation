using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public static class Localization
    {
        public static Quaternion GetRotation(this Matrix4x4 m)
        {
            // Modified from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }

        //Modified from https://answers.unity.com/questions/1030082/convert-vector-3-to-string-and-reverse-c.html
        //Convert a Vector3 array to string.
        public static void SerializeVector3Array(StringBuilder sb, List<Vector3> aVectors, Transform tf)
        {
            foreach (Vector3 v in aVectors)
            {
                Vector3 vt = tf.TransformPoint(v);
                sb.Append(vt.x).Append(" ").Append(vt.y).Append(" ").Append(vt.z).Append("\n");
            }
        }

        //Convert a string of a 4x4 transformation matrix to a Vector4 array.
        public static Matrix4x4 DeserializeVector4Array(string aData)
        {
            string[] vectors = aData.Split('|');
            if (vectors.Length != 4)
                throw new System.FormatException("Error in the received transformation matrix. Expected 4 rows but got" + vectors.Length);
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < vectors.Length; i++)
            {
                string[] values = vectors[i].Split(' ');

                if (values.Length != 4)
                    throw new System.FormatException("Error in the received transformation matrix. Expected 4 components per row but got " + values.Length);
                result.SetColumn(i, new Vector4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])));
            }
            return result;
        }

        //Convert an int array of mesh triangles to a string in the format of .ply. 
        public static void SerializeIntArray(StringBuilder sb, List<int> ints, int indexCorrection)
        {
            int len = ints.Count;
            int i = 0;
            while (i < len)
            {
                sb.Append("3 ").Append(ints[i] + indexCorrection).Append(" ").Append(ints[i + 1] + indexCorrection).Append(" ").Append(ints[i + 2] + indexCorrection).Append("\n");
                i += 3;
            }
        }

        public static string SerializeVector3Position(Vector3 position)
        {
            string result;
            result = position.x + " " + position.y + " " + position.z;
            return result;
        }
    }
}