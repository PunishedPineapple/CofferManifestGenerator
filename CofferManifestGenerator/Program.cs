using System.Text.RegularExpressions;

using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CofferManifestGenerator;

internal class Program
{
	static void Main( string[] args )
	{
		var luminaOptions = new LuminaOptions
		{
			LoadMultithreaded = true,
			CacheFileResources = true,
			PanicOnSheetChecksumMismatch = true,
			DefaultExcelLanguage = Lumina.Data.Language.English,
		};

		try
		{
			mGameData = new GameData( Path.Combine( args[0], "game\\sqpack" ), luminaOptions );
			mItemSheet = mGameData.Excel.GetSheet<Item>();
			mRecipeSheet = mGameData.Excel.GetSheet<Recipe>();

			Console.WriteLine( "Lumina is ready: {0}", mGameData.DataPath );
		}
		catch( Exception e )
		{
			Console.WriteLine( $"Error initializing Lumina:\r\n{e}" );
			return;
		}

		if( args.Length > 1 && args[1].ToLower() == "list" )
		{
			GenerateCofferListOnly();
		}
		else
		{
			GenerateCofferManifests();
		}
	}

	private static void GenerateCofferListOnly()
	{
		Dictionary<UInt32, string> cofferDict = new();
		int rowCount = 0;

		foreach( var row in mItemSheet )
		{
			++rowCount;
			if( row.Name.ToString().Contains( "Coffer (IL", StringComparison.InvariantCultureIgnoreCase ) )
			{
				cofferDict.TryAdd( row.RowId, row.Name );
			}
		}

		Console.WriteLine( $"Found {cofferDict.Count} gear coffers of any type in {rowCount} rows." );

		try
		{
			var outFile = File.CreateText( ".\\CofferList.csv" );
			outFile.WriteLine( "Item ID, Name" );
			foreach( var entry in cofferDict )
			{
				outFile.WriteLine( $"{entry.Key},{entry.Value}" );
			}
			outFile.Close();
		}
		catch( Exception e )
		{
			Console.WriteLine( $"Error writing coffer list:\r\n{e}" );
		}
	}

