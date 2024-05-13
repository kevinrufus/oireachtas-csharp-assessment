using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;


namespace OireachtasAPI
{
    public class Program
    {
        public static string LEGISLATION_DATASET = "legislation.json";
        public static string MEMBERS_DATASET = "members.json";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please provide a filter message as a command-line argument.");
                return;
            }

            string filterMessage = args[0];
            string[] filterArgs = new string[args.Length - 1];
            Array.Copy(args, 1, filterArgs, 0, args.Length - 1);

            // Call the appropriate filter method based on the filter message
            switch (filterMessage.ToLower())
            {
                case "filterbylastupdated":
                    if (filterArgs.Length < 2)
                    {
                        Console.WriteLine("Please provide 'since' and 'until' dates for the filterByLastUpdated method.");
                        return;
                    }

                    DateTime since, until;
                    if (!DateTime.TryParse(filterArgs[0], out since) || !DateTime.TryParse(filterArgs[1], out until))
                    {
                        Console.WriteLine("Invalid date format. Dates should be in yyyy-MM-dd format.");
                        return;
                    }

                    // Call filterBillsByLastUpdated method with the provided dates
                    filterBillsByLastUpdated(since, until);
                    break;

                case "filterbills":
                    // Call filterBillsSponsoredBy method with any provided arguments
                    if (filterArgs.Length > 0)
                    {
                        string pId = filterArgs[0]; // Assuming the first argument is the pId
                        filterBillsSponsoredBy(pId);
                    }
                    else
                    {
                        Console.WriteLine("Please provide a pId argument.");
                    }
                    break;

                // Add more cases for other filter methods if needed

                default:
                    Console.WriteLine("Invalid filter message. Available options: filterByLastUpdated, filterBills, etc.");
                    break;
            }

            // Wait for user input before closing the console window
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static Func<string, dynamic> load = jfname =>
        {
        
            if (Uri.TryCreate(jfname, UriKind.Absolute, out Uri uriResult))
            {
                using (var client = new WebClient())
                {
                    string json = client.DownloadString(jfname);
                    return JsonConvert.DeserializeObject(json);
                }
            }
            else
            {
                return JsonConvert.DeserializeObject((new System.IO.StreamReader(jfname)).ReadToEnd());
            }
        };

        /// <summary>
        /// Return bills sponsored by the member with the specified pId
        /// </summary>
        /// <param name="pId">The pId value for the member</param>
        /// <returns>List of bill records</returns>
        public static List<dynamic> filterBillsSponsoredBy(string pId)
        {
            // Load data from the files or URLs
            dynamic leg = load(LEGISLATION_DATASET);
            dynamic mem = load(MEMBERS_DATASET);

            // Create a dictionary to map member's full name to their PID
            Dictionary<string, string> memberNameToId = new Dictionary<string, string>();
            foreach (dynamic member in mem["results"])
            {
                string fullName = (string)member["member"]["fullName"];
                string memberId = (string)member["member"]["pId"];
                memberNameToId[fullName] = memberId;
            }

            // Create a list to store bills sponsored by the given member
            List<dynamic> ret = new List<dynamic>();

            // Iterate over each bill in the legislation dataset
            foreach (dynamic res in leg["results"])
            {
                // Check if the current bill has sponsors
                if (res["bill"]["sponsors"] != null)
                {
                    // Iterate over each sponsor in the sponsors list of the current bill
                    foreach (dynamic sponsor in res["bill"]["sponsors"])
                    {
                        // Get the sponsor's full name
                        string sponsorFullName = (string)sponsor["sponsor"]["by"]["showAs"];

                        // Check if the sponsor's full name is present in the dictionary
                        if (sponsorFullName != null)
                        {
                            if (memberNameToId.ContainsKey(sponsorFullName))
                            {
                                // Get the corresponding PID from the dictionary
                                string sponsorId = memberNameToId[sponsorFullName];

                                // Check if the sponsor's PID matches the given member's PID
                                if (sponsorId == pId)
                                {
                                    ret.Add(res["bill"]); // Add the bill to the result list
                                    break; // No need to continue checking other sponsors for the same bill
                                }
                            }
                        }

                    }
                }
            }

            return ret; // Return the list of bills sponsored by the given member

        }


        /// <summary>
        /// Return bills updated within the specified date range
        /// </summary>
        /// <param name="since">The lastUpdated value for the bill should be greater than or equal to this date</param>
        /// <param name="until">The lastUpdated value for the bill should be less than or equal to this date.If unspecified, until will default to today's date</param>
        /// <returns>List of bill records</returns>
        public static List<dynamic> filterBillsByLastUpdated(DateTime since, DateTime until)
        {
            // Load data from the files or URLs
            dynamic leg = load(LEGISLATION_DATASET);

            // If until is unspecified, default it to today's date
            if (until == default(DateTime))
            {
                until = DateTime.Today;
            }

            // Create a list to store bills updated within the specified date range
            List<dynamic> ret = new List<dynamic>();

            // Iterate over each bill in the legislation dataset
            foreach (dynamic res in leg["results"])
            {
                // Parse the lastUpdated date of the bill
                DateTime lastUpdated = DateTime.Parse((string)res["bill"]["lastUpdated"]);

                // Check if the lastUpdated date falls within the specified date range
                if (lastUpdated >= since && lastUpdated <= until)
                {
                    ret.Add(res["bill"]); // Add the bill to the result list
                }
            }

            return ret; // Return the list of bills updated within the specified date range
        }
    }
}
