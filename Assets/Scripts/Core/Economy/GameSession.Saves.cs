using System;
using System.Collections.Generic;
using System.Linq;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Saves;
using CRS.Core.Selling;

namespace CRS.Core.Economy
{
    /// <summary>Saving a playthrough to plain data and rebuilding it.</summary>
    public sealed partial class GameSession
    {
        /// <summary>How many finished deals are kept in a save.</summary>
        public const int SavedHistoryLimit = 50;

        // ---------- save ----------

        /// <summary>A snapshot of everything needed to continue the game later.</summary>
        public SaveData ToSave()
        {
            var data = new SaveData
            {
                seed = seed,
                day = Day,
                money = Player.Money,
                garageCapacity = Player.GarageCapacity,
                nextListingId = Market.NextListingId,
                nextBuyerId = buyers.NextId,
                reputation = Reputation,
                nextClaimId = nextClaimId,
            };

            foreach (var claim in claims)
            {
                data.claims.Add(new ClaimData
                {
                    id = claim.Id,
                    car = ToData(claim.Car),
                    amount = claim.Amount,
                    dueDay = claim.DueDay,
                    odometerFraud = claim.OdometerFraud,
                    defects = claim.Defects,
                });
            }

            foreach (var owned in Player.Garage)
            {
                data.garage.Add(new OwnedCarData
                {
                    listingId = owned.ListingId,
                    car = ToData(owned.Car),
                    purchasePrice = owned.PurchasePrice,
                    invested = owned.Invested,
                    historyChecked = owned.Report.HistoryChecked,
                    diagnosticsDone = owned.Report.DiagnosticsDone,
                    knownDefectComponents = owned.ComponentsWithKnownDefects.OrderBy(c => c).ToList(),
                });
            }

            foreach (var listing in Market.Listings)
            {
                var item = new ListingData
                {
                    id = listing.Id,
                    car = ToData(listing.Car),
                    seller = listing.Seller,
                    askingPrice = listing.AskingPrice,
                    descriptionKeys = listing.DescriptionKeys.ToList(),
                };
                if (reports.TryGetValue(listing.Id, out var report))
                {
                    item.historyChecked = report.HistoryChecked;
                    item.diagnosticsDone = report.DiagnosticsDone;
                }
                if (negotiations.TryGetValue(listing.Id, out var deal))
                {
                    item.hasNegotiation = true;
                    item.negotiation = new NegotiationData
                    {
                        currentPrice = deal.CurrentPrice,
                        patienceLeft = deal.PatienceLeft,
                        hasDeal = deal.HasDeal,
                        sellerLeft = deal.SellerLeft,
                        stubbornness = deal.Stubbornness,
                        baseReservation = deal.BaseReservation,
                        turns = deal.Turns.Select(t => new NegotiationTurnData
                        {
                            offer = t.Offer, reaction = t.Reaction, sellerPrice = t.SellerPrice, phraseKey = t.PhraseKey,
                        }).ToList(),
                    };
                }
                data.market.Add(item);
            }

            foreach (var sale in sales)
            {
                int index = IndexInGarage(sale.Owned);
                if (index < 0) continue;
                data.sales.Add(new SaleData
                {
                    garageIndex = index,
                    askingPrice = sale.AskingPrice,
                    dayListed = sale.DayListed,
                    realMileage = sale.RealMileage,
                    offers = sale.Offers.Select(o => new BuyerOfferData
                    {
                        id = o.Id, type = o.Type, openingPrice = o.OpeningPrice, price = o.Price, maxPrice = o.MaxPrice,
                        patienceLeft = o.PatienceLeft, stubbornness = o.Stubbornness, foundProblems = o.FoundProblems,
                        phraseKey = o.PhraseKey,
                        turns = o.Turns.Select(t => new BuyerTurnData
                        {
                            demand = t.Demand, reaction = t.Reaction, price = t.Price, phraseKey = t.PhraseKey,
                        }).ToList(),
                    }).ToList(),
                });
            }

            foreach (var record in history.Skip(Math.Max(0, history.Count - SavedHistoryLimit)))
            {
                data.history.Add(new SaleRecordData
                {
                    car = ToData(record.Car),
                    day = record.Day,
                    purchasePrice = record.PurchasePrice,
                    invested = record.Invested,
                    salePrice = record.SalePrice,
                    buyer = record.Buyer,
                    hadHiddenProblems = record.HadHiddenProblems,
                });
            }

            return data;
        }