	private static void GenerateCofferManifests()
	{
		Dictionary<UInt32, CofferInfo> cofferDict = new();
		int rowCount = 0;
		int blacklistedCount = 0;

		Regex cofferRegex = new( @"(.*) (weapon|head|chest|hand|leg|foot|earring|necklace|bracelet|ring)(| gear) coffer \(il ([0-9]+)\)" );

		foreach( var row in mItemSheet )
		{
			++rowCount;
			var match = cofferRegex.Match( row.Name.ToString().ToLower() );
			if( match.Success )
			{
				CofferInfo cofferData = new()
				{
					Name = row.Name,
					SeriesName = match.Groups[1].Value,
					EquipSlot = SlotExtensions.Parse( match.Groups[2].Value ),
					ItemLevel = int.Parse( match.Groups[4].Value ),
				};

				cofferDict.TryAdd( row.RowId, cofferData );
			}
		}

		Console.WriteLine( $"Found {cofferDict.Count} single-slot coffers in {rowCount} rows." );

		try
		{
			var outFile = File.CreateText( ".\\CofferData.csv" );
			outFile.WriteLine( "Item ID, Name, Series, Equip Slot, Item Level" );
			foreach( var entry in cofferDict )
			{
				outFile.WriteLine( $"{entry.Key},{entry.Value.Name},{entry.Value.SeriesName},{entry.Value.EquipSlot},{entry.Value.ItemLevel}" );
			}
			outFile.Close();
		}
		catch( Exception e )
		{
			Console.WriteLine( $"Error writing coffer data:\r\n{e}" );
		}

		Dictionary<uint,List<uint>> cofferManifests = new();

		foreach( var coffer in cofferDict )
		{
			List<uint> cofferRecipes = new();

			if( mBlacklistedCoffers.Contains( coffer.Key ) )
			{
				++blacklistedCount;
				continue;
			}

			foreach( var recipe in mRecipeSheet )
			{
				//***** TODO:	How can we reasonably check the series name, since recipes in the series might have different names for some pieces.  A few
				//*****			coffers, like the ruthenium ring coffer, have no items with the series name in them. The best option may be to just print out
				//*****			recipe names after comparing ilvl and jobs and manually look for any that don't fit in.  We *may* be able to check whether
				//*****			the series name is in the craft name or one of its ingredients.

				if( //recipe.ItemResult.Value.Name.ToString().ToLower().Contains( coffer.Value.SeriesName.ToLower() ) &&
					recipe.ItemResult.Value.LevelItem.Value.RowId == coffer.Value.ItemLevel &&
					!(
						recipe.SecretRecipeBook.Value.RowId > 0 &&
						recipe.RecipeLevelTable.Value.Stars > 0
					) &&
					(
						( recipe.ItemResult.Value.EquipSlotCategory.Value.MainHand != 0 && coffer.Value.EquipSlot == Slot.Weapon ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.OffHand != 0 && coffer.Value.EquipSlot == Slot.Weapon ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Head != 0 && coffer.Value.EquipSlot == Slot.Head ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Body != 0 && coffer.Value.EquipSlot == Slot.Body ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Gloves != 0 && coffer.Value.EquipSlot == Slot.Hands ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Legs != 0 && coffer.Value.EquipSlot == Slot.Legs ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Feet != 0 && coffer.Value.EquipSlot == Slot.Feet ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Ears != 0 && coffer.Value.EquipSlot == Slot.Earring ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Neck != 0 && coffer.Value.EquipSlot == Slot.Neck ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.Wrists != 0 && coffer.Value.EquipSlot == Slot.Bracelet ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.FingerR != 0 && coffer.Value.EquipSlot == Slot.Ring ) ||
						( recipe.ItemResult.Value.EquipSlotCategory.Value.FingerL != 0 && coffer.Value.EquipSlot == Slot.Ring )
					) &&
					!(
						recipe.ItemResult.Value.ClassJobCategory.Value.CRP ||
						recipe.ItemResult.Value.ClassJobCategory.Value.BSM ||
						recipe.ItemResult.Value.ClassJobCategory.Value.ARM ||
						recipe.ItemResult.Value.ClassJobCategory.Value.GSM ||
						recipe.ItemResult.Value.ClassJobCategory.Value.LTW ||
						recipe.ItemResult.Value.ClassJobCategory.Value.WVR ||
						recipe.ItemResult.Value.ClassJobCategory.Value.ALC ||
						recipe.ItemResult.Value.ClassJobCategory.Value.CUL ||
						recipe.ItemResult.Value.ClassJobCategory.Value.MIN ||
						recipe.ItemResult.Value.ClassJobCategory.Value.BTN ||
						recipe.ItemResult.Value.ClassJobCategory.Value.FSH
						)
					)
				{
					cofferRecipes.Add( recipe.ItemResult.Value.RowId );
				}
			}

			if( cofferRecipes.Count > 0 ) cofferManifests.TryAdd( coffer.Key, cofferRecipes );
		}

		Console.WriteLine( $"Ignored {blacklistedCount} blacklisted coffers (out of {mBlacklistedCoffers.Length} expected)." );

		try
		{
			var outFile = File.CreateText( ".\\CofferManifests.csv" );
			foreach( var entry in cofferManifests )
			{
				string line = $"{entry.Key}";
				foreach( var item in entry.Value ) line += $",{item}";
				outFile.WriteLine( line );
			}
			outFile.Close();

			outFile = File.CreateText( ".\\CofferManifests_Resolved.csv" );
			outFile.WriteLine( "Item ID, Name, Contents..." );
			foreach( var entry in cofferManifests )
			{
				string line = $"{entry.Key},{mItemSheet.GetRow( entry.Key ).Name}";
				foreach( var item in entry.Value ) line += $",{mItemSheet.GetRow( item ).Name}";
				outFile.WriteLine( line );
			}
			outFile.Close();
		}
		catch( Exception e )
		{
			Console.WriteLine( $"Error writing coffer manifests:\r\n{e}" );
		}

		Console.WriteLine( $"Exported manifests for {cofferManifests.Count} coffers that met all requirements." );
	}

	private static GameData mGameData = null;
	private static ExcelSheet<Item> mItemSheet = null;
	private static ExcelSheet<Recipe> mRecipeSheet = null;

	//	Coffers that don't get weeded out by our other checks.
	private static readonly uint[] mBlacklistedCoffers =
	{
		31691,	//	Level 50 Weapon Coffer (IL 70)
		35884,  //	High Allagan Weapon Coffer (IL 115)
		36136,	//	Vortex Weapon Coffer (IL 70)
		36146,	//	Sophic Weapon Coffer (IL 255)
		36147,	//	Zurvanite Weapon Coffer (IL 265)
		36152,	//	Suzaku Weapon Coffer (IL 385)
		36153,	//	Seiryu Weapon Coffer (IL 395)
		36158,	//	Emerald Weapon Coffer (IL 515)
		36159,	//	Diamond Zeta Weapon Coffer (IL 525)
		40296,	//	Voidcast Weapon Coffer (IL 645)
		41054,	//	Voidvessel Weapon Coffer (IL 655)
	};
}
