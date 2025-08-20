using System.Collections.Generic;
using System.Linq;

namespace Ibkr.Models;

public enum QuestionType
{
    PricePercentageConstraint,
    MissingMarketData,
    TickSizeLimit,
    OrderSizeLimit,
    TriggerAndFill,
    OrderValueLimit,
    MixedAllocation,
    CrossSideOrder,
    FractionTrading,
    CalledBond,
    SizeModificationLimit,
    MarketOrderRisk,
    StopOrderActivationRisk,
    MandatoryCapPrice,
    CashQuantity,
    CashQuantityOrder,
    CryptoMarketOrderRisk,
    StopOrderRisks,
    OslCryptoOrderWarning,
    OptionExerciseAtMoney,
    OmnibusAccountWarning,
    RapidEntryWindow,
    IlliquidSecurityRisk,
    MultipleAccounts,
    DisruptiveOrders,
    ClosePosition
}

public static class QuestionConstants
{
    public static readonly Dictionary<QuestionType, string> QuestionTexts = new()
    {
        [QuestionType.PricePercentageConstraint] = "price exceeds the Percentage constraint of 3%",
        [QuestionType.MissingMarketData] = "You are submitting an order without market data. We strongly recommend against this as it may result in erroneous and unexpected trades.",
        [QuestionType.TickSizeLimit] = "exceeds the Tick Size Limit of",
        [QuestionType.OrderSizeLimit] = "size exceeds the Size Limit of",
        [QuestionType.TriggerAndFill] = "This order will most likely trigger and fill immediately.",
        [QuestionType.OrderValueLimit] = "exceeds the Total Value Limit of",
        [QuestionType.MixedAllocation] = "Mixed allocation order warning",
        [QuestionType.CrossSideOrder] = "Cross side order warning",
        [QuestionType.FractionTrading] = "instrument does not support trading in fractions",
        [QuestionType.CalledBond] = "Called Bond warning",
        [QuestionType.SizeModificationLimit] = "size modification exceeds the size modification limit",
        [QuestionType.MarketOrderRisk] = "risks with Market Orders",
        [QuestionType.StopOrderActivationRisk] = "risks associated with stop orders once they become active",
        [QuestionType.MandatoryCapPrice] = "To avoid trading at a price that is not consistent with a fair and orderly market",
        [QuestionType.CashQuantity] = "Traders are responsible for understanding cash quantity details, which are provided on a best efforts basis only.",
        [QuestionType.CashQuantityOrder] = "Orders that express size using a monetary value (cash quantity) are provided on a non-guaranteed basis.",
        [QuestionType.CryptoMarketOrderRisk] = "market orders for Crypto",
        [QuestionType.StopOrderRisks] = "You are about to submit a stop order. Please be aware of the various stop order types available and the risks associated with each one.",
        [QuestionType.OslCryptoOrderWarning] = "OSL Digital Securities LTD Crypto Order Warning",
        [QuestionType.OptionExerciseAtMoney] = "Option Exercise at the Money warning",
        [QuestionType.OmnibusAccountWarning] = "order will be placed into current omnibus account",
        [QuestionType.RapidEntryWindow] = "Rapid Entry window",
        [QuestionType.IlliquidSecurityRisk] = "security has limited liquidity",
        [QuestionType.MultipleAccounts] = "This order will be distributed over multiple accounts. We strongly suggest you familiarize yourself with our allocation facilities before submitting orders.",
        [QuestionType.DisruptiveOrders] = "If your order is not immediately executable, our systems may, depending on market conditions, reject your order",
        [QuestionType.ClosePosition] = "Would you like to cancel all open orders and then place new closing order?"
    };

