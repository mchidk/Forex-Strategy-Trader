//==============================================================
// Forex Strategy Builder
// Copyright � Miroslav Popov. All rights reserved.
//==============================================================
// THIS CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE.
//==============================================================

using System;
using System.Drawing;
using ForexStrategyBuilder.Infrastructure.Entities;
using ForexStrategyBuilder.Infrastructure.Enums;
using ForexStrategyBuilder.Infrastructure.Interfaces;

namespace ForexStrategyBuilder.Indicators.Store
{
    public class MomentumMT_Pro : Indicator
    {
        public MomentumMT_Pro()
        {
            IndicatorName = "Momentum MT";
            PossibleSlots = SlotTypes.OpenFilter | SlotTypes.CloseFilter;
            SeparatedChart = true;

            IndicatorAuthor = "Miroslav Popov";
            IndicatorVersion = "2.0";
            IndicatorDescription = "A custom indicator for FSB and FST.";
        }

        public override void Initialize(SlotTypes slotType)
        {
            SlotType = slotType;

            IndParam.IndicatorType = TypeOfIndicator.Indicator;
            IndParam.ExecutionTime = ExecutionTime.DuringTheBar;

            // The ComboBox parameters
            IndParam.ListParam[0].Caption = "Logic";
            IndParam.ListParam[0].ItemList = new string[]
                {
                    "Momentum rises",
                    "Momentum falls",
                    "Momentum is higher than the Level line",
                    "Momentum is lower than the Level line",
                    "Momentum crosses the Level line upward",
                    "Momentum crosses the Level line downward",
                    "Momentum changes its direction upward",
                    "Momentum changes its direction downward"
                };
            IndParam.ListParam[0].Index = 0;
            IndParam.ListParam[0].Text = IndParam.ListParam[0].ItemList[IndParam.ListParam[0].Index];
            IndParam.ListParam[0].Enabled = true;
            IndParam.ListParam[0].ToolTip = "Logic of application of the indicator.";

            IndParam.ListParam[1].Caption = "Smoothing method";
            IndParam.ListParam[1].ItemList = Enum.GetNames(typeof (MAMethod));
            IndParam.ListParam[1].Index = (int) MAMethod.Simple;
            IndParam.ListParam[1].Text = IndParam.ListParam[1].ItemList[IndParam.ListParam[1].Index];
            IndParam.ListParam[1].Enabled = true;
            IndParam.ListParam[1].ToolTip = "The Moving Average method used for smoothing Momentum value.";

            IndParam.ListParam[2].Caption = "Base price";
            IndParam.ListParam[2].ItemList = Enum.GetNames(typeof (BasePrice));
            IndParam.ListParam[2].Index = (int) BasePrice.Close;
            IndParam.ListParam[2].Text = IndParam.ListParam[2].ItemList[IndParam.ListParam[2].Index];
            IndParam.ListParam[2].Enabled = true;
            IndParam.ListParam[2].ToolTip = "The price Momentum is based on.";

            // The NumericUpDown parameters
            IndParam.NumParam[0].Caption = "Period";
            IndParam.NumParam[0].Value = 14;
            IndParam.NumParam[0].Min = 1;
            IndParam.NumParam[0].Max = 200;
            IndParam.NumParam[0].Enabled = true;
            IndParam.NumParam[0].ToolTip = "The period of Momentum.";

            IndParam.NumParam[1].Caption = "Additional smoothing";
            IndParam.NumParam[1].Value = 0;
            IndParam.NumParam[1].Min = 0;
            IndParam.NumParam[1].Max = 200;
            IndParam.NumParam[1].Enabled = true;
            IndParam.NumParam[1].ToolTip = "The period of additional smoothing.";

            IndParam.NumParam[2].Caption = "Level";
            IndParam.NumParam[2].Value = 100;
            IndParam.NumParam[2].Min = 50;
            IndParam.NumParam[2].Max = 150;
            IndParam.NumParam[2].Point = 2;
            IndParam.NumParam[2].Enabled = true;
            IndParam.NumParam[2].ToolTip = "A critical level (for the appropriate logic).";

            // The CheckBox parameters
            IndParam.CheckParam[0].Caption = "Use previous bar value";
            IndParam.CheckParam[0].Enabled = true;
            IndParam.CheckParam[0].ToolTip = "Use the indicator value from the previous bar.";
        }

