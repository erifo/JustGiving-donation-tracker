using System;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Collections.Generic;
using JustGiving.Api.Sdk.Model.Page;

namespace JustGivingDonationTracker
{
    class Program
    {

        //Filenames
        static string file_totalRaised = "totalRaised.txt";
        static string file_latestName = "latestName.txt";
        static string file_latestAmount = "latestAmount.txt";
        static string file_topName = "topName.txt";
        static string file_topAmount = "topAmount.txt";

        static string appid = ""; //Identifies this very program for the JustGiving API.
        static string streamname;
        static int nameMaxLength = 20;

        //Function timer
        static Timer myTimer = new Timer();

        //API usage init.
        static JustGiving.Api.Sdk.JustGivingClient client = new JustGiving.Api.Sdk.JustGivingClient(appid);

        static void Main(string[] args)
        {
            Console.Title = "Just Giving Donation Tracker v4.2";
            Console.WriteLine("Dedicated to Cancer Research UK and you, because your donation will make a difference.\n");
            Console.WriteLine("Donation tracker started.");
            Console.WriteLine("Writing donation total to file:          " + file_totalRaised);
            Console.WriteLine("Writing latest donation name to file:    " + file_latestName);
            Console.WriteLine("Writing latest donation amount to file:  " + file_latestAmount);
            Console.WriteLine("Writing top donation name to file:       " + file_topName);
            Console.WriteLine("Writing top donation amount to file:     " + file_topAmount);

            setTrackerSource();
            Console.WriteLine("\n- - - - - TOTAL - - - - -");
            startTracker();

            Console.ReadLine();
        }

        static void setTrackerSource()
        {
            Console.WriteLine("\nPlease enter fundraiser ID.");
            streamname = Console.ReadLine();
        }

        static void gracefullyQuit(string msg)
        {
            myTimer.Stop();
            Console.WriteLine(msg);
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            Process.GetCurrentProcess().Kill();
        }

        static void startTracker()
        {
            myTimer.Elapsed += new ElapsedEventHandler(tProxy);
            myTimer.Interval = 5000;     
            myTimer.Enabled = true;
            trackerCycle();
        }


        static void tProxy(object source, ElapsedEventArgs e)
        {
            trackerCycle();
        }


        static void trackerCycle()
        {
            try
            {
                string totalRaised = downloadTotalRaised();
                updateTotalRaised(totalRaised);
            }
            catch (Exception)
            {
                gracefullyQuit("Could not connect to fundraiser: " + streamname);
            }

            try
            {
                List<FundraisingPageDonation> donations = downloadDonations();
                updateLatestDonator(donations);
                updateTopDonator(donations);
                //printDonations(donations);
            }
            catch (ArgumentOutOfRangeException)
            {
                gracefullyQuit("No donations found for fundraiser: " + streamname);
            }
            catch (JustGiving.Api.Sdk.Http.ResourceNotFoundException e)
            {
                gracefullyQuit("Unable to retrieve donations for fundraiser: " + streamname);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                gracefullyQuit("An unexpected error has occurred.");
            }
        }


        static string downloadTotalRaised()
        {
            var page = client.Page.Retrieve(streamname);
            return page.TotalRaised;
        }


        static List<FundraisingPageDonation> downloadDonations()
        {
            List<FundraisingPageDonation> donations = new List<FundraisingPageDonation>();
            for (int i = 1; i <= 2; i++)
            {
                var ds = client.Page.RetrieveDonationsForPage(streamname, 150, i);
                if (ds != null)
                {
                    foreach (var donation in ds.Donations)
                    {
                        donations.Add(donation);
                        
                    }
                }
            }
            return donations;
        }


        static void updateTotalRaised(string totalRaised)
        {
            double a = Convert.ToDouble(totalRaised);
            a = Math.Round(a, 3, MidpointRounding.AwayFromZero);
            string raised = '£' + Convert.ToString(a);
            raised = raised.Replace(",", ".");

            DateTime t = DateTime.Now;
            Console.WriteLine(t.ToLongTimeString()+"\t" + raised);
            File.WriteAllText(file_totalRaised, raised);
        }


        static FundraisingPageDonation getLatestDonation(List<FundraisingPageDonation> donations)
        {
            FundraisingPageDonation latest = donations[0];
            foreach (var donation in donations)
            {
                if (donation.DonationDate > latest.DonationDate)
                {
                    latest = donation;
                }
            }
            return latest;
        }


        static FundraisingPageDonation getHighestDonation(List<FundraisingPageDonation> donations)
        {
            FundraisingPageDonation top = donations[0];
            foreach (var donation in donations)
            {
                if (donation.Amount > top.Amount)
                {
                    top = donation;
                }
            }
            return top;
        }


        static void updateLatestDonator(List<FundraisingPageDonation> donations)
        {
            FundraisingPageDonation latest = getLatestDonation(donations);

            //Get and shorten name if too long.
            string name = latest.DonorDisplayName;
            if (name.Length > nameMaxLength)
            {
                name = name.Substring(0, nameMaxLength);
            }

            //Get donated amount.
            double a = (double)latest.Amount;
            a = Math.Round(a, 3, MidpointRounding.AwayFromZero);
            string amount = '£' + Convert.ToString(a);
            amount = amount.Replace(',', '.');

            //Write both to files.
            File.WriteAllText(file_latestName, name);
            File.WriteAllText(file_latestAmount, amount);
        }


        static void updateTopDonator(List<FundraisingPageDonation> donations)
        {
            FundraisingPageDonation top = getHighestDonation(donations);

            string name = top.DonorDisplayName;
            if (name.Length > nameMaxLength)
            {
                name = name.Substring(0, nameMaxLength);
            }

            double a = (double)top.Amount;
            a = Math.Round(a, 3, MidpointRounding.AwayFromZero);
            string amount = '£' + Convert.ToString(a);
            amount = amount.Replace(',', '.');

            File.WriteAllText(file_topName, name);
            File.WriteAllText(file_topAmount, amount);
        }

        static void printDonations(List<FundraisingPageDonation> donations)
        {
            foreach (var donation in donations)
            {
                Console.WriteLine("Name: " + donation.DonorDisplayName);
                Console.WriteLine("Donation: " + donation.Amount);
                Console.WriteLine("Msg: " + donation.Message);
                Console.WriteLine("Date: " + donation.DonationDate);
                Console.WriteLine("- - - - - - - - - -");
            }
            Console.WriteLine("Donations: " + donations.Count);
        }
    }
}