        private int IndexInGarage(OwnedCar owned)
        {
            for (int i = 0; i < Player.Garage.Count; i++)
                if (ReferenceEquals(Player.Garage[i], owned)) return i;
            return -1;
        }

        private static CarData ToData(Car car)
        {
            return new CarData
            {
                brandId = car.Brand.Id,
                modelId = car.Model.Id,
                year = car.Year,
                displayedMileage = car.DisplayedMileage,
                realMileage = car.RealMileage,
                wasTaxi = car.WasTaxi,
                condition = Car.Components.Select(car.VisibleCondition).ToList(),
                defects = car.HiddenDefects.Select(d => new DefectData
                {
                    component = d.Component, severity = d.Severity, repairCost = d.RepairCost,
                }).ToList(),
                washed = car.IsWashed,
                polished = car.IsPolished,
            };
        }

        // ---------- load ----------

        /// <summary>
        /// Rebuilds a playthrough from a save. Throws <see cref="FormatException"/> when the save
        /// is from another version or is damaged: the caller then starts a new game.
        /// </summary>
        public static GameSession FromSave(SaveData data)
        {
            if (data == null) throw new FormatException("No save data");
            if (data.version != SaveData.CurrentVersion)
                throw new FormatException("Unsupported save version: " + data.version);

            try
            {
                return Build(data);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new FormatException("Damaged save: " + e.Message, e);
            }
        }

