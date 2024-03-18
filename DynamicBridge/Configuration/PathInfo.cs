﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration;
public class PathInfo : IEquatable<PathInfo>
{
    public string Name;
    public int Indentation;
    public bool Glamourer = false;
    public bool Customize = false;
    public bool Moodles = false;

    public PathInfo(string name)
    {
        this.Name = name;
        this.Indentation = 0;
    }

    public PathInfo(string name, int indentation)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.Indentation = indentation;
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as PathInfo);
    }

    public bool Equals(PathInfo other)
    {
        return other is not null &&
               this.Name == other.Name &&
               this.Indentation == other.Indentation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.Indentation);
    }

    public static bool operator ==(PathInfo left, PathInfo right)
    {
        return EqualityComparer<PathInfo>.Default.Equals(left, right);
    }

    public static bool operator !=(PathInfo left, PathInfo right)
    {
        return !(left == right);
    }
}
