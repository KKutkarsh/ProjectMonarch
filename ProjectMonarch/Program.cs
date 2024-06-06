using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectMonarch
{
    /// <summary>
    /// Assumptions: 
    /// 1. As the requirement says that the time period is of actual monarchs of england and UK. I assume 
    /// that the last monarch, where there is start date but no end date is ruling till now.
    /// 2. There is no explaination of what to do when more than one object qualifies for the scenario ex. max rules, common name etc.
    /// hence I have assumed to show all the data that qualifies for the scenario.
    /// </summary>

    internal class Program
    {
        public static List<Monarch>? monarches;

        static async Task Main(string[] args)
        {
            //step 1. read all the data from the URL into a List
            await GetJsonData();
            CountMonarchsInDataSet();
            Console.WriteLine("\n ------------------------------------------------ \n");
            GetLongestRulingMonarchsInDataSet();
            Console.WriteLine("\n ------------------------------------------------ \n");
            GetTheLongestRulingHouseWithDuration();
            Console.WriteLine("\n ------------------------------------------------ \n");
            GetMostCommonFirstNameInDataSet();
            Console.WriteLine("\n ------------------------------------------------ \n");
            GetCurrentMonarchAndItsHouseRulingPeriod();
            Console.WriteLine("\n ------------------------------------------------ \n");
        }

        #region Methods
        private static async Task GetJsonData()
        {
            using HttpClient httpClient = new();

            string jsonData = string.Empty;
            try
            {
                jsonData = await httpClient.GetStringAsync(Constants.dataUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            monarches = JsonSerializer.Deserialize<List<Monarch>>(jsonData);
        }

        private static void CountMonarchsInDataSet()
        {
            Console.WriteLine("We have: {0} Monarchs in the data set", monarches?.Count);
        }

        private static void GetLongestRulingMonarchsInDataSet()
        {

            //var longestRulingMonarch = monarches?
            //.Select(x => new { x.Name, Duration = CalcuateDuration(m.Years) })
            //.OrderByDescending(m => m.Duration)
            //.FirstOrDefault();


            //better approach because more than one monarchs can have same duration
            var longestMonarches = monarches?.Select(x => new
            {
                Monarch = x,
                Duration = CalcuateDuration(x.Years)
            }
            ).GroupBy(x => x.Duration).OrderByDescending(g => g.Key).FirstOrDefault();

            Console.WriteLine("Longest Monarch Duration: {0} years", longestMonarches?.Key);

            foreach (var monarchObj in longestMonarches)
            {
                Console.WriteLine("monarch name : {0}, period : {1}, duration : {2})", monarchObj.Monarch.Name, monarchObj.Monarch.Years, monarchObj.Duration);
            }

        }

        private static void GetTheLongestRulingHouseWithDuration()
        {
            Dictionary<string, int> rulingHouseAndYears = new();
            foreach (Monarch monarch in monarches)
            {
                int duration = CalcuateDuration(monarch.Years);
                AddToDictionary(rulingHouseAndYears,monarch.House, duration);
            }
            //can be more than one monarchs hence group by
            var longestRulingHouses = rulingHouseAndYears.GroupBy(x=>x.Value).OrderByDescending(g => g.Key).First();

            Console.WriteLine("Longest Ruling House: {0} years", longestRulingHouses?.Key);

            foreach (KeyValuePair<string, int> rulinghouse in longestRulingHouses)
            {
                Console.WriteLine("Ruling House Name: {0}  for {1} years", rulinghouse.Key, rulinghouse.Value);
            }
        }

        private static void GetMostCommonFirstNameInDataSet()
        {
            Dictionary<string, int> namesWithOccuranceCount = new();
            foreach (Monarch monarch in monarches)
            {
                AddToDictionary(namesWithOccuranceCount, monarch.Name.Split(" ")[0], 1); //value one 
            }

            //can be more than one monarchs hence group by
            var mostCommonFirstNames = namesWithOccuranceCount.GroupBy(x => x.Value).OrderByDescending(g => g.Key).First();

            Console.WriteLine("Most common Name occurs: {0} times", mostCommonFirstNames?.Key);

            foreach (KeyValuePair<string, int> commonNames in mostCommonFirstNames)
            {
                Console.WriteLine("Most Common Name is: {0} occurs  {1} times", commonNames.Key, commonNames.Value);
            }
        }


        private static void GetCurrentMonarchAndItsHouseRulingPeriod()
        {
            Monarch? monarch = monarches?.Where(x=>x.Years.Split("-").Length == 2 && ValidateForCurrentMonarch(x.Years)).Single();

            List<Monarch>? selectedMonarchs = monarches?.Where(x=>x.House.ToUpper().Equals(monarch?.House.ToUpper())).ToList();
            int rulingyears = 0;
            foreach (var monarchObj in selectedMonarchs)
            {
                rulingyears += CalcuateDuration(monarchObj.Years);
            }


            Console.WriteLine("The house of current monarch is: {0}", monarch?.House);
            Console.WriteLine("the house has rulled for {0} years including current year", rulingyears);
        }

        private static bool ValidateForCurrentMonarch(string years)
        {
            string[] yearsArray = years.Split("-");
            int.TryParse(yearsArray[0], out int startYear);
            if (yearsArray.Length == 2 && startYear != 0 && string.IsNullOrWhiteSpace(yearsArray[1]))
            {
                return true;
            }
            return false;
        }

        private static void AddToDictionary(Dictionary<string, int> dict, string key, int value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] += value;
            }
            else
            {
                dict[key] = value;
            }
        }

        private static int CalcuateDuration(string years)
        {
            string[] yearsArray = years.Split('-');
            //means no range at max will be an year
            if (yearsArray.Length != 2 && yearsArray.Length == 1)
            {
                return 1;
            }

            //parse years to numbers if not parsed means not a valid year hence return 0

            _ = int.TryParse(yearsArray[0], out int startYear);
            if (startYear == 0)
            {
                return 0;
            }

            int endYear;
            //special condition to handle the the data where rulling monart has start date but no end date
            if (string.IsNullOrWhiteSpace(yearsArray[1]))
            {
                endYear = DateTime.Now.Year;
            }
            //same check as start date
            else if (!int.TryParse(yearsArray[1], out endYear))
            {
                return 0;
            }
            return endYear - startYear;
        }

        #endregion
    }

    #region classes

    internal class Monarch
    {
        public int Id { get; set; }
        [JsonPropertyName("nm")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("cty")]
        public string City { get; set; } = string.Empty;
        [JsonPropertyName("hse")]
        public string House { get; set; } = string.Empty;
        [JsonPropertyName("yrs")]
        public string Years { get; set; } = string.Empty;
        public int Duration { get; set; }
    }


    public static class Constants
    {
        public const string dataUri = "https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048/kings";
    }
    #endregion


}
