using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero
        {
            get { return new Vector3(0f, 0f, 0f); }
        }
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Vector3Int
    {
        public int x;
        public int y;
        public int z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    public struct Rect
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        public Rect(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public float x
        {
            get { return _x; }
        }

        public float y
        {
            get { return _y; }
        }

        public float width
        {
            get { return _width; }
        }

        public float height
        {
            get { return _height; }
        }
    }

    public struct Bounds
    {
        private Vector3 _center;
        private Vector3 _size;

        public Bounds(Vector3 center, Vector3 size)
        {
            _center = center;
            _size = size;
        }

        public Vector3 center
        {
            get { return _center; }
        }

        public Vector3 size
        {
            get { return _size; }
        }
    }

    public sealed class TextAsset
    {
        public byte[] bytes
        {
            get { return new byte[0]; }
        }
    }

    public static class Application
    {
        public static string persistentDataPath
        {
            get { return System.IO.Path.GetTempPath(); }
        }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute()
        {
        }

        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType)
        {
        }
    }
}

namespace Godot
{
    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Vector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public struct Vector2I
    {
        public int X;
        public int Y;

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Vector3I
    {
        public int X;
        public int Y;
        public int Z;

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Quaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public struct Color
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    public struct Rect2
    {
        public Vector2 Position;
        public Vector2 Size;

        public Rect2(float x, float y, float width, float height)
        {
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
        }
    }

    public struct Aabb
    {
        public Vector3 Position;
        public Vector3 Size;

        public Aabb(Vector3 position, Vector3 size)
        {
            Position = position;
            Size = size;
        }
    }

    public static class FileAccess
    {
        public static byte[] GetFileAsBytes(string path)
        {
            return new byte[0];
        }
    }

    public static class ProjectSettings
    {
        public static string GlobalizePath(string path)
        {
            return path;
        }
    }
}
