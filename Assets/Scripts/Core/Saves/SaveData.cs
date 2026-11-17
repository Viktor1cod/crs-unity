using System;
using System.Collections.Generic;
using CRS.Core.Cars;
using CRS.Core.Deals;
using CRS.Core.Selling;

namespace CRS.Core.Saves
{
    // Plain data for a saved game. Only public fields, lists and simple types,
    // so any JSON serializer (Unity's JsonUtility included) can write and read it.
    // When the format changes, raise SaveData.CurrentVersion: older saves are then
    // rejected by GameSession.FromSave and the player starts a new game.

    [Serializable]
    public sealed class SaveData
    {
        /// <summary>2: reputation and buyer claims.</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int seed;
        public int day = 1;
        public long money;
        public int garageCapacity;
        public int nextListingId = 1;
        public int nextBuyerId = 1;
        public int reputation = Reputation.Start;
        public int nextClaimId = 1;
        public List<ClaimData> claims = new List<ClaimData>();
        public List<OwnedCarData> garage = new List<OwnedCarData>();
        public List<ListingData> market = new List<ListingData>();
        public List<SaleData> sales = new List<SaleData>();
        public List<SaleRecordData> history = new List<SaleRecordData>();
    }

    [Serializable]
    public sealed class CarData
    {
        public string brandId;
        public string modelId;
        public int year;
        public int displayedMileage;
        public int realMileage;
        public bool wasTaxi;
        /// <summary>Visible condition per component, in the order of the CarComponent enum.</summary>
        public List<int> condition = new List<int>();
        public List<DefectData> defects = new List<DefectData>();
        public bool washed;
        public bool polished;
    }

    [Serializable]
    public sealed class DefectData
    {
        public CarComponent component;
        public int severity;
        public long repairCost;
    }

    [Serializable]
    public sealed class OwnedCarData
    {
        public int listingId;
        public CarData car = new CarData();
        public long purchasePrice;
        public long invested;
        public bool historyChecked;
        public bool diagnosticsDone;
        public List<CarComponent> knownDefectComponents = new List<CarComponent>();
    }

    [Serializable]
    public sealed class ListingData
    {
        public int id;
        public CarData car = new CarData();
        public SellerType seller;
        public long askingPrice;
        public List<string> descriptionKeys = new List<string>();
        public bool historyChecked;
        public bool diagnosticsDone;
        /// <summary>False when the player has not haggled over this listing yet.</summary>
        public bool hasNegotiation;
        public NegotiationData negotiation = new NegotiationData();
    }

    [Serializable]
    public sealed class NegotiationData
    {
        public long currentPrice;
        public int patienceLeft;
        public bool hasDeal;
        public bool sellerLeft;
        public double stubbornness;
        public long baseReservation;
        public List<NegotiationTurnData> turns = new List<NegotiationTurnData>();
    }

    [Serializable]
    public sealed class NegotiationTurnData
    {
        public long offer;
        public SellerReaction reaction;
        public long sellerPrice;
        public string phraseKey;
    }

    [Serializable]
    public sealed class SaleData
    {
        /// <summary>Index of the car in SaveData.garage.</summary>
        public int garageIndex;
        public long askingPrice;
        public int dayListed;
        public bool realMileage;
        public List<BuyerOfferData> offers = new List<BuyerOfferData>();
    }

    [Serializable]
    public sealed class BuyerOfferData
    {
        public int id;
        public BuyerType type;
        public long openingPrice;
        public long price;
        public long maxPrice;
        public int patienceLeft;
        public double stubbornness;
        public bool foundProblems;
        public string phraseKey;
        public List<BuyerTurnData> turns = new List<BuyerTurnData>();
    }

    [Serializable]
    public sealed class BuyerTurnData
    {
        public long demand;
        public BuyerReaction reaction;
        public long price;
        public string phraseKey;
    }

    [Serializable]
    public sealed class ClaimData
    {
        public int id;
        public CarData car = new CarData();
        public long amount;
        public int dueDay;
        public bool odometerFraud;
        public int defects;
    }

    [Serializable]
    public sealed class SaleRecordData
    {
        public CarData car = new CarData();
        public int day;
        public long purchasePrice;
        public long invested;
        public long salePrice;
        public BuyerType buyer;
        public bool hadHiddenProblems;
    }
}
