using System;
using System.Globalization;
using System.Numerics;

public class RotationConverting
{
    // http://www.euclideanspace.com/maths/geometry/rotations/conversions/angleToQuaternion/index.htm
    // https://www.andre-gaschler.com/rotationconverter/

    public static string[] EulerOrder = new string[]
    {
            "XYZ",
            "YXZ",
            "ZXY",
            "ZYX",
            "YZX",
            "XZY"
    };

    public static string[] RotTypes = new string[]
    {
            "deg",
            "rad"
    };

    public static float Clamp(float value, float min, float max)
    {
        return (float)System.Math.Max(min, System.Math.Min(max, value));
    }

    public static Vector3 Quaternion2Euler(Quaternion q, string order)
    {
        float x = 0;
        float y = 0;
        float z = 0;

        var sqx = q.X * q.X;
        var sqy = q.Y * q.Y;
        var sqz = q.Z * q.Z;
        var sqw = q.W * q.W;

        if (order == "XYZ")
        {
            x = (float)System.Math.Atan2(2 * (q.X * q.W - q.Y * q.Z), (sqw - sqx - sqy + sqz));
            y = (float)System.Math.Asin(Clamp(2 * (q.X * q.Z + q.Y * q.W), -1, 1));
            z = (float)System.Math.Atan2(2 * (q.Z * q.W - q.X * q.Y), (sqw + sqx - sqy - sqz));
        }
        else if (order == "YXZ")
        {
            x = (float)System.Math.Asin(Clamp(2 * (q.X * q.W - q.Y * q.Z), -1, 1));
            y = (float)System.Math.Atan2(2 * (q.X * q.Z + q.Y * q.W), (sqw - sqx - sqy + sqz));
            z = (float)System.Math.Atan2(2 * (q.X * q.Y + q.Z * q.W), (sqw - sqx + sqy - sqz));
        }
        else if (order == "ZXY")
        {
            x = (float)System.Math.Asin(Clamp(2 * (q.X * q.W + q.Y * q.Z), -1, 1));
            y = (float)System.Math.Atan2(2 * (q.Y * q.W - q.Z * q.X), (sqw - sqx - sqy + sqz));
            z = (float)System.Math.Atan2(2 * (q.Z * q.W - q.X * q.Y), (sqw - sqx + sqy - sqz));
        }
        else if (order == "ZYX")
        {
            x = (float)System.Math.Atan2(2 * (q.X * q.W + q.Z * q.Y), (sqw - sqx - sqy + sqz));
            y = (float)System.Math.Asin(Clamp(2 * (q.Y * q.W - q.X * q.Z), -1, 1));
            z = (float)System.Math.Atan2(2 * (q.X * q.Y + q.Z * q.W), (sqw + sqx - sqy - sqz));
        }
        else if (order == "YZX")
        {
            x = (float)System.Math.Atan2(2 * (q.X * q.W - q.Z * q.Y), (sqw - sqx + sqy - sqz));
            y = (float)System.Math.Atan2(2 * (q.Y * q.W - q.X * q.Z), (sqw + sqx - sqy - sqz));
            z = (float)System.Math.Asin(Clamp(2 * (q.X * q.Y + q.Z * q.W), -1, 1));
        }
        else if (order == "XZY")
        {
            x = (float)System.Math.Atan2(2 * (q.X * q.W + q.Y * q.Z), (sqw - sqx + sqy - sqz));
            y = (float)System.Math.Atan2(2 * (q.X * q.Z + q.Y * q.W), (sqw + sqx - sqy - sqz));
            z = (float)System.Math.Asin(Clamp(2 * (q.Z * q.W - q.X * q.Y), -1, 1));
        }

        return new Vector3(x, y, z);
    }

