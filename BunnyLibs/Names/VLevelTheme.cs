public static class vLevelTheme
{
	public const string
			Downtown = "Downtown",
			Industrial = "Industrial",
			MayorVillage = "MayorVillage",
			Park = "Park",
			Slums = "Slums",
			Uptown = "Uptown";

	public enum LevelTheme : int
	{
		Slums = 0,
		Industrial = 1,
		Park = 2,
		Downtown = 3,
		Uptown = 4,
		MayorVillage = 5
	}
}