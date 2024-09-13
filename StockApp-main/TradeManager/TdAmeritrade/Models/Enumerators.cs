using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{
    public enum PeriodTypes
    {
        day, // minute*
        month, //daily, weekly*
        year, //daily, weekly, monthly
        ytd //daily, weekly*
    }

    /// <summary>
    /// Valid frequency types by periodtype (defaults marked with an asterisk)
    /// </summary>
    public enum FrequencyType
    {
        minute, 
        daily, 
        weekly, 
        monthly 
    }

    public enum OrderInstruction
    {
        BUY,
        SELL,
        BUY_TO_COVER,
        SELL_SHORT,
        BUY_TO_OPEN,
        BUY_TO_CLOSE,
        SELL_TO_OPEN,
        SELL_TO_CLOSE,
        EXCHANGE
    }

    public enum OrderAssetType
    {
        EQUITY,
        OPTION,
        INDEX,
        MUTUAL_FUND,
        CASH_EQUIVALENT,
        FIXED_INCOME,
        CURRENCY

    }
    public enum OrderSession
    {
        NORMAL,
        AM,
        PM,
        SEAMLESS
    }

    public enum OrderDuration
    {
        DAY,
        GOOD_TILL_CANCEL,
        FILL_OR_KILL
    }

    public enum OrderType
    {
        MARKET,
        LIMIT,
        STOP,
        STOP_LIMIT,
        TRAILING_STOP,
        MARKET_ON_CLOSE,
        EXERCISE,
        TRAILING_STOP_LIMIT,
        NET_DEBIT,
        NET_CREDIT,
        NET_ZERO
    }

    public enum ComplexOrderStrategyType
    {
        NONE,
        COVERED,
        VERTICAL,
        BACK_RATIO,
        CALENDAR,
        DIAGONAL,
        STRADDLE,
        STRANGLE
    }

    public enum OrderStrategyType
    {
        SINGLE,
        OCO,
        TRIGGER
    }

    public enum OrderStatus
    {
        AWAITING_PARENT_ORDER,
        AWAITING_CONDITION,
        AWAITING_MANUAL_REVIEW,
        ACCEPTED,
        AWAITING_UR_OUT,
        PENDING_ACTIVATION,
        QUEUED,
        WORKING,
        REJECTED,
        PENDING_CANCEL,
        CANCELED,
        PENDING_REPLACE,
        REPLACED,
        FILLED,
        EXPIRED
    }

    public enum TradeStatus
    {
        Approved,
        Rejected,
        Cancel,
        Error
    }
}