    public static Quaternion Euler2Quaternion(Vector3 v, string order)
    {
        float x = v.X, y = v.Y, z = v.Z;

        float _x = 0;
        float _y = 0;
        float _z = 0;
        float _w = 0;

        var c1 = (float)System.Math.Cos(x / 2);
        var c2 = (float)System.Math.Cos(y / 2);
        var c3 = (float)System.Math.Cos(z / 2);

        var s1 = (float)System.Math.Sin(x / 2);
        var s2 = (float)System.Math.Sin(y / 2);
        var s3 = (float)System.Math.Sin(z / 2);

        if (order == "XYZ")
        {
            _x = s1 * c2 * c3 + c1 * s2 * s3;
            _y = c1 * s2 * c3 - s1 * c2 * s3;
            _z = c1 * c2 * s3 + s1 * s2 * c3;
            _w = c1 * c2 * c3 - s1 * s2 * s3;
        }
        else if (order == "YXZ")
        {
            _x = s1 * c2 * c3 + c1 * s2 * s3;
            _y = c1 * s2 * c3 - s1 * c2 * s3;
            _z = c1 * c2 * s3 - s1 * s2 * c3;
            _w = c1 * c2 * c3 + s1 * s2 * s3;
        }
        else if (order == "ZXY")
        {
            _x = s1 * c2 * c3 - c1 * s2 * s3;
            _y = c1 * s2 * c3 + s1 * c2 * s3;
            _z = c1 * c2 * s3 + s1 * s2 * c3;
            _w = c1 * c2 * c3 - s1 * s2 * s3;
        }
        else if (order == "ZYX")
        {
            _x = s1 * c2 * c3 - c1 * s2 * s3;
            _y = c1 * s2 * c3 + s1 * c2 * s3;
            _z = c1 * c2 * s3 - s1 * s2 * c3;
            _w = c1 * c2 * c3 + s1 * s2 * s3;
        }
        else if (order == "YZX")
        {
            _x = s1 * c2 * c3 + c1 * s2 * s3;
            _y = c1 * s2 * c3 + s1 * c2 * s3;
            _z = c1 * c2 * s3 - s1 * s2 * c3;
            _w = c1 * c2 * c3 - s1 * s2 * s3;
        }
        else if (order == "XZY")
        {
            _x = s1 * c2 * c3 - c1 * s2 * s3;
            _y = c1 * s2 * c3 - s1 * c2 * s3;
            _z = c1 * c2 * s3 + s1 * s2 * c3;
            _w = c1 * c2 * c3 + s1 * s2 * s3;
        }

        return new Quaternion(_x, _y, _z, _w);
    }

    public static (Vector3, float) Quaternion2Axis(Quaternion q)
    {
        if (q.W > 1) q = Quaternion.Normalize(q); // if w>1 acos and sqrt will produce errors, this cant happen if quaternion is normalised

        float angle = (float)(2 * Math.Acos(q.W));
        float s = (float)Math.Sqrt(1 - q.W * q.W); // assuming quaternion normalised then w is less than 1, so term always positive.

        if (s < 0.001) // test to avoid divide by zero, s is always positive due to sqrt
        {
            // if s close to zero then direction of axis not important
            return (
                new Vector3
                {
                    X = q.X, // if it is important that axis is normalised then replace with x=1; y=z=0;
                    Y = q.Y,
                    Z = q.Z
                },
                angle
            );
        }
        else
        {
            return (
                new Vector3
                {
                    X = q.X / s, // normalise axis
                    Y = q.Y / s,
                    Z = q.Z / s
                },
                angle
            );
        }
    }

    public static Quaternion Axis2Quaternion(Vector3 angles, float angle)
    {
        Vector3 vn = Vector3.Normalize(angles);
        float s = (float)Math.Sin(angle / 2);

        return new Quaternion
        {
            X = vn.X * s,
            Y = vn.Y * s,
            Z = vn.Z * s,
            W = (float)Math.Cos(angle / 2)
        };
    }

    public static Vector3 Angles2MagAxis((Vector3, float) fv)
    {
        return Vector3.Multiply(fv.Item1, fv.Item2);
    }

    public static float Deg2Rad(float f)
    {
        return (float)(f * (Math.PI / 180));
    }

    public static Vector3 Deg2Rad(Vector3 v)
    {
        return new Vector3
        {
            X = Deg2Rad(v.X),
            Y = Deg2Rad(v.Y),
            Z = Deg2Rad(v.Z)
        };
    }

    public static float Rad2Deg(float f)
    {
        return (float)(f * (180 / Math.PI));
    }

    public static Vector3 Rad2Deg(Vector3 v)
    {
        return new Vector3
        {
            X = Rad2Deg(v.X),
            Y = Rad2Deg(v.Y),
            Z = Rad2Deg(v.Z)
        };
    }

    public static string Vec2Str(Vector3 v)
    {
        return
            v.X.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            v.Y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            v.Z.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string VecAngle2Str((Vector3, float) ang)
    {
        return
            ang.Item1.X.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            ang.Item1.Y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            ang.Item1.Z.ToString("0.######", CultureInfo.InvariantCulture) + " " +
            ang.Item2.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string Quat2Str(Quaternion q)
    {
        return
            q.X.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            q.Y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            q.Z.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            q.W.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string Float2Str(float f)
    {
        return f.ToString("0.######", CultureInfo.InvariantCulture);
    }
}