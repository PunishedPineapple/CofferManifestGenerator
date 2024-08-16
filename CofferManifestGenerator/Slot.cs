using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CofferManifestGenerator;

internal enum Slot
{
	Weapon,
	Head,
	Body,
	Hands,
	Legs,
	Feet,
	Earring,
	Neck,
	Bracelet,
	Ring
}

internal static class SlotExtensions
{
	internal static Slot Parse( string str )
	{
		return str.ToLower() switch
		{
			"weapon" => Slot.Weapon,
			"head" => Slot.Head,
			"chest" => Slot.Body,
			"hand" => Slot.Hands,
			"leg" => Slot.Legs,
			"foot" => Slot.Feet,
			"earring" => Slot.Earring,
			"necklace" => Slot.Neck,
			"bracelet" => Slot.Bracelet,
			"ring" => Slot.Ring,
			_ => throw new Exception( $"Invalid slot name: {str}." ),
		};
	}
}