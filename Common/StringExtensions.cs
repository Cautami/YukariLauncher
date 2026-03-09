using System;
using Godot;

public static class StringExtensions
{
    extension(string str)
    {
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(str);
        }
    }

    extension(StringName str)
    {
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(str);
        }
    }
}