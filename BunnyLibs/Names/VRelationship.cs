using System.Collections.Generic;

public class VRelationship
{
    public const string
        Aligned = "Aligned",
        Annoyed = "Annoyed",
        Friendly = "Friendly",
        Hostile = "Hateful",
        Loyal = "Loyal",
        Neutral = "Neutral",
        Submissive = "Submissive";

    // Why not just use an enum instead?
    public static List<string> OrderedRelationships = new List<string>()
    {
        Hostile,    //  0
        Annoyed,    //  1
        Neutral,    //  2
        Friendly,   //  3
        Loyal,      //  4
        Aligned     //  5
    };

    /// <summary>
    /// Hostile = 0<br></br>Neutral = 2<br></br>Aligned = 5
    /// </summary>
    public static int GetRelationshipLevel(string relationship) =>
        OrderedRelationships.IndexOf(relationship);
}