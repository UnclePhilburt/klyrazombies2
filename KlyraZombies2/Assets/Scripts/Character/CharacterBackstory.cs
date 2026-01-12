using UnityEngine;

/// <summary>
/// Holds randomly generated backstory data for a player character.
/// </summary>
[System.Serializable]
public class CharacterBackstory
{
    public string characterName;
    public string formerOccupation;
    public string familyStatus;
    public string funFact;
    public int daysSurvived;
    public int age;

    /// <summary>
    /// Generates a random backstory for a new character.
    /// </summary>
    public static CharacterBackstory GenerateRandom()
    {
        var backstory = new CharacterBackstory();

        backstory.characterName = GetRandomName();
        backstory.formerOccupation = GetRandomOccupation();
        backstory.familyStatus = GetRandomFamilyStatus();
        backstory.funFact = GetRandomFunFact();
        backstory.daysSurvived = Random.Range(1, 90);
        backstory.age = Random.Range(22, 55);

        return backstory;
    }

    private static string GetRandomName()
    {
        string[] firstNames = new string[]
        {
            // Male names
            "James", "Marcus", "David", "Michael", "Robert", "William", "Joseph", "Thomas",
            "Carlos", "Derek", "Frank", "George", "Henry", "Ivan", "Jack", "Kevin",
            "Leo", "Nathan", "Oscar", "Paul", "Quinn", "Ray", "Sam", "Victor",
            // Female names
            "Sarah", "Emily", "Jessica", "Ashley", "Amanda", "Nicole", "Stephanie", "Jennifer",
            "Maria", "Diana", "Elena", "Fiona", "Grace", "Hannah", "Iris", "Julia",
            "Kate", "Laura", "Maya", "Nina", "Olivia", "Paula", "Rachel", "Tara",
            // Gender neutral
            "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Quinn", "Avery"
        };

        string[] lastNames = new string[]
        {
            "Walker", "Chen", "Rodriguez", "Smith", "Johnson", "Williams", "Brown", "Jones",
            "Garcia", "Miller", "Davis", "Martinez", "Anderson", "Taylor", "Thomas", "Moore",
            "Jackson", "Martin", "Lee", "Thompson", "White", "Harris", "Clark", "Lewis",
            "Young", "Hall", "Allen", "King", "Wright", "Scott", "Green", "Baker",
            "Kowalski", "Petrov", "Nakamura", "O'Brien", "Santos", "Kim", "Nguyen", "Patel"
        };

        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }

    private static string GetRandomOccupation()
    {
        string[] occupations = new string[]
        {
            "High School Teacher",
            "Nurse",
            "Construction Worker",
            "Software Developer",
            "Police Officer",
            "Firefighter",
            "Chef",
            "Accountant",
            "Mechanic",
            "Electrician",
            "Store Manager",
            "Bartender",
            "Security Guard",
            "Paramedic",
            "Office Worker",
            "Truck Driver",
            "Warehouse Worker",
            "Veterinarian",
            "Journalist",
            "Personal Trainer",
            "Real Estate Agent",
            "Bank Teller",
            "Dental Hygienist",
            "IT Technician",
            "Plumber",
            "Carpenter",
            "Fast Food Worker",
            "Delivery Driver",
            "College Student",
            "Unemployed",
            "Retired Military",
            "Ex-Con",
            "Small Business Owner",
            "Insurance Agent",
            "Graphic Designer",
            "Social Worker"
        };

        return occupations[Random.Range(0, occupations.Length)];
    }

    private static string GetRandomFamilyStatus()
    {
        string[] statuses = new string[]
        {
            "Lost contact with family on Day 1",
            "Searching for missing spouse",
            "Last of their family",
            "Kids are safe... hopefully",
            "Family didn't make it",
            "Separated from siblings during evacuation",
            "Parents are in another state",
            "Only child, parents deceased before outbreak",
            "Spouse turned, had to put them down",
            "Children evacuated with grandparents",
            "No family to speak of",
            "Twin sibling somewhere out there",
            "Ex-spouse has the kids, location unknown",
            "Whole family survived together... until last week",
            "Orphan, grew up in foster care",
            "Family trapped in quarantine zone",
            "Pregnant partner waiting at safehouse",
            "Taking care of elderly parent",
            "Lost everyone in the first wave"
        };

        return statuses[Random.Range(0, statuses.Length)];
    }

    private static string GetRandomFunFact()
    {
        string[] facts = new string[]
        {
            "Can hot-wire any car made before 2010",
            "Deathly afraid of clowns",
            "Has never fired a gun before the outbreak",
            "Was a competitive archer in college",
            "Knows sign language",
            "Allergic to penicillin",
            "Can identify edible plants",
            "Former Eagle Scout",
            "Has a tattoo they regret",
            "Plays guitar... badly",
            "Speaks three languages",
            "Was training for a marathon",
            "Has a metal plate in their leg",
            "Never learned to swim",
            "Can pick most locks",
            "Vegetarian (was, anyway)",
            "Has perfect 20/20 vision",
            "Sleepwalks occasionally",
            "Can do a perfect bird whistle",
            "Was about to propose before everything went wrong",
            "Still carries a photo of their dog",
            "Refuses to kill unless absolutely necessary",
            "Has surprisingly good aim",
            "Can sew and stitch wounds",
            "Knows basic first aid",
            "Gets motion sickness easily",
            "Left-handed",
            "Has a lucky coin they always carry",
            "Was supposed to fly out the day it started",
            "Knows morse code",
            "Can cook a meal from almost anything",
            "Has insomnia",
            "Former smoker, 2 years clean",
            "Afraid of heights",
            "Has a scar from a childhood accident",
            "Was in a band once",
            "Can read lips",
            "Double jointed",
            "Has a photographic memory for faces"
        };

        return facts[Random.Range(0, facts.Length)];
    }
}