        public override void Calculate(IDataSet dataSet)
        {
            DataSet = dataSet;

            // Reading the parameters
            MAMethod maMethod = (MAMethod) IndParam.ListParam[1].Index;
            BasePrice basePrice = (BasePrice) IndParam.ListParam[2].Index;
            int period = (int) IndParam.NumParam[0].Value;
            int smooth = (int) IndParam.NumParam[1].Value;
            double level = IndParam.NumParam[2].Value;
            int previous = IndParam.CheckParam[0].Checked ? 1 : 0;

            int firstBar = previous + period + smooth + 2;
            double[] adMomentum = new double[Bars];
            double[] adBasePrice = Price(basePrice);

            for (int bar = period; bar < Bars; bar++)
                adMomentum[bar] = 100*adBasePrice[bar]/adBasePrice[bar - period];

            if (smooth > 0)
                adMomentum = MovingAverage(smooth, 0, maMethod, adMomentum);

            // Saving the components
            Component = new IndicatorComp[3];

            Component[0] = new IndicatorComp();
            Component[0].CompName = "Momentum";
            Component[0].DataType = IndComponentType.IndicatorValue;
            Component[0].ChartType = IndChartType.Line;
            Component[0].ChartColor = Color.Blue;
            Component[0].FirstBar = firstBar;
            Component[0].Value = adMomentum;

            Component[1] = new IndicatorComp();
            Component[1].ChartType = IndChartType.NoChart;
            Component[1].FirstBar = firstBar;
            Component[1].Value = new double[Bars];

            Component[2] = new IndicatorComp();
            Component[2].ChartType = IndChartType.NoChart;
            Component[2].FirstBar = firstBar;
            Component[2].Value = new double[Bars];

            // Sets the Component's type
            switch (SlotType)
            {
                case SlotTypes.OpenFilter:
                    Component[1].DataType = IndComponentType.AllowOpenLong;
                    Component[1].CompName = "Is long entry allowed";
                    Component[2].DataType = IndComponentType.AllowOpenShort;
                    Component[2].CompName = "Is short entry allowed";
                    break;
                case SlotTypes.CloseFilter:
                    Component[1].DataType = IndComponentType.ForceCloseLong;
                    Component[1].CompName = "Close out long position";
                    Component[2].DataType = IndComponentType.ForceCloseShort;
                    Component[2].CompName = "Close out short position";
                    break;
                default:
                    break;
            }

            // Calculation of the logic
            IndicatorLogic indLogic = IndicatorLogic.It_does_not_act_as_a_filter;

            switch (IndParam.ListParam[0].Text)
            {
                case "Momentum rises":
                    indLogic = IndicatorLogic.The_indicator_rises;
                    SpecialValues = new double[] {100};
                    break;

                case "Momentum falls":
                    indLogic = IndicatorLogic.The_indicator_falls;
                    SpecialValues = new double[] {100};
                    break;

                case "Momentum is higher than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_higher_than_the_level_line;
                    SpecialValues = new double[] {level, 2*100 - level};
                    break;

                case "Momentum is lower than the Level line":
                    indLogic = IndicatorLogic.The_indicator_is_lower_than_the_level_line;
                    SpecialValues = new double[] {level, 2*100 - level};
                    break;

                case "Momentum crosses the Level line upward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_upward;
                    SpecialValues = new double[] {level, 2*100 - level};
                    break;

                case "Momentum crosses the Level line downward":
                    indLogic = IndicatorLogic.The_indicator_crosses_the_level_line_downward;
                    SpecialValues = new double[] {level, 2*100 - level};
                    break;

                case "Momentum changes its direction upward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_upward;
                    SpecialValues = new double[] {100};
                    break;

                case "Momentum changes its direction downward":
                    indLogic = IndicatorLogic.The_indicator_changes_its_direction_downward;
                    SpecialValues = new double[] {100};
                    break;

                default:
                    break;
            }

            OscillatorLogic(firstBar, previous, adMomentum, level, 2*100 - level, ref Component[1], ref Component[2],
                            indLogic);
        }

