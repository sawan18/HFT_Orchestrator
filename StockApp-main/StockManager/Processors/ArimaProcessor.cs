using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extreme.DataAnalysis;
using Extreme.Mathematics;
using Extreme.Statistics;
using Extreme.Statistics.TimeSeriesAnalysis;

namespace StockManager.Processors
{
    public class ArimaProcessor
    {
        public void FitModel()
        {
            // This QuickStart Sample fits an ARMA(2,1) model and
            // an ARIMA(0,1,1) model to sunspot data.

            // The time series data is stored in a numerical variable:
            var sunspots = Vector.Create(new double[] {
               6.338,
6.082,
5.9,
5.63,
5.576,
5.582,
5.844,
5.786,
6.016,
5.824,
5.932,
5.954,
5.878,
5.982,
6.368,
6.418,
6.756,
6.438,
6.758,
6.622,
6.322,
6.392,
6.282,
6.258,
6.08,
6.132,
6.246,
6.198,
6.298,
6.254,
6.302,
6.54,
6.85,
7.192,
6.67,
6.43,
6.454,
6.358,
6.132,
5.968,
5.79,
5.626,
5.902,
5.47,
5.484,
5.25,
5.22,
5.454,
5.654,
6.05,
5.818,
5.882,
5.988,
6.234,
5.884,
5.88,
6.06,
6.002,
5.902,
5.822,
5.99,
6.146,
5.9,
5.664,
5.738,
5.682,
5.682,
5.704,
5.628,
5.588,
5.71,
5.87,
5.474,
5.56,
5.656,
5.896,
6.078,
6.508,
6.268,
6.21,
6.18,
6.004,
6.132,
5.532,
5.508,
5.698,
5.856,
5.832,
5.96,
5.86,
5.88,
5.778,
5.85,
5.674,
5.68,
5.664,
5.528,
5.466,
5.612,
5.764,
5.608,
5.548,
5.57,
5.678,
5.484,
5.504,
5.476,
5.626,
5.85,
5.784,
6.3,
6.23,
6.308,
6.262,
6.064,
6.214,
6.322,
6.276,
6.164,
6.368,
6.584,
6.6,
6.494,
6.426,
6.454,
6.43,
6.646,
6.738,
6.764,
6.924,
6.78,
6.742,
6.78,
6.834,
6.914,
7.056,
7.052,
6.722,
6.762,
6.88,
6.918,
6.922,
6.886,
6.8,
6.856,
6.718,
6.738,
6.644,
6.774,
7.072            });

            // ARMA models (no differencing) are constructed from
            // the variable containing the time series data, and the
            // AR and MA orders. The following constructs an ARMA(2,1)
            // model:
            ArimaModel model = new ArimaModel(sunspots, 2, 1);

            // The Compute methods fits the model.
            model.Fit();

            // The model's Parameters collection contains the fitted values.
            // For an ARIMA(p,d,q) model, the first p parameters are the 
            // auto-regressive parameters. The last q parametere are the
            // moving average parameters.
            Console.WriteLine("Variable              Value    Std.Error  t-stat  p-Value");
            foreach (Parameter parameter in model.Parameters)
                // Parameter objects have the following properties:
                Console.WriteLine("{0,-20}{1,10:F5}{2,10:F5}{3,8:F2} {4,7:F4}",
                    // Name, usually the name of the variable:
                    parameter.Name,
                    // Estimated value of the parameter:
                    parameter.Value,
                    // Standard error:
                    parameter.StandardError,
                    // The value of the t statistic for the hypothesis that the parameter
                    // is zero.
                    parameter.Statistic,
                    // Probability corresponding to the t statistic.
                    parameter.PValue);


            // The log-likelihood of the computed solution is also available:
            Console.WriteLine("Log-likelihood: {0:F4}", model.LogLikelihood);
            // as is the Akaike Information Criterion (AIC):
            Console.WriteLine("AIC: {0:F4}", model.GetAkaikeInformationCriterion());
            // and the Baysian Information Criterion (BIC):
            Console.WriteLine("BIC: {0:F4}", model.GetBayesianInformationCriterion());

            // The Forecast method can be used to predict the next value in the series...
            double nextValue = model.Forecast();
            Console.WriteLine("One step ahead forecast: {0:F3}", nextValue);

            // or to predict a specified number of values:
            var nextValues = model.Forecast(5);
            Console.WriteLine("First five forecasts: {0:F3}", nextValues);


            // An integrated model (with differencing) is constructed
            // by supplying the degree of differencing. Note the order
            // of the orders is the traditional one for an ARIMA(p,d,q)
            // model (p, d, q).
            // The following constructs an ARIMA(0,1,1) model:
            ArimaModel model2 = new ArimaModel(sunspots, 0, 1, 1);

            // By default, the mean is assumed to be zero for an integrated model.
            // We can override this by setting the EstimateMean property to true:
            model2.EstimateMean = true;

            // The Compute methods fits the model.
            model2.Fit();

            // The mean shows up as one of the parameters.
            Console.WriteLine("Variable              Value    Std.Error  t-stat  p-Value");
            foreach (Parameter parameter in model2.Parameters)
                Console.WriteLine("{0,-20}{1,10:F5}{2,10:F5}{3,8:F2} {4,7:F4}",
                    parameter.Name,
                    parameter.Value,
                    parameter.StandardError,
                    parameter.Statistic,
                    parameter.PValue);

            // We can also get the error variance:
            Console.WriteLine("Error variance: {0:F4}", model2.ErrorVariance);

            Console.WriteLine("Log-likelihood: {0:F4}", model2.LogLikelihood);
            Console.WriteLine("AIC: {0:F4}", model2.GetAkaikeInformationCriterion());
            Console.WriteLine("BIC: {0:F4}", model2.GetBayesianInformationCriterion());

            // or to predict a specified number of values:
            nextValues = model2.Forecast(5);
            Console.WriteLine("First five forecasts: {0:F3}", nextValues);

            Console.Write("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
