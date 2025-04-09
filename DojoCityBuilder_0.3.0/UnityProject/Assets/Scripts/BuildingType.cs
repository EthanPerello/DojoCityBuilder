using System;

public class BuildingType
{
    private readonly uint value;

    private BuildingType(uint value)
    {
        this.value = value;
    }

    public static BuildingType Residential => new BuildingType(0);
    public static BuildingType Commercial => new BuildingType(1);
    public static BuildingType Industrial => new BuildingType(2);

    public static implicit operator uint(BuildingType type) => type.value;
    public static implicit operator BuildingType(uint value) => new BuildingType(value);

    public override string ToString()
    {
        return value switch
        {
            0 => "Residential",
            1 => "Commercial",
            2 => "Industrial",
            _ => "Unknown"
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is BuildingType other)
        {
            return value == other.value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}