        public override void SetDescription()
        {
            string sLevelLong = IndParam.NumParam[2].ValueToString;
            string sLevelShort = IndParam.NumParam[2].AnotherValueToString(2*100 - IndParam.NumParam[2].Value);

            EntryFilterLongDescription = "the " + ToString() + " ";
            EntryFilterShortDescription = "the " + ToString() + " ";
            ExitFilterLongDescription = "the " + ToString() + " ";
            ExitFilterShortDescription = "the " + ToString() + " ";

            switch (IndParam.ListParam[0].Text)
            {
                case "Momentum rises":
                    EntryFilterLongDescription += "rises";
                    EntryFilterShortDescription += "falls";
                    ExitFilterLongDescription += "rises";
                    ExitFilterShortDescription += "falls";
                    break;

                case "Momentum falls":
                    EntryFilterLongDescription += "falls";
                    EntryFilterShortDescription += "rises";
                    ExitFilterLongDescription += "falls";
                    ExitFilterShortDescription += "rises";
                    break;

                case "Momentum is higher than the Level line":
                    EntryFilterLongDescription += "is higher than the Level " + sLevelLong;
                    EntryFilterShortDescription += "is lower than the Level " + sLevelShort;
                    ExitFilterLongDescription += "is higher than the Level " + sLevelLong;
                    ExitFilterShortDescription += "is lower than the Level " + sLevelShort;
                    break;

                case "Momentum is lower than the Level line":
                    EntryFilterLongDescription += "is lower than the Level " + sLevelLong;
                    EntryFilterShortDescription += "is higher than the Level " + sLevelShort;
                    ExitFilterLongDescription += "is lower than the Level " + sLevelLong;
                    ExitFilterShortDescription += "is higher than the Level " + sLevelShort;
                    break;

                case "Momentum crosses the Level line upward":
                    EntryFilterLongDescription += "crosses the Level " + sLevelLong + " upward";
                    EntryFilterShortDescription += "crosses the Level " + sLevelShort + " downward";
                    ExitFilterLongDescription += "crosses the Level " + sLevelLong + " upward";
                    ExitFilterShortDescription += "crosses the Level " + sLevelShort + " downward";
                    break;

                case "Momentum crosses the Level line downward":
                    EntryFilterLongDescription += "crosses the Level " + sLevelLong + " downward";
                    EntryFilterShortDescription += "crosses the Level " + sLevelShort + " upward";
                    ExitFilterLongDescription += "crosses the Level " + sLevelLong + " downward";
                    ExitFilterShortDescription += "crosses the Level " + sLevelShort + " upward";
                    break;

                case "Momentum changes its direction upward":
                    EntryFilterLongDescription += "changes its direction upward";
                    EntryFilterShortDescription += "changes its direction downward";
                    ExitFilterLongDescription += "changes its direction upward";
                    ExitFilterShortDescription += "changes its direction downward";
                    break;

                case "Momentum changes its direction downward":
                    EntryFilterLongDescription += "changes its direction downward";
                    EntryFilterShortDescription += "changes its direction upward";
                    ExitFilterLongDescription += "changes its direction downward";
                    ExitFilterShortDescription += "changes its direction upward";
                    break;

                default:
                    break;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}{1} ({2}, {3}, {4}, {5})",
                                 IndicatorName,
                                 (IndParam.CheckParam[0].Checked ? "*" : ""),
                                 IndParam.ListParam[1].Text,
                                 IndParam.ListParam[2].Text,
                                 IndParam.NumParam[0].ValueToString,
                                 IndParam.NumParam[1].ValueToString);
        }
    }
}