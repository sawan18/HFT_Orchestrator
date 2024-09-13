using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    public enum Decision
    {
        NoAction = 0,
        Buy = 1,
        Sell = 2
    }

    public enum MacdDecisionMode
    {
        CrossoversOnly,
        Greedy,
        PeakTrends,
    }

    public enum Algorithm
    {
        Macd,
        PriceTrend,
        BestTrend
    }

    public enum MacdStage
    {
        NotApplicable,
        PositiveHistogramTrendingUp,
        PositiveHistogramTrendingDown,
        NegativeHistogramTrendingDown,
        NegativeHistogramTrendingUp
    }
}