        private static GameSession Build(SaveData data)
        {
            Require(data.day >= 1, "day");
            Require(data.money >= 0, "money");
            Require(data.garageCapacity >= 0, "garage capacity");
            Require(data.garage != null && data.market != null && data.sales != null && data.history != null
                    && data.claims != null, "lists");
            Require(data.reputation >= Selling.Reputation.Min && data.reputation <= Selling.Reputation.Max, "reputation");
            Require(data.garage.Count <= data.garageCapacity, "garage size");

            var player = new PlayerState(data.money, data.garageCapacity);
            foreach (var item in data.garage)
            {
                var report = new InspectionReport();
                if (item.historyChecked) report.MarkHistoryChecked();
                if (item.diagnosticsDone) report.MarkDiagnosticsDone();
                var owned = new OwnedCar(item.listingId, ToCar(item.car), item.purchasePrice, report);
                owned.AddInvestment(item.invested);
                foreach (var component in item.knownDefectComponents ?? new List<CarComponent>())
                    owned.MarkDefectsKnown(Defined(component));
                player.AddCar(owned);
            }

            var listings = new List<Listing>();
            foreach (var item in data.market)
            {
                Require(item.askingPrice > 0, "asking price");
                listings.Add(new Listing(item.id, ToCar(item.car), Defined(item.seller), item.askingPrice,
                    item.descriptionKeys ?? new List<string>()));
            }
            Require(listings.Select(l => l.Id).Distinct().Count() == listings.Count, "listing ids");

            // A different seed after every load: reloading the page must not replay the same board.
            int marketSeed = unchecked(data.seed * 31 + data.day * 7919 + data.nextListingId);
            var market = Marketplace.Restore(marketSeed, GameBalance.ListingsPerRefresh, GameBalance.MinAffordableListings,
                listings, Math.Max(data.nextListingId, listings.Count == 0 ? 1 : listings.Max(l => l.Id) + 1));

            var session = new GameSession(data.seed, data.day, player, market, data.nextBuyerId);
            session.Reputation = data.reputation;
            foreach (var item in data.claims)
                session.claims.Add(new Claim(item.id, ToCar(item.car), item.amount, item.dueDay, item.odometerFraud, item.defects));
            session.nextClaimId = Math.Max(data.nextClaimId,
                data.claims.Count == 0 ? 1 : data.claims.Max(c => c.id) + 1);

            for (int i = 0; i < listings.Count; i++)
            {
                var item = data.market[i];
                var listing = listings[i];
                if (item.historyChecked || item.diagnosticsDone)
                {
                    var report = session.ReportFor(listing);
                    if (item.historyChecked) report.MarkHistoryChecked();
                    if (item.diagnosticsDone) report.MarkDiagnosticsDone();
                }
                if (item.hasNegotiation && item.negotiation != null)
                {
                    var n = item.negotiation;
                    Require(n.currentPrice > 0, "negotiation price");
                    var turns = (n.turns ?? new List<NegotiationTurnData>())
                        .Select(t => new NegotiationTurn(t.offer, Defined(t.reaction), t.sellerPrice, t.phraseKey));
                    session.negotiations[listing.Id] = new Negotiation(listing,
                        unchecked(data.seed * 397 ^ listing.Id * 7919), n.currentPrice, n.patienceLeft,
                        n.hasDeal, n.sellerLeft, n.stubbornness, n.baseReservation, turns);
                }
            }

            foreach (var item in data.sales)
            {
                Require(item.garageIndex >= 0 && item.garageIndex < player.Garage.Count, "sale car");
                var owned = player.Garage[item.garageIndex];
                Require(session.SaleFor(owned) == null, "car listed twice");
                var sale = new SaleListing(owned, item.askingPrice, item.dayListed, item.realMileage);
                foreach (var o in item.offers ?? new List<BuyerOfferData>())
                {
                    var turns = (o.turns ?? new List<BuyerTurnData>())
                        .Select(t => new BuyerTurn(t.demand, Defined(t.reaction), t.price, t.phraseKey));
                    sale.AddOffer(BuyerOffer.Restore(o.id, Defined(o.type), o.openingPrice, o.maxPrice, o.stubbornness,
                        o.foundProblems, o.phraseKey, o.price, o.patienceLeft, turns));
                }
                session.sales.Add(sale);
            }

            foreach (var item in data.history)
                session.history.Add(new SaleRecord(ToCar(item.car), item.day, item.purchasePrice, item.invested,
                    item.salePrice, Defined(item.buyer), item.hadHiddenProblems));

            return session;
        }

        private static Car ToCar(CarData data)
        {
            Require(data != null, "car");
            var brand = BrandCatalog.Brand(data.brandId);
            var model = brand.Models.FirstOrDefault(m => m.Id == data.modelId);
            Require(model != null, "model " + data.modelId);
            Require(data.condition != null && data.condition.Count == Car.Components.Length, "condition");

            var condition = new Dictionary<CarComponent, int>();
            for (int i = 0; i < Car.Components.Length; i++)
                condition[Car.Components[i]] = Math.Max(0, Math.Min(100, data.condition[i]));

            var defects = (data.defects ?? new List<DefectData>())
                .Select(d => new HiddenDefect(Defined(d.component), d.severity, d.repairCost)).ToList();

            var car = new Car(brand, model, data.year, data.displayedMileage, data.realMileage, data.wasTaxi,
                condition, defects);
            if (data.washed) car.Wash();
            if (data.polished) car.Polish();
            return car;
        }

        private static T Defined<T>(T value) where T : struct
        {
            Require(Enum.IsDefined(typeof(T), value), typeof(T).Name + " " + value);
            return value;
        }

        private static void Require(bool condition, string what)
        {
            if (!condition) throw new FormatException("Damaged save: " + what);
        }
    }
}
