using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;

namespace StripeExample
{
    public enum Subscriptions
    {
        plan_spotifyBasic = 1, plan_spotifyRegular, plan_spotifyPremium
    }

    class Program
    {
        private const string STRIPESECRETKEY = "Stripe:SecretKey";
        private const string FILEPATH = "StripeCustomers.txt";

        static void Main(string[] args)
        {
            StripeConfiguration.SetApiKey(ConfigurationManager.AppSettings["Stripe:SecretKey"]);
            ClientSubscriptionId customerInfo = new ClientSubscriptionId();

            if (!File.Exists(FILEPATH))
            {
                var clientSubscription = GenerateClientAndSubscription();
                PersistCustomer(clientSubscription);
            }

            customerInfo = ReadCustomersFile();
            var input = "";

            Console.Clear();
            do
            {
                var subs = GetClientSubscription(customerInfo.SubscriptionId);
                Console.WriteLine($"You're subscribed to '{ subs.Items.Data.First().Plan.Nickname }'");
                Console.WriteLine("Spotify account");
                Console.WriteLine("1.- Subscribe to Basic");
                Console.WriteLine("2.- Subscribe to Regular");
                Console.WriteLine("3.- Subscribe to Premium");
                var option = Console.ReadLine();
                Console.Clear();

                if (ChangeSubscription(subs, option))
                {
                    Console.WriteLine("Your info was changed successfully.");
                    continue;
                }
                Console.WriteLine("Try next time");

            } while (input.Trim() != "close");
        }

        private static ClientSubscription GenerateClientAndSubscription()
        {
            var customerService = new StripeCustomerService();
            
            Console.WriteLine("Creating customer");
            var customerOptions = new StripeCustomerCreateOptions()
            {
                SourceToken = "tok_visa_debit",
                Description = "Test getting things done."
            };

            StripeCustomer customer = customerService.Create(customerOptions);

            Console.WriteLine("Customer created successfully");

            var subscriptionOptions = new StripeSubscriptionCreateOptions()
            {
                Items = new List<StripeSubscriptionItemOption>()
                {
                    new StripeSubscriptionItemOption()
                    {
                        PlanId = Subscriptions.plan_spotifyBasic.ToString(),
                        Quantity = 1
                    }
                }
            };

            var subscriptionService = new StripeSubscriptionService();
            StripeSubscription subscription = subscriptionService.Create(customer.Id, subscriptionOptions);

            return new ClientSubscription()
            {
                Customer = customer,
                Subscription = subscription
            };
        }

        private static StripeSubscription GetClientSubscription(string subscriptionId)
        {
            var subscriptionService = new StripeSubscriptionService();
            return subscriptionService.Get(subscriptionId);
        }

        private static bool ChangeSubscription(StripeSubscription subscription, string option)
        {
            try
            {
                var subscriptionService = new StripeSubscriptionService();

                var subscriptionToChange = (Subscriptions)(int.Parse(option));
                var actualSubscription = (Subscriptions)Enum.Parse(typeof(Subscriptions), subscription.Items.Data.First().Plan.Id);

                var isUpgrade = subscriptionToChange > actualSubscription ? true : false;

                var updateSubscription = new StripeSubscriptionUpdateOptions()
                {
                    Items = new List<StripeSubscriptionItemUpdateOption>()
                    {
                        new StripeSubscriptionItemUpdateOption()
                        {
                            Id = subscription.Items.Data.First().Id,
                            PlanId = subscriptionToChange.ToString()
                        }
                    },
                    Prorate = isUpgrade
                };

                StripeSubscription updatedSubscription = subscriptionService.Update(subscription.Id, updateSubscription);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error ocurred while updating your subscription.\nError: { ex.Message }");
                return false;
            }
        }

        private static void PersistCustomer(ClientSubscription subscription)
        {
            File.WriteAllText(FILEPATH, String.Empty);
            File.WriteAllLines(FILEPATH, new string[]
            {
                subscription.Customer.Id,
                subscription.Subscription.Id
            });
        }

        private static ClientSubscriptionId ReadCustomersFile()
        {
            TextReader tr = new StreamReader(FILEPATH);

            string customerId = tr.ReadLine();
            string subscriptionId = tr.ReadLine();

            tr.Close();

            return new ClientSubscriptionId
            {
                CustomerId = customerId,
                SubscriptionId = subscriptionId
            };
        }
    }

    public class ClientSubscription
    {
        public StripeCustomer Customer { get; set; }
        public StripeSubscription Subscription { get; set; }
    }

    public class ClientSubscriptionId
    {
        public string CustomerId { get; set; }
        public string SubscriptionId { get; set; }
    }
}
