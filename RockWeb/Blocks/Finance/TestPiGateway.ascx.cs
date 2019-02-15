// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Finance
{
    /// <summary>
    /// Temporary Block to Test the Pi Gateway.
    /// </summary>
    [DisplayName( "Test Pi Gateway" )]
    [Category( "Utility" )]
    [Description( "Temporary Block to Test the Pi Gateway." )]

    #region Block Attributes

    [FinancialGatewayField(
        "Financial Gateway",
        Key = AttributeKey.FinancialGateway,
        Description = "The payment gateway to use for Credit Card and ACH transactions.",
        Order = 0 )]

    [BooleanField(
        "Enable ACH",
        Key = AttributeKey.EnableACH,
        Order = 1 )]

    [AccountsField(
        "Accounts",
        Key = AttributeKey.AccountsToDisplay,
        Description = "The accounts to display. By default all active accounts with a Public Name will be displayed. If the account has a child account for the selected campus, the child account for that campus will be used.",
        Order = 2 )]
    #endregion Block Attributes

    public partial class TestPiGateway : RockBlock
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {
            public const string FinancialGateway = "FinancialGateway";
            public const string EnableACH = "EnableACH";
            public const string AccountsToDisplay = "AccountsToDisplay";
        }

        #endregion Attribute Keys

        Control _hostedPaymentInfoControl;

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            var gatewayComponent = GetFinancialGatewayComponent();
            var gateway = GetFinancialGateway();
            this.BlockUpdated += TestPiGateway_BlockUpdated;

            bool enableACH = this.GetAttributeValue( AttributeKey.EnableACH ).AsBoolean();
            if ( gatewayComponent != null )
            {
                _hostedPaymentInfoControl = gatewayComponent.GetHostedPaymentInfoControl( gateway, enableACH, "_hostedPaymentInfoControl" );
                phHostedPaymentControl.Controls.Add( _hostedPaymentInfoControl );
                var submitScript = gatewayComponent.GetHostPaymentInfoSubmitScript( gateway, _hostedPaymentInfoControl );

                btnSubmitPaymentInfo.Attributes["onclick"] = submitScript;
            }

            if ( _hostedPaymentInfoControl is IHostedGatewayPaymentControlTokenEvent )
            {
                ( _hostedPaymentInfoControl as IHostedGatewayPaymentControlTokenEvent ).TokenReceived += _hostedPaymentInfoControl_TokenReceived;
            }

            var rockContext = new RockContext();
            var selectableAccountIds = new FinancialAccountService( rockContext ).GetByGuids( this.GetAttributeValues( AttributeKey.AccountsToDisplay ).AsGuidList() ).Select( a => a.Id ).ToArray();

            caapAccountInfo.SelectableAccountIds = selectableAccountIds;
        }

        /// <summary>
        /// Handles the TokenReceived event of the _hostedPaymentInfoControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _hostedPaymentInfoControl_TokenReceived( object sender, EventArgs e )
        {
            var gatewayComponent = GetFinancialGatewayComponent();
            var gateway = GetFinancialGateway();
            lTokenOutput.Text = gatewayComponent.GetHostedPaymentInfoToken( gateway, _hostedPaymentInfoControl );
        }

        private void TestPiGateway_BlockUpdated( object sender, EventArgs e )
        {
            // reload whole page instead of doing a partial when block config changes
            NavigateToCurrentPageReference();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        private Rock.Model.FinancialGateway _financialGateway = null;

        /// <summary>
        /// Gets the financial gateway (model) that is configured for this block
        /// </summary>
        /// <returns></returns>
        private Rock.Model.FinancialGateway GetFinancialGateway()
        {
            if ( _financialGateway == null )
            {
                RockContext rockContext = new RockContext();
                var financialGatewayGuid = this.GetAttributeValue( AttributeKey.FinancialGateway ).AsGuid();
                _financialGateway = new FinancialGatewayService( rockContext ).GetNoTracking( financialGatewayGuid );
            }

            return _financialGateway;
        }


        private IHostedGatewayComponent _financialGatewayComponent = null;

        /// <summary>
        /// Gets the financial gateway component that is configured for this block
        /// </summary>
        /// <returns></returns>
        private IHostedGatewayComponent GetFinancialGatewayComponent()
        {
            if ( _financialGatewayComponent == null )
            {
                var financialGateway = GetFinancialGateway();
                if ( financialGateway != null )
                {
                    _financialGatewayComponent = financialGateway.GetGatewayComponent() as IHostedGatewayComponent;
                }
            }

            return _financialGatewayComponent;
        }

        /// <summary>
        /// Handles the Click event of the btnProcessSale control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnProcessSale_Click( object sender, EventArgs e )
        {
            var gatewayComponent = GetFinancialGatewayComponent();
            var gateway = GetFinancialGateway();
            if ( caapAccountInfo.AmountEntryMode == Rock.Web.UI.Controls.CampusAccountAmountPicker.AccountAmountEntryMode.SingleAccount )
            {
                if ( !caapAccountInfo.SelectedAmount.HasValue )
                {
                    nbAmountRequired.Visible = true;
                    return;
                }
            }
            else
            {
                if ( !caapAccountInfo.AccountAmounts.Any( a => a.Amount != 0 ) )
                {
                    nbAmountRequired.Visible = true;
                    return;
                }
            }

            nbAmountRequired.Visible = false;

            var amount = caapAccountInfo.SelectedAmount.Value;
            var referencePaymentInfo = new ReferencePaymentInfo
            {
                Amount = amount,
                ReferenceNumber = gatewayComponent.GetHostedPaymentInfoToken( gateway, _hostedPaymentInfoControl ),
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                Street1 = acAddress.Street1,
                Street2 = acAddress.Street2,
                City = acAddress.City,
                State = acAddress.State,
                PostalCode = acAddress.PostalCode,
                Country = acAddress.Country,
                Phone = pnbPhone.Number,
                Email = tbEmail.Text
                //GatewayPersonIdentifier = "TODO when a FinancialPersonSavedAccount is selected instead of entering a new payment"
            };

            string errorMessage;
            var financialTransaction = gatewayComponent.Charge( gateway, referencePaymentInfo, out errorMessage );
            if ( financialTransaction == null )
            {
                ceSaleResponse.Text = errorMessage;
            }
            else
            {
                ceSaleResponse.Text = financialTransaction.ToJson( Formatting.Indented );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCreateCustomer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCreateCustomer_Click( object sender, EventArgs e )
        {
            /*var gateway = new Rock.TransNational.Pi.PiGateway();
            PaymentInfo paymentInfo = new PaymentInfo
            {
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                Street1 = acAddress.Street1,
                Street2 = acAddress.Street2,
                City = acAddress.City,
                State = acAddress.State,
                PostalCode = acAddress.PostalCode,
                Country = acAddress.Country,
                Email = tbEmail.Text,
                Phone = pnbPhone.Text
            };

            Rock.TransNational.Pi.CreateCustomerResponse customerResponse = gateway.CreateCustomer( tbApiKey.Text, hfResponseToken.Value, paymentInfo );
            ceCreateCustomerResponse.Text = customerResponse.ToJson( Formatting.Indented );

            if ( customerResponse.Data == null )
            {
                tbCustomerId.Text = "";
            }
            else
            {
                tbCustomerId.Text = customerResponse.Data.Id;
            }*/

            // TODO;
        }

        /// <summary>
        /// Handles the Click event of the btnGetPlans control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnGetPlans_Click( object sender, EventArgs e )
        {
            //var gateway = new Rock.TransNational.Pi.PiGateway();
            //var getPlansResponse = gateway.GetPlans( tbApiKey.Text );
            //ceGetPlansResponse.Text = getPlansResponse.ToJson( Formatting.Indented );
        }

        /// <summary>
        /// Handles the Click event of the btnCreatePlan control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCreatePlan_Click( object sender, EventArgs e )
        {
            /*var financialGateway = GetFinancialGateway();
            var gateway = new Rock.TransNational.Pi.PiGateway();
            Rock.TransNational.Pi.CreatePlanParameters planParameters = new Rock.TransNational.Pi.CreatePlanParameters
            {
                Name = tbPlanName.Text,
                Description = tbPlanDescription.Text,
                Amount = tbPlanAmount.Text.AsDecimal(),
                BillingCycleInterval = tbPlanBillingCycleInterval.Text.AsInteger(),
                BillingFrequency = ddlPlanBillingFrequency.SelectedValueAsEnum<Rock.TransNational.Pi.BillingFrequency>(),
                BillingDays = tbPlanBillingDays.Text,
                Duration = 0
            };

            var test = planParameters.ToJson( Formatting.Indented );

            var createPlanResponse = gateway.CreatePlan( gateway.GetGatewayUrl( financialGateway ), "api_1D4yJCYJNXV3MqnOmAlEMHmYYeg", planParameters );
            if ( createPlanResponse.Data != null )
            {
                tbPlanId.Text = createPlanResponse.Data.Id;
            }
            else
            {
                tbPlanId.Text = null;
            }

            ceCreatePlanResponse.Text = createPlanResponse.ToJson( Formatting.Indented );
            */
        }

        /// <summary>
        /// Handles the Click event of the btnCreateSubscription control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCreateSubscription_Click( object sender, EventArgs e )
        {
            /*var gateway = new Rock.TransNational.Pi.PiGateway();
            Rock.TransNational.Pi.CreateSubscriptionParameters subscriptionParameters = new Rock.TransNational.Pi.CreateSubscriptionParameters
            {
                Customer = new Rock.TransNational.Pi.SubscriptionCustomer
                {
                    Id = tbCustomerId.Text
                },
                NextBillDate = tbSubscriptionNextBillDate.Text.AsDateTime().Value,
                PlanId = tbPlanId.Text,
                Description = tbSubscriptionDescription.Text,
                Amount = tbSubscriptionAmount.Text.AsDecimal(),
                BillingCycleInterval = tbSubscriptionBillingCycleInterval.Text.AsIntegerOrNull(),
                BillingFrequency = ddlSubscriptionBillingFrequency.SelectedValueAsEnumOrNull<Rock.TransNational.Pi.BillingFrequency>(),
                BillingDays = tbSubscriptionBillingDays.Text,
                Duration = 0
            };

            var test = subscriptionParameters.ToJson( Formatting.Indented );

            var createSubscriptionResponse = gateway.CreateSubscription( tbApiKey.Text, subscriptionParameters );
            if ( createSubscriptionResponse.Data != null )
            {
                tbCreateSubscriptionResponse_SubscriptionId.Text = createSubscriptionResponse.Data.Id;
            }
            else
            {
                tbCreateSubscriptionResponse_SubscriptionId.Text = null;
            }

            ceCreateSubscriptionResponse.Text = createSubscriptionResponse.ToJson( Formatting.Indented );
            */
        }

        /// <summary>
        /// Handles the Click event of the btnGetCustomerTransactionStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnGetCustomerTransactionStatus_Click( object sender, EventArgs e )
        {
            /*var gateway = new Rock.TransNational.Pi.PiGateway();
            var queryTransactionStatusRequest = new Rock.TransNational.Pi.QueryTransactionStatusRequest
            {
                CustomerIdSearch = new Rock.TransNational.Pi.QuerySearchString { ComparisonOperator = "=", SearchValue = tbCustomerId.Text }
            };


            var queryJson = queryTransactionStatusRequest.ToJson( Formatting.Indented );
            var response = gateway.QueryTransactionStatus( tbApiKey.Text, queryTransactionStatusRequest );

            ceQueryTransactionStatus.Text = response.ToJson( Formatting.Indented );
            */
        }
    }
}