﻿using System.Collections.Generic;
// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel;
using Rock.Model;
using Rock.Reporting.Dashboard;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting.Dashboard
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Bar Chart" )]
    [Category( "Dashboard" )]
    [Description( "DashboardWidget using flotcharts" )]
    public partial class BarChartDashboardWidget : ChartDashboardWidget
    {
        /// <summary>
        /// Loads the chart.
        /// </summary>
        public override void LoadChart()
        {
            bcExample.StartDate = this.DateRange.Start;
            bcExample.EndDate = this.DateRange.End;
            bcExample.MetricValueType = this.MetricValueType;
            bcExample.MetricId = this.MetricId;
            bcExample.EntityId = this.EntityId;
            bcExample.Title = this.Title;
            bcExample.Subtitle = this.Subtitle;
            bcExample.CombineValues = this.CombineValues;
            bcExample.ShowTooltip = false;
            bcExample.ChartClick += bcExample_ChartClick;

            bcExample.Options.SetChartStyle( this.ChartStyle );

            nbMetricWarning.Visible = !this.MetricId.HasValue;
        }

        /// <summary>
        /// Bcs the example_ chart click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        public void bcExample_ChartClick( object sender, FlotChart.ChartClickArgs e )
        {
            if ( this.DetailPageGuid.HasValue )
            {
                Dictionary<string, string> qryString = new Dictionary<string, string>();
                qryString.Add( "MetricId", this.MetricId.ToString() );
                qryString.Add( "SeriesId", e.SeriesId );
                qryString.Add( "YValue", e.YValue.ToString() );
                qryString.Add( "DateTimeValue", e.DateTimeValue.ToString( "o" ) );
                NavigateToPage( this.DetailPageGuid.Value, qryString );
            }
        }
    }
}