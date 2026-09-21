using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Entities;
using App.Infrastructure;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace App.API.Data
{
    /// <summary>
    /// Highly realistic, professional database seeder using the Bogus library.
    /// Seeds 210+ Philippine customer profiles, inquiry leads, and operational service requests
    /// across the past 180 days to populate BI dashboards, analytics charts, and retention modules.
    /// </summary>
    public static class DatabaseSeeder
    {
        private static readonly string[] FilipinoFirstNames =
        {
            "Jasper", "Althea", "Juan", "Maria", "Jose", "Angelica", "Mark", "Princess", "Paolo",
            "Christian", "Mary Grace", "Joshua", "Bea", "Gabriel", "Rhea", "Arnel", "Kristine",
            "Emmanuel", "Jessa", "Ronaldo", "Camille", "Angelo", "Maricel", "Francis", "Patricia",
            "Danilo", "Aileen", "Rafael", "Stephanie", "Lito", "Charmaine", "Dennis", "Rowena",
            "Jerome", "Giselle", "Eduardo", "Roxanne", "Alden", "Kathryn", "Dingdong", "Marian",
            "Carlo", "Ramon", "Antonio", "Vicente", "Rodrigo", "Ferdinand", "Renato", "Ernesto"
        };

        private static readonly string[] FilipinoLastNames =
        {
            "Dela Cruz", "Santos", "Reyes", "Garcia", "Mendoza", "Torres", "Bautista", "Flores",
            "Gonzales", "Villanueva", "Castillo", "Ramos", "Aquino", "Navarro", "Mercado", "De Leon",
            "Pascual", "Salazar", "Rivera", "Soriano", "Fernandez", "Tolentino", "Manalo", "Domingo",
            "Ocampo", "Santiago", "Valdez", "Pineda", "Guzman", "Cortez", "Corpuz", "Marquez",
            "Delos Reyes", "Alcantara", "Aguilar", "Abad", "Beltran", "Enriquez", "Fajardo", "Legaspi"
        };

        private static readonly string[] CorporateCompanies =
        {
            "Apex BPO Solutions Inc.", "Golden Peak Holdings Corp.", "Sunlight Properties & Dev.",
            "Bayview Tech Hub Philippines", "Pearl Logistics & Transport", "Manila Crest Commercial Co.",
            "Prime Vista Realty Group", "Horizon BPO Global Inc.", "Pacific Coast Warehousing",
            "Metro Star Innovations Corp.", "Cebu Harbor Shipping Lines", "Davao Highlands Agritech",
            "Pinnacle Financial Services", "Blue Diamond Retail Chain", "AeroTech Logistics PH",
            "Summit Corporate Center Inc.", "Sterling Business Hub", "Grand Imperial Hotel & Suites",
            "Vanguard Security & Facilities", "Zenith Commercial Plaza", "Samal Island Leisure Resorts Corp."
        };

        private static readonly string[] LocalHubs =
        {
            "Davao City",
            "Samal",
            "Cebu City",
            "Manila",
            "Quezon City",
            "Iloilo City",
            "Baguio City"
        };

        private static readonly string[] MobilePrefixes =
        {
            "0917", "0918", "0998", "0922", "0927", "0928", "0932", "0933",
            "0939", "0945", "0956", "0966", "0977", "0981", "0995"
        };

        private static readonly string[] LeadSources =
        {
            "Facebook Ads", "Google Search", "Website Direct", "Referral", "Walk-in"
        };

        private static readonly string[] AssignedStaffList =
        {
            "Pedro Reyes", "Maria Santos", "Mark Anthony", "Sarah Jane", "Michael John", "Jessica Mae"
        };

        private static readonly string[] SpecialRequestsPool =
        {
            "Focus on master bedroom, kitchen grease traps, and balcony windows.",
            "Requires eco-friendly and pet-safe chemical detergents.",
            "Client requested crew to bring heavy-duty HEPA vacuum cleaners.",
            "Disinfection misting required for all high-touch surfaces.",
            "Strict non-smoking crew; require gate pass assistance at lobby.",
            "Thorough grout scrub and scale removal in all bathrooms.",
            "Careful dusting around fragile artwork and electronic equipment.",
            "Provide formal signed checklist upon completion for turnover."
        };

        private static readonly string[] CompletionNotes =
        {
            "Service completed on schedule. Client gave positive verbal feedback.",
            "Client signed service acknowledgment slip; highly satisfied with deep clean.",
            "High satisfaction rating; client expressed interest in quarterly recurring plan.",
            "Completed post-cleaning walkthrough with property manager.",
            "First-time customer discount voucher applied; follow-up scheduled.",
            "Cleaning crew arrived on time; completed checklist submitted to supervisor."
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            // Set fixed seed for reproducibility across re-runs
            Randomizer.Seed = new Random(1337);

            // 1. Check volume requirement: If already 200+ customers exist, return immediately
            if (await context.Customers.AnyAsync())
            {
                if (await context.Customers.CountAsync() >= 200)
                    return;

                // Clean existing incomplete test records for fresh seed
                context.ServiceRequests.RemoveRange(context.ServiceRequests);
                context.Leads.RemoveRange(context.Leads);
                context.Customers.RemoveRange(context.Customers);
                await context.SaveChangesAsync();
            }

            // 2. Ensure default service catalog exists
            if (!await context.Services.AnyAsync())
            {
                var defaultServices = new List<Service>
                {
                    new() { ServiceName = "General House Cleaning", Category = "Residential", BasePrice = 1500m, Description = "Standard routine residential cleaning" },
                    new() { ServiceName = "Deep Cleaning", Category = "Residential", BasePrice = 3200m, Description = "Intensive scrub and sanitization of entire space" },
                    new() { ServiceName = "Move-in Sanitization", Category = "Residential", BasePrice = 2800m, Description = "Detailed prep cleaning prior to move-in" },
                    new() { ServiceName = "Move-out Detailed Clean", Category = "Residential", BasePrice = 2900m, Description = "Turnover cleaning for tenancy inspection" },
                    new() { ServiceName = "Office Cleaning", Category = "Commercial", BasePrice = 4500m, Description = "Workplace workstation and common area sanitation" },
                    new() { ServiceName = "Restaurant Cleaning", Category = "Commercial", BasePrice = 5500m, Description = "Grease removal, dining hall and kitchen deep clean" },
                    new() { ServiceName = "Commercial Sanitization", Category = "Commercial", BasePrice = 4800m, Description = "Hospital-grade misting and surface disinfection" },
                    new() { ServiceName = "Window Cleaning", Category = "Specialty", BasePrice = 2200m, Description = "Interior and exterior streak-free glass washing" },
                    new() { ServiceName = "Post-Construction Cleaning", Category = "Specialty", BasePrice = 6500m, Description = "Heavy dust, paint splatter and adhesive removal" },
                    new() { ServiceName = "Carpet & Upholstery Cleaning", Category = "Specialty", BasePrice = 2600m, Description = "Hot water extraction and allergen treatment" }
                };
                await context.Services.AddRangeAsync(defaultServices);
                await context.SaveChangesAsync();
            }

            var catalogServices = await context.Services.ToListAsync();
            var now = DateTime.UtcNow;

            // 3. Customers Faker (210 items)
            // - 70% Individual, 30% Company
            // - Spread dates across last 180 days (6 months)
            // - Realistic Filipino names, phones, clean lowercase emails, and local hubs
            var customerIdCounter = 1;
            var customerFaker = new Faker<Customer>("en")
                .RuleFor(c => c.CustomerType, f => f.Random.Bool(0.30f) ? "Company" : "Individual")
                .RuleFor(c => c.CustomerName, (f, c) =>
                {
                    if (c.CustomerType == "Company")
                    {
                        return f.PickRandom(CorporateCompanies);
                    }
                    var first = f.PickRandom(FilipinoFirstNames);
                    var last = f.PickRandom(FilipinoLastNames);
                    return $"{first} {last}";
                })
                .RuleFor(c => c.ContactInfo, (f, c) =>
                {
                    var prefix = f.PickRandom(MobilePrefixes);
                    var phoneSuffix = f.Random.Number(1000000, 9999999);
                    var phone = $"{prefix}{phoneSuffix}";

                    var slug = c.CustomerName.ToLowerInvariant()
                        .Replace(" ", ".")
                        .Replace(",", "")
                        .Replace("&", "")
                        .Replace("..", ".");
                    var domain = f.PickRandom("gmail.com", "outlook.com", "yahoo.com", "cleaningcrm.ph");
                    var email = $"{slug}{customerIdCounter++}@{domain}";

                    return $"{phone} | {email}";
                })
                .RuleFor(c => c.ServiceLocation, f => f.PickRandom(LocalHubs))
                .RuleFor(c => c.IsActive, _ => true)
                .RuleFor(c => c.CreatedAt, f =>
                {
                    // Spread across the last 180 days
                    var daysAgo = f.Random.Number(1, 180);
                    return now.AddDays(-daysAgo).AddHours(f.Random.Number(8, 17)).AddMinutes(f.Random.Number(0, 59));
                });

            var customers = customerFaker.Generate(210);

            // Ensure first 25 customers were created 65-150 days ago to populate At-Risk accounts naturally
            var bogusRng = new Random(1337);
            for (int i = 0; i < 25 && i < customers.Count; i++)
            {
                customers[i].CreatedAt = now.AddDays(-bogusRng.Next(65, 150));
            }

            // 4. Leads Faker (210 items)
            var leadServiceInterests = new[]
            {
                "Deep Cleaning", "General House Cleaning", "Move-in Sanitization", "Move-out Detailed Clean"
            };

            var leads = new List<Lead>();
            var leadFaker = new Faker<Lead>("en")
                .RuleFor(l => l.LeadSource, f => f.PickRandom(LeadSources))
                .RuleFor(l => l.ServiceOfInterest, f => f.PickRandom(leadServiceInterests))
                .RuleFor(l => l.IsActive, _ => true);

            for (int i = 0; i < customers.Count; i++)
            {
                var cust = customers[i];
                var lead = leadFaker.Generate();
                lead.LeadName = cust.CustomerName;
                lead.ContactInfo = cust.ContactInfo;
                lead.InquiryDetails = $"Customer inquired about {lead.ServiceOfInterest} services in {cust.ServiceLocation}.";
                lead.CreatedAt = cust.CreatedAt;
                leads.Add(lead);
            }

            // 5. ServiceRequests Faker (210 items + repeat bookings)
            // - Link directly to customer & lead
            // - Status: 75% Completed, 25% Pending
            var serviceRequests = new List<ServiceRequest>();
            var requestFaker = new Faker<ServiceRequest>("en")
                .RuleFor(sr => sr.Status, f => f.Random.Bool(0.75f) ? "Completed" : "Pending")
                .RuleFor(sr => sr.SpecialRequests, f => f.Random.Bool(0.5f) ? f.PickRandom(SpecialRequestsPool) : null)
                .RuleFor(sr => sr.AssignedSalesStaff, f => f.PickRandom(AssignedStaffList))
                .RuleFor(sr => sr.IsActive, _ => true);

            for (int i = 0; i < customers.Count; i++)
            {
                var cust = customers[i];
                var lead = leads[i];
                var sr = requestFaker.Generate();

                // Select matching or random service from catalog
                var selectedService = catalogServices.FirstOrDefault(s => s.ServiceName == lead.ServiceOfInterest)
                                     ?? catalogServices[i % catalogServices.Count];

                sr.Customer = cust;
                sr.Lead = lead;
                sr.Service = selectedService;
                sr.ServiceId = selectedService.ServiceId;
                sr.RequestedService = selectedService.ServiceName;

                // Dates
                var bookingDate = cust.CreatedAt.AddDays(bogusRng.Next(1, 3));
                var preferredDate = bookingDate.AddDays(bogusRng.Next(1, 5));
                DateTime? followUpDate = null;

                if (sr.Status == "Completed")
                {
                    // Customers with index < 25 are At-Risk: last completed > 60 days ago, no follow up
                    if (i >= 25)
                    {
                        followUpDate = preferredDate.AddDays(bogusRng.Next(14, 45));
                    }
                    sr.Notes = CompletionNotes[i % CompletionNotes.Length];
                    sr.ActualPrice = selectedService.BasePrice + (bogusRng.Next(0, 3) * 250m);
                }
                else
                {
                    sr.Notes = "Awaiting dispatch scheduling and crew confirmation.";
                    sr.ActualPrice = selectedService.BasePrice;
                    followUpDate = preferredDate.AddDays(bogusRng.Next(2, 7));
                }

                sr.BookingDate = bookingDate;
                sr.PreferredDate = preferredDate;
                sr.FollowUpDate = followUpDate;
                sr.CreatedAt = bookingDate;

                serviceRequests.Add(sr);

                // Add repeat bookings for ~35% of accounts (index >= 35) to give strong Repeat Customer Rate
                if (i >= 35 && i % 3 == 0)
                {
                    var repeatService = catalogServices[bogusRng.Next(catalogServices.Count)];
                    var repeatBookingDate = bookingDate.AddDays(bogusRng.Next(14, 40));
                    if (repeatBookingDate < now)
                    {
                        var repeatRequest = new ServiceRequest
                        {
                            Customer = cust,
                            Lead = lead,
                            Service = repeatService,
                            ServiceId = repeatService.ServiceId,
                            RequestedService = repeatService.ServiceName,
                            BookingDate = repeatBookingDate,
                            PreferredDate = repeatBookingDate.AddDays(bogusRng.Next(1, 4)),
                            FollowUpDate = repeatBookingDate.AddDays(30),
                            SpecialRequests = "Repeat client requested standard recurring package.",
                            Notes = "Repeat service rendered successfully.",
                            AssignedSalesStaff = sr.AssignedSalesStaff,
                            Status = "Completed",
                            ActualPrice = repeatService.BasePrice,
                            IsActive = true,
                            CreatedAt = repeatBookingDate
                        };
                        serviceRequests.Add(repeatRequest);
                    }
                }
            }

            // 6. Save to Database via EF Core
            await context.Customers.AddRangeAsync(customers);
            await context.Leads.AddRangeAsync(leads);
            await context.ServiceRequests.AddRangeAsync(serviceRequests);
            await context.SaveChangesAsync();
        }
    }
}
