using System.Collections.Generic;
using Vintagestory.API.Common;

namespace ServerAuth;

public class PlayerFreeze(double x, double y, double z)
{
    public double X = x;
    public double Y = y;
    public double Z = z;

    public Dictionary<int, ItemStack> hotbar = [];
    public Dictionary<int, ItemStack> backpack = [];
    public Dictionary<int, ItemStack> ground = [];
    public Dictionary<int, ItemStack> mouse = [];
    public Dictionary<int, ItemStack> crafting = [];
    public Dictionary<int, ItemStack> character = [];
}

public class ItemstackFreeze { }