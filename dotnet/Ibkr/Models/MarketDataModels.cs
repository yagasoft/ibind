using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ibkr.Models
{
    /// <summary>
    /// Request for live market data snapshot. Conids must be IBKR contract IDs as strings.
    /// Fields are selected from <see cref="MarketDataField"/> and will be serialised as string IDs (e.g., "31").
    /// </summary>
    public sealed class LiveMarketdataSnapshotRequest
    {
        public List<string> Conids { get; set; } = new();

        [JsonConverter(typeof(MarketDataFieldListAsStringsConverter))]
        public List<MarketDataField> Fields { get; set; } = new();
    }

    /// <summary>
    /// Response wrapper carrying one snapshot per requested contract.
    /// </summary>
    public sealed class LiveMarketdataSnapshotResponse
    {
        public List<MarketDataSnapshot> Snapshots { get; set; } = new();
    }

    /// <summary>
    /// A single instrument snapshot. IBKR returns the selected field IDs as top-level JSON properties.
    /// Unrequested fields may be missing (null here).
    /// </summary>
    public sealed class MarketDataSnapshot
    {
        /// <summary>Contract identifier from IBKR’s database.</summary>
        [JsonPropertyName("conid")] public long Conid { get; set; }

        /// <summary>Server identifier for the snapshot delivery session.</summary>
        [JsonPropertyName("server_id")] public string? ServerId { get; set; }

        /// <summary>Unix epoch (ms) when this record was last updated.</summary>
        [JsonPropertyName("_updated")] public long? Updated { get; set; }

        /// <summary>
        /// Raw Volume (deprecated) – Volume for the day, provided in long form without K/M formatting.
        /// For high precision volume prefer field 7762.
        /// </summary>
        [JsonPropertyName("87_raw")] public string? VolumeRaw { get; set; }

        // -------- Typed market data fields (mapped by numeric ID) --------

        /// <summary>
        /// Last Price – The last price at which the contract traded.
        /// May contain prefixes: C (previous close), H (trading halted).
        /// </summary>
        [JsonPropertyName("31")] public string? LastPrice { get; set; }

        /// <summary>Symbol.</summary>
        [JsonPropertyName("55")] public string? Symbol { get; set; }

        /// <summary>Text.</summary>
        [JsonPropertyName("58")] public string? Text { get; set; }

        /// <summary>High – Current day high price.</summary>
        [JsonPropertyName("70")] public string? High { get; set; }

        /// <summary>Low – Current day low price.</summary>
        [JsonPropertyName("71")] public string? Low { get; set; }

        /// <summary>
        /// Market Value – Current market value of your position in the security.
        /// Calculated with real-time market data (even without subscription).
        /// </summary>
        [JsonPropertyName("73")] public string? MarketValue { get; set; }

        /// <summary>Avg Price – The average price of the position.</summary>
        [JsonPropertyName("74")] public string? AvgPrice { get; set; }

        /// <summary>
        /// Unrealized PnL – Calculated with real-time market data (even without subscription).
        /// </summary>
        [JsonPropertyName("75")] public string? UnrealizedPnl { get; set; }

        /// <summary>Formatted position.</summary>
        [JsonPropertyName("76")] public string? FormattedPosition { get; set; }

        /// <summary>Formatted Unrealized PnL.</summary>
        [JsonPropertyName("77")] public string? FormattedUnrealizedPnl { get; set; }

        /// <summary>
        /// Daily PnL – P/L of the day since prior close (uses real-time data).
        /// </summary>
        [JsonPropertyName("78")] public string? DailyPnl { get; set; }

        /// <summary>
        /// Realized PnL – Realised profit or loss (uses real-time data).
        /// </summary>
        [JsonPropertyName("79")] public string? RealizedPnl { get; set; }

        /// <summary>Unrealized PnL % – Unrealised P/L in percentage.</summary>
        [JsonPropertyName("80")] public string? UnrealizedPnlPercent { get; set; }

        /// <summary>
        /// Change – Difference between last price and previous day’s close.
        /// </summary>
        [JsonPropertyName("82")] public string? Change { get; set; }

        /// <summary>
        /// Change % – Difference between last price and previous day’s close in percent.
        /// </summary>
        [JsonPropertyName("83")] public string? ChangePercent { get; set; }

        /// <summary>Bid Price – Highest-priced bid on the contract.</summary>
        [JsonPropertyName("84")] public string? BidPrice { get; set; }

        /// <summary>
        /// Ask Size – Number of units offered at the ask (US stocks divided by 100).
        /// </summary>
        [JsonPropertyName("85")] public string? AskSize { get; set; }

        /// <summary>Ask Price – Lowest-priced offer on the contract.</summary>
        [JsonPropertyName("86")] public string? AskPrice { get; set; }

        /// <summary>
        /// Volume – Formatted with K (thousands) or M (millions). For high precision use 7762.
        /// </summary>
        [JsonPropertyName("87")] public string? Volume { get; set; }

        /// <summary>
        /// Bid Size – Number of units bid for at the bid price (US stocks divided by 100).
        /// </summary>
        [JsonPropertyName("88")] public string? BidSize { get; set; }

        /// <summary>Exchange.</summary>
        [JsonPropertyName("6004")] public string? Exchange { get; set; }

        /// <summary>Conid – IBKR contract identifier.</summary>
        [JsonPropertyName("6008")] public int? FieldConid { get; set; }

        /// <summary>SecType – Asset class of the instrument.</summary>
        [JsonPropertyName("6070")] public string? SecType { get; set; }

        /// <summary>Months.</summary>
        [JsonPropertyName("6072")] public string? Months { get; set; }

        /// <summary>Regular Expiry.</summary>
        [JsonPropertyName("6073")] public string? RegularExpiry { get; set; }

        /// <summary>Marker for market data delivery method (similar to request id).</summary>
        [JsonPropertyName("6119")] public string? DeliveryMarker { get; set; }

        /// <summary>
        /// Underlying Conid – Use /trsrv/secdef for more information.
        /// </summary>
        [JsonPropertyName("6457")] public int? UnderlyingConid { get; set; }

        /// <summary>Service Params.</summary>
        [JsonPropertyName("6508")] public string? ServiceParams { get; set; }

        /// <summary>
        /// Market Data Availability – three chars:
        /// First: R(RealTime), D(Delayed), Z(Frozen), Y(Frozen Delayed), N(Not Subscribed)
        /// Second: P(Snapshot), p(Consolidated)
        /// Third: B(Book).
        /// </summary>
        [JsonPropertyName("6509")] public string? MarketDataAvailability { get; set; }

        /// <summary>Company name.</summary>
        [JsonPropertyName("7051")] public string? CompanyName { get; set; }

        /// <summary>
        /// Ask Exch – SMART contributors (A=AMEX, C=CBOE, I=ISE, X=PHLX, N=PSE, B=BOX, Q=NASDAQOM, Z=BATS, W=CBOE2, T=NASDAQBX, M=MIAX, H=GEMINI, E=EDGX, J=MERCURY).
        /// </summary>
        [JsonPropertyName("7057")] public string? AskExch { get; set; }

        /// <summary>Last Exch – same coding as Ask Exch.</summary>
        [JsonPropertyName("7058")] public string? LastExch { get; set; }

        /// <summary>Last Size – Units traded at the last price.</summary>
        [JsonPropertyName("7059")] public string? LastSize { get; set; }

        /// <summary>Bid Exch – same coding as Ask Exch.</summary>
        [JsonPropertyName("7068")] public string? BidExch { get; set; }

        /// <summary>Implied Vol./Hist. Vol % – IV/HV ratio in percent.</summary>
        [JsonPropertyName("7084")] public string? IvOverHvPercent { get; set; }

        /// <summary>Put/Call Interest – Put OI / Call OI for the trading day.</summary>
        [JsonPropertyName("7085")] public string? PutCallInterest { get; set; }

        /// <summary>Put/Call Volume – Put volume / Call volume for the trading day.</summary>
        [JsonPropertyName("7086")] public string? PutCallVolume { get; set; }

        /// <summary>Hist. Vol. % – 30-day real-time historical volatility.</summary>
        [JsonPropertyName("7087")] public string? HistoricalVolPercent { get; set; }

        /// <summary>Hist. Vol. Close % – HV based on previous close.</summary>
        [JsonPropertyName("7088")] public string? HistoricalVolClosePercent { get; set; }

        /// <summary>Opt. Volume – Option volume.</summary>
        [JsonPropertyName("7089")] public string? OptionVolume { get; set; }

        /// <summary>Conid + Exchange.</summary>
        [JsonPropertyName("7094")] public string? ConidAndExchange { get; set; }

        /// <summary>canBeTraded – 1 if the contract is tradeable, else 0.</summary>
        [JsonPropertyName("7184")] public string? CanBeTraded { get; set; }

        /// <summary>Contract Description.</summary>
        [JsonPropertyName("7219")] public string? ContractDescription1 { get; set; }

        /// <summary>Contract Description.</summary>
        [JsonPropertyName("7220")] public string? ContractDescription2 { get; set; }

        /// <summary>Listing Exchange.</summary>
        [JsonPropertyName("7221")] public string? ListingExchange { get; set; }

        /// <summary>Industry.</summary>
        [JsonPropertyName("7280")] public string? Industry { get; set; }

        /// <summary>Category (within industry).</summary>
        [JsonPropertyName("7281")] public string? Category { get; set; }

        /// <summary>Average Volume – 90-day average daily trading volume.</summary>
        [JsonPropertyName("7282")] public string? AverageVolume { get; set; }

        /// <summary>
        /// Option Implied Vol. % – Underlying’s forward 30-day IV based on two expiries.
        /// </summary>
        [JsonPropertyName("7283")] public string? OptionImpliedVolPercent { get; set; }

        /// <summary>Historical volatility (30d) – deprecated, use 7087.</summary>
        [JsonPropertyName("7284")] public string? HistoricVol30D { get; set; }

        /// <summary>Put/Call Ratio.</summary>
        [JsonPropertyName("7285")] public string? PutCallRatio { get; set; }

        /// <summary>Dividend Amount – Next dividend amount.</summary>
        [JsonPropertyName("7286")] public string? DividendAmount { get; set; }

        /// <summary>
        /// Dividend Yield % – Next 12 months’ expected dividends per share / current price.
        /// For derivatives, through expiry date.
        /// </summary>
        [JsonPropertyName("7287")] public string? DividendYieldPercent { get; set; }

        /// <summary>Ex-date of the dividend.</summary>
        [JsonPropertyName("7288")] public string? DividendExDate { get; set; }

        /// <summary>Market Cap.</summary>
        [JsonPropertyName("7289")] public string? MarketCap { get; set; }

        /// <summary>P/E.</summary>
        [JsonPropertyName("7290")] public string? Pe { get; set; }

        /// <summary>EPS.</summary>
        [JsonPropertyName("7291")] public string? Eps { get; set; }

        /// <summary>
        /// Cost Basis – Position size × average price × multiplier.
        /// </summary>
        [JsonPropertyName("7292")] public string? CostBasis { get; set; }

        /// <summary>52 Week High.</summary>
        [JsonPropertyName("7293")] public string? High52Week { get; set; }

        /// <summary>52 Week Low.</summary>
        [JsonPropertyName("7294")] public string? Low52Week { get; set; }

        /// <summary>Open – Today’s opening price.</summary>
        [JsonPropertyName("7295")] public string? Open { get; set; }

        /// <summary>Close – Today’s closing price.</summary>
        [JsonPropertyName("7296")] public string? Close { get; set; }

        /// <summary>Delta – dOption/dUnderlying.</summary>
        [JsonPropertyName("7308")] public string? Delta { get; set; }

        /// <summary>Gamma – dDelta/dUnderlying.</summary>
        [JsonPropertyName("7309")] public string? Gamma { get; set; }

        /// <summary>Theta – Time decay of the option.</summary>
        [JsonPropertyName("7310")] public string? Theta { get; set; }

        /// <summary>Vega – dOption/dVolatility (per 1%).</summary>
        [JsonPropertyName("7311")] public string? Vega { get; set; }

        /// <summary>
        /// Opt. Volume Change % – Today’s option volume vs average (percent).
        /// </summary>
        [JsonPropertyName("7607")] public string? OptionVolumeChangePercent { get; set; }

        /// <summary>
        /// Implied Vol. % – IV for a specific option strike (use 7283 for underlying).
        /// </summary>
        [JsonPropertyName("7633")] public string? ImpliedVolPercent { get; set; }

        /// <summary>
        /// Mark – Ask if ask &lt; last, Bid if bid &gt; last, otherwise last.
        /// </summary>
        [JsonPropertyName("7635")] public string? Mark { get; set; }

        /// <summary>Shortable Shares – Shares available to short.</summary>
        [JsonPropertyName("7636")] public string? ShortableShares { get; set; }

        /// <summary>Fee Rate – Interest rate on borrowed shares.</summary>
        [JsonPropertyName("7637")] public string? FeeRate { get; set; }

        /// <summary>Option Open Interest.</summary>
        [JsonPropertyName("7638")] public string? OptionOpenInterest { get; set; }

        /// <summary>
        /// % of Mark Value – Contract’s mark value as % of account’s total mark value.
        /// </summary>
        [JsonPropertyName("7639")] public string? PercentOfMarkValue { get; set; }

        /// <summary>Shortable – Ease of shorting the security.</summary>
        [JsonPropertyName("7644")] public string? Shortable { get; set; }

        /// <summary>Morningstar Rating (requires subscription).</summary>
        [JsonPropertyName("7655")] public string? MorningstarRating { get; set; }

        /// <summary>Dividends – Next 12 months’ expected dividends per share.</summary>
        [JsonPropertyName("7671")] public string? Dividends { get; set; }

        /// <summary>Dividends TTM – Last 12 months’ dividends per share.</summary>
        [JsonPropertyName("7672")] public string? DividendsTtm { get; set; }

        /// <summary>EMA(200) – Exponential moving average (N=200).</summary>
        [JsonPropertyName("7674")] public string? Ema200 { get; set; }

        /// <summary>EMA(100) – Exponential moving average (N=100).</summary>
        [JsonPropertyName("7675")] public string? Ema100 { get; set; }

        /// <summary>EMA(50) – Exponential moving average (N=50).</summary>
        [JsonPropertyName("7676")] public string? Ema50 { get; set; }

        /// <summary>EMA(20) – Exponential moving average (N=20).</summary>
        [JsonPropertyName("7677")] public string? Ema20 { get; set; }

        /// <summary>Price/EMA(200) – Ratio?1 in percent.</summary>
        [JsonPropertyName("7678")] public string? PriceOverEma200 { get; set; }

        /// <summary>Price/EMA(100) – Ratio?1 in percent.</summary>
        [JsonPropertyName("7679")] public string? PriceOverEma100 { get; set; }

        /// <summary>Price/EMA(50) – Ratio?1 in percent.</summary>
        [JsonPropertyName("7680")] public string? PriceOverEma50 { get; set; }

        /// <summary>Price/EMA(20) – Ratio?1 in percent.</summary>
        [JsonPropertyName("7681")] public string? PriceOverEma20 { get; set; }

        /// <summary>Change Since Open – Last minus open.</summary>
        [JsonPropertyName("7682")] public string? ChangeSinceOpen { get; set; }

        /// <summary>Upcoming Event (requires subscription).</summary>
        [JsonPropertyName("7683")] public string? UpcomingEvent { get; set; }

        /// <summary>Upcoming Event Date (requires subscription).</summary>
        [JsonPropertyName("7684")] public string? UpcomingEventDate { get; set; }

        /// <summary>Upcoming Analyst Meeting (requires subscription).</summary>
        [JsonPropertyName("7685")] public string? UpcomingAnalystMeeting { get; set; }

        /// <summary>Upcoming Earnings (requires subscription).</summary>
        [JsonPropertyName("7686")] public string? UpcomingEarnings { get; set; }

        /// <summary>Upcoming Misc Event (requires subscription).</summary>
        [JsonPropertyName("7687")] public string? UpcomingMiscEvent { get; set; }

        /// <summary>Recent Analyst Meeting (requires subscription).</summary>
        [JsonPropertyName("7688")] public string? RecentAnalystMeeting { get; set; }

        /// <summary>Recent Earnings (requires subscription).</summary>
        [JsonPropertyName("7689")] public string? RecentEarnings { get; set; }

        /// <summary>Recent Misc Event (requires subscription).</summary>
        [JsonPropertyName("7690")] public string? RecentMiscEvent { get; set; }

        /// <summary>Probability of Max Return – Customer-implied.</summary>
        [JsonPropertyName("7694")] public string? ProbabilityOfMaxReturn1 { get; set; }

        /// <summary>Break Even points.</summary>
        [JsonPropertyName("7695")] public string? BreakEven { get; set; }

        /// <summary>
        /// SPX Delta – Beta-weighted Delta (Delta × dollar-adjusted beta; beta adjusted by close ratio).
        /// </summary>
        [JsonPropertyName("7696")] public string? SpxDelta { get; set; }

        /// <summary>Futures Open Interest – Total open contracts.</summary>
        [JsonPropertyName("7697")] public string? FuturesOpenInterest { get; set; }

        /// <summary>Last Yield – Bond yield at last price (to worst).</summary>
        [JsonPropertyName("7698")] public string? LastYield { get; set; }

        /// <summary>Bid Yield – Bond yield at bid (to worst).</summary>
        [JsonPropertyName("7699")] public string? BidYield { get; set; }

        /// <summary>Probability of Max Return – Customer-implied.</summary>
        [JsonPropertyName("7700")] public string? ProbabilityOfMaxReturn2 { get; set; }

        /// <summary>Probability of Max Loss – Customer-implied.</summary>
        [JsonPropertyName("7702")] public string? ProbabilityOfMaxLoss { get; set; }

        /// <summary>Profit Probability – Customer-implied probability of any gain.</summary>
        [JsonPropertyName("7703")] public string? ProfitProbability { get; set; }

        /// <summary>Organization Type.</summary>
        [JsonPropertyName("7704")] public string? OrganizationType { get; set; }

        /// <summary>Debt Class.</summary>
        [JsonPropertyName("7705")] public string? DebtClass { get; set; }

        /// <summary>Ratings – Ratings issued for bond contract.</summary>
        [JsonPropertyName("7706")] public string? Ratings { get; set; }

        /// <summary>Bond State Code.</summary>
        [JsonPropertyName("7707")] public string? BondStateCode { get; set; }

        /// <summary>Bond Type.</summary>
        [JsonPropertyName("7708")] public string? BondType { get; set; }

        /// <summary>Last Trading Date.</summary>
        [JsonPropertyName("7714")] public string? LastTradingDate { get; set; }

        /// <summary>Issue Date.</summary>
        [JsonPropertyName("7715")] public string? IssueDate { get; set; }

        /// <summary>Beta – Against standard index.</summary>
        [JsonPropertyName("7718")] public string? Beta { get; set; }

        /// <summary>Ask Yield – Bond yield at ask (to worst).</summary>
        [JsonPropertyName("7720")] public string? AskYield { get; set; }

        /// <summary>Prior Close – Yesterday’s closing price.</summary>
        [JsonPropertyName("7741")] public string? PriorClose { get; set; }

        /// <summary>
        /// Volume Long – High precision volume for the day (use instead of formatted 87).
        /// </summary>
        [JsonPropertyName("7762")] public string? VolumeLong { get; set; }

        /// <summary>
        /// hasTradingPermissions – 1 if user has trading permissions for this contract, else 0.
        /// </summary>
        [JsonPropertyName("7768")] public string? HasTradingPermissions { get; set; }

        /// <summary>
        /// Daily PnL Raw – Day’s P/L since prior close (real-time) as raw value.
        /// </summary>
        [JsonPropertyName("7920")] public string? DailyPnlRaw { get; set; }

        /// <summary>
        /// Cost Basis Raw – Position size × average price × multiplier (raw).
        /// </summary>
        [JsonPropertyName("7921")] public string? CostBasisRaw { get; set; }
    }

    /// <summary>
    /// Market data field IDs selectable in snapshot requests.
    /// Values match IBKR numeric field IDs.
    /// </summary>
    public enum MarketDataField
    {
        /// <summary>31 – Last Price (may be prefixed C/H).</summary>
        LastPrice = 31,

        /// <summary>55 – Symbol.</summary>
        Symbol = 55,

        /// <summary>58 – Text.</summary>
        Text = 58,

        /// <summary>70 – High (day).</summary>
        High = 70,

        /// <summary>71 – Low (day).</summary>
        Low = 71,

        /// <summary>73 – Market Value (uses real-time data).</summary>
        MarketValue = 73,

        /// <summary>74 – Avg Price (position).</summary>
        AvgPrice = 74,

        /// <summary>75 – Unrealized PnL (real-time).</summary>
        UnrealizedPnl = 75,

        /// <summary>76 – Formatted position.</summary>
        FormattedPosition = 76,

        /// <summary>77 – Formatted Unrealized PnL.</summary>
        FormattedUnrealizedPnl = 77,

        /// <summary>78 – Daily PnL (real-time).</summary>
        DailyPnl = 78,

        /// <summary>79 – Realized PnL (real-time).</summary>
        RealizedPnl = 79,

        /// <summary>80 – Unrealized PnL %.</summary>
        UnrealizedPnlPercent = 80,

        /// <summary>82 – Change vs prior close.</summary>
        Change = 82,

        /// <summary>83 – Change % vs prior close.</summary>
        ChangePercent = 83,

        /// <summary>84 – Bid Price.</summary>
        BidPrice = 84,

        /// <summary>85 – Ask Size (US stocks ÷ 100).</summary>
        AskSize = 85,

        /// <summary>86 – Ask Price.</summary>
        AskPrice = 86,

        /// <summary>87 – Volume (formatted K/M).</summary>
        Volume = 87,

        /// <summary>88 – Bid Size (US stocks ÷ 100).</summary>
        BidSize = 88,

        /// <summary>6004 – Exchange.</summary>
        Exchange = 6004,

        /// <summary>6008 – Conid (field form).</summary>
        Conid = 6008,

        /// <summary>6070 – SecType (asset class).</summary>
        SecType = 6070,

        /// <summary>6072 – Months.</summary>
        Months = 6072,

        /// <summary>6073 – Regular Expiry.</summary>
        RegularExpiry = 6073,

        /// <summary>6119 – Delivery method marker.</summary>
        DeliveryMarker = 6119,

        /// <summary>6457 – Underlying Conid.</summary>
        UnderlyingConid = 6457,

        /// <summary>6508 – Service Params.</summary>
        ServiceParams = 6508,

        /// <summary>
        /// 6509 – Market Data Availability (R/D/Z/Y/N, P/p, B).
        /// </summary>
        MarketDataAvailability = 6509,

        /// <summary>7051 – Company name.</summary>
        CompanyName = 7051,

        /// <summary>7057 – Ask Exch (SMART contributors).</summary>
        AskExch = 7057,

        /// <summary>7058 – Last Exch (SMART contributors).</summary>
        LastExch = 7058,

        /// <summary>7059 – Last Size.</summary>
        LastSize = 7059,

        /// <summary>7068 – Bid Exch (SMART contributors).</summary>
        BidExch = 7068,

        /// <summary>7084 – Implied Vol./Hist. Vol %.</summary>
        IvOverHvPercent = 7084,

        /// <summary>7085 – Put/Call Interest.</summary>
        PutCallInterest = 7085,

        /// <summary>7086 – Put/Call Volume.</summary>
        PutCallVolume = 7086,

        /// <summary>7087 – Hist. Vol. % (30-day real-time).</summary>
        HistoricalVolPercent = 7087,

        /// <summary>7088 – Hist. Vol. Close %.</summary>
        HistoricalVolClosePercent = 7088,

        /// <summary>7089 – Option Volume.</summary>
        OptionVolume = 7089,

        /// <summary>7094 – Conid + Exchange.</summary>
        ConidAndExchange = 7094,

        /// <summary>7184 – canBeTraded (1/0).</summary>
        CanBeTraded = 7184,

        /// <summary>7219 – Contract Description.</summary>
        ContractDescription1 = 7219,

        /// <summary>7220 – Contract Description.</summary>
        ContractDescription2 = 7220,

        /// <summary>7221 – Listing Exchange.</summary>
        ListingExchange = 7221,

        /// <summary>7280 – Industry.</summary>
        Industry = 7280,

        /// <summary>7281 – Category.</summary>
        Category = 7281,

        /// <summary>7282 – Average Volume (90-day).</summary>
        AverageVolume = 7282,

        /// <summary>7283 – Option Implied Vol. % (underlying).</summary>
        OptionImpliedVolPercent = 7283,

        /// <summary>7284 – Historical volatility % (deprecated; use 7087).</summary>
        HistoricVol30D = 7284,

        /// <summary>7285 – Put/Call Ratio.</summary>
        PutCallRatio = 7285,

        /// <summary>7286 – Dividend Amount.</summary>
        DividendAmount = 7286,

        /// <summary>7287 – Dividend Yield %.</summary>
        DividendYieldPercent = 7287,

        /// <summary>7288 – Ex-date of the dividend.</summary>
        DividendExDate = 7288,

        /// <summary>7289 – Market Cap.</summary>
        MarketCap = 7289,

        /// <summary>7290 – P/E.</summary>
        Pe = 7290,

        /// <summary>7291 – EPS.</summary>
        Eps = 7291,

        /// <summary>7292 – Cost Basis.</summary>
        CostBasis = 7292,

        /// <summary>7293 – 52 Week High.</summary>
        High52Week = 7293,

        /// <summary>7294 – 52 Week Low.</summary>
        Low52Week = 7294,

        /// <summary>7295 – Open.</summary>
        Open = 7295,

        /// <summary>7296 – Close.</summary>
        Close = 7296,

        /// <summary>7308 – Delta.</summary>
        Delta = 7308,

        /// <summary>7309 – Gamma.</summary>
        Gamma = 7309,

        /// <summary>7310 – Theta.</summary>
        Theta = 7310,

        /// <summary>7311 – Vega.</summary>
        Vega = 7311,

        /// <summary>7607 – Option Volume Change %.</summary>
        OptionVolumeChangePercent = 7607,

        /// <summary>7633 – Implied Vol. % (specific strike).</summary>
        ImpliedVolPercent = 7633,

        /// <summary>7635 – Mark.</summary>
        Mark = 7635,

        /// <summary>7636 – Shortable Shares.</summary>
        ShortableShares = 7636,

        /// <summary>7637 – Fee Rate.</summary>
        FeeRate = 7637,

        /// <summary>7638 – Option Open Interest.</summary>
        OptionOpenInterest = 7638,

        /// <summary>7639 – % of Mark Value.</summary>
        PercentOfMarkValue = 7639,

        /// <summary>7644 – Shortable.</summary>
        Shortable = 7644,

        /// <summary>7655 – Morningstar Rating.</summary>
        MorningstarRating = 7655,

        /// <summary>7671 – Dividends (next 12 months).</summary>
        Dividends = 7671,

        /// <summary>7672 – Dividends TTM (last 12 months).</summary>
        DividendsTtm = 7672,

        /// <summary>7674 – EMA(200).</summary>
        Ema200 = 7674,

        /// <summary>7675 – EMA(100).</summary>
        Ema100 = 7675,

        /// <summary>7676 – EMA(50).</summary>
        Ema50 = 7676,

        /// <summary>7677 – EMA(20).</summary>
        Ema20 = 7677,

        /// <summary>7678 – Price/EMA(200) ? 1 (percent).</summary>
        PriceOverEma200 = 7678,

        /// <summary>7679 – Price/EMA(100) ? 1 (percent).</summary>
        PriceOverEma100 = 7679,

        /// <summary>7680 – Price/EMA(50) ? 1 (percent).</summary>
        PriceOverEma50 = 7680,

        /// <summary>7681 – Price/EMA(20) ? 1 (percent).</summary>
        PriceOverEma20 = 7681,

        /// <summary>7682 – Change Since Open.</summary>
        ChangeSinceOpen = 7682,

        /// <summary>7683 – Upcoming Event (subscription).</summary>
        UpcomingEvent = 7683,

        /// <summary>7684 – Upcoming Event Date (subscription).</summary>
        UpcomingEventDate = 7684,

        /// <summary>7685 – Upcoming Analyst Meeting (subscription).</summary>
        UpcomingAnalystMeeting = 7685,

        /// <summary>7686 – Upcoming Earnings (subscription).</summary>
        UpcomingEarnings = 7686,

        /// <summary>7687 – Upcoming Misc Event (subscription).</summary>
        UpcomingMiscEvent = 7687,

        /// <summary>7688 – Recent Analyst Meeting (subscription).</summary>
        RecentAnalystMeeting = 7688,

        /// <summary>7689 – Recent Earnings (subscription).</summary>
        RecentEarnings = 7689,

        /// <summary>7690 – Recent Misc Event (subscription).</summary>
        RecentMiscEvent = 7690,

        /// <summary>7694 – Probability of Max Return.</summary>
        ProbabilityOfMaxReturn1 = 7694,

        /// <summary>7695 – Break Even.</summary>
        BreakEven = 7695,

        /// <summary>7696 – SPX Delta (beta-weighted).</summary>
        SpxDelta = 7696,

        /// <summary>7697 – Futures Open Interest.</summary>
        FuturesOpenInterest = 7697,

        /// <summary>7698 – Last Yield (bond; to worst).</summary>
        LastYield = 7698,

        /// <summary>7699 – Bid Yield (bond; to worst).</summary>
        BidYield = 7699,

        /// <summary>7700 – Probability of Max Return.</summary>
        ProbabilityOfMaxReturn2 = 7700,

        /// <summary>7702 – Probability of Max Loss.</summary>
        ProbabilityOfMaxLoss = 7702,

        /// <summary>7703 – Profit Probability.</summary>
        ProfitProbability = 7703,

        /// <summary>7704 – Organization Type.</summary>
        OrganizationType = 7704,

        /// <summary>7705 – Debt Class.</summary>
        DebtClass = 7705,

        /// <summary>7706 – Ratings (bond).</summary>
        Ratings = 7706,

        /// <summary>7707 – Bond State Code.</summary>
        BondStateCode = 7707,

        /// <summary>7708 – Bond Type.</summary>
        BondType = 7708,

        /// <summary>7714 – Last Trading Date.</summary>
        LastTradingDate = 7714,

        /// <summary>7715 – Issue Date.</summary>
        IssueDate = 7715,

        /// <summary>7718 – Beta (vs index).</summary>
        Beta = 7718,

        /// <summary>7720 – Ask Yield (bond; to worst).</summary>
        AskYield = 7720,

        /// <summary>7741 – Prior Close.</summary>
        PriorClose = 7741,

        /// <summary>7762 – Volume Long (high precision).</summary>
        VolumeLong = 7762,

        /// <summary>7768 – hasTradingPermissions (1/0).</summary>
        HasTradingPermissions = 7768,

        /// <summary>7920 – Daily PnL Raw (real-time).</summary>
        DailyPnlRaw = 7920,

        /// <summary>7921 – Cost Basis Raw.</summary>
        CostBasisRaw = 7921,
    }

    // ---------------------- Converters ----------------------

    /// <summary>
    /// Serialises a list of MarketDataField values as an array of string IDs (e.g., ["31","84"]).
    /// Also supports deserialising from either strings or numbers.
    /// </summary>
    public sealed class MarketDataFieldListAsStringsConverter : JsonConverter<List<MarketDataField>>
    {
        public override List<MarketDataField>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for fields.");

            var list = new List<MarketDataField>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v1))
                            list.Add((MarketDataField)v1);
                        else
                            throw new JsonException("Invalid field id.");
                        break;

                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out var v2))
                            list.Add((MarketDataField)v2);
                        else
                            throw new JsonException("Invalid numeric field id.");
                        break;

                    default:
                        throw new JsonException("Invalid token in fields array.");
                }
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<MarketDataField> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var f in value)
            {
                writer.WriteStringValue(((int)f).ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteEndArray();
        }
    }
}