    public static readonly Dictionary<string, (QuestionType Type, string Text)> MessageIdToQuestion = new()
    {
        ["o163"] = (QuestionType.PricePercentageConstraint, "The following order exceeds the price percentage limit"),
        ["o354"] = (QuestionType.MissingMarketData, "You are submitting an order without market data. We strongly recommend against this as it may result in erroneous and unexpected trades. Are you sure you want to submit this order?"),
        ["o382"] = (QuestionType.TickSizeLimit, "The following value exceeds the tick size limit"),
        ["o383"] = (QuestionType.OrderSizeLimit, "The following order \"BUY 650 AAPL NASDAQ.NMS\" size exceeds the Size Limit of 500.\nAre you sure you want to submit this order?"),
        ["o403"] = (QuestionType.TriggerAndFill, "This order will most likely trigger and fill immediately.\nAre you sure you want to submit this order?"),
        ["o451"] = (QuestionType.OrderValueLimit, "The following order \"BUY 650 AAPL NASDAQ.NMS\" value estimate of 124,995.00 USD exceeds \nthe Total Value Limit of 100,000 USD.\nAre you sure you want to submit this order?"),
        ["o2136"] = (QuestionType.MixedAllocation, "Mixed allocation order warning"),
        ["o2137"] = (QuestionType.CrossSideOrder, "Cross side order warning"),
        ["o2165"] = (QuestionType.FractionTrading, "Warns that instrument does not support trading in fractions outside regular trading hours"),
        ["o10082"] = (QuestionType.CalledBond, "Called Bond warning"),
        ["o10138"] = (QuestionType.SizeModificationLimit, "The following order size modification exceeds the size modification limit."),
        ["o10151"] = (QuestionType.MarketOrderRisk, "Warns about risks with Market Orders"),
        ["o10152"] = (QuestionType.StopOrderActivationRisk, "Warns about risks associated with stop orders once they become active"),
        ["o10153"] = (QuestionType.MandatoryCapPrice, "<h4>Confirm Mandatory Cap Price</h4>To avoid trading at a price that is not consistent with a fair and orderly market, IB may set a cap (for a buy order) or sell order). THIS MAY CAUSE AN ORDER THAT WOULD OTHERWISE BE MARKETABLE TO NOT BE TRADED."),
        ["o10164"] = (QuestionType.CashQuantity, "Traders are responsible for understanding cash quantity details, which are provided on a best efforts basis only."),
        ["o10223"] = (QuestionType.CashQuantityOrder, "<h4>Cash Quantity Order Confirmation</h4>Orders that express size using a monetary value (cash quantity) are provided on a non-guaranteed basis. The system simulates the order by cancelling it once the specified amount is spent (for buy orders) or collected (for sell orders). In addition to the monetary value, the order uses a maximum size that is calculated using the Cash Quantity Estimate Factor, which you can modify in Presets."),
        ["o10288"] = (QuestionType.CryptoMarketOrderRisk, "Warns about risks associated with market orders for Crypto"),
        ["o10331"] = (QuestionType.StopOrderRisks, "You are about to submit a stop order. Please be aware of the various stop order types available and the risks associated with each one.\nAre you sure you want to submit this order?"),
        ["o10332"] = (QuestionType.OslCryptoOrderWarning, "OSL Digital Securities LTD Crypto Order Warning"),
        ["o10333"] = (QuestionType.OptionExerciseAtMoney, "Option Exercise at the Money warning"),
        ["o10334"] = (QuestionType.OmnibusAccountWarning, "Warns that order will be placed into current omnibus account instead of currently selected global account."),
        ["o10335"] = (QuestionType.RapidEntryWindow, "Serves internal Rapid Entry window."),
        ["o10336"] = (QuestionType.IlliquidSecurityRisk, "This security has limited liquidity. If you choose to trade this security, there is a heightened risk that you may not be able to close your position at the time you wish, at a price you wish, and/or without incurring a loss. Confirm that you understand the risks of trading illiquid securities.\nAre you sure you want to submit this order?"),
        ["p6"] = (QuestionType.MultipleAccounts, "This order will be distributed over multiple accounts. We strongly suggest you familiarize yourself with our allocation facilities before submitting orders."),
        ["p12"] = (QuestionType.DisruptiveOrders, "If your order is not immediately executable, our systems may, depending on market conditions, reject your order if its limit price is more than the allowed amount away from the reference price at that time. If this happens, you will not receive a fill. This is a control designed to ensure that we comply with our regulatory obligations to avoid submitting disruptive orders to the marketplace.\nUse the Price Management Algo?")
    };

    private static readonly Dictionary<QuestionType, string> QuestionTypeToMessageId = MessageIdToQuestion
        .Where(kv => kv.Value.Type != default)
        .ToDictionary(kv => kv.Value.Type, kv => kv.Key);

    public static string GetMessageId(QuestionType questionType)
    {
        if (!QuestionTypeToMessageId.TryGetValue(questionType, out var id))
            throw new KeyNotFoundException($"QuestionType {questionType} is not currently dynamically mapped to a message id. Please look the ID up manually.");
        return id;
    }
}
